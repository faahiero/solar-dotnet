using Solar.Domain.Common;
using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class Allocation : IAuditableEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? AllocationTagId { get; set; }
    public int ProfileId { get; set; }
    public AllocationStatus Status { get; set; } = AllocationStatus.Pending;

    public double? ParcialGrade { get; set; }
    public double? FinalExamGrade { get; set; }
    public double? FinalGrade { get; set; }
    public decimal? WorkingHours { get; set; }
    public GradeSituation? GradeSituation { get; set; }

    public long? UpdatedByUserId { get; set; }
    public int? OriginGroupId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Profile? Profile { get; set; }
    public AllocationTag? AllocationTag { get; set; }
}
