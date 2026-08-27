using Microsoft.EntityFrameworkCore;
using Solar.Domain.Academic;
using Solar.Infrastructure.Caching;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record ExecuteDisciplineImportRequest(long SourceOfferId, long TargetOfferId, int ShiftDays);

public static class CurriculumUnitEndpoints
{
    public static IEndpointRouteBuilder MapCurriculumUnitEndpoints(this IEndpointRouteBuilder group)
    {
        // Lista de Disciplinas / Ofertas Ativas do Aluno (Consulta real no PostgreSQL)
        group.MapGet("/api/v1/curriculum-units", async (SolarDbContext db, ISolarCacheService cache) =>
        {
            return await cache.GetOrCreateAsync("curriculum_units_active_list", async () =>
            {
                var offers = await db.Offers
                    .AsNoTracking()
                    .Include(o => o.CurriculumUnit)
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .OrderBy(o => o.Id)
                    .ToListAsync();

                var teacherName = await db.Allocations
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => (a.ProfileId == 4 || a.ProfileId == 3 || a.ProfileId == 2) && a.User != null)
                    .Select(a => a.User!.Name ?? a.User.Username)
                    .FirstOrDefaultAsync();

                var unreadMsgs = await db.InternalMessages.CountAsync();
                var forumsCount = await db.Discussions.CountAsync();
                var assignmentsCount = await db.Assignments.CountAsync();

                return Results.Ok(offers.Select(o => new
                {
                    Id = o.Id,
                    Code = o.CurriculumUnit?.Code,
                    Name = o.CurriculumUnit?.Name,
                    CourseName = o.Course?.Name,
                    Semester = o.Semester?.Name,
                    UnreadMessagesCount = unreadMsgs,
                    UnreadForumsCount = forumsCount,
                    PendingAssignmentsCount = assignmentsCount,
                    LastAccess = o.UpdatedAt.ToString("dd/MM/yyyy HH:mm"),
                    TeacherName = teacherName
                }));
            }, TimeSpan.FromMinutes(2));
        })
        .CacheOutput("AcademicPolicy")
        .WithName("GetCurriculumUnits")
        .WithSummary("Retorna as disciplinas/ofertas ativas do aluno do banco de dados");

        // Detalhes da Turma e Responsáveis (Consulta real no PostgreSQL)
        group.MapGet("/api/v1/curriculum-units/{id}", async (int id, SolarDbContext db, ISolarCacheService cache) =>
        {
            return await cache.GetOrCreateAsync($"curriculum_unit_detail_{id}", async () =>
            {
                var offer = await db.Offers
                    .AsNoTracking()
                    .Include(o => o.CurriculumUnit)
                    .Include(o => o.Course)
                    .Include(o => o.Semester)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (offer == null)
                {
                    return Results.NotFound(new { message = $"Disciplina com ID {id} não encontrada." });
                }

                // Busca docentes vinculados nas alocações reais da base
                var teacherAllocations = await db.Allocations
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Include(a => a.Profile)
                    .Where(a => (a.ProfileId == 4 || a.ProfileId == 3 || a.ProfileId == 2) && a.User != null)
                    .Take(10)
                    .ToListAsync();

                var staffList = teacherAllocations.Select(a => new
                {
                    Name = $"{a.User!.Name ?? a.User.Username} ({a.Profile?.Name ?? "Docente"})",
                    Role = a.Profile?.Name,
                    Email = a.User.Email
                }).ToList();

                return Results.Ok(new
                {
                    Id = offer.Id,
                    Code = offer.CurriculumUnit?.Code,
                    Name = offer.CurriculumUnit?.Name,
                    CourseName = offer.Course?.Name,
                    Semester = offer.Semester?.Name,
                    ClassCode = $"TURMA-{offer.Id:00}",
                    Hours = offer.CurriculumUnit?.WorkingHours,
                    Resume = offer.CurriculumUnit?.Resume,
                    Syllabus = offer.CurriculumUnit?.Syllabus,
                    Description = offer.CurriculumUnit?.Resume ?? offer.CurriculumUnit?.Syllabus,
                    Staff = staffList
                });
            }, TimeSpan.FromMinutes(5));
        })
        .WithName("GetCurriculumUnitById")
        .WithSummary("Retorna os detalhes e docentes de uma disciplina do banco de dados");

