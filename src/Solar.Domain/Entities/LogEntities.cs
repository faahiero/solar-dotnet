namespace Solar.Domain.Entities;

public class LogAccess
{
    public long Id { get; set; }
    public int? LogType { get; set; } = 1;
    public long? UserId { get; set; }
    public long? AllocationTagId { get; set; }
    public string? Ip { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public AllocationTag? AllocationTag { get; set; }
}

public class LogAction
{
    public long Id { get; set; }
    public int LogType { get; set; }
    public long UserId { get; set; }
    public long? AcademicAllocationId { get; set; }
    public long? AcademicAllocationUserId { get; set; }
    public long? AllocationTagId { get; set; }
    public string? Description { get; set; }
    public string? Ip { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public AcademicAllocation? AcademicAllocation { get; set; }
    public AcademicAllocationUser? AcademicAllocationUser { get; set; }
    public AllocationTag? AllocationTag { get; set; }
}
