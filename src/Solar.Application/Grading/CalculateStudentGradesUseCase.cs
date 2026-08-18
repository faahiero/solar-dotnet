using Solar.Domain.Entities;
using Solar.Domain.Grading;

namespace Solar.Application.Grading;

public record CalculateStudentGradesCommand
{
    public long UserId { get; init; }
    public long AllocationId { get; init; }
    public IReadOnlyList<GradingEvaluationInput> Activities { get; init; } = [];
    public GradingCourseCriteria Criteria { get; init; } = new();
}

public class CalculateStudentGradesUseCase
{
    private readonly GradingCalculationService _gradingService;

    public CalculateStudentGradesUseCase(GradingCalculationService gradingService)
    {
        _gradingService = gradingService;
    }

    public GradingCalculationResult Execute(CalculateStudentGradesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var result = _gradingService.Calculate(command.Activities, command.Criteria);
        return result;
    }

    public void ApplyToAllocation(Allocation allocation, GradingCalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(allocation);
        ArgumentNullException.ThrowIfNull(result);

        allocation.ParcialGrade = result.ParcialGrade;
        allocation.FinalExamGrade = result.FinalExamGrade;
        allocation.FinalGrade = result.FinalGrade;
        allocation.WorkingHours = (decimal)result.TotalWorkingHours;
        allocation.GradeSituation = result.Situation;
        allocation.UpdatedAt = DateTime.UtcNow;
    }
}