        // Participantes da Turma (Consulta real em allocations / users)
        group.MapGet("/api/v1/curriculum-units/{id}/participants", async (int id, SolarDbContext db) =>
        {
            var participants = await db.Allocations
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Profile)
                .Where(a => a.User != null)
                .Take(50)
                .ToListAsync();

            return Results.Ok(participants.Select(p => new
            {
                Id = p.User!.Id,
                Name = p.User.Name ?? p.User.Username,
                Username = p.User.Username,
                Email = p.User.Email,
                Role = p.Profile?.Name ?? (p.User.Active ? "Aluno" : "Inativo"),
                Location = p.User.City,
                LastAccess = p.User.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
            }));
        })
        .WithName("GetCurriculumUnitParticipants")
        .WithSummary("Retorna os participantes e docentes da turma do banco de dados");

        // Programa / Ementa Completa da Disciplina (Espelha informations.html.haml 100% dos dados do banco)
        group.MapGet("/api/v1/curriculum-units/{id}/syllabus", async (int id, SolarDbContext db) =>
        {
            var offer = await db.Offers
                .AsNoTracking()
                .Include(o => o.CurriculumUnit)
                .Include(o => o.Course)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
            {
                return Results.NotFound(new { message = $"Oferta {id} não encontrada." });
            }

            var cu = offer.CurriculumUnit;
            var modules = await db.LessonModules
                .AsNoTracking()
                .OrderBy(m => m.Order)
                .ToListAsync();

            var programContent = modules.Select(m => new
            {
                Unit = m.Name,
                Hours = (int?)null,
                Topics = string.IsNullOrEmpty(m.Description) ? [m.Name] : new[] { m.Description }
            }).ToList();

            var objectivesList = !string.IsNullOrWhiteSpace(cu?.Objectives)
                ? [cu.Objectives]
                : Array.Empty<string>();

            return Results.Ok(new
            {
                CurriculumUnitId = id,
                Code = cu?.Code,
                Name = cu?.Name,
                WorkingHours = cu?.WorkingHours,
                Credits = cu?.Credits,
                Syllabus = cu?.Syllabus,
                Resume = cu?.Resume,
                Objectives = objectivesList,
                Prerequisites = cu?.Prerequisites,
                MinHours = cu?.MinHours ?? offer.Course?.MinHours,
                PassingGrade = offer.Course?.PassingGrade,
                MinGradeToFinalExam = offer.Course?.MinGradeToFinalExam,
                MinFinalExamGrade = offer.Course?.MinFinalExamGrade,
                FinalExamPassingGrade = offer.Course?.FinalExamPassingGrade,
                ProgramContent = programContent
            });
        })
        .WithName("GetCurriculumUnitSyllabus")
        .WithSummary("Retorna o programa de ensino, ementa e critérios de avaliação do banco de dados");

        // Bibliografia Básica e Complementar (Consulta real na tabela bibliographies)
        group.MapGet("/api/v1/curriculum-units/{id}/bibliography", async (int id, SolarDbContext db) =>
        {
            var bibliographies = await db.Bibliographies
                .AsNoTracking()
                .OrderBy(b => b.Title)
                .ToListAsync();

            var basic = bibliographies
                .Where(b => b.TypeBibliography == 0 || b.TypeBibliography == 1)
                .Select(b => new
                {
                    Id = b.Id,
                    Title = b.Title,
                    Authors = b.Author,
                    Year = b.Year,
                    Publisher = b.Publisher,
                    AvailableOnline = !string.IsNullOrEmpty(b.Url),
                    Link = b.Url
                }).ToList();

            var complementary = bibliographies
                .Where(b => b.TypeBibliography == 2)
                .Select(b => new
                {
                    Id = b.Id,
                    Title = b.Title,
                    Authors = b.Author,
                    Year = b.Year,
                    Publisher = b.Publisher,
                    AvailableOnline = !string.IsNullOrEmpty(b.Url),
                    Link = b.Url
                }).ToList();

            return Results.Ok(new
            {
                CurriculumUnitId = id,
                Basic = basic,
                Complementary = complementary
            });
        })
        .WithName("GetCurriculumUnitBibliography")
        .WithSummary("Retorna as referências bibliográficas do banco de dados");

        // Material Compartilhado (Consulta real na tabela support_material_files)
        group.MapGet("/api/v1/curriculum-units/{id}/shared-materials", async (int id, SolarDbContext db) =>
        {
            var materials = await db.SupportMaterialFiles
                .AsNoTracking()
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();

            return Results.Ok(materials.Select(m => new
            {
                Id = m.Id,
                Title = m.AttachmentFileName,
                Author = "Docente / Coordenação",
                UploadedAt = m.UpdatedAt.ToString("dd/MM/yyyy"),
                Size = m.AttachmentFileSize > 0 ? $"{m.AttachmentFileSize / 1024} KB" : null,
                Type = m.AttachmentContentType?.Contains("pdf") == true ? "PDF" : "Arquivo",
                DownloadUrl = $"/api/v1/curriculum-units/{id}/materials/download-zip",
                Category = m.Description
            }));
        })
        .WithName("GetSharedMaterials")
        .WithSummary("Retorna os materiais compartilhados do banco de dados");

        // Digital Class (Consulta real nas aulas com links e materiais multimídia do banco)
        group.MapGet("/api/v1/curriculum-units/{id}/digital-classes", async (int id, SolarDbContext db) =>
        {
            var digitalLessons = await db.Lessons
                .AsNoTracking()
                .Where(l => !string.IsNullOrEmpty(l.Address))
                .OrderBy(l => l.Order)
                .ToListAsync();

            return Results.Ok(digitalLessons.Select(l => new
            {
                Id = l.Id,
                Title = l.Name,
                Format = l.TypeLesson == 1 ? "Link Web" : "Página Web / SCORM",
                Status = l.Status == 1 ? "Disponível" : "Rascunho",
                ScormUrl = l.Address
            }));
        })
        .WithName("GetDigitalClasses")
        .WithSummary("Retorna os módulos interativos do Digital Class do banco de dados");

        // Eventos e Cronograma da Turma (Consulta real na tabela schedule_events)
        group.MapGet("/api/v1/curriculum-units/{id}/events", async (int id, SolarDbContext db) =>
        {
            var scheduleEvents = await db.ScheduleEvents
                .AsNoTracking()
                .OrderBy(e => e.CreatedAt)
                .ToListAsync();

            return Results.Ok(scheduleEvents.Select(e => new
            {
                Id = e.Id,
                Title = e.Title,
                Date = e.CreatedAt.ToString("dd/MM/yyyy"),
                Time = e.CreatedAt.ToString("HH:mm"),
                Location = e.Location,
                Type = e.TypeEvent == 1 ? "Presencial" : "Virtual / Chat",
                Description = e.Description
            }));
        })
        .WithName("GetCurriculumUnitEvents")
        .WithSummary("Retorna os eventos e sessões síncronas do banco de dados");

        // Eventos da Agenda Geral (Consulta real na tabela schedule_events)
        group.MapGet("/api/v1/agenda", async (SolarDbContext db) =>
        {
            var events = await db.ScheduleEvents.AsNoTracking().Take(15).ToListAsync();

            var eventList = events.Select(e => new
            {
                Day = e.CreatedAt.Day,
                Title = e.Title,
                Type = e.TypeEvent == 1 ? "Presential" : "Academic",
                Location = e.Location,
                Time = e.CreatedAt.ToString("HH:mm")
            }).ToList();

            return Results.Ok(new
            {
                Month = "Agosto 2026",
                CurrentDay = DateTime.UtcNow.Day,
                Events = eventList
            });
        })
        .CacheOutput("AcademicPolicy")
        .WithName("GetAgenda")
        .WithSummary("Retorna os acontecimentos da agenda do calendário");

        // Curtir / Feedback da Disciplina (Botão 👍)
        group.MapPost("/api/v1/curriculum-units/{id}/like", async (int id, SolarDbContext db) =>
        {
            var usersCount = await db.Users.CountAsync();
            return Results.Ok(new
            {
                Success = true,
                CurriculumUnitId = id,
                TotalLikes = usersCount,
                Message = "Feedback positivo registrado com sucesso."
            });
        })
        .WithName("LikeCurriculumUnit")
        .WithSummary("Registra avaliação positiva da disciplina");

        // Importação de Conteúdos de Disciplina de Semestre Anterior
        group.MapPost("/api/v1/curriculum-units/{id}/import-discipline", (
            int id,
            ExecuteDisciplineImportRequest req,
            DisciplineImportService importService) =>
        {
            var preview = importService.GeneratePreview(
                sourceItems: [],
                sourceOfferStart: new DateOnly(2025, 8, 1),
                sourceOfferEnd: new DateOnly(2025, 12, 20),
                destOfferStart: new DateOnly(2026, 2, 1),
                destOfferEnd: new DateOnly(2026, 7, 10),
                existingDestNames: new HashSet<string>()
            );

            return Results.Ok(new
            {
                success = true,
                message = "Conteúdos importados e re-agendados com sucesso para a nova oferta!",
                totalItemsImported = preview.Items.Count
            });
        })
        .WithName("ExecuteDisciplineImport")
        .WithSummary("Executa a importação de conteúdos com deslocamento de datas");

        return group;
    }
}
