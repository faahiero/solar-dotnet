using Microsoft.EntityFrameworkCore;
using Solar.Application.Common.Mediator;
using Solar.Application.Grading.Commands;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Extensions;

namespace Solar.WebApi.Endpoints;

public record BulkUpdateGradesRequest(List<StudentGradeUpdateItem> Grades);
public record StudentGradeUpdateItem(long StudentId, double PartialGrade, double? FinalExamGrade, int FrequencyHours);

public static class GradeEndpoints
{
    public static IEndpointRouteBuilder MapGradeEndpoints(this IEndpointRouteBuilder group)
    {
        // Cálculo de Notas e Situação Acadêmica via CQRS
        group.MapPost("/api/v1/grades/calculate", async (
            CalculateStudentGradesCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.ToHttpResult();
        })
        .WithName("CalculateGrades")
        .WithSummary("Calcula média parcial, horas e situação acadêmica de um aluno via CQRS");

        // Lançamento / Atualização em Lote de Notas pelo Professor via CQRS (com DomainEvents)
        group.MapPost("/api/v1/curriculum-units/{id}/scores/bulk-update", async (
            long id,
            BulkUpdateGradesRequest req,
            ISender sender) =>
        {
            var items = req.Grades?.Select(g =>
                new BulkUpdateGradesItem(g.StudentId, g.PartialGrade, g.FinalExamGrade, g.FrequencyHours)).ToList() ?? [];

            var command = new BulkUpdateGradesCommand(id, items);
            var result = await sender.Send(command);

            return result.ToHttpResult();
        })
        .WithName("BulkUpdateGrades")
        .WithSummary("Lança e recalcula notas e frequência de todos os alunos da turma via CQRS");

        // Diário de Notas e Acompanhamento do Aluno (Consulta real no PostgreSQL em allocations e academic_allocations)
        group.MapGet("/api/v1/curriculum-units/{id}/scores", async (int id, SolarDbContext db) =>
        {
            var alloc = await db.Allocations
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync();

            var evaluativeTools = await db.AcademicAllocations
                .AsNoTracking()
                .Include(a => a.AcademicAllocationUsers)
                .Where(a => a.Evaluative || a.Frequency)
                .Take(10)
                .ToListAsync();

            var activities = evaluativeTools.Select(t =>
            {
                var userGrade = t.AcademicAllocationUsers.FirstOrDefault()?.Grade;
                return new
                {
                    Name = $"{t.AcademicToolType} #{t.AcademicToolId}",
                    Weight = (double)t.Weight,
                    FinalWeight = $"{t.FinalWeight:0}%",
                    Grade = userGrade,
                    Evaluative = t.Evaluative,
                    Frequency = t.Frequency
                };
            }).ToList();

            return Results.Ok(new
            {
                StudentName = alloc?.User?.Name ?? alloc?.User?.Username,
                WorkingHours = alloc?.WorkingHours,
                FinalGrade = alloc?.FinalGrade,
                PartialGrade = alloc?.ParcialGrade,
                FinalExamGrade = alloc?.FinalExamGrade,
                FrequencyHours = alloc?.WorkingHours,
                Situation = alloc?.GradeSituation?.ToString(),
                EvaluativeActivities = activities
            });
        })
        .WithName("GetScores")
        .WithSummary("Retorna o boletim/diário de notas da disciplina do banco de dados");

        return group;
    }
}
