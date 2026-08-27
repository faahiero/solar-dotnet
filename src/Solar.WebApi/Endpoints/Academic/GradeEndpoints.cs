using Solar.Application.Common.Mediator;
using Solar.Application.Grading.Commands;
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

        // Diário de Notas e Acompanhamento do Aluno (Espelha 12_turma_acompanhamento_notas.png)
        group.MapGet("/api/v1/curriculum-units/{id}/scores", (int id) =>
        {
            return Results.Ok(new
            {
                CurriculumUnitId = id,
                PassingGrade = 7.0,
                TotalWorkingHours = 64,
                Items = new[]
                {
                    new { Id = 1, Description = "Fórum de Discussão 1 (Módulo 1)", Weight = 2.0, Grade = 9.0, MaxGrade = 10.0, Status = "Avaliado" },
                    new { Id = 2, Description = "Trabalho Experimental 1 - Relatório", Weight = 3.0, Grade = 8.5, MaxGrade = 10.0, Status = "Avaliado" },
                    new { Id = 3, Description = "Prova Online Semestral 1", Weight = 5.0, Grade = 7.5, MaxGrade = 10.0, Status = "Avaliado" }
                },
                Summary = new
                {
                    PartialGrade = 8.1,
                    FinalExamGrade = (double?)null,
                    FinalGrade = 8.1,
                    CompletedHours = 56,
                    TotalHours = 64,
                    AttendancePercentage = 87.5,
                    Situation = "Aprovado por Média"
                }
            });
        })
        .WithName("GetScores")
        .WithSummary("Retorna o boletim/diário de notas da disciplina");

        return group;
    }
}
