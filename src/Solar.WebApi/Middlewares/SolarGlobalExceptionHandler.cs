using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Solar.WebApi.Middlewares;

public class SolarGlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<SolarGlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public SolarGlobalExceptionHandler(ILogger<SolarGlobalExceptionHandler> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exceção não tratada capturada pelo SolarGlobalExceptionHandler: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro Interno no Servidor Solar LMS",
            Detail = _environment.IsDevelopment() || _environment.IsEnvironment("Testing")
                ? exception.Message
                : "Ocorreu uma falha inesperada ao processar sua requisição. O incidente foi registrado para auditoria.",
            Instance = httpContext.Request.Path,
            Type = "https://solar.virtual.ufc.br/errors/internal-server-error"
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
