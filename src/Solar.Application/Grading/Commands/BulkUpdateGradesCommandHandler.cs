using Solar.Application.Common;
using Solar.Application.Common.Mediator;
using Solar.Domain.Common;
using Solar.Domain.Events;
using Solar.Domain.Grading;

namespace Solar.Application.Grading.Commands;

public record BulkUpdateGradesItem(long StudentId, double PartialGrade, double? FinalExamGrade, int FrequencyHours);

public record BulkUpdateGradesCommand(long CurriculumUnitId, List<BulkUpdateGradesItem> Grades) : ICommand<Result<BulkUpdateGradesResult>>;

public record BulkUpdateGradesResult(
    long CurriculumUnitId,
    int ProcessedCount,
    List<CalculatedStudentScoreItem> Results
);

public record CalculatedStudentScoreItem(
    long StudentId,
    double PartialGrade,
    double? FinalExamGrade,
    int FrequencyHours,
    double FinalGrade,
    string Situation,
    bool IsFrequencySufficient
);

public class BulkUpdateGradesCommandHandler : ICommandHandler<BulkUpdateGradesCommand, Result<BulkUpdateGradesResult>>
{
    private readonly GradingCalculationService _gradingService;
    private readonly IDomainEventDispatcher _dispatcher;

    public BulkUpdateGradesCommandHandler(
        GradingCalculationService gradingService,
        IDomainEventDispatcher dispatcher)
    {
        _gradingService = gradingService;
        _dispatcher = dispatcher;
    }

    public async Task<Result<BulkUpdateGradesResult>> HandleAsync(BulkUpdateGradesCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Grades == null || !command.Grades.Any())
        {
            return Result<BulkUpdateGradesResult>.Failure(
                Error.Validation("Grading.EmptyGrades", "Lista de notas vazia ou não informada."));
        }

        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 4.0,
            FinalExamPassingGrade = 5.0,
            TotalWorkingHours = 64,
            MinHoursPercentage = 75.0,
            HasFinalExamInOffering = true
        };

        var calculatedResults = new List<CalculatedStudentScoreItem>();
        var domainEvents = new List<IDomainEvent>();

        foreach (var g in command.Grades)
        {
            var activities = new List<GradingEvaluationInput>
            {
                new()
                {
                    ActivityId = 1,
                    Name = "Nota Parcial N1",
                    IsEvaluative = true,
                    IsFrequency = true,
                    Weight = 1.0,
                    FinalWeight = 100.0,
                    StudentGrade = g.PartialGrade,
                    StudentWorkingHours = g.FrequencyHours
                }
            };

            if (g.FinalExamGrade.HasValue)
            {
                activities.Add(new()
                {
                    ActivityId = 2,
                    Name = "Avaliação Final",
                    IsEvaluative = true,
                    IsFinalExam = true,
                    IsFrequency = false,
                    Weight = 1.0,
                    FinalWeight = 100.0,
                    StudentGrade = g.FinalExamGrade.Value,
                    StudentWorkingHours = 0
                });
            }

            var calc = _gradingService.Calculate(activities, criteria);

            calculatedResults.Add(new CalculatedStudentScoreItem(
                g.StudentId,
                g.PartialGrade,
                g.FinalExamGrade,
                g.FrequencyHours,
                calc.FinalGrade,
                calc.Situation.ToString(),
                calc.IsFrequencySufficient
            ));

            domainEvents.Add(new GradeUpdatedDomainEvent(
                AllocationId: command.CurriculumUnitId,
                UserId: g.StudentId,
                FinalGrade: calc.FinalGrade,
                Situation: calc.Situation
            ));
        }

        // Dispara eventos de domínio para os handlers assíncronos desacoplados
        await _dispatcher.DispatchAsync(domainEvents, cancellationToken);

        return Result<BulkUpdateGradesResult>.Success(new BulkUpdateGradesResult(
            command.CurriculumUnitId,
            calculatedResults.Count,
            calculatedResults
        ));
    }
}
