namespace Solar.Domain.Entities;

public class RelatedTaggable
{
    public long Id { get; set; }
    public bool? GroupStatus { get; set; }
    public long? GroupId { get; set; }
    public long? GroupAtId { get; set; }

    public long? OfferId { get; set; }
    public long? OfferAtId { get; set; }

    public long? SemesterId { get; set; }
    public long? CourseId { get; set; }
    public long? CourseAtId { get; set; }

    public long? CurriculumUnitId { get; set; }
    public long? CurriculumUnitAtId { get; set; }

    public long? CurriculumUnitTypeId { get; set; }
    public long? CurriculumUnitTypeAtId { get; set; }

    public long? OfferScheduleId { get; set; }

    public IReadOnlyList<long> GetRelatedTagIds(bool includeUpper = true, bool includeLower = true, string? scope = null)
    {
        var tags = new HashSet<long>();

        if (scope == null)
        {
            if (GroupAtId.HasValue) tags.Add(GroupAtId.Value);
            if (OfferAtId.HasValue) tags.Add(OfferAtId.Value);
            if (CurriculumUnitAtId.HasValue) tags.Add(CurriculumUnitAtId.Value);
            if (CourseAtId.HasValue) tags.Add(CourseAtId.Value);
            if (CurriculumUnitTypeAtId.HasValue) tags.Add(CurriculumUnitTypeAtId.Value);
        }
        else
        {
            switch (scope.ToLowerInvariant())
            {
                case "group":
                    if (GroupAtId.HasValue) tags.Add(GroupAtId.Value);
                    if (includeUpper)
                    {
                        if (OfferAtId.HasValue) tags.Add(OfferAtId.Value);
                        if (CurriculumUnitAtId.HasValue) tags.Add(CurriculumUnitAtId.Value);
                        if (CourseAtId.HasValue) tags.Add(CourseAtId.Value);
                        if (CurriculumUnitTypeAtId.HasValue) tags.Add(CurriculumUnitTypeAtId.Value);
                    }
                    break;
                case "offer":
                    if (OfferAtId.HasValue) tags.Add(OfferAtId.Value);
                    if (includeLower && GroupAtId.HasValue) tags.Add(GroupAtId.Value);
                    if (includeUpper)
                    {
                        if (CurriculumUnitAtId.HasValue) tags.Add(CurriculumUnitAtId.Value);
                        if (CourseAtId.HasValue) tags.Add(CourseAtId.Value);
                        if (CurriculumUnitTypeAtId.HasValue) tags.Add(CurriculumUnitTypeAtId.Value);
                    }
                    break;
                case "curriculum_unit":
                    if (CurriculumUnitAtId.HasValue) tags.Add(CurriculumUnitAtId.Value);
                    if (includeLower)
                    {
                        if (OfferAtId.HasValue) tags.Add(OfferAtId.Value);
                        if (GroupAtId.HasValue) tags.Add(GroupAtId.Value);
                    }
                    if (includeUpper && CurriculumUnitTypeAtId.HasValue) tags.Add(CurriculumUnitTypeAtId.Value);
                    break;
            }
        }

        return tags.ToList();
    }
}
