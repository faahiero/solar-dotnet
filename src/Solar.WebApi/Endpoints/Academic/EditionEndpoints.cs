using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record UpdateToolManagementItemRequest(long Id, double Weight, double FinalWeight, bool Evaluative, bool Frequency);
public record UpdateToolManagementRequest(List<UpdateToolManagementItemRequest> Tools);

public static class EditionEndpoints
{
    public static IEndpointRouteBuilder MapEditionEndpoints(this IEndpointRouteBuilder group)
    {
        // Retorna as ferramentas de edição disponíveis para a turma/oferta (Espelha editions_controller#items)
        group.MapGet("/api/v1/curriculum-units/{id}/edition/items", async (int id, SolarDbContext db) =>
        {
            var offer = await db.Offers
                .AsNoTracking()
                .Include(o => o.CurriculumUnit)
                .Include(o => o.Course)
                .Include(o => o.Semester)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
            {
                return Results.NotFound(new { message = $"Oferta {id} não encontrada." });
            }

            var groupEntity = await db.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.OfferId == id);

            return Results.Ok(new
            {
                CurriculumUnitId = id,
                CurriculumUnitName = offer.CurriculumUnit?.Name,
                CourseName = offer.Course?.Name,
                SemesterName = offer.Semester?.Name,
                ClassCode = groupEntity?.Code ?? $"TURMA-{id:00}",
                TypeName = "Curso de Graduacao a Distancia",
                Sections = new[]
                {
                    new
                    {
                        Category = "Comunicação",
                        Tools = new[]
                        {
                            new { Key = "webconference", Name = "Webconferência", Icon = "video", Url = $"/api/v1/curriculum-units/{id}/chat" },
                            new { Key = "discussion", Name = "Fórum", Icon = "users", Url = $"/api/v1/curriculum-units/{id}/discussions" }
                        }
                    },
                    new
                    {
                        Category = "Educação",
                        Tools = new[]
                        {
                            new { Key = "digital_class", Name = "Digital Class", Icon = "class", Url = $"/api/v1/curriculum-units/{id}/digital-classes" },
                            new { Key = "exam", Name = "Prova Online", Icon = "exam", Url = $"/api/v1/curriculum-units/{id}/exams" },
                            new { Key = "lesson", Name = "Aulas", Icon = "book", Url = $"/api/v1/curriculum-units/{id}/lessons" },
                            new { Key = "support_material", Name = "Material de Apoio", Icon = "archive", Url = $"/api/v1/curriculum-units/{id}/shared-materials" },
                            new { Key = "assignment", Name = "Trabalhos", Icon = "homework", Url = $"/api/v1/curriculum-units/{id}/assignments" },
                            new { Key = "schedule_event", Name = "Eventos", Icon = "calendar", Url = $"/api/v1/curriculum-units/{id}/events" }
                        }
                    },
                    new
                    {
                        Category = "Informações Gerais",
                        Tools = new[]
                        {
                            new { Key = "bibliography", Name = "Bibliografia", Icon = "library", Url = $"/api/v1/curriculum-units/{id}/bibliography" },
                            new { Key = "schedule", Name = "Agenda", Icon = "calendar", Url = "/api/v1/agenda" },
                            new { Key = "allocation", Name = "Participantes / Alocações", Icon = "user-add", Url = $"/api/v1/curriculum-units/{id}/participants" }
                        }
                    }
                }
            });
        })
        .WithName("GetEditionItems")
        .WithSummary("Retorna a árvore de ferramentas de edição da disciplina");

        // Retorna a Gerência de Atividades Avaliativas / Frequência (Espelha editions_controller#tool_management)
        group.MapGet("/api/v1/curriculum-units/{id}/edition/tool-management", async (int id, SolarDbContext db) =>
        {
            var academicAllocations = await db.AcademicAllocations
                .AsNoTracking()
                .OrderBy(aa => aa.AcademicToolType)
                .ThenBy(aa => aa.Id)
                .ToListAsync();

            var assignments = await db.Assignments.ToDictionaryAsync(a => a.Id, a => a.Name);
            var discussions = await db.Discussions.ToDictionaryAsync(d => d.Id, d => d.Name);
            var exams = await db.Exams.ToDictionaryAsync(e => e.Id, e => e.Name);
            var events = await db.ScheduleEvents.ToDictionaryAsync(e => e.Id, e => e.Title);

            var list = academicAllocations.Select(aa =>
            {
                string toolName = aa.AcademicToolType switch
                {
                    "Assignment" => assignments.TryGetValue(aa.AcademicToolId, out var n) ? n : $"Trabalho #{aa.AcademicToolId}",
                    "Discussion" => discussions.TryGetValue(aa.AcademicToolId, out var n) ? n : $"Fórum #{aa.AcademicToolId}",
                    "Exam" => exams.TryGetValue(aa.AcademicToolId, out var n) ? n : $"Prova #{aa.AcademicToolId}",
                    "ScheduleEvent" => events.TryGetValue(aa.AcademicToolId, out var n) ? n : $"Evento #{aa.AcademicToolId}",
                    _ => $"{aa.AcademicToolType} #{aa.AcademicToolId}"
                };

                return new
                {
                    aa.Id,
                    ToolType = aa.AcademicToolType,
                    ToolId = aa.AcademicToolId,
                    Name = toolName,
                    Weight = (double)aa.Weight,
                    FinalWeight = (double)aa.FinalWeight,
                    Evaluative = aa.Evaluative,
                    Frequency = aa.Frequency
                };
            }).ToList();

            return Results.Ok(new
            {
                CurriculumUnitId = id,
                TotalTools = list.Count,
                Tools = list
            });
        })
        .WithName("GetToolManagement")
        .WithSummary("Retorna a matriz de gerência de atividades avaliativas e de frequência");

        // Atualização em lote de pesos e status de atividades avaliativas
        group.MapPut("/api/v1/curriculum-units/{id}/edition/tool-management", async (
            int id,
            UpdateToolManagementRequest req,
            SolarDbContext db) =>
        {
            if (req.Tools == null || !req.Tools.Any())
            {
                return Results.BadRequest(new { error = "Nenhuma ferramenta informada para atualização." });
            }

            var ids = req.Tools.Select(t => t.Id).ToList();
            var entities = await db.AcademicAllocations.Where(aa => ids.Contains(aa.Id)).ToListAsync();

            foreach (var entity in entities)
            {
                var update = req.Tools.FirstOrDefault(t => t.Id == entity.Id);
                if (update != null)
                {
                    entity.Weight = (decimal)update.Weight;
                    entity.FinalWeight = (decimal)update.FinalWeight;
                    entity.Evaluative = update.Evaluative;
                    entity.Frequency = update.Frequency;
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                Success = true,
                Message = $"Configurações de {entities.Count} ferramenta(s) avaliativa(s) atualizadas com sucesso!"
            });
        })
        .WithName("UpdateToolManagement")
        .WithSummary("Atualiza pesos, notas e frequências das ferramentas da turma");

        return group;
    }
}
