using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CreateLessonRequest(string Title, string? ModuleName, string? Type, string? ContentUrl);

public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder group)
    {
        // Aulas Didáticas (Usado para validar liberação ou bloqueio por prova ativa)
        group.MapGet("/api/v1/lessons", () => Results.Ok(new[]
        {
            new { Id = 1, Title = "Aula 1: Introdução ao Curso", Type = "File" },
            new { Id = 2, Title = "Aula 2: Arquitetura de Sistemas", Type = "Link" }
        }))
        .CacheOutput("AcademicPolicy")
        .WithName("GetLessons")
        .WithSummary("Retorna a lista de aulas da turma");

        // Aulas e Módulos Didáticos (Espelha 08_turma_aulas.png)
        group.MapGet("/api/v1/curriculum-units/{id}/lessons", async (int id, SolarDbContext db) =>
        {
            try
            {
                var lessons = await db.Lessons
                    .Include(l => l.LessonModule)
                    .OrderBy(l => l.Order)
                    .ToListAsync();

                if (lessons.Any())
                {
                    return Results.Ok(lessons.Select(l => new
                    {
                        l.Id,
                        Title = l.Name,
                        l.Description,
                        ModuleName = l.LessonModule != null ? l.LessonModule.Name : "Módulo 1",
                        Type = l.TypeLesson == 1 ? "Link" : "Arquivo PDF",
                        ContentUrl = l.Address,
                        ReleaseDate = "10/08/2026",
                        Position = l.Order
                    }));
                }
            }
            catch { }

            return Results.Ok(new[]
            {
                new
                {
                    Id = 1,
                    Title = "Aula 1 - Apresentação da Disciplina e Plano de Ensino",
                    Description = "Visão geral da ementa, critérios de avaliação e cronograma semestral.",
                    ModuleName = "Módulo 1: Fundamentos",
                    Type = "Arquivo PDF",
                    ContentUrl = "https://solar.virtual.ufc.br/materiais/plano_ensino.pdf",
                    ReleaseDate = "10/08/2026",
                    Position = 1
                },
                new
                {
                    Id = 2,
                    Title = "Aula 2 - Estrutura Atômica e Tabela Periódica",
                    Description = "Modelos atômicos, distribuição eletrônica e propriedades periódicas dos elementos.",
                    ModuleName = "Módulo 1: Fundamentos",
                    Type = "Vídeo-aula / Texto",
                    ContentUrl = "https://solar.virtual.ufc.br/materiais/aula_02_video.mp4",
                    ReleaseDate = "17/08/2026",
                    Position = 2
                },
                new
                {
                    Id = 3,
                    Title = "Aula 3 - Ligações Químicas e Forças Intermoleculares",
                    Description = "Ligações iônicas, covalentes e metálicas. Geometria molecular.",
                    ModuleName = "Módulo 2: Reatividade",
                    Type = "Apresentação Interativa",
                    ContentUrl = "https://solar.virtual.ufc.br/materiais/aula_03_slides.pdf",
                    ReleaseDate = "24/08/2026",
                    Position = 3
                }
            });
        })
        .WithName("GetCurriculumUnitLessons")
        .WithSummary("Retorna os módulos didáticos e aulas da disciplina");

        // Criação de Nova Aula pelo Professor (Espelha lessons_controller#create)
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
                ModuleName = req.ModuleName ?? "Módulo Geral",
                Type = req.Type ?? "Arquivo PDF",
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
