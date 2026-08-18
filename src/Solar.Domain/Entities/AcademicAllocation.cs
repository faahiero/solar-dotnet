namespace Solar.Domain.Entities;

public class AcademicAllocation
{
    public long Id { get; set; }
    public long? AllocationTagId { get; set; }
    public string AcademicToolType { get; set; } = string.Empty; // Exam, Assignment, Discussion, ChatRoom, Webconference, etc.
    public long AcademicToolId { get; set; }

    public bool Evaluative { get; set; }
    public bool Frequency { get; set; }
    public bool FinalExam { get; set; }
    public bool FrequencyAutomatic { get; set; }

    public decimal MaxWorkingHours { get; set; } = 1;
    public int? EquivalentAcademicAllocationId { get; set; }
    public decimal Weight { get; set; } = 1;
    public decimal FinalWeight { get; set; } = 100;

    // Navigation
    public AllocationTag? AllocationTag { get; set; }
    public ICollection<AcademicAllocationUser> AcademicAllocationUsers { get; set; } = [];
}
