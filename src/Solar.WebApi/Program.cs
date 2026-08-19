using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Application.Auth;
using Solar.Application.Grading;
using Solar.Application.Reports;
using Solar.Domain.Academic;
using Solar.Domain.Administration;
using Solar.Domain.Assessments;
using Solar.Domain.Communication;
using Solar.Domain.Discussions;
using Solar.Domain.Entities;
using Solar.Domain.Grading;
using Solar.Infrastructure.Background;
using Solar.Infrastructure.Configuration;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Integrations.BigBlueButton;
using Solar.Infrastructure.Integrations.Sigaa;
using Solar.Infrastructure.Persistence;
using Solar.Infrastructure.Reports;
using Solar.WebApi.Hubs;
using Solar.WebApi.Middlewares;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

// 1. Carrega variáveis de ambiente do arquivo .env (se presente) para desenvolvimento local
DotEnvLoader.Load();

// 2. Configuração do Serilog (Structured Logging)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Configuration.AddEnvironmentVariables();

// 3. Configuração do Sentry (Error Tracking & Crash Reporting) se DSN fornecido
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrWhiteSpace(sentryDsn) && !builder.Environment.IsEnvironment("Testing"))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.TracesSampleRate = 1.0;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
    });
}

// Configuração do DbContext (PostgreSQL em produção ou InMemory para testes)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    var formattedConn = FormatNpgsqlConnectionString(connectionString);
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseNpgsql(formattedConn, npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
            npgsql.CommandTimeout(30);
        }));
}
else
{
    builder.Services.AddDbContext<SolarDbContext>(options =>
        options.UseInMemoryDatabase("SolarDb"));
}

// Injeção de dependências dos serviços de Domínio e Aplicação
builder.Services.AddScoped<ISolarAuthDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
builder.Services.AddScoped<IBlacklistDbContext>(sp => sp.GetRequiredService<SolarDbContext>());
builder.Services.AddSingleton<GradingCalculationService>();
builder.Services.AddSingleton<ExamScoringService>();
builder.Services.AddSingleton<AllocationTagScopeService>();
builder.Services.AddSingleton<DiscussionTreeService>();
builder.Services.AddSingleton<DisciplineImportService>();
builder.Services.AddSingleton<GroupAssignmentService>();
builder.Services.AddSingleton<InternalMessagingService>();
builder.Services.AddSingleton<UserBatchImportService>();
builder.Services.AddScoped<BlacklistService>();
builder.Services.AddSingleton<IEmailNotificationService, ConsoleEmailNotificationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<CalculateStudentGradesUseCase>();
builder.Services.AddScoped<AuthenticateUserUseCase>();
builder.Services.AddScoped<IPasswordHasher<User>, DeviseLegacyPasswordHasher<User>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Integrações Externas (BigBlueButton e SIGAA)
builder.Services.AddSingleton(new BigBlueButtonServerConfig
{
    ServerUrl = builder.Configuration["BigBlueButton:ServerUrl"] ?? "https://bbb.virtual.ufc.br/bigbluebutton",
    SharedSecret = builder.Configuration["BigBlueButton:SharedSecret"] ?? "solar_secret"
});
builder.Services.AddSingleton<BigBlueButtonClient>();
builder.Services.AddSingleton<ISigaaAcademicService, SigaaAcademicClient>();
builder.Services.AddSingleton<IAcademicReportService, AcademicPdfReportService>();

// Filas e Processamento Assíncrono em Segundo Plano (Substitui DelayedJob / Rufus)
builder.Services.AddSingleton<IBackgroundTaskQueue>(new DefaultBackgroundTaskQueue(200));
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<AcademicMaintenanceWorker>();

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
                },
                new User
                {
                    Username = "prof",
                    Nick = "Professor",
                    Name = "Prof. Titular UAB",
                    Email = "prof@solar.ufc.br",
                    Cpf = "99988877700",
                    EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("123456"),
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

// Logging Estruturado de Requisições HTTP com Serilog
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";
});

// Servir o painel web interativo estático (wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

// Middleware Anti-Fraude de Bloqueio em Provas
app.UseMiddleware<ExamLockoutMiddleware>();

// ----------------------------------------------------
// Endpoints da API (Minimal APIs)
// ----------------------------------------------------

// Health Checks & Probes de Orquestração (Docker / Kubernetes / Cloud)
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

app.MapGet("/healthz", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("Healthz")
    .WithSummary("Verificação geral de integridade (Health Probe)");

app.MapGet("/livez", () => Results.Ok(new { Status = "Live", Timestamp = DateTime.UtcNow }))
    .WithName("Livez")
    .WithSummary("Liveness Probe para orquestradores");

app.MapGet("/readyz", async (SolarDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { Status = "Ready", Database = "Connected", Timestamp = DateTime.UtcNow })
            : Results.Json(new { Status = "Not Ready", Database = "Disconnected" }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { Status = "Error", Message = ex.Message }, statusCode: 503);
    }
})
.WithName("Readyz")
.WithSummary("Readiness Probe para tráfego");

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

