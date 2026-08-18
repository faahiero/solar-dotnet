namespace Solar.Domain.Entities;

public class ExamResponse
{
    public long Id { get; set; }
    public long ExamUserAttemptId { get; set; }
    public long QuestionId { get; set; }
    public double? Grade { get; set; }
    public int? Duration { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ExamUserAttempt? ExamUserAttempt { get; set; }
    public Question? Question { get; set; }
    public ICollection<ExamResponseQuestionItem> SelectedItems { get; set; } = [];
}
