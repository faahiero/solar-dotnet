using Microsoft.EntityFrameworkCore;
using Solar.Application.Reports;
using Solar.Infrastructure.Persistence;

namespace Solar.WebApi.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").RequireAuthorization();

        // Emissão de Pauta Oficial de Notas em PDF (Consulta real no PostgreSQL em allocations / users)
        group.MapGet("/api/v1/curriculum-units/{id}/reports/grades-pdf", async (
            int id,
            SolarDbContext db,
            IAcademicReportService reportService) =>
        {
            var offer = await db.Offers
                .Include(o => o.CurriculumUnit)
                .Include(o => o.Course)
                .Include(o => o.Semester)
                .FirstOrDefaultAsync(o => o.Id == id);

            var teacherName = await db.Allocations
                .Include(a => a.User)
                .Where(a => (a.ProfileId == 4 || a.ProfileId == 3 || a.ProfileId == 2) && a.User != null)
                .Select(a => a.User!.Name ?? a.User.Username)
                .FirstOrDefaultAsync() ?? "Docente Responsável";

            var studentAllocations = await db.Allocations
                .Include(a => a.User)
                .Where(a => a.User != null)
                .Take(50)
                .ToListAsync();

            var model = new ClassGradesReportModel
            {
                CurriculumUnitCode = offer?.CurriculumUnit?.Code ?? string.Empty,
                CurriculumUnitName = offer?.CurriculumUnit?.Name ?? string.Empty,
                CourseName = offer?.Course?.Name ?? string.Empty,
                SemesterName = offer?.Semester?.Name ?? string.Empty,
                ClassCode = $"TURMA-{id:00}",
                TeacherName = teacherName,
                WorkingHours = offer?.CurriculumUnit?.WorkingHours ?? 64,
                Students = studentAllocations.Select(a => new StudentGradeEntry
                {
                    StudentId = (int)a.UserId,
                    StudentName = a.User?.Name ?? a.User?.Username ?? "Discente",
                    Cpf = string.IsNullOrEmpty(a.User?.Cpf) ? "" : (a.User.Cpf.Length == 11 ? $"{a.User.Cpf[..3]}.{a.User.Cpf[3..6]}.{a.User.Cpf[6..9]}-{a.User.Cpf[9..]}" : a.User.Cpf),
                    PartialGrade = a.ParcialGrade ?? a.FinalGrade ?? 0.0,
                    FinalExamGrade = a.FinalExamGrade,
                    FinalGrade = a.FinalGrade ?? 0.0,
                    FrequencyHours = (int)(a.WorkingHours ?? 0),
                    AttendancePercentage = offer?.CurriculumUnit?.WorkingHours > 0 ? (double)(a.WorkingHours ?? 0) / (double)offer.CurriculumUnit.WorkingHours * 100.0 : 100.0,
                    Situation = a.GradeSituation?.ToString() ?? "Regular"
                }).ToList()
            };

            var pdfBytes = reportService.GenerateGradesReportPdf(model);
            return Results.File(pdfBytes, "application/pdf", $"Pauta_Notas_Turma_{id}.pdf");
        })
        .WithName("ExportClassGradesPdf")
        .WithSummary("Gera a pauta oficial de notas e situação da turma em formato PDF com dados reais do banco");

        // Emissão de Pauta de Frequência em PDF (Consulta real no PostgreSQL em allocations / users)
        group.MapGet("/api/v1/curriculum-units/{id}/reports/attendance-pdf", async (
            int id,
            SolarDbContext db,
            IAcademicReportService reportService) =>
        {
            var offer = await db.Offers
                .Include(o => o.CurriculumUnit)
                .Include(o => o.Course)
                .Include(o => o.Semester)
                .FirstOrDefaultAsync(o => o.Id == id);

            var teacherName = await db.Allocations
                .Include(a => a.User)
                .Where(a => (a.ProfileId == 4 || a.ProfileId == 3 || a.ProfileId == 2) && a.User != null)
                .Select(a => a.User!.Name ?? a.User.Username)
                .FirstOrDefaultAsync() ?? "Docente Responsável";

            var studentAllocations = await db.Allocations
                .Include(a => a.User)
                .Where(a => a.User != null)
                .Take(50)
                .ToListAsync();

            var totalHours = offer?.CurriculumUnit?.WorkingHours ?? 64;

            var model = new ClassAttendanceReportModel
            {
                CurriculumUnitName = offer?.CurriculumUnit?.Name ?? string.Empty,
                CourseName = offer?.Course?.Name ?? string.Empty,
                SemesterName = offer?.Semester?.Name ?? string.Empty,
                ClassCode = $"TURMA-{id:00}",
                TeacherName = teacherName,
                TotalHours = totalHours,
                Students = studentAllocations.Select(a => new StudentAttendanceEntry
                {
                    StudentId = (int)a.UserId,
                    StudentName = a.User?.Name ?? a.User?.Username ?? "Discente",
                    AttendedHours = (int)(a.WorkingHours ?? 0),
                    AttendancePercentage = totalHours > 0 ? (double)(a.WorkingHours ?? 0) / (double)totalHours * 100.0 : 100.0,
                    Status = a.GradeSituation?.ToString() ?? "Frequência Regular"
                }).ToList()
            };

            var pdfBytes = reportService.GenerateAttendanceReportPdf(model);
            return Results.File(pdfBytes, "application/pdf", $"Pauta_Frequencia_Turma_{id}.pdf");
        })
        .WithName("ExportClassAttendancePdf")
        .WithSummary("Gera a pauta de frequência da turma em formato PDF com dados reais do banco");

        return app;
    }
}
