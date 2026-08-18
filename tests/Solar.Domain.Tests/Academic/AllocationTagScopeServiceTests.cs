using FluentAssertions;
using Solar.Domain.Academic;
using Solar.Domain.Entities;
using Xunit;

namespace Solar.Domain.Tests.Academic;

public class AllocationTagScopeServiceTests
{
    private readonly AllocationTagScopeService _scopeService = new();

    [Fact]
    public void GroupTag_Should_Inherit_Upper_Tags_From_Offer_And_Course()
    {
        // Arrange
        long groupTagId = 100;
        long offerTagId = 200;
        long courseTagId = 300;
        long unitTagId = 400;

        var relatedRows = new List<RelatedTaggable>
        {
            new()
            {
                GroupAtId = groupTagId,
                OfferAtId = offerTagId,
                CurriculumUnitAtId = unitTagId,
                CourseAtId = courseTagId
            }
        };

        // Act (Aluno na Turma quer ver conteúdos herdados da Oferta e do Curso)
        var relatedTags = _scopeService.GetRelatedTagIds(relatedRows, groupTagId, includeUpper: true, includeLower: false);

        // Assert
        relatedTags.Should().Contain([groupTagId, offerTagId, unitTagId, courseTagId]);
    }

    [Fact]
    public void OfferTag_Should_Propagate_Down_To_All_Child_Groups()
    {
        // Arrange: Oferta 200 possui duas turmas (Grupo A = 101, Grupo B = 102)
        long offerTagId = 200;
        long groupA_TagId = 101;
        long groupB_TagId = 102;

        var relatedRows = new List<RelatedTaggable>
        {
            new() { GroupAtId = groupA_TagId, OfferAtId = offerTagId },
            new() { GroupAtId = groupB_TagId, OfferAtId = offerTagId }
        };

        // Act (Professor na Oferta quer listar participantes de todas as turmas filhas)
        var relatedTags = _scopeService.GetRelatedTagIds(relatedRows, offerTagId, includeUpper: false, includeLower: true);

        // Assert
        relatedTags.Should().Contain([offerTagId, groupA_TagId, groupB_TagId]);
    }

    [Fact]
    public void UnmatchedTag_Should_Return_Only_Target_Tag()
    {
        // Arrange
        var relatedRows = new List<RelatedTaggable>();

        // Act
        var relatedTags = _scopeService.GetRelatedTagIds(relatedRows, 999);

        // Assert
        relatedTags.Should().ContainSingle().Which.Should().Be(999);
    }
}
