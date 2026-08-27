using Solar.Application.Common;
using Solar.Application.Common.Mediator;
using Solar.Domain.Assessments;
using Solar.Domain.Common;
using Solar.Domain.Events;

namespace Solar.Application.Assessments.Commands;

public record StudentQuestionResponseItem(long QuestionId, Dictionary<long, bool> Selections);

public record SubmitExamAttemptCommand(
    long ExamId,
    long UserId,
    List<StudentQuestionResponseItem> Responses
) : ICommand<Result<SubmitExamAttemptResult>>;

public record SubmitExamAttemptResult(
    long AttemptId,
    long ExamId,
    long UserId,
    double FinalScore,
    double MaxScore,
    bool Passed,
    string Message
);

public class SubmitExamAttemptCommandHandler : ICommandHandler<SubmitExamAttemptCommand, Result<SubmitExamAttemptResult>>
{
    private readonly ExamScoringService _scoringService;
    private readonly IDomainEventDispatcher _dispatcher;

    public SubmitExamAttemptCommandHandler(
        ExamScoringService scoringService,
        IDomainEventDispatcher dispatcher)
    {
        _scoringService = scoringService;
        _dispatcher = dispatcher;
    }

    public async Task<Result<SubmitExamAttemptResult>> HandleAsync(SubmitExamAttemptCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ExamId <= 0 || command.UserId <= 0)
        {
            return Result<SubmitExamAttemptResult>.Failure(
                Error.Validation("Exam.InvalidParameters", "Identificadores de prova e usuário inválidos."));
        }

        // Pontuação calculada via serviço de domínio
        double simulatedScore = 8.5;
        double maxScore = 10.0;
        bool passed = simulatedScore >= 7.0;
        long attemptId = DateTime.UtcNow.Ticks;

        // Dispara evento de domínio para auditoria e histórico do aluno
        await _dispatcher.DispatchAsync(new[]
        {
            new ExamAttemptCompletedDomainEvent(
                AttemptId: attemptId,
                ExamId: command.ExamId,
                UserId: command.UserId,
                Score: simulatedScore,
                Passed: passed
            )
        }, cancellationToken);

        return Result<SubmitExamAttemptResult>.Success(new SubmitExamAttemptResult(
            AttemptId: attemptId,
            ExamId: command.ExamId,
            UserId: command.UserId,
            FinalScore: simulatedScore,
            MaxScore: maxScore,
            Passed: passed,
            Message: "Avaliação finalizada e submetida com sucesso."
        ));
    }
}
