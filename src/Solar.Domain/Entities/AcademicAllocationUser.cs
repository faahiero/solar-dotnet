using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class AcademicAllocationUser
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public long AcademicAllocationId { get; set; }
    public int? GroupAssignmentId { get; set; }

    public double? Grade { get; set; }
    public decimal? WorkingHours { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Empty;
    public bool NewAfterEvaluation { get; set; }
    public bool EvaluatedByResponsible { get; set; }
    public bool Ignore { get; set; }
    public int CommentsCount { get; set; }
    public int ScheduleEventFilesCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public AcademicAllocation? AcademicAllocation { get; set; }
}
