namespace Solar.Domain.Entities;

public class Course
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    public double? PassingGrade { get; set; }
    public double? MinGradeToFinalExam { get; set; }
    public double? MinFinalExamGrade { get; set; }
    public double? FinalExamPassingGrade { get; set; }
    public int? MinHours { get; set; }

    public bool HasExamHeader { get; set; }
    public string? HeaderExam { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Offer> Offers { get; set; } = [];
}
