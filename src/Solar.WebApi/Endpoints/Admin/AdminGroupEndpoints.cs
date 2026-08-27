using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Filters;

namespace Solar.WebApi.Endpoints;

public record CreateGroupRequest(long OfferId, string? Code, string? Name, string? Location, bool? Status, bool? Integrated, long? MainGroupId, int? DigitalClassDirectoryId);
public record UpdateGroupRequest(long? OfferId, string? Code, string? Name, string? Location, bool? Status, bool? Integrated, long? MainGroupId, int? DigitalClassDirectoryId);

public record CreateAllocationRequest(long UserId, int ProfileId, long? AllocationTagId, AllocationStatus? Status);
public record UpdateAllocationRequest(long? AllocationTagId, int? ProfileId, AllocationStatus? Status, double? ParcialGrade, double? FinalExamGrade, double? FinalGrade, decimal? WorkingHours, GradeSituation? GradeSituation, long? UpdatedByUserId, int? OriginGroupId);

public static class AdminGroupEndpoints
{
    public static IEndpointRouteBuilder MapAdminGroupEndpoints(this IEndpointRouteBuilder group)
    {
        // ----------------------------------------------------
        // CRUD de Turmas / Grupos (Groups - Espelha groups_controller)
        // ----------------------------------------------------
        group.MapGet("/api/v1/groups", async (
            long? offerId,
            string? search,
            int? page,
            int? pageSize,
            SolarDbContext db) =>
        {
            int currentPage = Math.Max(1, page ?? 1);
            int size = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Groups
                .Include(g => g.Offer)
                .AsQueryable();

            if (offerId.HasValue)
            {
                query = query.Where(g => g.OfferId == offerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(g =>
                    (g.Name != null && g.Name.ToLower().Contains(s)) ||
                    (g.Code != null && g.Code.ToLower().Contains(s)) ||
                    (g.Location != null && g.Location.ToLower().Contains(s))
                );
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(g => g.Name)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(g => new
                {
                    g.Id,
                    g.OfferId,
                    g.Code,
                    g.Name,
                    g.Location,
                    g.Status,
                    g.Integrated,
                    g.MainGroupId,
                    g.DigitalClassDirectoryId,
                    g.CreatedAt,
                    g.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                TotalCount = totalCount,
                Page = currentPage,
                PageSize = size,
                TotalPages = (int)Math.Ceiling((double)totalCount / size),
                Groups = items
            });
        })
        .WithName("GetGroups")
        .WithSummary("Lista as turmas/grupos cadastrados");

        group.MapGet("/api/v1/groups/{id}", async (long id, SolarDbContext db) =>
        {
            var groupEntity = await db.Groups
                .Include(g => g.Offer)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (groupEntity == null)
            {
                return Results.NotFound(new { error = "Turma/Grupo não encontrado." });
            }

            return Results.Ok(new
            {
                groupEntity.Id,
                groupEntity.OfferId,
                groupEntity.Code,
                groupEntity.Name,
                groupEntity.Location,
                groupEntity.Status,
                groupEntity.Integrated,
                groupEntity.MainGroupId,
                groupEntity.DigitalClassDirectoryId,
                groupEntity.CreatedAt,
                groupEntity.UpdatedAt
            });
        })
        .WithName("GetGroupById")
        .WithSummary("Retorna os detalhes de uma turma/grupo");

        group.MapPost("/api/v1/groups", async (CreateGroupRequest req, SolarDbContext db) =>
        {
            var offerExists = await db.Offers.AnyAsync(o => o.Id == req.OfferId);
            if (!offerExists)
            {
                return Results.BadRequest(new { error = "A oferta acadêmica especificada não existe." });
            }

            var newGroup = new Group
            {
                OfferId = req.OfferId,
                Code = req.Code,
                Name = req.Name,
                Location = req.Location,
                Status = req.Status ?? true,
                Integrated = req.Integrated ?? false,
                MainGroupId = req.MainGroupId,
                DigitalClassDirectoryId = req.DigitalClassDirectoryId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Groups.Add(newGroup);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/groups/{newGroup.Id}", new
            {
                newGroup.Id,
                newGroup.OfferId,
                newGroup.Code,
                newGroup.Name,
                newGroup.Location,
                newGroup.Status,
                newGroup.Integrated,
                newGroup.CreatedAt
            });
        })
        .AddEndpointFilter<ValidationFilter<CreateGroupRequest>>()
        .WithName("CreateGroup")
        .WithSummary("Cria uma nova turma/grupo");

        group.MapPut("/api/v1/groups/{id}", async (long id, UpdateGroupRequest req, SolarDbContext db) =>
        {
            var groupEntity = await db.Groups.FindAsync(id);
            if (groupEntity == null)
            {
                return Results.NotFound(new { error = "Turma/Grupo não encontrado." });
            }

            if (req.OfferId.HasValue)
            {
                var offerExists = await db.Offers.AnyAsync(o => o.Id == req.OfferId.Value);
                if (!offerExists) return Results.BadRequest(new { error = "A oferta especificada não existe." });
                groupEntity.OfferId = req.OfferId.Value;
            }

            if (req.Code != null) groupEntity.Code = req.Code;
            if (req.Name != null) groupEntity.Name = req.Name;
            if (req.Location != null) groupEntity.Location = req.Location;
            if (req.Status.HasValue) groupEntity.Status = req.Status.Value;
            if (req.Integrated.HasValue) groupEntity.Integrated = req.Integrated.Value;
            if (req.MainGroupId.HasValue) groupEntity.MainGroupId = req.MainGroupId.Value;
            if (req.DigitalClassDirectoryId.HasValue) groupEntity.DigitalClassDirectoryId = req.DigitalClassDirectoryId.Value;
            groupEntity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                groupEntity.Id,
                groupEntity.OfferId,
                groupEntity.Code,
                groupEntity.Name,
                groupEntity.Location,
                groupEntity.Status,
                groupEntity.Integrated,
                groupEntity.UpdatedAt
            });
        })
        .WithName("UpdateGroup")
        .WithSummary("Atualiza os dados de uma turma/grupo");

        group.MapDelete("/api/v1/groups/{id}", async (long id, SolarDbContext db) =>
        {
            var groupEntity = await db.Groups.FindAsync(id);
            if (groupEntity == null)
            {
                return Results.NotFound(new { error = "Turma/Grupo não encontrado." });
            }

            db.Groups.Remove(groupEntity);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Turma/Grupo removido com sucesso." });
        })
        .WithName("DeleteGroup")
        .WithSummary("Exclui uma turma/grupo");

        // ----------------------------------------------------
        // CRUD de Alocações (Allocations - Espelha allocations_controller)
        // ----------------------------------------------------
        group.MapGet("/api/v1/allocations", async (
            long? userId,
            long? allocationTagId,
            int? profileId,
            AllocationStatus? status,
            int? page,
            int? pageSize,
            SolarDbContext db) =>
        {
            int currentPage = Math.Max(1, page ?? 1);
            int size = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Allocations
                .Include(a => a.User)
                .Include(a => a.Profile)
                .Include(a => a.AllocationTag)
                .AsQueryable();

            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (allocationTagId.HasValue) query = query.Where(a => a.AllocationTagId == allocationTagId.Value);
            if (profileId.HasValue) query = query.Where(a => a.ProfileId == profileId.Value);
            if (status.HasValue) query = query.Where(a => a.Status == status.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    UserName = a.User != null ? a.User.Name : null,
                    UserCpf = a.User != null ? a.User.Cpf : null,
                    a.ProfileId,
                    ProfileName = a.Profile != null ? a.Profile.Name : null,
                    a.AllocationTagId,
                    a.Status,
                    a.ParcialGrade,
                    a.FinalExamGrade,
                    a.FinalGrade,
                    a.WorkingHours,
                    a.GradeSituation,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                TotalCount = totalCount,
                Page = currentPage,
                PageSize = size,
                TotalPages = (int)Math.Ceiling((double)totalCount / size),
                Allocations = items
            });
        })
        .WithName("GetAllocations")
        .WithSummary("Lista as alocações de usuários no sistema");

