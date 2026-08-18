using Solar.Domain.Entities;
using Solar.Domain.Enums;

namespace Solar.Domain.Assessments;

/// <summary>
/// Serviço de domínio para correção automática e consolidação de notas de Provas.
/// Mapeado a partir de app/models/exam.rb, app/models/question.rb e app/models/exam_response.rb.
/// </summary>
public class ExamScoringService
{
    /// <summary>
    /// Calcula a nota obtida em uma questão individual com base no tipo da questão e nas respostas do aluno.
    /// </summary>
    public double CalculateQuestionScore(
        Question question,
        double questionMaxScore,
        IReadOnlyDictionary<long, bool> studentSelections)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(studentSelections);

        if (question.QuestionItems.Count == 0 || questionMaxScore <= 0)
        {
            return 0.0;
        }

        return question.TypeQuestion switch
        {
            QuestionType.SingleChoice => GradeSingleChoice(question, questionMaxScore, studentSelections),
            QuestionType.Multiple => GradeMultipleClassic(question, questionMaxScore, studentSelections),
            QuestionType.MultipleWeighted => GradeMultipleWeighted(question, questionMaxScore, studentSelections),
            QuestionType.TrueFalse => GradeTrueFalse(question, questionMaxScore, studentSelections),
            _ => 0.0
        };
    }

    private static double GradeSingleChoice(
        Question question,
        double questionMaxScore,
        IReadOnlyDictionary<long, bool> studentSelections)
    {
        var correctItem = question.QuestionItems.FirstOrDefault(i => i.Value);
        if (correctItem == null) return 0.0;

        // Verifica se o aluno marcou como true exatamente o item correto
        if (studentSelections.TryGetValue(correctItem.Id, out bool selected) && selected)
        {
            // E não marcou nenhum outro
            int totalSelectedTrue = studentSelections.Count(s => s.Value);
            if (totalSelectedTrue == 1)
            {
                return questionMaxScore;
            }
        }

        return 0.0;
    }

    private static double GradeMultipleClassic(
        Question question,
        double questionMaxScore,
        IReadOnlyDictionary<long, bool> studentSelections)
    {
        var correctItems = question.QuestionItems.Where(i => i.Value).Select(i => i.Id).ToHashSet();
        var incorrectItems = question.QuestionItems.Where(i => !i.Value).Select(i => i.Id).ToHashSet();

        if (correctItems.Count == 0) return 0.0;

        int correctHits = 0;
        int falseHits = 0;

        foreach (var (itemId, selected) in studentSelections)
        {
            if (selected)
            {
                if (correctItems.Contains(itemId)) correctHits++;
                else if (incorrectItems.Contains(itemId)) falseHits++;
            }
        }

        double scoreFraction = (double)(correctHits - falseHits) / correctItems.Count;
        double finalScore = Math.Max(0.0, scoreFraction * questionMaxScore);
        return Math.Round(finalScore, 2);
    }

    private static double GradeMultipleWeighted(
        Question question,
        double questionMaxScore,
        IReadOnlyDictionary<long, bool> studentSelections)
    {
        var correctItems = question.QuestionItems.Where(i => i.Value).Select(i => i.Id).ToHashSet();
        var incorrectItems = question.QuestionItems.Where(i => !i.Value).Select(i => i.Id).ToHashSet();

        double fraction = 0.0;

        if (correctItems.Count > 0)
        {
            int correctHits = studentSelections.Count(s => s.Value && correctItems.Contains(s.Key));
            fraction += (double)correctHits / correctItems.Count;
        }

        if (incorrectItems.Count > 0)
        {
            int incorrectHits = studentSelections.Count(s => s.Value && incorrectItems.Contains(s.Key));
            fraction -= (double)incorrectHits / incorrectItems.Count;
        }

        double finalScore = Math.Max(0.0, fraction * questionMaxScore);
        return Math.Round(finalScore, 2);
    }

    private static double GradeTrueFalse(
        Question question,
        double questionMaxScore,
        IReadOnlyDictionary<long, bool> studentSelections)
    {
        int totalItems = question.QuestionItems.Count;
        if (totalItems == 0) return 0.0;

        int correctMatches = 0;

        foreach (var item in question.QuestionItems)
        {
            if (studentSelections.TryGetValue(item.Id, out bool studentAnswer))
            {
                if (studentAnswer == item.Value)
                {
                    correctMatches++;
                }
            }
        }

        double finalScore = ((double)correctMatches / totalItems) * questionMaxScore;
        return Math.Round(finalScore, 2);
    }

    /// <summary>
    /// Consolida as notas das tentativas do aluno de acordo com o critério configurado na prova (GREATER, AVERAGE, LAST).
    /// </summary>
    public double ConsolidateAttemptsGrade(
        IReadOnlyList<double> completedAttemptGrades,
        AttemptsCalculationCriterion criterion)
    {
        ArgumentNullException.ThrowIfNull(completedAttemptGrades);

        if (completedAttemptGrades.Count == 0)
        {
            return 0.0;
        }

        return criterion switch
        {
            AttemptsCalculationCriterion.Greater => completedAttemptGrades.Max(),
            AttemptsCalculationCriterion.Average => Math.Round(completedAttemptGrades.Average(), 2),
            AttemptsCalculationCriterion.Last => completedAttemptGrades.Last(),
            _ => completedAttemptGrades.Max()
        };
    }
}
