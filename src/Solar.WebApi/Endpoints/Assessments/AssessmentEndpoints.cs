using Microsoft.EntityFrameworkCore;
using Solar.Domain.Assessments;
using Solar.Infrastructure.Persistence;
using Solar.WebApi.Extensions;

namespace Solar.WebApi.Endpoints;

public record ExamSubmissionRequest(Dictionary<int, int> Answers);

public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").RequireAuthorization();

        // Listagem de Provas Online da Disciplina (Consulta real na tabela exams)
        group.MapGet("/api/v1/curriculum-units/{id}/exams", async (int id, SolarDbContext db) =>
        {
            var exams = await db.Exams
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return Results.Ok(exams.Select(e => new
            {
                e.Id,
                e.Name,
                e.Description,
                DurationMinutes = e.Duration ?? 60,
                e.BlockContent,
                CreatedAt = e.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                UpdatedAt = e.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
            }));
        })
        .WithName("GetExams")
        .WithSummary("Retorna as provas online da disciplina do banco de dados");

        // Iniciar Prova Online (Consulta real na tabela questions e question_items)
        group.MapPost("/api/v1/curriculum-units/{id}/exams/{examId}/start", async (int id, int examId, SolarDbContext db) =>
        {
            var exam = await db.Exams
                .AsNoTracking()
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .ThenInclude(q => q!.QuestionItems)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                return Results.NotFound(new { message = "Avaliação online não encontrada." });
            }

            var questions = exam.ExamQuestions
                .Where(eq => eq.Question != null)
                .Select(eq => new
                {
                    eq.Question!.Id,
                    Enunciation = eq.Question.Enunciation,
                    Type = eq.Question.TypeQuestion.ToString(),
                    Items = eq.Question.QuestionItems.Select(qi => new
                    {
                        qi.Id,
                        Text = qi.Description,
                        Correct = qi.Value
                    })
                });

            return Results.Ok(new
            {
                ExamId = exam.Id,
                exam.Name,
                exam.Description,
                DurationMinutes = exam.Duration ?? 60,
                StartedAt = DateTime.UtcNow,
                exam.BlockContent,
                Questions = questions
            });
        })
        .WithName("StartExam")
        .WithSummary("Inicia a realização de uma prova online com dados reais de questões do banco");

        // Submissão e Correção Automática de Prova Online via CQRS
        group.MapPost("/api/v1/curriculum-units/{id}/exams/{examId}/submit", async (
            long id,
            long examId,
            ExamSubmissionRequest submission,
            Solar.Application.Common.Mediator.ISender sender) =>
        {
            var responses = submission.Answers.Select(a =>
                new Solar.Application.Assessments.Commands.StudentQuestionResponseItem(
                    a.Key,
                    new Dictionary<long, bool> { { a.Value, true } }
                )).ToList();

            var command = new Solar.Application.Assessments.Commands.SubmitExamAttemptCommand(
                ExamId: examId,
                UserId: 1, // Usuário autenticado
                Responses: responses
            );

            var result = await sender.Send(command);
            return result.ToHttpResult();
        })
        .WithName("SubmitExam")
        .WithSummary("Submete as respostas e calcula a nota da prova online via CQRS");

        return app;
    }
}