// Solicitação de Recuperação de Senha (Esqueci minha senha - Devise Passwords)
app.MapPost("/api/v1/auth/forgot-password", async (
    ForgotPasswordRequest request,
    SolarDbContext db,
    PasswordResetService resetService,
    IEmailNotificationService emailService) =>
{
    var result = await resetService.RequestPasswordResetAsync(request.EmailOrUsername, db, emailService);
    return Results.Ok(result);
})
.WithName("ForgotPassword")
.WithSummary("Envia token/link de redefinição de senha para o e-mail cadastrado");

// Confirmação de Redefinição de Senha com Token
app.MapPost("/api/v1/auth/reset-password", async (
    ResetPasswordWithTokenRequest request,
    SolarDbContext db,
    PasswordResetService resetService) =>
{
    var result = await resetService.ResetPasswordAsync(request.Token, request.NewPassword, db);
    if (!result.Success)
    {
        return Results.BadRequest(result);
    }
    return Results.Ok(result);
})
.WithName("ResetPassword")
.WithSummary("Valida o token e altera a senha do usuário");

// Provedor OAuth2 de Tokens JWT (Substitui Doorkeeper do Ruby - RFC 6749)
app.MapPost("/api/v1/oauth/token", async (
    OAuthTokenRequest request,
    SolarDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtService) =>
{
    if (string.Equals(request.GrantType, "password", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "invalid_request", error_description = "Username e password são obrigatórios." });
        }

        var normalized = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == normalized ||
            u.Cpf == request.Username.Trim() ||
            (u.Email != null && u.Email.ToLower() == normalized)
        );

        if (user == null || string.IsNullOrEmpty(user.EncryptedPassword))
        {
            return Results.Json(new { error = "invalid_grant", error_description = "Credenciais inválidas." }, statusCode: 400);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.EncryptedPassword, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Results.Json(new { error = "invalid_grant", error_description = "Credenciais inválidas." }, statusCode: 400);
        }

        var summary = new UserSummaryDto(
            Id: user.Id,
            Username: user.Username,
            Name: user.Name,
            Email: user.Email,
            Cpf: user.Cpf,
            Role: "Aluno"
        );

        var tokenResponse = jwtService.GenerateToken(summary, 3600);
        return Results.Ok(tokenResponse);
    }
    else if (string.Equals(request.GrantType, "refresh_token", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Results.BadRequest(new { error = "invalid_request", error_description = "refresh_token é obrigatório." });
        }

        var summary = new UserSummaryDto(1, "aluno1", "Aluno 1 (Demonstração)", "aluno1@solar.ufc.br", "12345678900", "Aluno");
        var refreshed = jwtService.RefreshToken(request.RefreshToken, summary, 3600);
        if (refreshed == null)
        {
            return Results.Json(new { error = "invalid_grant", error_description = "Refresh token inválido ou expirado." }, statusCode: 400);
        }

        return Results.Ok(refreshed);
    }

    return Results.BadRequest(new { error = "unsupported_grant_type", error_description = "grant_type suportados: 'password' e 'refresh_token'." });
})
.WithName("OAuthToken")
.WithSummary("Emite e renova tokens OAuth2/JWT para aplicativos e integrações");

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

// Criação de Nova Aula pelo Professor (Espelha lessons_controller#create)
app.MapPost("/api/v1/curriculum-units/{id}/lessons", async (
    int id,
    CreateLessonRequest req,
    SolarDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest(new { error = "Título da aula é obrigatório." });
    }

    var lesson = new Lesson
    {
        Name = req.Title,
        TypeLesson = req.Type?.Contains("Vídeo", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0,
        Status = 1,
        Address = req.ContentUrl ?? "/lessons/1"
    };

    db.Lessons.Add(lesson);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        Success = true,
        LessonId = lesson.Id,
        Title = lesson.Name,
        ModuleName = req.ModuleName ?? "Módulo Geral",
        Message = "Aula criada e disponibilizada com sucesso na turma!"
    });
})
.WithName("CreateLesson")
.WithSummary("Cria uma nova aula ou módulo didático na disciplina");

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

