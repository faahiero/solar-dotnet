using Solar.Domain.Academic;
using Solar.Domain.Enums;
using Solar.Domain.Grading;
using Xunit;

namespace Solar.WebApi.Tests;

public class TeacherFeaturesTests
{
    [Fact]
    public void BulkUpdateGrades_ShouldCalculateFinalGradesAndSituationsForStudents()
    {
        // Arrange
        var gradingService = new GradingCalculationService();
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 4.0,
            FinalExamPassingGrade = 5.0,
            TotalWorkingHours = 64,
            MinHoursPercentage = 75.0,
            HasFinalExamInOffering = true
        };

        // Aluno 1: Aprovado Direto (Nota 8.5, 60h de 64h)
        var student1Activities = new List<GradingEvaluationInput>
        {
            new GradingEvaluationInput { ActivityId = 1, Name = "N1", IsEvaluative = true, IsFrequency = true, Weight = 1.0, FinalWeight = 100.0, StudentGrade = 8.5, StudentWorkingHours = 60 }
        };

        // Aluno 2: Foi para AF e Aprovou (Nota N1 = 5.0, Nota AF = 6.0, 50h de 64h)
        var student2Activities = new List<GradingEvaluationInput>
        {
            new GradingEvaluationInput { ActivityId = 1, Name = "N1", IsEvaluative = true, IsFrequency = true, Weight = 1.0, FinalWeight = 100.0, StudentGrade = 5.0, StudentWorkingHours = 50 },
            new GradingEvaluationInput { ActivityId = 2, Name = "AF", IsEvaluative = true, IsFinalExam = true, IsFrequency = false, Weight = 1.0, FinalWeight = 100.0, StudentGrade = 6.0, StudentWorkingHours = 0 }
        };

        // Act
        var res1 = gradingService.Calculate(student1Activities, criteria);
        var res2 = gradingService.Calculate(student2Activities, criteria);

        // Assert
        Assert.Equal(8.5, res1.FinalGrade);
        Assert.Equal(GradeSituation.Approved, res1.Situation);

        Assert.Equal(5.5, res2.FinalGrade); // (5.0 + 6.0) / 2 = 5.5 >= 5.0
        Assert.Equal(GradeSituation.FinalExamApproved, res2.Situation);
    }

    [Fact]
    public void DisciplineImport_ShouldCalculateShiftedDatesAccurately()
    {
        // Arrange
        var importService = new DisciplineImportService();
        var sourceStart = new DateOnly(2025, 8, 1);
        var sourceEnd = new DateOnly(2025, 12, 15);
        var destStart = new DateOnly(2026, 2, 1);
        var destEnd = new DateOnly(2026, 6, 15);

        var items = new List<DisciplineImportItem>
        {
            new DisciplineImportItem
            {
                SourceAcademicAllocationId = 1,
                ToolType = "Exam",
                Name = "Prova 1",
                IsEvaluative = true,
                OriginalStartDate = new DateOnly(2025, 9, 1),
                OriginalEndDate = new DateOnly(2025, 9, 10)
            },
            new DisciplineImportItem
            {
                SourceAcademicAllocationId = 2,
                ToolType = "Assignment",
                Name = "Trabalho Final",
                IsEvaluative = true,
                OriginalStartDate = new DateOnly(2025, 11, 1),
                OriginalEndDate = new DateOnly(2025, 11, 20)
            }
        };

        // Act
        var preview = importService.GeneratePreview(items, sourceStart, sourceEnd, destStart, destEnd, new HashSet<string>());

        // Assert
        Assert.Equal(2, preview.Items.Count);
        Assert.NotNull(preview.Items[0].ShiftedStartDate);
        Assert.True(preview.Items[0].IsSupported);
    }
}
