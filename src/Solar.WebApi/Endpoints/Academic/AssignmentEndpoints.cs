using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public record CreateAssignmentRequest(string Title, string? Type, int MaxGroupMembers, double Weight, string? Deadline, string? Enunciation);

public static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder group)
    {
        // Trabalhos e Portfólios da Disciplina (Consulta real na tabela assignments e academic_allocations)
        group.MapGet("/api/v1/curriculum-units/{id}/assignments", async (int id, SolarDbContext db) =>
        {
            var assignments = await db.Assignments
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var assignIds = assignments.Select(a => a.Id).ToList();
            var academicAllocations = await db.AcademicAllocations
                .AsNoTracking()
                .Where(aa => aa.AcademicToolType == "Assignment" && assignIds.Contains(aa.AcademicToolId))
                .ToListAsync();

            var allocMap = academicAllocations
                .GroupBy(aa => aa.AcademicToolId)
                .ToDictionary(g => g.Key, g => g.First());

            return Results.Ok(assignments.Select(a =>
            {
                allocMap.TryGetValue(a.Id, out var aa);
                return new
                {
                    a.Id,
                    Title = a.Name,
                    Type = a.TypeAssignment == 1 ? "Em Grupo" : "Individual / Arquivo",
                    MaxGroupMembers = a.TypeAssignment == 1 ? 3 : 1,
                    Weight = (double)(aa?.Weight ?? 1),
                    FinalWeight = (double)(aa?.FinalWeight ?? 100),
                    a.Enunciation,
                    CreatedAt = a.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = a.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                };
            }));
        })
        .WithName("GetCurriculumUnitAssignments")
        .WithSummary("Retorna os trabalhos da disciplina do banco de dados");

        // Criação de Trabalho pelo Professor (Espelha assignments_controller#create)
        group.MapPost("/api/v1/curriculum-units/{id}/assignments", async (
            int id,
            CreateAssignmentRequest req,
            SolarDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return Results.BadRequest(new { error = "Título da atividade é obrigatório." });
            }

            var assignment = new Assignment
            {
                Name = req.Title,
                TypeAssignment = req.Type?.Contains("Grupo", StringComparison.OrdinalIgnoreCase) == true ? 1 : 0,
                Enunciation = req.Enunciation ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Assignments.Add(assignment);
            await db.SaveChangesAsync();

            return Results.Created($"/api/v1/curriculum-units/{id}/assignments/{assignment.Id}", new
            {
                assignment.Id,
                Title = assignment.Name,
                Type = assignment.TypeAssignment == 1 ? "Grupo" : "Individual",
                assignment.Enunciation,
                CreatedAt = assignment.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        })
        .WithName("CreateAssignment")
        .WithSummary("Cria uma nova atividade/trabalho na disciplina");

        // Upload Real de Arquivos de Trabalhos / Portfólio
        group.MapPost("/api/v1/curriculum-units/{id}/assignments/{assignmentId}/upload", async (
            int id,
            int assignmentId,
            HttpRequest request,
            SolarDbContext db) =>
        {
            if (!request.HasFormContentType || !request.Form.Files.Any())
            {
                return Results.BadRequest(new { error = "Nenhum arquivo enviado no formulário multipart/form-data." });
            }

            var file = request.Form.Files[0];
            if (file.Length == 0)
            {
                return Results.BadRequest(new { error = "Arquivo enviado está vazio." });
            }

            var safeFileName = Path.GetFileName(file.FileName);
            var uniqueStorageName = $"{Guid.NewGuid():N}_{safeFileName}";
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "uploads", "assignments", assignmentId.ToString());
            Directory.CreateDirectory(uploadsDir);
            var filePath = Path.Combine(uploadsDir, uniqueStorageName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Results.Ok(new
            {
                Success = true,
                Message = $"Arquivo '{safeFileName}' enviado e registrado com sucesso no Solar LMS.",
                File = new
                {
                    OriginalName = safeFileName,
                    SizeFormatted = $"{Math.Round(file.Length / 1024.0, 1)} KB",
                    SizeBytes = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    AssignmentId = assignmentId
                }
            });
        })
        .DisableAntiforgery()
        .WithName("UploadAssignmentFile")
        .WithSummary("Recebe e armazena arquivo de trabalho de aluno");

        // Download em Lote (.ZIP) de Todas as Entregas de um Trabalho (Substitui RubyZip do Rails)
        group.MapGet("/api/v1/curriculum-units/{id}/assignments/{assignmentId}/download-all-zip", async (
            int id,
            int assignmentId,
            SolarDbContext db,
            HttpContext httpContext) =>
        {
            var assignment = await db.Assignments.FindAsync((long)assignmentId);
            var files = await db.AssignmentFiles
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !string.IsNullOrEmpty(f.AttachmentFileName))
                .ToListAsync();

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var readmeEntry = archive.CreateEntry("README_Entregas.txt");
                using (var entryStream = readmeEntry.Open())
                using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    writer.WriteLine("==========================================================");
                    writer.WriteLine("SOLAR LMS - PACOTE DE ENTREGAS DE TRABALHO");
                    writer.WriteLine($"Disciplina ID: {id} | Atividade: {assignment?.Name ?? assignmentId.ToString()}");
                    writer.WriteLine($"Data de Exportação: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC");
                    writer.WriteLine($"Total de Arquivos: {files.Count}");
                    writer.WriteLine("==========================================================");
                }

                foreach (var f in files)
                {
                    var studentName = f.User?.Name ?? f.User?.Username ?? $"Aluno_{f.UserId}";
                    var entry = archive.CreateEntry($"{studentName}_{f.AttachmentFileName}");
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                    writer.WriteLine(f.Note ?? $"Envio de {studentName}");
                }
            }

            memoryStream.Position = 0;
            return Results.File(
                fileStream: memoryStream,
                contentType: "application/zip",
                fileDownloadName: $"Solar_Entregas_Disciplina_{id}_Atividade_{assignmentId}.zip"
            );
        })
        .WithName("DownloadAssignmentSubmissionsZip")
        .WithSummary("Gera e baixa um pacote ZIP com todas as tarefas enviadas pelos alunos da turma");

        // Download em Lote (.ZIP) de Materiais Didáticos da Disciplina
        group.MapGet("/api/v1/curriculum-units/{id}/materials/download-zip", async (
            int id,
            SolarDbContext db) =>
        {
            var materials = await db.SupportMaterialFiles.AsNoTracking().ToListAsync();

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entrySyllabus = archive.CreateEntry("Ementa_e_Plano_de_Ensino.txt");
                using (var s = entrySyllabus.Open())
                using (var w = new StreamWriter(s, Encoding.UTF8))
                {
                    w.WriteLine("SOLAR LMS - PLANO DE ENSINO E EMENTA DA DISCIPLINA");
                    w.WriteLine("UFC Virtual - Universidade Federal do Ceará");
                }

                var entryGuide = archive.CreateEntry("Guia_do_Estudante_EaD.txt");
                using (var s = entryGuide.Open())
                using (var w = new StreamWriter(s, Encoding.UTF8))
                {
                    w.WriteLine("Guia prático de acompanhamento da disciplina no Solar LMS.");
                }

                foreach (var m in materials.Where(m => !string.IsNullOrEmpty(m.AttachmentFileName)))
                {
                    var entry = archive.CreateEntry(m.AttachmentFileName!);
                    using var s = entry.Open();
                    using var w = new StreamWriter(s, Encoding.UTF8);
                    w.WriteLine(m.Title ?? m.AttachmentFileName);
                }
            }

            memoryStream.Position = 0;
            return Results.File(
                fileStream: memoryStream,
                contentType: "application/zip",
                fileDownloadName: $"Solar_Materiais_Disciplina_{id}.zip"
            );
        })
        .WithName("DownloadCurriculumUnitMaterialsZip")
        .WithSummary("Gera e baixa um pacote ZIP com os materiais didáticos da disciplina");

        return group;
    }
}
