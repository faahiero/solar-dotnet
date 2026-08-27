using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Solar.Application.Common.Mediator.Behaviors;

public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("[Mediator] Executando comando/query: {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("[Mediator] {RequestName} concluído com sucesso em {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[Mediator] {RequestName} falhou após {ElapsedMilliseconds} ms: {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
