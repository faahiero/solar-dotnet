namespace Solar.Domain.Entities;

public class ExamQuestion
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long QuestionId { get; set; }
    public double Score { get; set; } = 1.0;
    public int? Order { get; set; }
    public bool Annulled { get; set; }
    public bool UseQuestion { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Exam? Exam { get; set; }
    public Question? Question { get; set; }
}
