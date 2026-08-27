namespace Solar.WebApi.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            if (!headers.ContainsKey("X-Content-Type-Options"))
                headers.Append("X-Content-Type-Options", "nosniff");

            if (!headers.ContainsKey("X-Frame-Options"))
                headers.Append("X-Frame-Options", "SAMEORIGIN");

            if (!headers.ContainsKey("Referrer-Policy"))
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            if (!headers.ContainsKey("X-XSS-Protection"))
                headers.Append("X-XSS-Protection", "1; mode=block");

            if (!headers.ContainsKey("Permissions-Policy"))
                headers.Append("Permissions-Policy", "camera=(), microphone=(self \"https://bbb.virtual.ufc.br\"), geolocation=()");

            if (!headers.ContainsKey("Strict-Transport-Security") && context.Request.IsHttps)
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            if (!headers.ContainsKey("Content-Security-Policy"))
                headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; img-src 'self' data: https:; frame-src 'self' https://bbb.virtual.ufc.br; connect-src 'self' ws: wss: https:;");

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
