namespace Solar.Domain.Entities;

public class ExamResponseQuestionItem
{
    public long Id { get; set; }
    public long ExamResponseId { get; set; }
    public long QuestionItemId { get; set; }
    public bool? Value { get; set; } // Marcação do aluno (marcou true ou false)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExamResponse? ExamResponse { get; set; }
    public QuestionItem? QuestionItem { get; set; }
}
