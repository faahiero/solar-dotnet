using Microsoft.EntityFrameworkCore;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Logging;

namespace Solar.WebApi.Endpoints;

public static class AdminObservabilityEndpoints
{
    public static IEndpointRouteBuilder MapAdminObservabilityEndpoints(this IEndpointRouteBuilder group)
    {
        // Listagem de Perfis Acadêmicos (Espelha perfis do Solar)
        group.MapGet("/api/v1/admin/profiles", () => Results.Ok(new[]
        {
            new { Id = 1, Name = "Aluno", Code = "student", Description = "Acesso ao ambiente de aprendizagem e realização de atividades." },
            new { Id = 2, Name = "Tutor a Distância", Code = "tutor_distance", Description = "Acompanhamento pedagógico, moderação de fóruns e correções." },
            new { Id = 3, Name = "Tutor Presencial", Code = "tutor_presential", Description = "Atendimento presencial no polo de apoio." },
            new { Id = 4, Name = "Professor", Code = "teacher", Description = "Gestão pedagógica, criação de aulas, provas e lançamento de notas." },
            new { Id = 8, Name = "Coordenador", Code = "coordinator", Description = "Supervisão acadêmica do curso e ofertas." },
            new { Id = 16, Name = "Administrador Geral", Code = "admin", Description = "Acesso total à administração do Solar LMS." }
        }))
        .WithName("AdminGetProfiles")
        .WithSummary("Retorna a lista de perfis e papéis do sistema");

        // Dashboard de Observabilidade e Logs Estruturados em Tempo Real
        group.MapGet("/api/v1/admin/logs", (int? limit, string? level, string? search) =>
        {
            var logs = SolarLogSink.GetRecentLogs(limit ?? 250, level, search);
            var all = SolarLogSink.GetRecentLogs(1000);
            var stats = new
            {
                Total = all.Count,
                Errors = all.Count(l => l.Level.Equals("Error", StringComparison.OrdinalIgnoreCase) || l.Level.Equals("Fatal", StringComparison.OrdinalIgnoreCase)),
                Warnings = all.Count(l => l.Level.Equals("Warning", StringComparison.OrdinalIgnoreCase)),
                Infos = all.Count(l => l.Level.Equals("Information", StringComparison.OrdinalIgnoreCase)),
                Debugs = all.Count(l => l.Level.Equals("Debug", StringComparison.OrdinalIgnoreCase) || l.Level.Equals("Verbose", StringComparison.OrdinalIgnoreCase))
            };

            return Results.Ok(new
            {
                Stats = stats,
                Count = logs.Count,
                Logs = logs
            });
        })
        .WithName("AdminGetLogs")
        .WithSummary("Consulta os logs estruturados em tempo real para o dashboard administrativo");

        group.MapPost("/api/v1/admin/logs/clear", () =>
        {
            SolarLogSink.Clear();
            return Results.Ok(new { Success = true, Message = "Buffer de logs limpo com sucesso." });
        })
        .WithName("AdminClearLogs")
        .WithSummary("Limpa o buffer de logs em memória");

        // Métricas e Telemetria Operacional
        group.MapGet("/api/v1/admin/metrics", async (SolarDbContext db) =>
        {
            var totalUsers = await db.Users.CountAsync();
            var activeUsers = await db.Users.CountAsync(u => u.Active);
            var totalAllocations = await db.Allocations.CountAsync();
            var totalOffers = await db.Offers.CountAsync();
            var totalGroups = await db.Groups.CountAsync();
            var totalCurriculumUnits = await db.CurriculumUnits.CountAsync();

            var memoryMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2);
            var gc0 = GC.CollectionCount(0);
            var gc1 = GC.CollectionCount(1);
            var gc2 = GC.CollectionCount(2);

            return Results.Ok(new
            {
                Users = new { Total = totalUsers, Active = activeUsers },
                Allocations = new { Total = totalAllocations },
                Academic = new { Offers = totalOffers, Groups = totalGroups, CurriculumUnits = totalCurriculumUnits },
                System = new
                {
                    MemoryUsageMB = memoryMb,
                    GarbageCollection = new { Gen0 = gc0, Gen1 = gc1, Gen2 = gc2 },
                    Framework = ".NET 10 (Linux Native)",
                    Timestamp = DateTime.UtcNow
                }
            });
        })
        .WithName("AdminGetMetrics")
        .WithSummary("Retorna telemetria operacional e volumetria do banco de dados");

        return group;
    }
}