        group.MapGet("/api/v1/allocations/{id}", async (long id, SolarDbContext db) =>
        {
            var allocation = await db.Allocations
                .Include(a => a.User)
                .Include(a => a.Profile)
                .Include(a => a.AllocationTag)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (allocation == null)
            {
                return Results.NotFound(new { error = "Alocação não encontrada." });
            }

            return Results.Ok(new
            {
                allocation.Id,
                allocation.UserId,
                UserName = allocation.User?.Name,
                allocation.ProfileId,
                ProfileName = allocation.Profile?.Name,
                allocation.AllocationTagId,
                allocation.Status,
                allocation.ParcialGrade,
                allocation.FinalExamGrade,
                allocation.FinalGrade,
                allocation.WorkingHours,
                allocation.GradeSituation,
                allocation.CreatedAt,
                allocation.UpdatedAt
            });
        })
        .WithName("GetAllocationById")
        .WithSummary("Retorna os detalhes de uma alocação específica");

        group.MapPost("/api/v1/allocations", async (CreateAllocationRequest req, SolarDbContext db) =>
        {
            var userExists = await db.Users.AnyAsync(u => u.Id == req.UserId);
            if (!userExists) return Results.BadRequest(new { error = "Usuário não encontrado." });

            var profileExists = await db.Profiles.AnyAsync(p => p.Id == req.ProfileId);
            if (!profileExists) return Results.BadRequest(new { error = "Perfil não encontrado." });

            var allocation = new Allocation
            {
                UserId = req.UserId,
                ProfileId = req.ProfileId,
                AllocationTagId = req.AllocationTagId,
                Status = req.Status ?? AllocationStatus.Activated,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Allocations.Add(allocation);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/allocations/{allocation.Id}", new
            {
                allocation.Id,
                allocation.UserId,
                allocation.ProfileId,
                allocation.AllocationTagId,
                allocation.Status,
                allocation.CreatedAt
            });
        })
        .AddEndpointFilter<ValidationFilter<CreateAllocationRequest>>()
        .WithName("CreateAllocation")
        .WithSummary("Cria uma nova alocação de usuário em tag/turma");

