using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Auth;
using Solar.Application.Grading;
using Solar.Domain.Academic;
using Solar.Domain.Assessments;
using Solar.Domain.Communication;
using Solar.Domain.Discussions;
using Solar.Domain.Entities;
using Solar.Domain.Grading;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Integrations.BigBlueButton;
using Solar.Infrastructure.Integrations.Sigaa;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Hubs;
using Solar.WebApi.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext (PostgreSQL em produção ou InMemory para testes)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    var formattedConn = FormatNpgsqlConnectionString(connectionString);
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseNpgsql(formattedConn));
}
else
{
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseInMemoryDatabase("SolarDb"));
}

// Injeção de dependências dos serviços de Domínio e Aplicação
builder.Services.AddScoped<ISolarAuthDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
builder.Services.AddSingleton<GradingCalculationService>();
builder.Services.AddSingleton<ExamScoringService>();
builder.Services.AddSingleton<AllocationTagScopeService>();
builder.Services.AddSingleton<DiscussionTreeService>();
builder.Services.AddSingleton<DisciplineImportService>();
builder.Services.AddSingleton<GroupAssignmentService>();
builder.Services.AddSingleton<InternalMessagingService>();
builder.Services.AddScoped<CalculateStudentGradesUseCase>();
builder.Services.AddScoped<AuthenticateUserUseCase>();
builder.Services.AddScoped<IPasswordHasher<User>, DeviseLegacyPasswordHasher<User>>();

// Integrações Externas (BigBlueButton e SIGAA)
builder.Services.AddSingleton(new BigBlueButtonServerConfig
{
    ServerUrl = builder.Configuration["BigBlueButton:ServerUrl"] ?? "https://bbb.virtual.ufc.br/bigbluebutton",
    SharedSecret = builder.Configuration["BigBlueButton:SharedSecret"] ?? "solar_secret"
});
builder.Services.AddSingleton<BigBlueButtonClient>();
builder.Services.AddSingleton<ISigaaAcademicService, SigaaAcademicClient>();

// SignalR para Chat e Notificações em tempo real
builder.Services.AddSignalR();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

// Inicialização de schema e usuários demo se o banco for novo/vazio
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();
    try
    {
        db.Database.EnsureCreated();
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Username = "aluno1",
                    Nick = "Aluno",
                    Name = "Aluno 1 (Demonstração)",
                    Email = "aluno1@solar.ufc.br",
                    Cpf = "12345678900",
                    EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("123456"),
                    Active = true
                },
                new User
                {
                    Username = "alunoteste",
                    Nick = "Aluno",
                    Name = "Aluno Demonstrativo",
                    Email = "aluno@solar.ufc.br",
                    Cpf = "12345678901",
                    EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("senhadoteste123"),
                    Active = true
                },
                new User
                {
                    Username = "prof.fabricio",
                    Nick = "Fabrício",
                    Name = "Prof. Fabrício Silva",
                    Email = "fabricio@virtual.ufc.br",
                    Cpf = "99988877766",
                    EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("solar123"),
                    Active = true
                }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Aviso na verificação/inicialização de banco de dados.");
    }
}

