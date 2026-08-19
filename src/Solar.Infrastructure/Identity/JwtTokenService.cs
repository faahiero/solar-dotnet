using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Solar.Application.Auth;

namespace Solar.Infrastructure.Identity;

public class JwtTokenConfig
{
    public string SecretKey { get; set; } = "SolarLmsSecretKeyUfcVirtualEnterprise2026SecureJwtTokenSignatureKey123!";
    public string Issuer { get; set; } = "SolarLms.UfcVirtual";
    public string Audience { get; set; } = "SolarLms.Clients";
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtTokenConfig _config;
    private static readonly Dictionary<string, (long UserId, DateTime ExpiresAt)> _activeRefreshTokens = new();

    public JwtTokenService(JwtTokenConfig? config = null)
    {
        _config = config ?? new JwtTokenConfig();
    }

    public OAuthTokenResponse GenerateToken(UserSummaryDto user, int expirationSeconds = 3600)
    {
        var handler = new JsonWebTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.UniqueName] = user.Username,
            [JwtRegisteredClaimNames.Name] = user.Name ?? user.Username,
            [JwtRegisteredClaimNames.Email] = user.Email ?? "",
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            ["role"] = user.Role,
            ["cpf"] = user.Cpf ?? ""
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config.Issuer,
            Audience = _config.Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddSeconds(expirationSeconds),
            SigningCredentials = credentials
        };

        var accessToken = handler.CreateToken(descriptor);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(32);
        var refreshToken = Convert.ToHexString(refreshTokenBytes).ToLowerInvariant();

        lock (_activeRefreshTokens)
        {
            _activeRefreshTokens[refreshToken] = (user.Id, DateTime.UtcNow.AddDays(7));
        }

        return new OAuthTokenResponse(
            AccessToken: accessToken,
            TokenType: "Bearer",
            ExpiresIn: expirationSeconds,
            RefreshToken: refreshToken,
            Scope: "read write",
            User: user
        );
    }

    public OAuthTokenResponse? RefreshToken(string refreshToken, UserSummaryDto user, int expirationSeconds = 3600)
    {
        lock (_activeRefreshTokens)
        {
            if (!_activeRefreshTokens.TryGetValue(refreshToken, out var tokenData))
            {
                return null;
            }

            if (DateTime.UtcNow > tokenData.ExpiresAt || tokenData.UserId != user.Id)
            {
                _activeRefreshTokens.Remove(refreshToken);
                return null;
            }

            _activeRefreshTokens.Remove(refreshToken);
        }

        return GenerateToken(user, expirationSeconds);
    }
}
