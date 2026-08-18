using Solar.Domain.Enums;

namespace Solar.Domain.Entities;

public class AllocationTag
{
    public long Id { get; set; }
    public int? GroupId { get; set; }
    public int? OfferId { get; set; }
    public int? CurriculumUnitId { get; set; }
    public int? CourseId { get; set; }
    public long? CurriculumUnitTypeId { get; set; }

    public bool SettedSituation { get; set; }
    public DateOnly? SituationDate { get; set; }
    public int? SituationDateAcId { get; set; }
    public CalculationType CalculationType { get; set; } = CalculationType.WeightedFormula;

    // Navigation
    public ICollection<Allocation> Allocations { get; set; } = [];
    public ICollection<AcademicAllocation> AcademicAllocations { get; set; } = [];
}
