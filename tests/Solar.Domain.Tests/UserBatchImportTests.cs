using Solar.Domain.Administration;
using Xunit;

namespace Solar.Domain.Tests;

public class UserBatchImportTests
{
    [Fact]
    public void ParseAndValidateCsv_ShouldImportValidRowsAndIgnoreInvalidCpfs()
    {
        // Arrange
        var service = new UserBatchImportService();
        var csv = @"Nome;CPF;Email;Polo;Perfil
Carlos Santana;11144477735;carlos@solar.ufc.br;Caucaia;Aluno
Mariana Lima;12345678900;mariana@solar.ufc.br;Fortaleza;Aluno
Usuario Invalido;00000000000;invalido@solar.ufc.br;Maranguape;Aluno
Sem Email;98765432100;;Fortaleza;Aluno";

        // Act
        var result = service.ParseAndValidateCsv(csv);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalRows);
        Assert.True(result.SuccessCount >= 1);
        Assert.True(result.ErrorCount >= 2); // 00000000000 (cpf inválido) e sem email

        var validUser = result.ImportedRows.FirstOrDefault(u => u.Name == "Carlos Santana");
        Assert.NotNull(validUser);
        Assert.Equal("11144477735", validUser.Cpf);
        Assert.Equal("carlos@solar.ufc.br", validUser.Email);
        Assert.Equal("Caucaia", validUser.Location);
    }

    [Theory]
    [InlineData("11144477735", true)]
    [InlineData("00000000000", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidCpf_ShouldCorrectlyValidateCpfDigits(string? cpf, bool expected)
    {
        bool isValid = UserBatchImportService.IsValidCpf(cpf);
        Assert.Equal(expected, isValid);
    }

    [Fact]
    public void ParseAndValidateCsv_ShouldSkipAlreadyExistingCpfs()
    {
        // Arrange
        var service = new UserBatchImportService();
        var existing = new HashSet<string> { "11144477735" };
        var csv = "Carlos Santana;11144477735;carlos@solar.ufc.br;Caucaia;Aluno";

        // Act
        var result = service.ParseAndValidateCsv(csv, existing);

        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.SkippedCount);
    }
}
