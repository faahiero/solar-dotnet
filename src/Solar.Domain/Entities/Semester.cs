namespace Solar.Domain.Entities;

public class Semester
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long OfferScheduleId { get; set; }
    public long EnrollmentScheduleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Schedule? OfferSchedule { get; set; }
    public Schedule? EnrollmentSchedule { get; set; }
    public ICollection<Offer> Offers { get; set; } = [];
}
