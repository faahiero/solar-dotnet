namespace Solar.Domain.Entities;

public class CurriculumUnitType
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool AllowsEnrollment { get; set; } = true;
    public string? IconName { get; set; } = "icon_type_free_course.png";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CurriculumUnit> CurriculumUnits { get; set; } = [];
}
