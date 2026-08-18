namespace Solar.Domain.Entities;

public class ExamUserAttempt
{
    public long Id { get; set; }
    public long AcademicAllocationUserId { get; set; }
    public double? Grade { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public bool Complete { get; set; }
    public int TotalTime { get; set; } // Segundos

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
    public ICollection<ExamResponse> Responses { get; set; } = [];
}
