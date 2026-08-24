using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Domain.Administration;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Logging;

namespace Solar.WebApi.Endpoints;

public record AddBlacklistRequest(string Cpf, string? Reason, long? UserId);
public record AdminResetPasswordRequest(string? NewPassword);

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
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
        .CacheOutput("StaticCatalogPolicy")
        .WithName("AdminGetProfiles")
        .WithSummary("Retorna a lista de perfis e papéis do sistema");

        // Dashboard de Observabilidade e Logs Estruturados em Tempo Real
        app.MapGet("/api/v1/admin/logs", (int? limit, string? level, string? search) =>
        {
            var logs = SolarLogSink.GetRecentLogs(limit ?? 250, level, search);
            var all = SolarLogSink.GetRecentLogs(1000);
            
            var errorCount = all.Count(l => l.Level.Equals("Error", StringComparison.OrdinalIgnoreCase));
            var warnCount = all.Count(l => l.Level.Equals("Warning", StringComparison.OrdinalIgnoreCase));
            var infoCount = all.Count(l => l.Level.Equals("Information", StringComparison.OrdinalIgnoreCase));
            var elapsedList = all.Where(l => l.ElapsedMs.HasValue).Select(l => l.ElapsedMs!.Value).ToList();
            var avgLatency = elapsedList.Count > 0 ? elapsedList.Average() : 0;
            var maxLatency = elapsedList.Count > 0 ? elapsedList.Max() : 0;

            return Results.Ok(new
            {
                Total = all.Count,
                ErrorCount = errorCount,
                WarningCount = warnCount,
                InformationCount = infoCount,
                AverageLatencyMs = Math.Round(avgLatency, 2),
                MaxLatencyMs = Math.Round(maxLatency, 2),
                Logs = logs
            });
        })
        .WithName("AdminGetLogs")
        .WithSummary("Consulta os logs estruturados em tempo real para o dashboard administrativo");

        app.MapPost("/api/v1/admin/logs/clear", () =>
        {
            SolarLogSink.Clear();
            return Results.Ok(new { Success = true, Message = "Buffer de logs limpo com sucesso." });
        })
        .WithName("AdminClearLogs")
        .WithSummary("Limpa o buffer de logs em memória");

        return app;
    }
}