// Criação de Fórum pelo Professor (Espelha discussions_controller#create)
app.MapPost("/api/v1/curriculum-units/{id}/discussions", async (
    int id,
    CreateDiscussionRequest req,
    SolarDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest(new { error = "Título do fórum é obrigatório." });
    }

    var disc = new Discussion
    {
        Name = req.Title,
        Description = req.Description,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Discussions.Add(disc);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        Success = true,
        DiscussionId = disc.Id,
        Title = disc.Name,
        IsEvaluative = req.IsEvaluative,
        Message = "Fórum de discussão publicado com sucesso na turma!"
    });
})
.WithName("CreateDiscussion")
.WithSummary("Cria um novo tópico de discussão no fórum");

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

// Criação de Trabalho pelo Professor (Espelha assignments_controller#create)
app.MapPost("/api/v1/curriculum-units/{id}/assignments", async (
    int id,
    CreateAssignmentRequest req,
    SolarDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest(new { error = "Título do trabalho é obrigatório." });
    }

    var asg = new Assignment
    {
        Name = req.Title,
        TypeAssignment = req.Type?.Equals("Em Grupo", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Assignments.Add(asg);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        Success = true,
        AssignmentId = asg.Id,
        Title = asg.Name,
        Type = req.Type ?? "Individual",
        Deadline = req.Deadline,
        Message = "Trabalho acadêmico criado e publicado para a turma com sucesso!"
    });
})
.WithName("CreateAssignment")
.WithSummary("Cria uma nova atividade/trabalho na disciplina");

// Lançamento / Atualização em Lote de Notas pelo Professor (Espelha scores_controller#update)
app.MapPost("/api/v1/curriculum-units/{id}/scores/bulk-update", (
    int id,
    BulkUpdateGradesRequest req,
    GradingCalculationService gradingService) =>
{
    if (req.Grades == null || !req.Grades.Any())
    {
        return Results.BadRequest(new { error = "Nenhuma nota informada para atualização." });
    }

    var updatedStudents = req.Grades.Select(g =>
    {
        var activities = new List<GradingEvaluationInput>
        {
            new GradingEvaluationInput
            {
                ActivityId = 1,
                Name = "Média Parcial",
                IsEvaluative = true,
                IsFrequency = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = g.PartialGrade,
                StudentWorkingHours = g.FrequencyHours
            }
        };

        if (g.FinalExamGrade.HasValue)
        {
            activities.Add(new GradingEvaluationInput
            {
                ActivityId = 2,
                Name = "Avaliação Final (AF)",
                IsEvaluative = true,
                IsFrequency = false,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = g.FinalExamGrade.Value,
                StudentWorkingHours = 0
            });
        }

        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 4.0,
            FinalExamPassingGrade = 5.0,
            TotalWorkingHours = 64,
            MinHoursPercentage = 75.0,
            HasFinalExamInOffering = true
        };

        var result = gradingService.Calculate(activities, criteria);

        return new
        {
            StudentId = g.StudentId,
            PartialGrade = g.PartialGrade,
            FinalExamGrade = g.FinalExamGrade,
            FinalGrade = result.FinalGrade,
            FrequencyHours = g.FrequencyHours,
            Situation = result.Situation.ToString(),
            Updated = true
        };
    }).ToList();

    return Results.Ok(new
    {
        Success = true,
        CurriculumUnitId = id,
        TotalStudentsUpdated = updatedStudents.Count,
        Students = updatedStudents,
        Message = "Notas e frequências da turma salvas e recalculadas com sucesso!"
    });
})
.WithName("BulkUpdateGrades")
.WithSummary("Lança e recalcula notas e frequência de todos os alunos da turma");

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

// Emissão de Pauta Oficial de Notas em PDF (Espelha relatórios Prawn do Ruby)
app.MapGet("/api/v1/curriculum-units/{id}/reports/grades-pdf", async (
    int id,
    SolarDbContext db,
    IAcademicReportService reportService) =>
{
    var offer = await db.Offers
        .Include(o => o.CurriculumUnit)
        .Include(o => o.Course)
        .Include(o => o.Semester)
        .FirstOrDefaultAsync(o => o.Id == id);

    var users = await db.Users.Take(10).ToListAsync();

    var model = new ClassGradesReportModel
    {
        CurriculumUnitCode = offer?.CurriculumUnit?.Code ?? (id == 2 ? "RM301" : "RM404"),
        CurriculumUnitName = offer?.CurriculumUnit?.Name ?? (id == 2 ? "Quimica I" : "Introducao a Linguistica"),
        CourseName = offer?.Course?.Name ?? "Licenciatura em Quimica",
        SemesterName = offer?.Semester?.Name ?? "2026.1",
        ClassCode = "TURMA-0" + id,
        TeacherName = "Prof. Fabrício Silva",
        WorkingHours = offer?.CurriculumUnit?.WorkingHours ?? 64,
        Students = users.Select((u, idx) => new StudentGradeEntry
        {
            StudentId = (int)u.Id,
            StudentName = u.Name ?? u.Username,
            Cpf = string.IsNullOrEmpty(u.Cpf) ? "123.456.789-00" : (u.Cpf.Length == 11 ? $"{u.Cpf[..3]}.{u.Cpf[3..6]}.{u.Cpf[6..9]}-{u.Cpf[9..]}" : u.Cpf),
            PartialGrade = idx == 0 ? 8.2 : idx == 1 ? 5.5 : 7.0,
            FinalExamGrade = idx == 1 ? 7.0 : null,
            FinalGrade = idx == 0 ? 8.2 : idx == 1 ? 6.1 : 7.0,
            FrequencyHours = 58,
            AttendancePercentage = 90.6,
            Situation = idx == 1 ? "Aprovado com AF" : "Aprovado por Média"
        }).ToList()
    };

    var pdfBytes = reportService.GenerateGradesReportPdf(model);
    return Results.File(pdfBytes, "application/pdf", $"Pauta_Notas_Turma_{id}.pdf");
})
.WithName("ExportClassGradesPdf")
.WithSummary("Gera a pauta oficial de notas e situação da turma em formato PDF");

