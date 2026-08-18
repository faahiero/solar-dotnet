namespace Solar.Domain.Entities;

public class Offer
{
    public long Id { get; set; }
    public long? CurriculumUnitId { get; set; }
    public long? CourseId { get; set; }
    public long? EnrollmentScheduleId { get; set; }
    public long? OfferScheduleId { get; set; }
    public long SemesterId { get; set; }
    public bool AllowPermanentChanges { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CurriculumUnit? CurriculumUnit { get; set; }
    public Course? Course { get; set; }
    public Semester? Semester { get; set; }
    public Schedule? OfferSchedule { get; set; }
    public Schedule? EnrollmentSchedule { get; set; }
    public ICollection<Group> Groups { get; set; } = [];
}
