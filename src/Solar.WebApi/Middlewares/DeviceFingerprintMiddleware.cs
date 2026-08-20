using Solar.Application.Auth;

namespace Solar.WebApi.Middlewares;

public class DeviceFingerprintMiddleware
{
    private readonly RequestDelegate _next;

    public DeviceFingerprintMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        string? token = null;
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader["Bearer ".Length..].Trim();
        }
        else if (context.Request.Cookies.TryGetValue("solar_access_token", out var cookieToken))
        {
            token = cookieToken;
        }

        if (!string.IsNullOrEmpty(token))
        {
            if (jwtTokenService.IsTokenRevoked(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "TOKEN_REVOKED",
                    message = "Credencial revogada após encerramento de sessão."
                });
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgent = context.Request.Headers.UserAgent.ToString();

            if (!jwtTokenService.ValidateDeviceFingerprint(token, clientIp, userAgent))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "DEVICE_FINGERPRINT_MISMATCH",
                    message = "Acesso bloqueado: Este token foi emitido para outro dispositivo/endereço de rede por motivos de segurança."
                });
                return;
            }
        }

        await _next(context);
    }
}