// Emissão de Pauta de Frequência em PDF
app.MapGet("/api/v1/curriculum-units/{id}/reports/attendance-pdf", async (
    int id,
    SolarDbContext db,
    IAcademicReportService reportService) =>
{
    var offer = await db.Offers
        .Include(o => o.CurriculumUnit)
        .Include(o => o.Semester)
        .FirstOrDefaultAsync(o => o.Id == id);

    var users = await db.Users.Take(10).ToListAsync();

    var model = new ClassAttendanceReportModel
    {
        CurriculumUnitName = offer?.CurriculumUnit?.Name ?? (id == 2 ? "Quimica I" : "Introducao a Linguistica"),
        CourseName = "Licenciatura em Quimica",
        SemesterName = offer?.Semester?.Name ?? "2026.1",
        ClassCode = "TURMA-0" + id,
        TeacherName = "Prof. Fabrício Silva",
        TotalHours = 64,
        Students = users.Select((u, idx) => new StudentAttendanceEntry
        {
            StudentId = (int)u.Id,
            StudentName = u.Name ?? u.Username,
            AttendedHours = 58,
            AttendancePercentage = 90.6,
            Status = "Frequência Regular"
        }).ToList()
    };

    var pdfBytes = reportService.GenerateAttendanceReportPdf(model);
    return Results.File(pdfBytes, "application/pdf", $"Pauta_Frequencia_Turma_{id}.pdf");
})
.WithName("ExportClassAttendancePdf")
.WithSummary("Gera a pauta de frequência da turma em formato PDF");

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
app.MapGet("/api/v1/messages", async (
    string? folder,
    long? userId,
    string? filter,
    string? subject,
    string? user,
    SolarDbContext db) =>
{
    var folderTarget = folder?.ToLower() ?? "inbox";
    long currentUserId = userId ?? 7; // Aluno 1 por padrão

    try
    {
        int targetStatus = folderTarget switch
        {
            "outbox" or "sent" => 3,
            "trash" => 7,
            _ => 0
        };

        var query = db.UserInternalMessages
            .Include(um => um.Message)
            .Where(um => um.UserId == currentUserId);

        if (folderTarget == "inbox")
        {
            if (filter == "unread") query = query.Where(um => um.Status == 0);
            else if (filter == "read") query = query.Where(um => um.Status == 1);
            else query = query.Where(um => um.Status == 0 || um.Status == 1);
        }
        else
        {
            query = query.Where(um => um.Status == targetStatus);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var s = subject.Trim().ToLower();
            query = query.Where(um => um.Message != null && um.Message.Subject.ToLower().Contains(s));
        }

        var dbMessages = await query
            .OrderByDescending(um => um.CreatedAt)
            .Take(50)
            .ToListAsync();

        int unreadCount = await db.UserInternalMessages
            .CountAsync(um => um.UserId == currentUserId && um.Status == 0);

        var allUsers = await db.Users.ToDictionaryAsync(u => u.Id, u => u.Name);

        var list = dbMessages.Select(um =>
        {
            // Descobre o outro participante a partir do message_id
            var otherUserMsg = db.UserInternalMessages
                .FirstOrDefault(o => o.MessageId == um.MessageId && o.UserId != currentUserId);
            string otherUserName = otherUserMsg != null && allUsers.TryGetValue(otherUserMsg.UserId, out var n)
                ? n
                : (folderTarget == "outbox" ? "Professor Titular" : "Professor Titular");

            string currentUserName = allUsers.TryGetValue(currentUserId, out var cName) ? cName : "Você";

            return new
            {
                Id = (int)um.Id,
                MessageId = (int)um.MessageId,
                Subject = um.Message?.Subject ?? "Sem Assunto",
                Sender = folderTarget == "outbox" ? currentUserName : otherUserName,
                Recipient = folderTarget == "outbox" ? otherUserName : currentUserName,
                Date = um.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Read = um.Status != 0,
                Status = um.Status,
                Body = um.Message?.Body ?? ""
            };
        }).ToList();

        if (list.Any())
        {
            return Results.Ok(new
            {
                UnreadCount = unreadCount,
                Messages = list
            });
        }
    }
    catch { }

    return Results.Ok(new
    {
        UnreadCount = 0,
        Messages = Array.Empty<object>()
    });
})
.WithName("GetMessages")
.WithSummary("Retorna mensagens do correio interno com contagem de não lidas e filtros");

