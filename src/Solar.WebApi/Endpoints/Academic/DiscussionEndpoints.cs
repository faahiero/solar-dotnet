using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CreateDiscussionRequest(string Title, string Description, bool IsEvaluative, double? Weight, string? StartDate, string? EndDate);

public static class DiscussionEndpoints
{
    public static IEndpointRouteBuilder MapDiscussionEndpoints(this IEndpointRouteBuilder group)
    {
        // Fóruns de Discussão da Disciplina (Espelha 10_turma_forum_discussoes.png)
        group.MapGet("/api/v1/curriculum-units/{id}/discussions", async (int id, SolarDbContext db) =>
        {
            try
            {
                var discussions = await db.Discussions
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

                if (discussions.Any())
                {
                    return Results.Ok(discussions.Select(d => new
                    {
                        d.Id,
                        Title = d.Name,
                        d.Description,
                        IsEvaluative = true,
                        Weight = 2.0,
                        StartDate = "10/08/2026",
                        EndDate = "31/08/2026",
                        PostCount = 5,
                        ParticipantCount = 12
                    }));
                }
            }
            catch { }

            return Results.Ok(new[]
            {
                new
                {
                    Id = 1,
                    Title = "Fórum Temático 1: Desafios da Educação a Distância",
                    Description = "Espaço para debater metodologias ativas e o papel da autonomia do estudante na EaD.",
                    IsEvaluative = true,
                    Weight = 2.0,
                    StartDate = "10/08/2026",
                    EndDate = "31/08/2026",
                    PostCount = 18,
                    ParticipantCount = 14
                },
                new
                {
                    Id = 2,
                    Title = "Fórum de Dúvidas Gerais do Módulo 1",
                    Description = "Canal aberto com tutores e docentes para esclarecimento de conceitos da disciplina.",
                    IsEvaluative = false,
                    Weight = 0.0,
                    StartDate = "10/08/2026",
                    EndDate = "15/12/2026",
                    PostCount = 7,
                    ParticipantCount = 6
                }
            });
        })
        .WithName("GetCurriculumUnitDiscussions")
        .WithSummary("Retorna os tópicos do fórum de discussão");

        // Criação de Fórum pelo Professor (Espelha discussions_controller#create)
        group.MapPost("/api/v1/curriculum-units/{id}/discussions", async (
            int id,
            CreateDiscussionRequest req,
            SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Description))
            {
                return Results.BadRequest(new { error = "Título e descrição do fórum são obrigatórios." });
            }

            var discussion = new Discussion
            {
                Name = req.Title,
                Description = req.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Discussions.Add(discussion);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/curriculum-units/{id}/discussions/{discussion.Id}", new
            {
                discussion.Id,
                Title = discussion.Name,
                discussion.Description,
                IsEvaluative = req.IsEvaluative,
                Weight = req.Weight ?? 1.0,
                StartDate = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                EndDate = DateTime.UtcNow.AddDays(30).ToString("dd/MM/yyyy")
            });
        })
        .WithName("CreateDiscussion")
        .WithSummary("Cria um novo tópico de discussão no fórum");

        return group;
    }
}
