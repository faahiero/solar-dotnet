using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Domain.Administration;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Solar.Infrastructure.Identity;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Logging;

namespace Solar.WebApi.Endpoints;

public record AddBlacklistRequest(string Cpf, string? Reason, long? UserId);
public record AdminResetPasswordRequest(string? NewPassword);

// DTOs para Turmas (Groups)
public record CreateGroupRequest(long OfferId, string? Code, string? Name, string? Location, bool? Status, bool? Integrated, long? MainGroupId, int? DigitalClassDirectoryId);
public record UpdateGroupRequest(long? OfferId, string? Code, string? Name, string? Location, bool? Status, bool? Integrated, long? MainGroupId, int? DigitalClassDirectoryId);

// DTOs para Alocações (Allocations)
public record CreateAllocationRequest(long UserId, int ProfileId, long? AllocationTagId, AllocationStatus? Status);
public record UpdateAllocationRequest(long? AllocationTagId, int? ProfileId, AllocationStatus? Status, double? ParcialGrade, double? FinalExamGrade, double? FinalGrade, decimal? WorkingHours, GradeSituation? GradeSituation, long? UpdatedByUserId, int? OriginGroupId);

// DTOs para Semestres (Semesters)
public record CreateSemesterRequest(string Name, long? OfferScheduleId, long? EnrollmentScheduleId);
public record UpdateSemesterRequest(string? Name, long? OfferScheduleId, long? EnrollmentScheduleId);

