using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Domain.Administration;
using Solar.Domain.Entities;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record AddBlacklistRequest(string Cpf, string? Reason, long? UserId);
public record AdminResetPasswordRequest(string? NewPassword);

public static class AdminUserEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder group)
    {
        // Importação em Lote de Usuários (Substitui Roo Gem do Ruby)
        group.MapPost("/api/v1/admin/import/users-batch", async (
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
        group.MapGet("/api/v1/admin/users", async (
            string? query,
            int? page,
            int? pageSize,
            string? role,
            bool? activeOnly,
            SolarDbContext db) =>
        {
            int currentPage = Math.Max(1, page ?? 1);
            int size = Math.Clamp(pageSize ?? 20, 1, 100);

            var usersQuery = db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.Name.ToLower().Contains(q) ||
                    u.Username.ToLower().Contains(q) ||
                    (u.Email != null && u.Email.ToLower().Contains(q)) ||
                    (u.Cpf != null && u.Cpf.Contains(q))
                );
            }

            if (activeOnly.HasValue && activeOnly.Value)
            {
                usersQuery = usersQuery.Where(u => u.Active);
            }

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderBy(u => u.Name)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Name,
                    u.Email,
                    u.Cpf,
                    u.Active,
                    u.Integrated,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                TotalCount = totalCount,
                Page = currentPage,
                PageSize = size,
                TotalPages = (int)Math.Ceiling((double)totalCount / size),
                Users = users
            });
        })
        .WithName("AdminGetUsers")
        .WithSummary("Lista e pesquisa usuários para a gestão administrativa");

        // Listagem de CPFs na Blacklist
        group.MapGet("/api/v1/admin/blacklist", async (SolarDbContext db) =>
        {
            var list = await db.UserBlacklists
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.Id,
                    b.Cpf,
                    b.Reason,
                    b.UserId,
                    b.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(list);
        })
        .WithName("AdminGetBlacklist")
        .WithSummary("Retorna a lista de CPFs bloqueados na blacklist");

        // Adicionar CPF à Blacklist
        group.MapPost("/api/v1/admin/blacklist", async (
            AddBlacklistRequest req,
            SolarDbContext db,
            BlacklistService blacklistService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Cpf))
            {
                return Results.BadRequest(new { error = "CPF é obrigatório." });
            }

            var entry = await blacklistService.AddToBlacklistAsync(req.Cpf, req.Reason ?? "", req.UserId, db);
            if (entry == null)
            {
                return Results.BadRequest(new { error = "CPF inválido ou já existente na blacklist." });
            }

            return Results.Ok(new { Success = true, Message = "CPF adicionado à blacklist com sucesso." });
        })
        .WithName("AdminAddToBlacklist")
        .WithSummary("Adiciona um CPF à blacklist do Solar LMS");

        // Remover CPF da Blacklist
        group.MapDelete("/api/v1/admin/blacklist/{cpf}", async (
            string cpf,
            SolarDbContext db,
            BlacklistService blacklistService) =>
        {
            var removed = await blacklistService.RemoveFromBlacklistAsync(cpf, db);
            if (!removed)
            {
                return Results.NotFound(new { error = "CPF não encontrado na blacklist." });
            }

            return Results.Ok(new { Success = true, Message = "CPF removido da blacklist com sucesso." });
        })
        .WithName("AdminRemoveFromBlacklist")
        .WithSummary("Remove um CPF da blacklist do Solar LMS");

        // Redefinição Administrativa de Senha
        group.MapPost("/api/v1/admin/users/{id}/reset-password", async (
            int id,
            AdminResetPasswordRequest req,
            SolarDbContext db) =>
        {
            var user = await db.Users.FindAsync((long)id);
            if (user == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            var newPass = string.IsNullOrWhiteSpace(req?.NewPassword) ? "solar123" : req.NewPassword;
            user.EncryptedPassword = DeviseLegacyPasswordHasher<User>.ComputeSha1(newPass);
            user.PasswordSalt = null;
            user.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                Success = true,
                Message = $"Senha do usuário '{user.Username}' redefinida com sucesso.",
                TemporaryPassword = newPass
            });
        })
        .WithName("AdminResetUserPassword")
        .WithSummary("Redefine administrativamente a senha de um usuário");

        return group;
    }
}
