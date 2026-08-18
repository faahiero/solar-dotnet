using FluentAssertions;
using Solar.Domain.Academic;
using Xunit;

namespace Solar.Domain.Tests.Academic;

public class DisciplineImportServiceTests
{
    private readonly DisciplineImportService _importService = new();

    [Fact]
    public void ShiftDate_Should_Adjust_Date_By_Offer_Offset()
    {
        // Arrange (Origem: 01/03/2026 -> Destino: 01/08/2026 = 153 dias de deslocamento)
        var sourceOfferStart = new DateOnly(2026, 3, 1);
        var destOfferStart = new DateOnly(2026, 8, 1);
        var destOfferEnd = new DateOnly(2026, 12, 15);

        var originalActivityDate = new DateOnly(2026, 3, 15); // 14 dias após o início

        // Act
        var shiftedDate = _importService.ShiftDate(originalActivityDate, sourceOfferStart, destOfferStart, destOfferEnd);

        // Assert
        shiftedDate.Should().Be(new DateOnly(2026, 8, 15));
    }

    [Fact]
    public void ShiftDate_Should_Clamp_To_Dest_Offer_End_Date_When_Exceeding()
    {
        // Arrange
        var sourceOfferStart = new DateOnly(2026, 3, 1);
        var destOfferStart = new DateOnly(2026, 8, 1);
        var destOfferEnd = new DateOnly(2026, 11, 30); // Semestre mais curto

        var lateActivityDate = new DateOnly(2026, 7, 10); // Quase no final do semestre antigo

        // Act
        var shiftedDate = _importService.ShiftDate(lateActivityDate, sourceOfferStart, destOfferStart, destOfferEnd);

        // Assert
        shiftedDate.Should().Be(destOfferEnd); // Travado no último dia do novo semestre
    }

    [Fact]
    public void GeneratePreview_Should_Flag_Webconference_As_Unsupported_And_Detect_Conflicts()
    {
        // Arrange
        var sourceOfferStart = new DateOnly(2026, 3, 1);
        var sourceOfferEnd = new DateOnly(2026, 7, 15);
        var destOfferStart = new DateOnly(2026, 8, 1);
        var destOfferEnd = new DateOnly(2026, 12, 15);

        var items = new List<DisciplineImportItem>
        {
            new()
            {
                SourceAcademicAllocationId = 1,
                ToolType = "Exam",
                Name = "Prova Bimestral 1",
                OriginalStartDate = new DateOnly(2026, 4, 1),
                OriginalEndDate = new DateOnly(2026, 4, 1)
            },
            new()
            {
                SourceAcademicAllocationId = 2,
                ToolType = "Webconference", // Sessão BBB ao vivo não deve ser clonada
                Name = "Aula Inaugural ao Vivo",
                OriginalStartDate = new DateOnly(2026, 3, 5)
            }
        };

        var existingNamesInDest = new HashSet<string> { "prova bimestral 1" }; // Conflito de nome existente

        // Act
        var preview = _importService.GeneratePreview(
            items, sourceOfferStart, sourceOfferEnd, destOfferStart, destOfferEnd, existingNamesInDest);

        // Assert
        preview.Items.Should().HaveCount(2);

        // Prova
        preview.Items[0].IsSupported.Should().BeTrue();
        preview.Items[0].HasConflict.Should().BeTrue();
        preview.Items[0].ShiftedStartDate.Should().Be(new DateOnly(2026, 9, 1));

        // Webconferência
        preview.Items[1].IsSupported.Should().BeFalse();
        preview.Items[1].ShiftedStartDate.Should().BeNull();
    }

    [Fact]
    public void ValidateEvaluativeWeights_Should_Validate_Sum_Of_100()
    {
        // Assert
        _importService.ValidateEvaluativeWeights([40.0, 60.0]).Should().BeTrue();
        _importService.ValidateEvaluativeWeights([30.0, 30.0, 40.0]).Should().BeTrue();
        _importService.ValidateEvaluativeWeights([]).Should().BeTrue();

        _importService.ValidateEvaluativeWeights([40.0, 40.0]).Should().BeFalse(); // Soma 80 != 100
    }
}
