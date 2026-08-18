using FluentAssertions;
using Solar.Domain.Assessments;
using Solar.Domain.Entities;
using Solar.Domain.Enums;
using Xunit;

namespace Solar.Domain.Tests.Assessments;

public class ExamScoringServiceTests
{
    private readonly ExamScoringService _scoringService = new();

    [Fact]
    public void SingleChoice_Should_Return_Full_Score_When_Correct_Item_Is_Selected()
    {
        // Arrange
        var question = new Question
        {
            Id = 1,
            TypeQuestion = QuestionType.SingleChoice,
            QuestionItems =
            [
                new() { Id = 10, Description = "Opção A", Value = false },
                new() { Id = 11, Description = "Opção B (Correta)", Value = true },
                new() { Id = 12, Description = "Opção C", Value = false }
            ]
        };

        var studentAnswers = new Dictionary<long, bool>
        {
            { 10, false },
            { 11, true },
            { 12, false }
        };

        // Act
        var score = _scoringService.CalculateQuestionScore(question, 2.5, studentAnswers);

        // Assert
        score.Should().Be(2.5);
    }

    [Fact]
    public void SingleChoice_Should_Return_Zero_When_Wrong_Item_Is_Selected()
    {
        // Arrange
        var question = new Question
        {
            Id = 1,
            TypeQuestion = QuestionType.SingleChoice,
            QuestionItems =
            [
                new() { Id = 10, Description = "Opção A", Value = false },
                new() { Id = 11, Description = "Opção B (Correta)", Value = true }
            ]
        };

        var studentAnswers = new Dictionary<long, bool>
        {
            { 10, true },
            { 11, false }
        };

        // Act
        var score = _scoringService.CalculateQuestionScore(question, 2.0, studentAnswers);

        // Assert
        score.Should().Be(0.0);
    }

    [Fact]
    public void MultipleClassic_Should_Apply_Penalty_For_Incorrect_Selections()
    {
        // Arrange: 2 corretas (10, 11) e 2 incorretas (12, 13)
        // Aluno marcou 1 correta (10) e 1 incorreta (12)
        // Fração = (1 acerto - 1 erro) / 2 corretas = 0 / 2 = 0
        var question = new Question
        {
            Id = 2,
            TypeQuestion = QuestionType.Multiple,
            QuestionItems =
            [
                new() { Id = 10, Value = true },
                new() { Id = 11, Value = true },
                new() { Id = 12, Value = false },
                new() { Id = 13, Value = false }
            ]
        };

        var studentAnswers = new Dictionary<long, bool>
        {
            { 10, true },
            { 12, true }
        };

        // Act
        var score = _scoringService.CalculateQuestionScore(question, 4.0, studentAnswers);

        // Assert
        score.Should().Be(0.0);
    }

    [Fact]
    public void MultipleWeighted_Should_Calculate_Partial_Correctly()
    {
        // Arrange: 2 corretas (10, 11) e 2 incorretas (12, 13)
        // Aluno marcou 2 corretas (10, 11) e nenhuma incorreta
        // Fração = 2/2 = 1.0 -> Nota = 1.0 * 5.0 = 5.0
        var question = new Question
        {
            Id = 3,
            TypeQuestion = QuestionType.MultipleWeighted,
            QuestionItems =
            [
                new() { Id = 10, Value = true },
                new() { Id = 11, Value = true },
                new() { Id = 12, Value = false },
                new() { Id = 13, Value = false }
            ]
        };

        var studentAnswers = new Dictionary<long, bool>
        {
            { 10, true },
            { 11, true }
        };

        // Act
        var score = _scoringService.CalculateQuestionScore(question, 5.0, studentAnswers);

        // Assert
        score.Should().Be(5.0);
    }

    [Fact]
    public void TrueFalse_Should_Score_Proportionally_To_Correctly_Judged_Items()
    {
        // Arrange: 4 itens (V, F, V, F). Aluno acertou 3 de 4.
        // Nota = (3 / 4) * 2.0 = 1.5
        var question = new Question
        {
            Id = 4,
            TypeQuestion = QuestionType.TrueFalse,
            QuestionItems =
            [
                new() { Id = 10, Value = true },
                new() { Id = 11, Value = false },
                new() { Id = 12, Value = true },
                new() { Id = 13, Value = false }
            ]
        };

        var studentAnswers = new Dictionary<long, bool>
        {
            { 10, true },  // Acertou (V)
            { 11, false }, // Acertou (F)
            { 12, true },  // Acertou (V)
            { 13, true }   // Errou (Marcou V, era F)
        };

        // Act
        var score = _scoringService.CalculateQuestionScore(question, 2.0, studentAnswers);

        // Assert
        score.Should().Be(1.5);
    }

    [Theory]
    [InlineData(AttemptsCalculationCriterion.Greater, 9.0)]
    [InlineData(AttemptsCalculationCriterion.Average, 7.0)]
    [InlineData(AttemptsCalculationCriterion.Last, 8.0)]
    public void ConsolidateAttemptsGrade_Should_Apply_Configured_Criterion(
        AttemptsCalculationCriterion criterion,
        double expectedGrade)
    {
        // Arrange (Tentativas com notas: 4.0, 9.0, 8.0)
        // Média = (4.0 + 9.0 + 8.0) / 3 = 21 / 3 = 7.0
        var attemptGrades = new List<double> { 4.0, 9.0, 8.0 };

        // Act
        var consolidated = _scoringService.ConsolidateAttemptsGrade(attemptGrades, criterion);

        // Assert
        consolidated.Should().Be(expectedGrade);
    }
}
