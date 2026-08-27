using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CreateDiscussionRequest(string Title, string Description, bool IsEvaluative, double? Weight, string? StartDate, string? EndDate);

public static class DiscussionEndpoints
{
    public static IEndpointRouteBuilder MapDiscussionEndpoints(this IEndpointRouteBuilder group)
    {
        // Fóruns de Discussão da Disciplina (Consulta real na tabela discussions e posts)
        group.MapGet("/api/v1/curriculum-units/{id}/discussions", async (int id, SolarDbContext db) =>
        {
            var discussions = await db.Discussions
                .AsNoTracking()
                .Include(d => d.Posts)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var discIds = discussions.Select(d => d.Id).ToList();
            var academicAllocations = await db.AcademicAllocations
                .AsNoTracking()
                .Where(aa => aa.AcademicToolType == "Discussion" && discIds.Contains(aa.AcademicToolId))
                .ToListAsync();

            var allocMap = academicAllocations
                .GroupBy(aa => aa.AcademicToolId)
                .ToDictionary(g => g.Key, g => g.First());

            return Results.Ok(discussions.Select(d =>
            {
                allocMap.TryGetValue(d.Id, out var aa);
                return new
                {
                    d.Id,
                    Title = d.Name,
                    d.Description,
                    IsEvaluative = aa?.Evaluative ?? false,
                    IsFrequency = aa?.Frequency ?? false,
                    Weight = (double)(aa?.Weight ?? 1),
                    FinalWeight = (double)(aa?.FinalWeight ?? 100),
                    CreatedAt = d.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = d.UpdatedAt.ToString("dd/MM/yyyy HH:mm"),
                    PostCount = d.Posts?.Count ?? 0,
                    ParticipantCount = d.Posts?.Select(p => p.UserId).Distinct().Count() ?? 0
                };
            }));
        })
        .WithName("GetCurriculumUnitDiscussions")
        .WithSummary("Retorna os tópicos do fórum de discussão do banco de dados");

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
                CreatedAt = discussion.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        })
        .WithName("CreateDiscussion")
        .WithSummary("Cria um novo tópico de discussão no fórum");

        return group;
    }
}
