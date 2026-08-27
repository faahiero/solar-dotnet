using Solar.Application.Common.Mediator;
using Solar.Domain.Common;
using Solar.Domain.Grading;

namespace Solar.Application.Grading.Commands;

public record CalculateStudentGradesCommand : ICommand<Result<GradingCalculationResult>>
{
    public long UserId { get; init; }
    public long AllocationId { get; init; }
    public IReadOnlyList<GradingEvaluationInput> Activities { get; init; } = [];
    public GradingCourseCriteria Criteria { get; init; } = new();
}

public class CalculateStudentGradesCommandHandler : ICommandHandler<CalculateStudentGradesCommand, Result<GradingCalculationResult>>
{
    private readonly GradingCalculationService _gradingService;

    public CalculateStudentGradesCommandHandler(GradingCalculationService gradingService)
    {
        _gradingService = gradingService;
    }

    public Task<Result<GradingCalculationResult>> HandleAsync(CalculateStudentGradesCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Activities == null)
        {
            return Task.FromResult(Result<GradingCalculationResult>.Failure(
                Error.Validation("Grading.InvalidActivities", "A lista de atividades avaliativas é obrigatória.")));
        }

        var result = _gradingService.Calculate(command.Activities, command.Criteria ?? new());
        return Task.FromResult(Result<GradingCalculationResult>.Success(result));
    }
}
