using Solar.Domain.Assessments;
using Solar.WebApi.Extensions;

namespace Solar.WebApi.Endpoints;

public record ExamSubmissionRequest(Dictionary<int, int> Answers);

public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").RequireAuthorization();

        // Listagem de Provas Online da Disciplina
        group.MapGet("/api/v1/curriculum-units/{id}/exams", () => Results.Ok(new[]
        {
            new
            {
                Id = 1,
                Name = "Prova Online 1 - Avaliação Semestral",
                Description = "Avaliação oficial individual cobrindo os módulos 1 e 2. Trava anti-fraude ativada.",
                DurationMinutes = 60,
                TotalQuestions = 4,
                BlockContent = true, // Trava Anti-Fraude
                Status = "Aberta",
                Deadline = "15/09/2026 23:59",
                AttemptsAllowed = 1,
                AttemptsMade = 0
            }
        }))
        .WithName("GetExams")
        .WithSummary("Retorna as provas online da disciplina");

        // Iniciar Prova Online (Gera tentativa e ativa trava anti-fraude)
        group.MapPost("/api/v1/curriculum-units/{id}/exams/{examId}/start", (int id, int examId) =>
        {
            return Results.Ok(new
            {
                ExamId = examId,
                Name = "Prova Online 1 - Avaliação Semestral",
                DurationMinutes = 60,
                StartedAt = DateTime.UtcNow,
                BlockContent = true,
                Questions = new[]
                {
                    new
                    {
                        Id = 101,
                        Enunciation = "1. Qual é a principal característica das ligações covalentes nos compostos orgânicos?",
                        Type = "SingleChoice",
                        Items = new[]
                        {
                            new { Id = 1, Text = "A) Compartilhamento de pares de elétrons entre átomos", Correct = true },
                            new { Id = 2, Text = "B) Transferência total de elétrons com formação de cátions e ânions", Correct = false },
                            new { Id = 3, Text = "C) Atração eletrostática exclusiva entre metais alcalinos", Correct = false },
                            new { Id = 4, Text = "D) Ausência total de nuvem eletrônica", Correct = false }
                        }
                    },
                    new
                    {
                        Id = 102,
                        Enunciation = "2. Em relação à Primeira Lei da Termodinâmica, assinale a afirmação correta:",
                        Type = "SingleChoice",
                        Items = new[]
                        {
                            new { Id = 5, Text = "A) A energia total de um sistema isolado permanece constante (ΔU = Q - W)", Correct = true },
                            new { Id = 6, Text = "B) A entropia do universo sempre diminui em processos espontâneos", Correct = false },
                            new { Id = 7, Text = "C) O calor não pode ser convertido em trabalho sob nenhuma condição", Correct = false },
                            new { Id = 8, Text = "D) Todo trabalho se transforma em massa pura", Correct = false }
                        }
                    },
                    new
                    {
                        Id = 103,
                        Enunciation = "3. O equilíbrio químico dinâmico é caracterizado quando:",
                        Type = "SingleChoice",
                        Items = new[]
                        {
                            new { Id = 9, Text = "A) As velocidades das reações direta e inversa tornam-se iguais", Correct = true },
                            new { Id = 10, Text = "B) Todos os reagentes são completamente consumidos a zero", Correct = false },
                            new { Id = 11, Text = "C) A pressão do sistema cai instantaneamente para vácuo", Correct = false }
                        }
                    }
                }
            });
        })
        .WithName("StartExam")
        .WithSummary("Inicia a realização de uma prova online");

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