// DTOs para Cursos (Courses)
public record CreateCourseRequest(string Name, string? Code, double? PassingGrade, double? MinGradeToFinalExam, double? MinFinalExamGrade, double? FinalExamPassingGrade, int? MinHours);
public record UpdateCourseRequest(string? Name, string? Code, double? PassingGrade, double? MinGradeToFinalExam, double? MinFinalExamGrade, double? FinalExamPassingGrade, int? MinHours);

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // ----------------------------------------------------
        // Importação e Usuários
        // ----------------------------------------------------

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

        // ----------------------------------------------------
        // CRUD de Turmas / Grupos (Groups - Espelha groups_controller)
        // ----------------------------------------------------
        app.MapGet("/api/v1/groups", async (
            long? offerId,
            string? search,
            int? page,
            int? pageSize,
            SolarDbContext db) =>
        {
            int currentPage = page ?? 1;
            int size = pageSize ?? 50;

            var query = db.Groups
                .Include(g => g.Offer)
                    .ThenInclude(o => o!.CurriculumUnit)
                .AsQueryable();

            if (offerId.HasValue && offerId.Value > 0)
            {
                query = query.Where(g => g.OfferId == offerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(g => (g.Name != null && g.Name.ToLower().Contains(s)) ||
                                         (g.Code != null && g.Code.ToLower().Contains(s)) ||
                                         (g.Location != null && g.Location.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();
            var groups = await query
                .OrderBy(g => g.Name ?? g.Code)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(g => new
                {
                    g.Id,
                    g.OfferId,
                    CurriculumUnitName = g.Offer != null && g.Offer.CurriculumUnit != null ? g.Offer.CurriculumUnit.Name : null,
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
                Total = total,
                Page = currentPage,
                PageSize = size,
                Groups = groups
            });
        })
        .WithName("GetGroups")
        .WithSummary("Lista as turmas/grupos cadastrados");

        app.MapGet("/api/v1/groups/{id}", async (long id, SolarDbContext db) =>
        {
            var group = await db.Groups
                .Include(g => g.Offer)
                    .ThenInclude(o => o!.CurriculumUnit)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return Results.NotFound(new { error = "Turma não encontrada." });
            }

            return Results.Ok(new
            {
                group.Id,
                group.OfferId,
                CurriculumUnitName = group.Offer?.CurriculumUnit?.Name,
                group.Code,
                group.Name,
                group.Location,
                group.Status,
                group.Integrated,
                group.MainGroupId,
                group.DigitalClassDirectoryId,
                group.CreatedAt,
                group.UpdatedAt
            });
        })
        .WithName("GetGroupById")
        .WithSummary("Retorna os detalhes de uma turma/grupo");

        app.MapPost("/api/v1/groups", async (CreateGroupRequest req, SolarDbContext db) =>
        {
            var offerExists = await db.Offers.AnyAsync(o => o.Id == req.OfferId);
            if (!offerExists)
            {
                return Results.BadRequest(new { error = "Oferta especificada não existe." });
            }

            if (!string.IsNullOrWhiteSpace(req.Code))
            {
                var duplicate = await db.Groups.AnyAsync(g => g.OfferId == req.OfferId && g.Code == req.Code);
                if (duplicate)
                {
                    return Results.Conflict(new { error = "Já existe uma turma com este código nesta oferta." });
                }
            }

            var group = new Group
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

            db.Groups.Add(group);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/groups/{group.Id}", new
            {
                group.Id,
                group.OfferId,
                group.Code,
                group.Name,
                group.Location,
                group.Status,
                group.Integrated,
                group.CreatedAt
            });
        })
        .WithName("CreateGroup")
        .WithSummary("Cria uma nova turma/grupo");

        app.MapPut("/api/v1/groups/{id}", async (long id, UpdateGroupRequest req, SolarDbContext db) =>
        {
            var group = await db.Groups.FindAsync(id);
            if (group == null)
            {
                return Results.NotFound(new { error = "Turma não encontrada." });
            }

            if (req.OfferId.HasValue && req.OfferId.Value != group.OfferId)
            {
                var offerExists = await db.Offers.AnyAsync(o => o.Id == req.OfferId.Value);
                if (!offerExists) return Results.BadRequest(new { error = "Oferta especificada não existe." });
                group.OfferId = req.OfferId.Value;
            }

            if (req.Code != null) group.Code = req.Code;
            if (req.Name != null) group.Name = req.Name;
            if (req.Location != null) group.Location = req.Location;
            if (req.Status.HasValue) group.Status = req.Status.Value;
            if (req.Integrated.HasValue) group.Integrated = req.Integrated.Value;
            if (req.MainGroupId.HasValue) group.MainGroupId = req.MainGroupId.Value;
            if (req.DigitalClassDirectoryId.HasValue) group.DigitalClassDirectoryId = req.DigitalClassDirectoryId.Value;

            group.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                group.Id,
                group.OfferId,
                group.Code,
                group.Name,
                group.Location,
                group.Status,
                group.Integrated,
                group.UpdatedAt
            });
        })
        .WithName("UpdateGroup")
        .WithSummary("Atualiza os dados de uma turma/grupo");

        app.MapDelete("/api/v1/groups/{id}", async (long id, SolarDbContext db) =>
        {
            var group = await db.Groups.FindAsync(id);
            if (group == null)
            {
                return Results.NotFound(new { error = "Turma não encontrada." });
            }

            // Verifica integridade referencial nas alocações
            var hasAllocations = await db.Allocations.AnyAsync(a => a.AllocationTag != null && a.AllocationTag.GroupId == id);
            if (hasAllocations)
            {
                return Results.Conflict(new { error = "Não é possível remover a turma pois existem alocações vinculadas a ela." });
            }

            db.Groups.Remove(group);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Turma removida com sucesso." });
        })
        .WithName("DeleteGroup")
        .WithSummary("Remove uma turma caso não haja alocações vinculadas");

        // ----------------------------------------------------
        // CRUD de Alocações (Allocations - Espelha allocations_controller)
        // ----------------------------------------------------
        app.MapGet("/api/v1/allocations", async (
            long? userId,
            long? allocationTagId,
            int? profileId,
            int? page,
            int? pageSize,
            SolarDbContext db) =>
        {
            int currentPage = page ?? 1;
            int size = pageSize ?? 50;

            var query = db.Allocations
                .Include(a => a.User)
                .Include(a => a.Profile)
                .Include(a => a.AllocationTag)
                .AsQueryable();

            if (userId.HasValue && userId.Value > 0)
                query = query.Where(a => a.UserId == userId.Value);

            if (allocationTagId.HasValue && allocationTagId.Value > 0)
                query = query.Where(a => a.AllocationTagId == allocationTagId.Value);

            if (profileId.HasValue && profileId.Value > 0)
                query = query.Where(a => a.ProfileId == profileId.Value);

            var total = await query.CountAsync();
            var allocations = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    UserName = a.User != null ? a.User.Name ?? a.User.Username : null,
                    a.ProfileId,
                    ProfileName = a.Profile != null ? a.Profile.Name : null,
                    a.AllocationTagId,
                    a.Status,
                    a.ParcialGrade,
                    a.FinalExamGrade,
                    a.FinalGrade,
                    a.WorkingHours,
                    GradeSituation = a.GradeSituation != null ? a.GradeSituation.ToString() : null,
                    a.CreatedAt,
                    a.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(new
            {
                Total = total,
                Page = currentPage,
                PageSize = size,
                Allocations = allocations
            });
        })
        .WithName("GetAllocations")
        .WithSummary("Lista as alocações de usuários no sistema");

        app.MapGet("/api/v1/allocations/{id}", async (long id, SolarDbContext db) =>
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
                UserName = allocation.User?.Name ?? allocation.User?.Username,
                allocation.ProfileId,
                ProfileName = allocation.Profile?.Name,
                allocation.AllocationTagId,
                allocation.Status,
                allocation.ParcialGrade,
                allocation.FinalExamGrade,
                allocation.FinalGrade,
                allocation.WorkingHours,
                GradeSituation = allocation.GradeSituation?.ToString(),
                allocation.CreatedAt,
                allocation.UpdatedAt
            });
        })
        .WithName("GetAllocationById")
        .WithSummary("Retorna os detalhes de uma alocação específica");

        app.MapPost("/api/v1/allocations", async (CreateAllocationRequest req, SolarDbContext db) =>
        {
            var userExists = await db.Users.AnyAsync(u => u.Id == req.UserId);
            if (!userExists) return Results.BadRequest(new { error = "Usuário não encontrado." });

            var profileExists = await db.Profiles.AnyAsync(p => p.Id == req.ProfileId);
            if (!profileExists) return Results.BadRequest(new { error = "Perfil não encontrado." });

            var duplicate = await db.Allocations.AnyAsync(a => a.UserId == req.UserId && a.AllocationTagId == req.AllocationTagId && a.ProfileId == req.ProfileId);
            if (duplicate)
            {
                return Results.Conflict(new { error = "Usuário já está alocado nesta tag com este perfil." });
            }

            var allocation = new Allocation
            {
                UserId = req.UserId,
                ProfileId = req.ProfileId,
                AllocationTagId = req.AllocationTagId,
                Status = req.Status ?? AllocationStatus.Pending,
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
        .WithName("CreateAllocation")
        .WithSummary("Cria uma nova alocação de usuário em tag/turma");

        app.MapPut("/api/v1/allocations/{id}", async (long id, UpdateAllocationRequest req, SolarDbContext db) =>
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
                allocation.FinalGrade,
                allocation.UpdatedAt
            });
        })
        .WithName("UpdateAllocation")
        .WithSummary("Atualiza notas, situação ou status de uma alocação");

        app.MapDelete("/api/v1/allocations/{id}", async (long id, SolarDbContext db) =>
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
        .WithSummary("Remove uma alocação de usuário");

        // ----------------------------------------------------
        // CRUD de Semestres (Semesters - Espelha semesters_controller)
        // ----------------------------------------------------
        app.MapGet("/api/v1/semesters", async (SolarDbContext db) =>
        {
            var semesters = await db.Semesters
                .OrderByDescending(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.OfferScheduleId,
                    s.EnrollmentScheduleId,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(semesters);
        })
        .WithName("GetSemesters")
        .WithSummary("Lista os semestres acadêmicos");

        app.MapGet("/api/v1/semesters/{id}", async (long id, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            return Results.Ok(new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.CreatedAt,
                semester.UpdatedAt
            });
        })
        .WithName("GetSemesterById")
        .WithSummary("Retorna os detalhes de um semestre");

        app.MapPost("/api/v1/semesters", async (CreateSemesterRequest req, SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Nome do semestre é obrigatório." });

            var duplicate = await db.Semesters.AnyAsync(s => s.Name.ToLower() == req.Name.Trim().ToLower());
            if (duplicate) return Results.Conflict(new { error = "Já existe um semestre com este nome." });

            var semester = new Semester
            {
                Name = req.Name.Trim(),
                OfferScheduleId = req.OfferScheduleId ?? 1,
                EnrollmentScheduleId = req.EnrollmentScheduleId ?? 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Semesters.Add(semester);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/semesters/{semester.Id}", new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.CreatedAt
            });
        })
        .WithName("CreateSemester")
        .WithSummary("Cria um novo semestre acadêmico");

        app.MapPut("/api/v1/semesters/{id}", async (long id, UpdateSemesterRequest req, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            if (!string.IsNullOrWhiteSpace(req.Name)) semester.Name = req.Name.Trim();
            if (req.OfferScheduleId.HasValue) semester.OfferScheduleId = req.OfferScheduleId.Value;
            if (req.EnrollmentScheduleId.HasValue) semester.EnrollmentScheduleId = req.EnrollmentScheduleId.Value;

            semester.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                semester.Id,
                semester.Name,
                semester.OfferScheduleId,
                semester.EnrollmentScheduleId,
                semester.UpdatedAt
            });
        })
        .WithName("UpdateSemester")
        .WithSummary("Atualiza um semestre acadêmico");

        app.MapDelete("/api/v1/semesters/{id}", async (long id, SolarDbContext db) =>
        {
            var semester = await db.Semesters.FindAsync(id);
            if (semester == null) return Results.NotFound(new { error = "Semestre não encontrado." });

            var hasOffers = await db.Offers.AnyAsync(o => o.SemesterId == id);
            if (hasOffers) return Results.Conflict(new { error = "Não é possível excluir o semestre pois existem ofertas vinculadas." });

            db.Semesters.Remove(semester);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Semestre removido com sucesso." });
        })
        .WithName("DeleteSemester")
        .WithSummary("Remove um semestre acadêmico");

        // ----------------------------------------------------
        // CRUD de Cursos (Courses - Espelha courses_controller)
        // ----------------------------------------------------
        app.MapGet("/api/v1/courses", async (SolarDbContext db) =>
        {
            var courses = await db.Courses
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Name,
                    c.PassingGrade,
                    c.MinGradeToFinalExam,
                    c.MinFinalExamGrade,
                    c.FinalExamPassingGrade,
                    c.MinHours,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(courses);
        })
        .WithName("GetCourses")
        .WithSummary("Lista os cursos acadêmicos");

        app.MapGet("/api/v1/courses/{id}", async (long id, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            return Results.Ok(new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.MinGradeToFinalExam,
                course.MinFinalExamGrade,
                course.FinalExamPassingGrade,
                course.MinHours,
                course.CreatedAt,
                course.UpdatedAt
            });
        })
        .WithName("GetCourseById")
        .WithSummary("Retorna os detalhes de um curso");

        app.MapPost("/api/v1/courses", async (CreateCourseRequest req, SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Nome do curso é obrigatório." });

            var course = new Course
            {
                Name = req.Name.Trim(),
                Code = req.Code?.Trim(),
                PassingGrade = req.PassingGrade ?? 7.0,
                MinGradeToFinalExam = req.MinGradeToFinalExam ?? 4.0,
                MinFinalExamGrade = req.MinFinalExamGrade ?? 4.0,
                FinalExamPassingGrade = req.FinalExamPassingGrade ?? 5.0,
                MinHours = req.MinHours ?? 64,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Courses.Add(course);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/courses/{course.Id}", new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.CreatedAt
            });
        })
        .WithName("CreateCourse")
        .WithSummary("Cria um novo curso acadêmico");

        app.MapPut("/api/v1/courses/{id}", async (long id, UpdateCourseRequest req, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            if (!string.IsNullOrWhiteSpace(req.Name)) course.Name = req.Name.Trim();
            if (req.Code != null) course.Code = req.Code.Trim();
            if (req.PassingGrade.HasValue) course.PassingGrade = req.PassingGrade.Value;
            if (req.MinGradeToFinalExam.HasValue) course.MinGradeToFinalExam = req.MinGradeToFinalExam.Value;
            if (req.MinFinalExamGrade.HasValue) course.MinFinalExamGrade = req.MinFinalExamGrade.Value;
            if (req.FinalExamPassingGrade.HasValue) course.FinalExamPassingGrade = req.FinalExamPassingGrade.Value;
            if (req.MinHours.HasValue) course.MinHours = req.MinHours.Value;

            course.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                course.Id,
                course.Code,
                course.Name,
                course.PassingGrade,
                course.UpdatedAt
            });
        })
        .WithName("UpdateCourse")
        .WithSummary("Atualiza dados de um curso");

        app.MapDelete("/api/v1/courses/{id}", async (long id, SolarDbContext db) =>
        {
            var course = await db.Courses.FindAsync(id);
            if (course == null) return Results.NotFound(new { error = "Curso não encontrado." });

            var hasOffers = await db.Offers.AnyAsync(o => o.CourseId == id);
            if (hasOffers) return Results.Conflict(new { error = "Não é possível excluir o curso pois existem ofertas vinculadas." });

            db.Courses.Remove(course);
            await db.SaveChangesAsync();

            return Results.Ok(new { Success = true, Message = "Curso removido com sucesso." });
        })
        .WithName("DeleteCourse")
        .WithSummary("Remove um curso acadêmico");

        return app;
    }
}