        group.MapPut("/api/v1/allocations/{id}", async (long id, UpdateAllocationRequest req, SolarDbContext db) =>
        {
            var allocation = await db.Allocations.FindAsync(id);
            if (allocation == null)
            {
                return Results.NotFound(new { error = "Alocação não encontrada." });
            }

            if (req.AllocationTagId.HasValue) allocation.AllocationTagId = req.AllocationTagId.Value;
            if (req.ProfileId.HasValue) allocation.ProfileId = req.ProfileId.Value;
            if (req.Status.HasValue) allocation.Status = req.Status.Value;
            if (req.ParcialGrade.HasValue) allocation.ParcialGrade = req.ParcialGrade.Value;
            if (req.FinalExamGrade.HasValue) allocation.FinalExamGrade = req.FinalExamGrade.Value;
            if (req.FinalGrade.HasValue) allocation.FinalGrade = req.FinalGrade.Value;
            if (req.WorkingHours.HasValue) allocation.WorkingHours = req.WorkingHours.Value;
            if (req.GradeSituation.HasValue) allocation.GradeSituation = req.GradeSituation.Value;
            if (req.UpdatedByUserId.HasValue) allocation.UpdatedByUserId = req.UpdatedByUserId.Value;
            if (req.OriginGroupId.HasValue) allocation.OriginGroupId = req.OriginGroupId.Value;
            allocation.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                allocation.Id,
                allocation.UserId,
                allocation.ProfileId,
                allocation.AllocationTagId,
                allocation.Status,
                allocation.ParcialGrade,
                allocation.FinalExamGrade,
                allocation.FinalGrade,
                allocation.WorkingHours,
                allocation.GradeSituation,
                allocation.UpdatedAt
            });
        })
        .WithName("UpdateAllocation")
        .WithSummary("Atualiza notas, situação ou status de uma alocação");

        group.MapDelete("/api/v1/allocations/{id}", async (long id, SolarDbContext db) =>
        {
            var allocation = await db.Allocations.FindAsync(id);
            if (allocation == null)
            {
                return Results.NotFound(new { error = "Alocação não encontrada." });
            }

            db.Allocations.Remove(allocation);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Alocação removida com sucesso." });
        })
        .WithName("DeleteAllocation")
        .WithSummary("Exclui uma alocação");

        return group;
    }
}
