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
}
