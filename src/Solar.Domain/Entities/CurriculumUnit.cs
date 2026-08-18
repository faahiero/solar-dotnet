namespace Solar.Domain.Entities;

public class CurriculumUnit
{
    public long Id { get; set; }
    public long CurriculumUnitTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Resume { get; set; }
    public string? Syllabus { get; set; }
    public string? Objectives { get; set; }
    public string? Prerequisites { get; set; }
    public double? Credits { get; set; }
    public int? WorkingHours { get; set; }
    public int? MinHours { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CurriculumUnitType? CurriculumUnitType { get; set; }
    public ICollection<Offer> Offers { get; set; } = [];
}
