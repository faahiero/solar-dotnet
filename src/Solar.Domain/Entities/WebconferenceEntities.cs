namespace Solar.Domain.Entities;

public class Webconference
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime InitialTime { get; set; }
    public int Duration { get; set; }
    public bool IsRecorded { get; set; } = true;
    public bool SharedBetweenGroups { get; set; } = false;
    public string? OriginMeetingId { get; set; }
    public int? Server { get; set; }
    public bool Downloadable { get; set; } = false;
    public bool AnyoneCanSubmit { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

public class AssignmentWebconference
{
    public long Id { get; set; }
    public long AcademicAllocationUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime InitialTime { get; set; }
    public int Duration { get; set; }
    public bool IsRecorded { get; set; } = true;
    public string? OriginMeetingId { get; set; }
    public bool Final { get; set; } = false;
    public int? Server { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
}
