namespace Solar.Domain.Entities;

public class QuestionItem
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Value { get; set; } // Verdadeiro se for alternativa correta / gabarito
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Question? Question { get; set; }
}
