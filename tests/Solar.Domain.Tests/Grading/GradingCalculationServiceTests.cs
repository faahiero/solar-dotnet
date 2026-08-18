using FluentAssertions;
using Solar.Domain.Enums;
using Solar.Domain.Grading;
using Xunit;

namespace Solar.Domain.Tests.Grading;

public class GradingCalculationServiceTests
{
    private readonly GradingCalculationService _service = new();

    [Fact]
    public void Should_Calculate_Approved_Directly_When_Parcial_Grade_Meets_Passing_Grade()
    {
        // Arrange: Dois blocos avaliativos de 40% e 60% (DISTINCT final_weight somam 100)
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 3.0,
            FinalExamPassingGrade = 5.0,
            TotalWorkingHours = 64,
            MinHoursPercentage = 75.0
        };

        // Bloco A (40%): Aluno tirou 8.0 -> Contribuição = 0.40 * 8.0 = 3.2
        // Bloco B (60%): Aluno tirou 7.0 -> Contribuição = 0.60 * 7.0 = 4.2
        // Média Parcial = 3.2 + 4.2 = 7.4 >= 7.0 (Aprovado)
        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova Teórica (Bloco 40%)",
                IsEvaluative = true,
                IsFrequency = true,
                Weight = 1.0,
                FinalWeight = 40.0,
                StudentGrade = 8.0,
                StudentWorkingHours = 30.0
            },
            new()
            {
                ActivityId = 2,
                Name = "Projeto Prático (Bloco 60%)",
                IsEvaluative = true,
                IsFrequency = true,
                Weight = 1.0,
                FinalWeight = 60.0,
                StudentGrade = 7.0,
                StudentWorkingHours = 30.0
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(7.4);
        result.FinalGrade.Should().Be(7.4);
        result.TotalWorkingHours.Should().Be(60.0); // 60h >= 48h (75% de 64h)
        result.Situation.Should().Be(GradeSituation.Approved);
        result.IsFrequencySufficient.Should().BeTrue();
    }

    [Fact]
    public void Should_Calculate_Weighted_Average_Correctly_With_Multiple_Activities_In_Same_Weight_Group()
    {
        // Arrange: Bloco 1 (40%): Duas provas com pesos 2 e 3
        // Nota Bloco 1 = ((8.0 * 2) + (6.0 * 3)) / (2 + 3) = (16 + 18) / 5 = 34 / 5 = 6.8
        // Contribuição Bloco 1 = 0.40 * 6.8 = 2.72
        // Bloco 2 (60%): Um trabalho com nota 10.0 -> Contribuição = 0.60 * 10.0 = 6.0
        // Média Parcial = 2.72 + 6.0 = 8.72
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            TotalWorkingHours = null // Sem controle estrito de horas
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova 1",
                IsEvaluative = true,
                Weight = 2.0,
                FinalWeight = 40.0,
                StudentGrade = 8.0
            },
            new()
            {
                ActivityId = 2,
                Name = "Prova 2",
                IsEvaluative = true,
                Weight = 3.0,
                FinalWeight = 40.0,
                StudentGrade = 6.0
            },
            new()
            {
                ActivityId = 3,
                Name = "Trabalho Final",
                IsEvaluative = true,
                Weight = 1.0,
                FinalWeight = 60.0,
                StudentGrade = 10.0
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(8.72);
        result.Situation.Should().Be(GradeSituation.Approved);
    }

    [Fact]
    public void Should_Use_Max_Grade_From_Equivalent_Activity_When_Student_Did_Better_In_Equivalent()
    {
        // Arrange (2ª Chamada / Atividade Equivalente substitui a nota menor)
        var criteria = new GradingCourseCriteria { PassingGrade = 7.0 };

        var regularActivity = new GradingEvaluationInput
        {
            ActivityId = 1,
            Name = "Prova 1",
            IsEvaluative = true,
            Weight = 1.0,
            FinalWeight = 100.0,
            StudentGrade = 4.0, // Aluno tirou 4.0 na primeira chamada
            EquivalentSubmissions =
            [
                new GradingEvaluationInput
                {
                    ActivityId = 2,
                    Name = "Prova 1 - 2ª Chamada (Equivalente)",
                    IsEvaluative = true,
                    EquivalentActivityId = 1,
                    StudentGrade = 9.0 // Tirou 9.0 na segunda chamada
                }
            ]
        };

        // Act
        var result = _service.Calculate([regularActivity], criteria);

        // Assert
        result.ParcialGrade.Should().Be(9.0);
        result.Situation.Should().Be(GradeSituation.Approved);
    }

    [Fact]
    public void Should_Set_Failed_Frequency_When_Total_Hours_Are_Below_Required_Minimum()
    {
        // Arrange
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            TotalWorkingHours = 100.0,
            MinHoursPercentage = 75.0 // Exige 75h
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova com Nota 10, mas pouca presença",
                IsEvaluative = true,
                IsFrequency = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = 10.0,
                StudentWorkingHours = 50.0 // Apenas 50h de 75h necessárias
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(10.0);
        result.TotalWorkingHours.Should().Be(50.0);
        result.IsFrequencySufficient.Should().BeFalse();
        result.Situation.Should().Be(GradeSituation.FailedFrequency);
    }

    [Fact]
    public void Should_Set_Final_Exam_Pending_When_Student_Is_Eligible_And_AF_Not_Yet_Submitted()
    {
        // Arrange
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 3.0,
            HasFinalExamInOffering = true
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova Regular",
                IsEvaluative = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = 5.5 // Entre 3.0 e 6.9 -> Tem direito a AF
            },
            new()
            {
                ActivityId = 2,
                Name = "Prova Final (AF)",
                IsEvaluative = true,
                IsFinalExam = true,
                StudentGrade = null // Ainda não realizada
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(5.5);
        result.FinalExamGrade.Should().BeNull();
        result.FinalGrade.Should().Be(5.5);
        result.IsEligibleForFinalExam.Should().BeTrue();
        result.Situation.Should().Be(GradeSituation.FinalExamPending);
    }

    [Fact]
    public void Should_Set_Failed_Directly_When_Parcial_Grade_Does_Not_Reach_Min_Grade_For_AF()
    {
        // Arrange
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 4.0 // Precisa de pelo menos 4.0 para AF
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova Regular",
                IsEvaluative = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = 2.5 // Abaixo de 4.0 -> Reprovado direto sem direito a AF
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(2.5);
        result.IsEligibleForFinalExam.Should().BeFalse();
        result.Situation.Should().Be(GradeSituation.Failed);
    }

    [Fact]
    public void Should_Approve_In_Final_Exam_When_Average_Of_Parcial_And_AF_Reaches_Passing_Grade()
    {
        // Arrange: Média Parcial = 5.0, Prova Final = 7.0 -> Média Final = (5.0 + 7.0)/2 = 6.0 >= 5.0 (FinalExamPassingGrade)
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 3.0,
            FinalExamPassingGrade = 5.0
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova Regular",
                IsEvaluative = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = 5.0
            },
            new()
            {
                ActivityId = 2,
                Name = "Prova Final",
                IsEvaluative = true,
                IsFinalExam = true,
                StudentGrade = 7.0
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(5.0);
        result.FinalExamGrade.Should().Be(7.0);
        result.FinalGrade.Should().Be(6.0);
        result.Situation.Should().Be(GradeSituation.FinalExamApproved);
    }

    [Fact]
    public void Should_Fail_In_Final_Exam_When_Average_Of_Parcial_And_AF_Is_Below_Passing_Grade()
    {
        // Arrange: Média Parcial = 4.0, Prova Final = 4.0 -> Média Final = (4.0 + 4.0)/2 = 4.0 < 5.0 -> Reprovado
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            MinGradeToFinalExam = 3.0,
            FinalExamPassingGrade = 5.0
        };

        var activities = new List<GradingEvaluationInput>
        {
            new()
            {
                ActivityId = 1,
                Name = "Prova Regular",
                IsEvaluative = true,
                Weight = 1.0,
                FinalWeight = 100.0,
                StudentGrade = 4.0
            },
            new()
            {
                ActivityId = 2,
                Name = "Prova Final",
                IsEvaluative = true,
                IsFinalExam = true,
                StudentGrade = 4.0
            }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(4.0);
        result.FinalExamGrade.Should().Be(4.0);
        result.FinalGrade.Should().Be(4.0);
        result.Situation.Should().Be(GradeSituation.Failed);
    }

    [Fact]
    public void Should_Calculate_Simple_Sum_When_Configured()
    {
        // Arrange
        var criteria = new GradingCourseCriteria
        {
            PassingGrade = 7.0,
            CalculationType = CalculationType.SimpleSum
        };

        var activities = new List<GradingEvaluationInput>
        {
            new() { ActivityId = 1, Name = "Quiz 1", IsEvaluative = true, StudentGrade = 3.5 },
            new() { ActivityId = 2, Name = "Quiz 2", IsEvaluative = true, StudentGrade = 4.0 }
        };

        // Act
        var result = _service.Calculate(activities, criteria);

        // Assert
        result.ParcialGrade.Should().Be(7.5);
        result.Situation.Should().Be(GradeSituation.Approved);
    }
}
