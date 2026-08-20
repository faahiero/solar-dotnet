using Solar.Application.Auth;
using Solar.Infrastructure.Identity;
using Xunit;

namespace Solar.WebApi.Tests;

public class OAuthTokenTests
{
    [Fact]
    public void GenerateToken_ShouldReturnValidBearerTokenAndClaims()
    {
        // Arrange
        var service = new JwtTokenService();
        var user = new UserSummaryDto(
            Id: 42,
            Username: "prof.fabricio",
            Name: "Prof. Fabrício Silva",
            Email: "fabricio@virtual.ufc.br",
            Cpf: "99988877766",
            Role: "Professor"
        );

        // Act
        var tokenResponse = service.GenerateToken(user, 1800);

        // Assert
        Assert.NotNull(tokenResponse);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.Equal(1800, tokenResponse.ExpiresIn);
        Assert.NotEmpty(tokenResponse.AccessToken);
        Assert.NotEmpty(tokenResponse.RefreshToken);
        Assert.Equal(42, tokenResponse.User?.Id);
        Assert.Equal("Professor", tokenResponse.User?.Role);
    }

    [Fact]
    public void RefreshToken_WithValidToken_ShouldRotateAndGenerateNewToken()
    {
        // Arrange
        var service = new JwtTokenService();
        var user = new UserSummaryDto(
            Id: 1,
            Username: "aluno1",
            Name: "Aluno 1",
            Email: "aluno1@solar.ufc.br",
            Cpf: "12345678900",
            Role: "Aluno"
        );

        var original = service.GenerateToken(user, 3600);

        // Act
        var refreshed = service.RefreshToken(original.RefreshToken, user, 3600);

        // Assert
        Assert.NotNull(refreshed);
        Assert.NotEmpty(refreshed.AccessToken);
        Assert.NotEqual(original.AccessToken, refreshed.AccessToken);
    }

    [Fact]
    public void GenerateToken_WithDeviceFingerprint_ShouldValidateMatchingDevice_AndRejectMismatch()
    {
        // Arrange
        var service = new JwtTokenService();
        var user = new UserSummaryDto(
            Id: 10,
            Username: "tutor1",
            Name: "Tutor Presencial",
            Email: "tutor@solar.ufc.br",
            Cpf: "11122233344",
            Role: "Tutor"
        );

        string originalIp = "192.168.1.50";
        string originalUserAgent = "Mozilla/5.0 (X11; Linux x86_64) Chrome/120.0.0.0";

        // Act: Gera token para dispositivo original
        var tokenResponse = service.GenerateToken(user, 3600, clientIp: originalIp, userAgent: originalUserAgent);

        // Validação 1: Mesmo dispositivo e IP -> Válido
        bool isSameDeviceValid = service.ValidateDeviceFingerprint(tokenResponse.AccessToken, originalIp, originalUserAgent);

        // Validação 2: Dispositivo roubado tentando usar em outro IP/UserAgent -> Inválido (Rejeitado)
        bool isAttackerIpValid = service.ValidateDeviceFingerprint(tokenResponse.AccessToken, "200.150.10.2", originalUserAgent);
        bool isAttackerBrowserValid = service.ValidateDeviceFingerprint(tokenResponse.AccessToken, originalIp, "curl/7.88.1");

        // Assert
        Assert.True(isSameDeviceValid, "Acesso do mesmo dispositivo/IP deve ser aceito");
        Assert.False(isAttackerIpValid, "Token roubado usado em outro IP deve ser rejeitado");
        Assert.False(isAttackerBrowserValid, "Token roubado usado em outro navegador/ferramenta deve ser rejeitado");
    }
}
