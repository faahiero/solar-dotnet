using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Solar.Infrastructure.Background;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Solar LMS Queued Background Service iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Encerrando normalmente
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a execução de tarefa em segundo plano no Solar LMS.");
            }
        }

        _logger.LogInformation("Solar LMS Queued Background Service encerrado.");
    }
}
