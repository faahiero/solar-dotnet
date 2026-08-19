namespace Solar.Application.Auth;

public record OAuthTokenRequest(
    string GrantType,
    string? Username,
    string? Password,
    string? RefreshToken,
    string? ClientId,
    string? ClientSecret
);

public record OAuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    string? Scope,
    UserSummaryDto? User
);

public record UserSummaryDto(
    long Id,
    string Username,
    string? Name,
    string? Email,
    string? Cpf,
    string Role
);

public interface IJwtTokenService
{
    OAuthTokenResponse GenerateToken(UserSummaryDto user, int expirationSeconds = 3600);
    OAuthTokenResponse? RefreshToken(string refreshToken, UserSummaryDto user, int expirationSeconds = 3600);
}