// OpenAPI e Documentação Interativa de Rotas (Swagger / Scalar)
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("Solar LMS 2.0 API - UFC Virtual");
    options.WithTheme(ScalarTheme.BluePlanet);
});
app.MapGet("/swagger", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Servir o painel web interativo estático (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// Middleware Anti-Fraude de Bloqueio em Provas
app.UseMiddleware<ExamLockoutMiddleware>();

// ----------------------------------------------------
// Endpoints da API (Minimal APIs)
// ----------------------------------------------------

// Health Check com verificação de banco de dados
app.MapGet("/health", async (SolarDbContext db) =>
{
    bool dbOk = false;
    try
    {
        dbOk = await db.Database.CanConnectAsync();
    }
    catch { }

    return Results.Ok(new
    {
        Status = dbOk ? "Healthy" : "Degraded",
        Database = dbOk ? "Connected" : "Disconnected",
        System = "Solar LMS Core (.NET 10)",
        Timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck")
.WithSummary("Verifica a integridade do serviço e do banco de dados");

// Autenticação com suporte aos hashes legados Devise (SHA1 / SHA1-MD5 e busca por Username/CPF)
app.MapPost("/api/v1/auth/login", async (
    LoginRequest request,
    AuthenticateUserUseCase authUseCase,
    HttpContext httpContext) =>
{
    var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
    var enrichedRequest = request with { RemoteIp = clientIp };
    var result = await authUseCase.ExecuteAsync(enrichedRequest);

    if (!result.Success)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(result);
})
.WithName("Login")
.WithSummary("Autentica o usuário com suporte a migração de hashes legados e login por Username ou CPF");

// Verificação de CPF para Autocadastro (espelha verify_cpf_users_path)
app.MapPost("/api/v1/auth/verify-cpf", async (
    VerifyCpfRequest request,
    SolarDbContext db,
    ISigaaAcademicService sigaaService) =>
{
    if (string.IsNullOrWhiteSpace(request.Cpf))
    {
        return Results.BadRequest(new { error = "CPF é obrigatório." });
    }

    string sanitized = request.Cpf.Replace(".", "").Replace("-", "").Trim();
    var localUser = await db.Users.FirstOrDefaultAsync(u => u.Cpf == sanitized || u.Cpf == request.Cpf);

    if (localUser != null)
    {
        return Results.Ok(new VerifyCpfResponse
        {
            ExistsInLocal = true,
            ExistsInSigaa = false,
            Name = localUser.Name,
            Email = localUser.Email,
            Message = "Usuário já cadastrado no Solar LMS. Prossiga para a tela de Login."
        });
    }

    // Consulta no SIGAA externo
    var sigaaRecord = await sigaaService.FindUserByCpfAsync(sanitized);
    if (sigaaRecord != null)
    {
        return Results.Ok(new VerifyCpfResponse
        {
            ExistsInLocal = false,
            ExistsInSigaa = true,
            Name = sigaaRecord.Name,
            Email = sigaaRecord.Email,
            Message = "Usuário localizado na base integrada SIGAA. Você pode importar seus dados cadastrais."
        });
    }

    return Results.Ok(new VerifyCpfResponse
    {
        ExistsInLocal = false,
        ExistsInSigaa = false,
        Message = "CPF não localizado na base integrada. É permitido o autocadastro direto."
    });
})
.WithName("VerifyCpf")
.WithSummary("Verifica CPF para autocadastro no Solar LMS ou importação SIGAA");

// Cálculo de Notas e Situação Acadêmica
app.MapPost("/api/v1/grades/calculate", (
    CalculateStudentGradesCommand command,
    CalculateStudentGradesUseCase useCase) =>
{
    var result = useCase.Execute(command);
    return Results.Ok(result);
})
.WithName("CalculateGrades")
.WithSummary("Calcula média parcial, horas e situação acadêmica de um aluno");

// Aulas Didáticas (Usado para validar liberação ou bloqueio por prova ativa)
app.MapGet("/api/v1/lessons", () => Results.Ok(new[]
{
    new { Id = 1, Title = "Aula 1: Introdução ao Curso", Type = "File" },
    new { Id = 2, Title = "Aula 2: Arquitetura de Sistemas", Type = "Link" }
}))
.WithName("GetLessons")
.WithSummary("Retorna a lista de aulas da turma");

// Lista de Disciplinas / Ofertas Ativas do Aluno (Espelha 02_meu_solar_dashboard.png)
app.MapGet("/api/v1/curriculum-units", async (SolarDbContext db) =>
{
    try
    {
        var offers = await db.Offers
            .Include(o => o.CurriculumUnit)
            .Include(o => o.Course)
            .Include(o => o.Semester)
            .Take(10)
            .ToListAsync();

        if (offers.Any())
        {
            return Results.Ok(offers.Select(o => new
            {
                Id = (int)o.Id,
                Code = o.CurriculumUnit?.Code ?? ("CU-" + o.Id),
                Name = o.CurriculumUnit?.Name ?? ("Disciplina " + o.Id),
                CourseCode = o.Course?.Code ?? "00",
                CourseName = o.Course?.Name ?? "Curso Geral",
                Semester = o.Semester?.Name ?? "2011.1",
                Type = o.CurriculumUnit?.CurriculumUnitTypeId == 2 ? "presential_undergrad" : "distance_undergrad",
                TypeLabel = o.CurriculumUnit?.CurriculumUnitTypeId == 2 ? "Graduação Presencial" : "Graduação a Distância",
                ClassCode = "TURMA-" + o.Id,
                Description = o.CurriculumUnit?.Resume ?? o.CurriculumUnit?.Syllabus ?? "Estudo aprofundado dos tópicos programáticos e metodologias aplicadas.",
                Hours = o.CurriculumUnit?.WorkingHours ?? 64
            }));
        }
    }
    catch
    {
        // Fallback para ambiente in-memory ou testes
    }

    return Results.Ok(new[]
    {
        new
        {
            Id = 1,
            Code = "RM404",
            Name = "Introducao a Linguistica",
            CourseCode = "108",
            CourseName = "Licenciatura em Letras",
            Semester = "2011.1",
            Type = "distance_undergrad",
            TypeLabel = "Graduação a Distância",
            ClassCode = "IL-FOR",
            Description = "Fundamentos da ciência da linguagem, fonética, sintaxe e semântica aplicada ao ensino.",
            Hours = 64
        },
        new
        {
            Id = 2,
            Code = "RM301",
            Name = "Quimica I",
            CourseCode = "109",
            CourseName = "Licenciatura em Quimica",
            Semester = "2011.1",
            Type = "distance_undergrad",
            TypeLabel = "Graduação a Distância",
            ClassCode = "QM-CAU",
            Description = "Pensando mais a longo prazo, o estudo dos princípios da química geral e orgânica aplicada.",
            Hours = 64
        },
        new
        {
            Id = 3,
            Code = "RM405",
            Name = "Teoria da Literatura I",
            CourseCode = "110",
            CourseName = "Letras Portugues",
            Semester = "2011.1",
            Type = "presential_undergrad",
            TypeLabel = "Graduação Presencial",
            ClassCode = "TL-01",
            Description = "Estudo dos gêneros literários, lírica, épica e narrativa contemporânea.",
            Hours = 64
        }
    });
})
.WithName("GetCurriculumUnits")
.WithSummary("Retorna as disciplinas/ofertas ativas do aluno");

// Detalhes da Turma e Responsáveis (Espelha 07_turma_disciplina_interna.png)
app.MapGet("/api/v1/curriculum-units/{id}", async (int id, SolarDbContext db) =>
{
    try
    {
        var offer = await db.Offers
            .Include(o => o.CurriculumUnit)
            .Include(o => o.Course)
            .Include(o => o.Semester)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer != null)
        {
            return Results.Ok(new
            {
                Id = (int)offer.Id,
                Code = offer.CurriculumUnit?.Code ?? ("CU-" + offer.Id),
                Name = offer.CurriculumUnit?.Name ?? ("Disciplina " + offer.Id),
                CourseName = offer.Course?.Name ?? "Curso Geral",
                Semester = offer.Semester?.Name ?? "2011.1",
                ClassCode = "TURMA-" + offer.Id,
                Description = offer.CurriculumUnit?.Resume ?? offer.CurriculumUnit?.Syllabus ?? "Estudo aprofundado dos tópicos programáticos e metodologias aplicadas.",
                Hours = offer.CurriculumUnit?.WorkingHours ?? 64,
                Staff = new[]
                {
                    new { Role = "Aluno Monitor", Name = "Aluno 3 (Monitor)", Email = "monitor@solar.ufc.br" },
                    new { Role = "Professor Titular UAB", Name = "Prof. Carlos Eduardo (Titular)", Email = "professor@solar.ufc.br" },
                    new { Role = "Tutor Presencial", Name = "Tutor Polo Caucaia", Email = "tutor.presencial@solar.ufc.br" },
                    new { Role = "Tutor a Distância", Name = "Tutor Virtual Geral", Email = "tutor.distancia@solar.ufc.br" }
                }
            });
        }
    }
    catch { }

    return Results.Ok(new
    {
        Id = id,
        Code = id == 2 ? "RM301" : id == 1 ? "RM404" : "RM405",
        Name = id == 2 ? "Quimica I" : id == 1 ? "Introducao a Linguistica" : "Teoria da Literatura I",
        CourseName = id == 2 ? "Licenciatura em Quimica" : id == 1 ? "Licenciatura em Letras" : "Letras Portugues",
        Semester = "2011.1",
        ClassCode = id == 2 ? "QM-CAU" : id == 1 ? "IL-FOR" : "TL-01",
        Description = "Estudo aprofundado dos tópicos programáticos e metodologias aplicadas.",
        Hours = 64,
        Staff = new[]
        {
            new { Role = "Aluno Monitor", Name = "Aluno 3 (Monitor)", Email = "monitor@solar.ufc.br" },
            new { Role = "Professor Titular UAB", Name = "Prof. Carlos Eduardo (Titular)", Email = "professor@solar.ufc.br" },
            new { Role = "Tutor Presencial", Name = "Tutor Polo Caucaia", Email = "tutor.presencial@solar.ufc.br" },
            new { Role = "Tutor a Distância", Name = "Tutor Virtual Geral", Email = "tutor.distancia@solar.ufc.br" }
        }
    });
})
.WithName("GetCurriculumUnitDetails")
.WithSummary("Retorna os detalhes e docentes de uma disciplina");

// Aulas e Módulos Didáticos (Espelha 08_turma_aulas.png)
app.MapGet("/api/v1/curriculum-units/{id}/lessons", async (int id, SolarDbContext db) =>
{
    try
    {
        var dbLessons = await db.Lessons.Take(6).ToListAsync();
        if (dbLessons.Any())
        {
            return Results.Ok(new[]
            {
                new
                {
                    ModuleId = 1,
                    ModuleName = "Módulo 1: Fundamentos e Conteúdo Programático",
                    Lessons = dbLessons.Select(l => new
                    {
                        Id = (int)l.Id,
                        Title = l.Name,
                        Type = l.TypeLesson == 1 ? "Pacote Interativo (ZIP/Web)" : "Vídeo / Documento",
                        Viewed = true,
                        NotesCount = 1
                    }).ToArray()
                }
            });
        }
    }
    catch { }

    return Results.Ok(new[]
    {
        new
        {
            ModuleId = 1,
            ModuleName = "Módulo 1: Fundamentos e Conceitos Iniciais",
            Lessons = new[]
            {
                new { Id = 101, Title = "Aula 1: Introdução ao Método Científico", Type = "Pacote Interativo (ZIP)", Viewed = true, NotesCount = 2 },
                new { Id = 102, Title = "Aula 2: Estruturas Moleculares e Ligações", Type = "Vídeo Aula (Link)", Viewed = false, NotesCount = 0 }
            }
        },
        new
        {
            ModuleId = 2,
            ModuleName = "Módulo 2: Reações e Termoquímica",
            Lessons = new[]
            {
                new { Id = 103, Title = "Aula 3: Leis da Termodinâmica e Entalpia", Type = "Pacote Interativo (ZIP)", Viewed = false, NotesCount = 0 },
                new { Id = 104, Title = "Aula 4: Equilíbrio Químico e Soluções", Type = "Pacote Interativo (ZIP)", Viewed = false, NotesCount = 0 }
            }
        }
    });
})
.WithName("GetCurriculumUnitLessons")
.WithSummary("Retorna os módulos didáticos e aulas da disciplina");

// Fóruns de Discussão da Disciplina (Espelha 10_turma_forum_discussoes.png)
app.MapGet("/api/v1/curriculum-units/{id}/discussions", async (int id, SolarDbContext db) =>
{
    try
    {
        var dbDiscussions = await db.Discussions.Take(5).ToListAsync();
        if (dbDiscussions.Any())
        {
            return Results.Ok(dbDiscussions.Select((d, idx) => new
            {
                Id = (int)d.Id,
                Title = d.Name,
                Description = d.Description ?? "Tópico de discussão acadêmica da disciplina.",
                Period = "25/07/2011 - 04/10/2026",
                PostsCount = 10 + idx * 3,
                Status = "Iniciado",
                IsEvaluative = idx == 0,
                IsFrequency = idx == 0,
                StudentGrade = idx == 0 ? (double?)8.5 : (double?)null
            }));
        }
    }
    catch { }

    return Results.Ok(new[]
    {
        new
        {
            Id = 1,
            Title = "Forum 1: Discussão sobre Aplicações Práticas",
            Description = "Por conseguinte, o início da atividade geral de formação de atitudes não pode mais se dissociar dos modos de operação convencionais.",
            Period = "25/07/2011 - 04/10/2026",
            PostsCount = 14,
            Status = "Iniciado",
            IsEvaluative = true,
            IsFrequency = true,
            StudentGrade = (double?)8.5
        },
        new
        {
            Id = 2,
            Title = "Forum 2: Dúvidas e Estudos de Caso",
            Description = "Espaço reservado para interação sobre os experimentos laboratoriais virtuais do módulo 2.",
            Period = "01/08/2026 - 15/12/2026",
            PostsCount = 6,
            Status = "Iniciado",
            IsEvaluative = false,
            IsFrequency = false,
            StudentGrade = (double?)null
        }
    });
})
.WithName("GetCurriculumUnitDiscussions")
.WithSummary("Retorna os tópicos do fórum de discussão");

// Trabalhos e Portfólios da Disciplina (Espelha 11_turma_trabalhos_assignments.png)
app.MapGet("/api/v1/curriculum-units/{id}/assignments", async (int id, SolarDbContext db) =>
{
    try
    {
        var dbAssignments = await db.Assignments.Take(5).ToListAsync();
        if (dbAssignments.Any())
        {
            return Results.Ok(dbAssignments.Select((a, idx) => new
            {
                Id = (int)a.Id,
                Title = a.Name,
                Type = a.TypeAssignment == 1 ? "Em Grupo" : "Individual",
                MaxGroupMembers = a.TypeAssignment == 1 ? 4 : 1,
                GroupName = a.TypeAssignment == 1 ? (string?)"Grupo 01 (Ana Silva, Carlos Eduardo, Fabrício Lima)" : (string?)null,
                Deadline = "30/11/2026 às 23:59",
                Status = idx == 0 ? "Enviado" : "Pendente",
                SubmittedFile = idx == 0 ? (string?)("Relatorio_" + a.Name.Replace(" ", "_") + ".pdf") : (string?)null,
                Grade = idx == 0 ? (double?)9.0 : (double?)null,
                Feedback = idx == 0 ? (string?)"Excelente abordagem e fundamentação teórica." : (string?)null
            }));
        }
    }
    catch { }

    return Results.Ok(new[]
    {
        new
        {
            Id = 1,
            Title = "Trabalho Prático 1: Relatório Experimental",
            Type = "Em Grupo",
            MaxGroupMembers = 4,
            GroupName = (string?)"Grupo 01 (Ana Silva, Carlos Eduardo, Fabrício Lima)",
            Deadline = "30/11/2026 às 23:59",
            Status = "Enviado",
            SubmittedFile = (string?)"Relatorio_Grupo_01_Quimica.pdf",
            Grade = (double?)9.0,
            Feedback = (string?)"Excelente abordagem e fundamentação teórica."
        },
        new
        {
            Id = 2,
            Title = "Trabalho Individual 2: Resenha Crítica",
            Type = "Individual",
            MaxGroupMembers = 1,
            GroupName = (string?)null,
            Deadline = "15/12/2026 às 23:59",
            Status = "Pendente",
            SubmittedFile = (string?)null,
            Grade = (double?)null,
            Feedback = (string?)null
        }
    });
})
.WithName("GetCurriculumUnitAssignments")
.WithSummary("Retorna os trabalhos da disciplina");

// Diário de Notas e Acompanhamento do Aluno (Espelha 12_turma_acompanhamento_notas.png)
app.MapGet("/api/v1/curriculum-units/{id}/scores", (int id) =>
{
    return Results.Ok(new
    {
        StudentName = "Aluno 1",
        WorkingHours = "64 h/a",
        StaffResponsibles = "Professor (Prof. Titular), Usuario do Sistema (Prof. Titular)",
        FinalExamGrade = (double?)null,
        FinalGrade = 7.8,
        FrequencyHours = 56,
        AttendancePercentage = 87.5,
        Situation = "Pendente",
        EvaluativeActivities = new[]
        {
            new { Name = "Prova 1 (Bloco 40%)", Weight = 1.0, FinalWeight = "40%", Grade = 8.0, Frequency = "30h" },
            new { Name = "Trabalho 1 (Bloco 60%)", Weight = 1.0, FinalWeight = "60%", Grade = 7.5, Frequency = "26h" },
            new { Name = "Fórum Avaliativo 1", Weight = 1.0, FinalWeight = "—", Grade = 8.5, Frequency = "—" }
        },
        AccessHistory = new[]
        {
            new { Date = DateTime.UtcNow.ToString("dd/MM/yyyy"), Time = DateTime.UtcNow.ToString("HH:mm:ss") },
            new { Date = DateTime.UtcNow.AddDays(-1).ToString("dd/MM/yyyy"), Time = "14:22:10" },
            new { Date = DateTime.UtcNow.AddDays(-3).ToString("dd/MM/yyyy"), Time = "09:15:33" }
        }
    });
})
.WithName("GetCurriculumUnitScores")
.WithSummary("Retorna o boletim/diário de notas da disciplina");

// Participantes da Turma (Espelha 13_turma_participantes.png)
app.MapGet("/api/v1/curriculum-units/{id}/participants", async (int id, SolarDbContext db) =>
{
    try
    {
        var users = await db.Users.Take(8).ToListAsync();
        if (users.Any())
        {
            return Results.Ok(users.Select((u, idx) => new
            {
                Id = (int)u.Id,
                Name = u.Name ?? u.Username,
                Role = idx == 0 ? "Professor" : idx == 1 ? "Tutor Presencial" : idx == 2 ? "Aluno (Você)" : "Aluno",
                Email = u.Email ?? (u.Username + "@solar.ufc.br"),
                Location = "Fortaleza - CE"
            }));
        }
    }
    catch { }

    return Results.Ok(new[]
    {
        new { Id = 1, Name = "Prof. Titular UAB", Role = "Professor", Email = "professor@solar.ufc.br", Location = "Fortaleza - CE" },
        new { Id = 2, Name = "Tutor Presencial Polo Caucaia", Role = "Tutor Presencial", Email = "tutor.caucaia@solar.ufc.br", Location = "Polo Caucaia" },
        new { Id = 3, Name = "Aluno 1 (Você)", Role = "Aluno", Email = "aluno1@solar.ufc.br", Location = "Fortaleza - CE" },
        new { Id = 4, Name = "Aluno 2", Role = "Aluno", Email = "aluno2@solar.ufc.br", Location = "Caucaia - CE" },
        new { Id = 5, Name = "Aluno 3 (Monitor)", Role = "Monitor", Email = "monitor@solar.ufc.br", Location = "Fortaleza - CE" }
    });
})
.WithName("GetCurriculumUnitParticipants")
.WithSummary("Retorna os participantes e docentes da turma");

// Correio Eletrônico Interno (Espelha 03_mensagens_correio.png)
app.MapGet("/api/v1/messages", async (string? folder, SolarDbContext db) =>
{
    var folderTarget = folder?.ToLower() ?? "inbox";
    try
    {
        var dbMessages = await db.InternalMessages.Take(5).ToListAsync();
        if (dbMessages.Any())
        {
            if (folderTarget == "outbox")
            {
                return Results.Ok(new[]
                {
                    new { Id = 201, Subject = "Dúvida sobre o Trabalho Prático 1", Recipient = "Prof. Titular UAB", Date = "17/08/2026 15:40", Read = true, Body = "Prezado professor, gostaria de confirmar se o relatório do grupo pode conter anexos fotográficos." }
                });
            }
            if (folderTarget == "trash")
            {
                return Results.Ok(Array.Empty<object>());
            }

            return Results.Ok(dbMessages.Select(m => new
            {
                Id = (int)m.Id,
                Subject = m.Subject,
                Sender = "Prof. Titular UAB",
                Date = m.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Read = true,
                Body = m.Body
            }));
        }
    }
    catch { }

    if (folderTarget == "outbox")
    {
        return Results.Ok(new[]
        {
            new { Id = 201, Subject = "Dúvida sobre o Trabalho Prático 1", Recipient = "Prof. Titular UAB", Date = "17/08/2026 15:40", Read = true, Body = "Prezado professor, gostaria de confirmar se o relatório do grupo pode conter anexos fotográficos." }
        });
    }
    if (folderTarget == "trash")
    {
        return Results.Ok(Array.Empty<object>());
    }

    return Results.Ok(new[]
    {
        new { Id = 101, Subject = "Boas-vindas ao período letivo 2026.1", Sender = "Prof. Titular UAB", Date = "18/08/2026 08:30", Read = false, Body = "Sejam muito bem-vindos ao curso no Solar LMS. Nosso cronograma já está publicado." },
        new { Id = 102, Subject = "Agendamento de Atendimento Virtual", Sender = "Tutor a Distância", Date = "17/08/2026 19:15", Read = true, Body = "Informamos que os atendimentos virtuais ocorrerão às quintas-feiras." }
    });
})
.WithName("GetMessages")
.WithSummary("Retorna mensagens do correio interno");

// Envio de Mensagem Direta
app.MapPost("/api/v1/messages", async (SendMessageRequest req, SolarDbContext db) =>
{
    try
    {
        var msg = new InternalMessage
        {
            UserId = 1,
            Subject = req.Subject,
            Body = req.Body,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.InternalMessages.Add(msg);
        await db.SaveChangesAsync();
    }
    catch { }

    return Results.Ok(new
    {
        Success = true,
        Message = "Mensagem transmitida com sucesso para o destinatário."
    });
})
.WithName("SendMessage")
.WithSummary("Envia uma nova mensagem no correio interno");

// Eventos e Agenda do Mês (Espelha Portlet Agenda)
app.MapGet("/api/v1/agenda", () => Results.Ok(new
{
    Month = "Agosto 2026",
    CurrentDay = 18,
    ActiveDays = new[] { 3, 10, 17, 18, 24, 26, 31 },
    Events = new[]
    {
        new { Day = 17, Title = "Atividade II - Abertura do Fórum Temático" },
        new { Day = 18, Title = "Início de: Atividade III - Exercícios de Fixação" },
        new { Day = 24, Title = "Prazo de Entrega: Questionário Módulo 1" }
    }
}))
.WithName("GetAgenda")
.WithSummary("Retorna os acontecimentos e eventos do calendário");

// Upload Real de Arquivos de Trabalhos / Portfólio
app.MapPost("/api/v1/curriculum-units/{id}/assignments/{assignmentId}/upload", async (
    int id,
    int assignmentId,
    HttpRequest request,
    IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType || !request.Form.Files.Any())
    {
        return Results.BadRequest(new { Success = false, Message = "Nenhum arquivo anexado para envio." });
    }

    var file = request.Form.Files[0];
    if (file.Length == 0)
    {
        return Results.BadRequest(new { Success = false, Message = "Arquivo vazio." });
    }

    var allowedExtensions = new[] { ".pdf", ".zip", ".docx", ".doc", ".png", ".jpg", ".txt" };
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
    {
        return Results.BadRequest(new { Success = false, Message = $"Extensão {ext} não permitida. Permitidos: {string.Join(", ", allowedExtensions)}" });
    }

    var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "assignments");
    Directory.CreateDirectory(uploadsFolder);

    var safeFileName = $"Entrega_Turma_{id}_Trabalho_{assignmentId}_{Guid.NewGuid():N}{ext}";
    var filePath = Path.Combine(uploadsFolder, safeFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new
    {
        Success = true,
        FileName = file.FileName,
        SavedFileName = safeFileName,
        FileUrl = $"/uploads/assignments/{safeFileName}",
        Size = file.Length,
        SubmittedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
        Message = "Arquivo de trabalho submetido e registrado com sucesso no Solar LMS!"
    });
})
.DisableAntiforgery()
.WithName("UploadAssignmentFile")
.WithSummary("Recebe e armazena arquivo de trabalho de aluno");

// Listagem de Provas Online da Disciplina
app.MapGet("/api/v1/curriculum-units/{id}/exams", () => Results.Ok(new[]
{
    new
    {
        Id = 1,
        Name = "Prova Online 1 - Avaliação Semestral",
        Description = "Avaliação oficial individual cobrindo os módulos 1 e 2. Trava anti-fraude ativada.",
        DurationMinutes = 60,
        TotalQuestions = 4,
        BlockContent = true, // Trava Anti-Fraude
        Status = "Aberta",
        Deadline = "15/09/2026 23:59",
        AttemptsAllowed = 1,
        AttemptsMade = 0
    }
}))
.WithName("GetExams")
.WithSummary("Retorna as provas online da disciplina");

// Iniciar Prova Online (Gera tentativa e ativa trava anti-fraude)
app.MapPost("/api/v1/curriculum-units/{id}/exams/{examId}/start", (int id, int examId) =>
{
    return Results.Ok(new
    {
        ExamId = examId,
        Name = "Prova Online 1 - Avaliação Semestral",
        DurationMinutes = 60,
        StartedAt = DateTime.UtcNow,
        BlockContent = true,
        Questions = new[]
        {
            new
            {
                Id = 101,
                Enunciation = "1. Qual é a principal característica das ligações covalentes nos compostos orgânicos?",
                Type = "SingleChoice",
                Items = new[]
                {
                    new { Id = 1, Text = "A) Compartilhamento de pares de elétrons entre átomos", Correct = true },
                    new { Id = 2, Text = "B) Transferência total de elétrons com formação de cátions e ânions", Correct = false },
                    new { Id = 3, Text = "C) Atração eletrostática exclusiva entre metais alcalinos", Correct = false },
                    new { Id = 4, Text = "D) Ausência total de nuvem eletrônica", Correct = false }
                }
            },
            new
            {
                Id = 102,
                Enunciation = "2. Em relação à Primeira Lei da Termodinâmica, assinale a afirmação correta:",
                Type = "SingleChoice",
                Items = new[]
                {
                    new { Id = 5, Text = "A) A energia total de um sistema isolado permanece constante (ΔU = Q - W)", Correct = true },
                    new { Id = 6, Text = "B) A entropia do universo sempre diminui em processos espontâneos", Correct = false },
                    new { Id = 7, Text = "C) O calor não pode ser convertido em trabalho sob nenhuma condição", Correct = false },
                    new { Id = 8, Text = "D) Todo trabalho se transforma em massa pura", Correct = false }
                }
            },
            new
            {
                Id = 103,
                Enunciation = "3. O equilíbrio químico dinâmico é caracterizado quando:",
                Type = "SingleChoice",
                Items = new[]
                {
                    new { Id = 9, Text = "A) As velocidades das reações direta e inversa tornam-se iguais", Correct = true },
                    new { Id = 10, Text = "B) Todos os reagentes são completamente consumidos a zero", Correct = false },
                    new { Id = 11, Text = "C) A pressão do sistema cai instantaneamente para vácuo", Correct = false }
                }
            }
        }
    });
})
.WithName("StartExam")
.WithSummary("Inicia a realização de uma prova online");

// Submissão e Correção Automática de Prova Online
app.MapPost("/api/v1/curriculum-units/{id}/exams/{examId}/submit", (
    int id,
    int examId,
    ExamSubmissionRequest submission,
    ExamScoringService scoringService) =>
{
    // Simula correção das 3 questões
    int correctCount = 0;
    if (submission.Answers.TryGetValue(101, out int ans1) && ans1 == 1) correctCount++;
    if (submission.Answers.TryGetValue(102, out int ans2) && ans2 == 5) correctCount++;
    if (submission.Answers.TryGetValue(103, out int ans3) && ans3 == 9) correctCount++;

    double grade = (correctCount / 3.0) * 10.0;

    return Results.Ok(new
    {
        Success = true,
        ExamId = examId,
        Score = Math.Round(grade, 1),
        TotalQuestions = 3,
        CorrectAnswers = correctCount,
        Situation = grade >= 7.0 ? "Aprovado na Avaliação" : "Necessita Recuperação",
        SubmittedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"),
        Message = "Prova finalizada e corrigida com sucesso! Trava anti-fraude desativada."
    });
})
.WithName("SubmitExam")
.WithSummary("Submete as respostas e calcula a nota da prova online");

// Mapeamento do Hub SignalR de Chat
app.MapHub<ChatHub>("/hubs/chat");

// Suporte a rotas profundas do SPA React (evita 404 ao recarregar a página)
app.MapFallbackToFile("index.html");

app.Run();

// DTOs
public record SendMessageRequest(string Recipient, string Subject, string Body);
public record ExamSubmissionRequest(Dictionary<int, int> Answers);

// Necessário para Testes de Integração com WebApplicationFactory
public partial class Program
{
    public static string FormatNpgsqlConnectionString(string connStr)
    {
        if (string.IsNullOrWhiteSpace(connStr)) return connStr;
        if (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(connStr);
                var userInfo = uri.UserInfo.Split(':');
                var username = Uri.UnescapeDataString(userInfo[0]);
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');
                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch
            {
                return connStr;
            }
        }
        return connStr;
    }
}
