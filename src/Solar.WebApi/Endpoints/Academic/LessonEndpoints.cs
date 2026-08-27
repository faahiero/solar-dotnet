using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CreateLessonRequest(string Title, string? ModuleName, string? Type, string? ContentUrl);

public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder group)
    {
        // Aulas Didáticas (Consulta real no banco de dados)
        group.MapGet("/api/v1/lessons", async (SolarDbContext db) =>
        {
            var lessons = await db.Lessons.AsNoTracking().Take(20).ToListAsync();
            return Results.Ok(lessons.Select(l => new
            {
                l.Id,
                Title = l.Name,
                Type = l.TypeLesson == 1 ? "Link" : "File"
            }));
        })
        .CacheOutput("AcademicPolicy")
        .WithName("GetLessons")
        .WithSummary("Retorna a lista de aulas da turma do banco de dados");

        // Aulas e Módulos Didáticos (Consulta real no banco de dados)
        group.MapGet("/api/v1/curriculum-units/{id}/lessons", async (int id, SolarDbContext db) =>
        {
            var modules = await db.LessonModules
                .AsNoTracking()
                .Include(m => m.Lessons)
                .OrderBy(m => m.Order)
                .ToListAsync();

            if (modules.Any())
            {
                return Results.Ok(modules.Select(m => new
                {
                    ModuleId = m.Id,
                    ModuleName = m.Name,
                    Lessons = m.Lessons.OrderBy(l => l.Order).Select(l => new
                    {
                        Id = l.Id,
                        Title = l.Name,
                        Type = l.TypeLesson == 1 ? "Página Web (UFC)" : "Página Web / PDF",
                        Viewed = false,
                        NotesCount = 0
                    })
                }));
            }

            var lessons = await db.Lessons
                .AsNoTracking()
                .OrderBy(l => l.Order)
                .ToListAsync();

            return Results.Ok(new[]
            {
                new
                {
                    ModuleId = 1L,
                    ModuleName = "modulo 1",
                    Lessons = lessons.Select(l => new
                    {
                        Id = l.Id,
                        Title = l.Name,
                        Type = l.TypeLesson == 1 ? "Página Web (UFC)" : "Página Web / PDF",
                        Viewed = false,
                        NotesCount = 0
                    })
                }
            });
        })
        .WithName("GetCurriculumUnitLessons")
        .WithSummary("Retorna os módulos didáticos e aulas da disciplina do banco de dados");

        // Criação de Nova Aula pelo Professor (Persistência real no banco)
        group.MapPost("/api/v1/curriculum-units/{id}/lessons", async (
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
                Description = req.ModuleName,
                Address = req.ContentUrl ?? "",
                TypeLesson = req.Type?.Contains("Link", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0,
                Order = 10,
                Status = 1
            };

            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/curriculum-units/{id}/lessons/{lesson.Id}", new
            {
                lesson.Id,
                Title = lesson.Name,
                ModuleName = req.ModuleName ?? "modulo 1",
                Type = req.Type ?? "Página Web (UFC)",
                ContentUrl = lesson.Address,
                ReleaseDate = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                Position = lesson.Order
            });
        })
        .WithName("CreateLesson")
        .WithSummary("Cria uma nova aula ou módulo didático na disciplina");

        return group;
    }
}
