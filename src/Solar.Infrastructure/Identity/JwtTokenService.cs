using System.Collections.Concurrent;
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

    public OAuthTokenResponse GenerateToken(UserSummaryDto user, int expirationSeconds = 3600, string? clientIp = null, string? userAgent = null)
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

        if (!string.IsNullOrEmpty(clientIp) || !string.IsNullOrEmpty(userAgent))
        {
            claims["dfp"] = ComputeFingerprint(clientIp, userAgent);
        }

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

    public OAuthTokenResponse? RefreshToken(string refreshToken, UserSummaryDto user, int expirationSeconds = 3600, string? clientIp = null, string? userAgent = null)
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

        return GenerateToken(user, expirationSeconds, clientIp, userAgent);
    }

    public bool ValidateDeviceFingerprint(string token, string? clientIp, string? userAgent)
    {
        try
        {
            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(token);
            if (jwt.TryGetClaim("dfp", out var claim))
            {
                var expected = ComputeFingerprint(clientIp, userAgent);
                return string.Equals(claim.Value, expected, StringComparison.OrdinalIgnoreCase);
            }
            return true; // Token legado ou sem amarração explícita é aceito
        }
        catch
        {
            return false;
        }
    }

    private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public void RevokeToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _revokedTokens.TryAdd(token, DateTime.UtcNow.AddDays(1));
        }
    }

    public bool IsTokenRevoked(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return true;
        if (_revokedTokens.TryGetValue(token, out var expiresAt))
        {
            if (DateTime.UtcNow > expiresAt)
            {
                _revokedTokens.TryRemove(token, out _);
                return false;
            }
            return true;
        }
        return false;
    }

    public static string ComputeFingerprint(string? clientIp, string? userAgent)
    {
        var raw = $"{clientIp?.Trim() ?? "unknown"}|{userAgent?.Trim() ?? "unknown"}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }
}