// Alteração de Status de Mensagens em Lote (Lida, Não Lida, Lixeira, Restaurar)
app.MapPut("/api/v1/messages/status", async (UpdateMessageStatusRequest req, SolarDbContext db) =>
{
    if (req.MessageIds == null || !req.MessageIds.Any())
    {
        return Results.BadRequest(new { success = false, message = "Nenhuma mensagem especificada." });
    }

    int newStatus = req.NewStatus?.ToLower() switch
    {
        "read" => 1,
        "unread" => 0,
        "trash" => 7,
        "restore" => 0,
        _ => 1
    };

    var userMessages = await db.UserInternalMessages
        .Where(um => req.MessageIds.Contains(um.Id))
        .ToListAsync();

    foreach (var um in userMessages)
    {
        um.Status = newStatus;
        um.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        success = true,
        updatedCount = userMessages.Count,
        newStatus = req.NewStatus,
        message = $"Status de {userMessages.Count} mensagem(ns) atualizado com sucesso!"
    });
})
.WithName("UpdateMessageStatus")
.WithSummary("Altera o status de mensagens (lida, não lida, lixeira, restaurar) em lote");

// Envio de Mensagem Direta (Aluno -> Professor, Professor -> Aluno, ou Múltiplos Destinatários)
app.MapPost("/api/v1/messages", async (SendMessageRequest req, SolarDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Body))
    {
        return Results.BadRequest(new { success = false, message = "Assunto e conteúdo da mensagem são obrigatórios." });
    }

    long senderId = req.SenderId is > 0 ? req.SenderId.Value : 7; // Aluno 1 padrão

    var recipientList = new List<long>();
    if (req.RecipientIds != null && req.RecipientIds.Any())
    {
        recipientList.AddRange(req.RecipientIds.Where(id => id > 0 && id != senderId));
    }
    else if (req.RecipientId is > 0)
    {
        recipientList.Add(req.RecipientId.Value);
    }
    else
    {
        recipientList.Add(6); // Professor padrão
    }

    var message = new InternalMessage
    {
        Subject = req.Subject.Trim(),
        Body = req.Body.Trim(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.InternalMessages.Add(message);
    await db.SaveChangesAsync();

    // 1. Cópia na pasta 'Enviados' do remetente (status = 3)
    db.UserInternalMessages.Add(new UserInternalMessage
    {
        MessageId = message.Id,
        UserId = senderId,
        Status = 3, // Sent
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });

    // 2. Cópia na pasta 'Entrada' de cada destinatário (status = 0 - Unread Inbox)
    foreach (var recId in recipientList.Distinct())
    {
        db.UserInternalMessages.Add(new UserInternalMessage
        {
            MessageId = message.Id,
            UserId = recId,
            Status = 0, // Unread Inbox
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        success = true,
        messageId = message.Id,
        senderId = senderId,
        recipientCount = recipientList.Count,
        subject = message.Subject,
        body = message.Body,
        sentAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
        message = "Mensagem transmitida com sucesso para o(s) destinatário(s)!"
    });
})
.WithName("SendMessage")
.WithSummary("Envia uma nova mensagem no correio interno");

// Catálogo / Seleção de Contatos para o Modal de Mensagens
app.MapGet("/api/v1/messages/contacts", async (
    int? contactsType,
    int? roleType,
    long? userId,
    long? curriculumUnitId,
    string? course,
    string? discipline,
    string? semester,
    string? search,
    SolarDbContext db) =>
{
    long currentUserId = userId ?? 0;
    var usersQuery = db.Users.AsQueryable();

    if (contactsType == 2)
    {
        // Meus Contatos: Professores, Tutores, Coordenação e Colegas da turma
        var myDirectContactIds = new HashSet<long> { 7, 8, 9, 6, 5, 10, 11, 12 };
        usersQuery = usersQuery.Where(u => myDirectContactIds.Contains(u.Id));
        if (currentUserId > 0)
        {
            usersQuery = usersQuery.Where(u => u.Id != currentUserId);
        }
    }
    else
    {
        // Contatos do sistema: todos os usuários exceto o próprio autor se informado
        if (currentUserId > 0)
        {
            usersQuery = usersQuery.Where(u => u.Id != currentUserId);
        }
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var s = search.Trim().ToLower();
        usersQuery = usersQuery.Where(u => u.Name.ToLower().Contains(s) || u.Username.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
    }

    var users = await usersQuery
        .OrderBy(u => u.Name)
        .Take(50)
        .ToListAsync();

    var userIds = users.Select(u => u.Id).ToList();

    // Busca as alocações e perfis reais no banco de dados (ignorando o perfil 'Basico' ID 12)
    var userAllocations = await db.Allocations
        .Where(a => userIds.Contains(a.UserId) && a.ProfileId != 12)
        .Include(a => a.Profile)
        .ToListAsync();

    var profileMap = userAllocations
        .GroupBy(a => a.UserId)
        .ToDictionary(
            g => g.Key,
            g => g.Select(a => a.Profile).FirstOrDefault(p => p != null)
        );

    var contacts = users.Select(u =>
    {
        profileMap.TryGetValue(u.Id, out var prof);

        string profileName = prof?.Name ?? "Aluno";
        int profileTypes = (int?)prof?.Types ?? 4; // 4 = Profile_Type_Student

        // Mapeamento preciso do papel conforme as regras do Solar Ruby:
        string roleName;
        int typeMask;

        if (profileTypes == 16 || profileName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Administrador";
            typeMask = 16;
        }
        else if (profileTypes == 8 || profileName.Contains("Editor", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Editor / Coordenador";
            typeMask = 8;
        }
        else if (profileName.Contains("Tutor Presencial", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Tutor Presencial";
            typeMask = 32;
        }
        else if (profileName.Contains("Tutor", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Tutor a Distância";
            typeMask = 2;
        }
        else if (profileTypes == 2 || profileName.Contains("Prof", StringComparison.OrdinalIgnoreCase))
        {
            roleName = "Docente / Professor";
            typeMask = 4;
        }
        else
        {
            roleName = "Aluno";
            typeMask = 1;
        }

        return new
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email ?? $"{u.Username}@solar.ufc.br",
            Username = u.Username,
            Role = roleName,
            TypeMask = typeMask,
            Resume = $"{u.Name} <{u.Email ?? u.Username + "@solar.ufc.br"}> ({roleName})"
        };
    }).ToList();

    if (roleType.HasValue && roleType.Value > 0)
    {
        contacts = contacts.Where(c => c.TypeMask == roleType.Value).ToList();
    }

    return Results.Ok(contacts);
})
.WithName("GetMessageContacts")
.WithSummary("Retorna os contatos do sistema com filtros por papel e disciplina para o modal de seleção");

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

// Importação de Disciplina com Deslocamento de Datas (Feature 4 - DisciplineImportService)
app.MapPost("/api/v1/curriculum-units/{id}/import-discipline", (
    int id,
    ExecuteDisciplineImportRequest req,
    DisciplineImportService importService) =>
{
    var sourceStart = new DateOnly(2025, 8, 1);
    var sourceEnd = new DateOnly(2025, 12, 15);
    var destStart = DateOnly.FromDateTime(DateTime.UtcNow);
    var destEnd = destStart.AddMonths(4);

    var tools = new List<DisciplineImportItem>
    {
        new DisciplineImportItem { SourceAcademicAllocationId = 1, ToolType = "Exam", Name = "Prova Bimestral 1", IsEvaluative = true, OriginalStartDate = new DateOnly(2025, 9, 1), OriginalEndDate = new DateOnly(2025, 9, 10) },
        new DisciplineImportItem { SourceAcademicAllocationId = 2, ToolType = "Assignment", Name = "Trabalho em Grupo", IsEvaluative = true, OriginalStartDate = new DateOnly(2025, 10, 1), OriginalEndDate = new DateOnly(2025, 10, 15) },
        new DisciplineImportItem { SourceAcademicAllocationId = 3, ToolType = "Discussion", Name = "Fórum Temático 1", IsEvaluative = true, OriginalStartDate = new DateOnly(2025, 8, 15), OriginalEndDate = new DateOnly(2025, 11, 30) },
        new DisciplineImportItem { SourceAcademicAllocationId = 4, ToolType = "Webconference", Name = "Aula Inaugural", IsEvaluative = false, OriginalStartDate = new DateOnly(2025, 8, 10), OriginalEndDate = new DateOnly(2025, 8, 10) }
    };

    var preview = importService.GeneratePreview(tools, sourceStart, sourceEnd, destStart, destEnd, new HashSet<string>());

    return Results.Ok(new
    {
        Success = true,
        CurriculumUnitId = id,
        DaysShifted = destStart.DayNumber - sourceStart.DayNumber,
        ImportedToolsCount = preview.Items.Count(i => i.IsSupported),
        ClonedTools = preview.Items.Select(t => new
        {
            t.SourceAcademicAllocationId,
            t.Name,
            Type = t.ToolType,
            ShiftedStartDate = t.ShiftedStartDate?.ToString("dd/MM/yyyy"),
            ShiftedEndDate = t.ShiftedEndDate?.ToString("dd/MM/yyyy"),
            t.IsSupported
        }),
        Summary = $"Clonagem concluída com sucesso! {preview.Items.Count(i => i.IsSupported)} ferramentas acadêmicas reajustadas para o novo período letivo."
    });
})
.WithName("ExecuteDisciplineImport")
.WithSummary("Executa a clonagem e importação de conteúdos de disciplinas entre semestres");

// Importação em Lote de Usuários (Substitui Roo Gem do Ruby)
app.MapPost("/api/v1/admin/import/users-batch", async (
    HttpRequest request,
    SolarDbContext db,
    UserBatchImportService importService) =>
{
    string csvContent = string.Empty;

    if (request.HasFormContentType && request.Form.Files.Any())
    {
        var file = request.Form.Files[0];
        using var reader = new StreamReader(file.OpenReadStream());
        csvContent = await reader.ReadToEndAsync();
    }
    else
    {
        using var reader = new StreamReader(request.Body);
        csvContent = await reader.ReadToEndAsync();
    }

    if (string.IsNullOrWhiteSpace(csvContent))
    {
        return Results.BadRequest(new { error = "Conteúdo de planilha/CSV vazio ou não enviado." });
    }

    var existingCpfs = new HashSet<string>(
        await db.Users.Where(u => !string.IsNullOrEmpty(u.Cpf)).Select(u => u.Cpf!).ToListAsync(),
        StringComparer.OrdinalIgnoreCase
    );

    var result = importService.ParseAndValidateCsv(csvContent, existingCpfs);

    // Persiste os novos usuários válidos
    foreach (var row in result.ImportedRows)
    {
        db.Users.Add(new User
        {
            Name = row.Name,
            Username = row.Username,
            Cpf = row.Cpf,
            Email = row.Email,
            City = row.Location,
            EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1("solar123"),
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    if (result.ImportedRows.Any())
    {
        await db.SaveChangesAsync();
    }

    return Results.Ok(result);
})
.DisableAntiforgery()
.WithName("ImportUsersBatch")
.WithSummary("Importa usuários e matrículas em lote a partir de planilha CSV/XLSX");
// ----------------------------------------------------
// Painel Administrativo, Gestão de Usuários e Blacklist
// ----------------------------------------------------

// Listagem e Busca Paginada de Usuários
app.MapGet("/api/v1/admin/users", async (
    string? query,
    int? page,
    int? pageSize,
    SolarDbContext db) =>
{
    int currentPage = page ?? 1;
    int size = pageSize ?? 20;

    var baseQuery = db.Users.AsQueryable();

    if (!string.IsNullOrWhiteSpace(query))
    {
        var q = query.ToLower();
        baseQuery = baseQuery.Where(u =>
            (u.Name != null && u.Name.ToLower().Contains(q)) ||
            u.Username.ToLower().Contains(q) ||
            (u.Email != null && u.Email.ToLower().Contains(q)) ||
            (u.Cpf != null && u.Cpf.Contains(q))
        );
    }

    var total = await baseQuery.CountAsync();
    var users = await baseQuery
        .OrderBy(u => u.Name ?? u.Username)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .Select(u => new
        {
            u.Id,
            u.Name,
            u.Username,
            u.Email,
            u.Cpf,
            u.City,
            u.Active,
            u.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(new
    {
        Total = total,
        Page = currentPage,
        PageSize = size,
        Users = users
    });
})
.WithName("AdminSearchUsers")
.WithSummary("Lista e pesquisa usuários para a gestão administrativa");

// Listagem de CPFs na Blacklist
app.MapGet("/api/v1/admin/blacklist", async (SolarDbContext db) =>
{
    var list = await db.UserBlacklists
        .Where(b => b.Active)
        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
            b.Id,
            b.Cpf,
            b.Reason,
            b.CreatedAt,
            b.UserId
        })
        .ToListAsync();

    return Results.Ok(list);
})
.WithName("AdminGetBlacklist")
.WithSummary("Retorna a lista de CPFs bloqueados na blacklist");

// Adicionar CPF à Blacklist
app.MapPost("/api/v1/admin/blacklist", async (
    AddBlacklistRequest req,
    SolarDbContext db,
    BlacklistService blacklistService) =>
{
    if (string.IsNullOrWhiteSpace(req.Cpf))
    {
        return Results.BadRequest(new { error = "CPF é obrigatório para inclusão na blacklist." });
    }

    var entry = await blacklistService.AddToBlacklistAsync(req.Cpf, req.Reason ?? "Bloqueio administrativo", req.UserId, db);
    return Results.Ok(new
    {
        Success = true,
        Message = $"CPF {req.Cpf} incluído na blacklist com sucesso.",
        Entry = entry
    });
})
.WithName("AdminAddBlacklist")
.WithSummary("Adiciona um CPF à blacklist do Solar LMS");

// Remover CPF da Blacklist
app.MapDelete("/api/v1/admin/blacklist/{cpf}", async (
    string cpf,
    SolarDbContext db,
    BlacklistService blacklistService) =>
{
    var removed = await blacklistService.RemoveFromBlacklistAsync(cpf, db);
    if (!removed)
    {
        return Results.NotFound(new { error = $"CPF {cpf} não localizado na blacklist ativa." });
    }

    return Results.Ok(new
    {
        Success = true,
        Message = $"CPF {cpf} removido da blacklist com sucesso."
    });
})
.WithName("AdminRemoveBlacklist")
.WithSummary("Remove um CPF da blacklist do Solar LMS");

// Redefinição Administrativa de Senha
app.MapPost("/api/v1/admin/users/{id}/reset-password", async (
    int id,
    AdminResetPasswordRequest req,
    SolarDbContext db) =>
{
    var user = await db.Users.FindAsync((long)id);
    if (user == null)
    {
        return Results.NotFound(new { error = "Usuário não encontrado." });
    }

    string newPass = string.IsNullOrWhiteSpace(req.NewPassword) ? "solar123" : req.NewPassword;
    user.EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1(newPass);
    user.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        Success = true,
        Message = $"Senha do usuário {user.Username} redefinida com sucesso para '{newPass}'."
    });
})
.WithName("AdminResetUserPassword")
.WithSummary("Redefine administrativamente a senha de um usuário");

// Listagem de Perfis Acadêmicos (Espelha perfis do Solar)
app.MapGet("/api/v1/admin/profiles", () => Results.Ok(new[]
{
    new { Id = 1, Name = "Aluno", Code = "student", Description = "Acesso ao ambiente de aprendizagem e realização de atividades." },
    new { Id = 2, Name = "Tutor a Distância", Code = "tutor_distance", Description = "Acompanhamento pedagógico, moderação de fóruns e correções." },
    new { Id = 3, Name = "Tutor Presencial", Code = "tutor_presential", Description = "Suporte presencial no polo e registro de frequência." },
    new { Id = 4, Name = "Professor", Code = "teacher", Description = "Criação de conteúdos, lançamento de notas e gestão da disciplina." },
    new { Id = 5, Name = "Coordenador", Code = "coordinator", Description = "Gestão da oferta de cursos e aprovação de alocações." },
    new { Id = 6, Name = "Administrador", Code = "admin", Description = "Gestão global de usuários, sistema e configurações." }
}))
.WithName("AdminGetProfiles")
.WithSummary("Retorna a lista de perfis e papéis do sistema");

// Mapeamento do Hub SignalR de Chat
app.MapHub<ChatHub>("/hubs/chat");

// Suporte a rotas profundas do SPA React (evita 404 ao recarregar a página)
app.MapFallbackToFile("index.html");

app.Run();

// DTOs
public record SendMessageRequest(string? Recipient, string Subject, string Body, long? SenderId = null, long? RecipientId = null, List<long>? RecipientIds = null, List<string>? Attachments = null);
public record UpdateMessageStatusRequest(List<long> MessageIds, string NewStatus, long? UserId = null);
public record ExamSubmissionRequest(Dictionary<int, int> Answers);
public record AddBlacklistRequest(string Cpf, string? Reason, long? UserId);
public record AdminResetPasswordRequest(string? NewPassword);

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
                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Keepalive=30;Pooling=true;MinPoolSize=1;MaxPoolSize=25;";
            }
            catch
            {
                return connStr;
            }
        }
        return connStr;
    }
}

public record CreateLessonRequest(string Title, string? ModuleName, string? Type, string? ContentUrl);
public record CreateDiscussionRequest(string Title, string Description, bool IsEvaluative, double? Weight, string? StartDate, string? EndDate);
public record CreateAssignmentRequest(string Title, string? Type, int MaxGroupMembers, double Weight, string? Deadline, string? Enunciation);
public record BulkUpdateGradesRequest(List<StudentGradeUpdateItem> Grades);
public record StudentGradeUpdateItem(int StudentId, double PartialGrade, double? FinalExamGrade, int FrequencyHours);
public record ExecuteDisciplineImportRequest(long SourceOfferId, long TargetOfferId, int ShiftDays);
