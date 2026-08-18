using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class Question
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Enunciation { get; set; } = string.Empty;
    public QuestionType TypeQuestion { get; set; } = QuestionType.SingleChoice;
    public bool Status { get; set; } = true;
    public bool Privacy { get; set; }
    public int? QuestionTextId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<QuestionItem> QuestionItems { get; set; } = [];
    public ICollection<ExamQuestion> ExamQuestions { get; set; } = [];
}
