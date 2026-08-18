using Solar.Domain.Entities;

namespace Solar.Domain.Academic;

/// <summary>
/// Serviço de resolução de escopo e herança hierárquica de turmas e tags (AllocationTags / RelatedTaggables).
/// Substitui as views SQL relacionais (vw_at_related_*) e o código legado de RelatedTaggable.rb.
/// </summary>
public class AllocationTagScopeService
{
    /// <summary>
    /// Calcula os IDs de tags vinculadas direta ou indiretamente (árvore superior e inferior).
    /// </summary>
    public IReadOnlyList<long> GetRelatedTagIds(
        IEnumerable<RelatedTaggable> taggables,
        long targetTagId,
        bool includeUpper = true,
        bool includeLower = true)
    {
        ArgumentNullException.ThrowIfNull(taggables);

        var matchingRows = taggables.Where(r =>
            r.GroupAtId == targetTagId ||
            r.OfferAtId == targetTagId ||
            r.CurriculumUnitAtId == targetTagId ||
            r.CourseAtId == targetTagId ||
            r.CurriculumUnitTypeAtId == targetTagId
        ).ToList();

        if (matchingRows.Count == 0)
        {
            return [targetTagId];
        }

        var result = new HashSet<long> { targetTagId };

        foreach (var row in matchingRows)
        {
            // Determinar o nível da tag de origem
            string? scope = null;
            if (row.GroupAtId == targetTagId) scope = "group";
            else if (row.OfferAtId == targetTagId) scope = "offer";
            else if (row.CurriculumUnitAtId == targetTagId) scope = "curriculum_unit";

            var rowTagIds = row.GetRelatedTagIds(includeUpper, includeLower, scope);
            foreach (var id in rowTagIds)
            {
                result.Add(id);
            }
        }

        return result.ToList();
    }
}
