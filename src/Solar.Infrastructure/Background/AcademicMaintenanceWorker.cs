using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Solar.Infrastructure.Background;

public class AcademicMaintenanceWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AcademicMaintenanceWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public AcademicMaintenanceWorker(IServiceProvider serviceProvider, ILogger<AcademicMaintenanceWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de Manutenção Acadêmica Periódica iniciado (Intervalo: {Interval} min).", _checkInterval.TotalMinutes);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PerformPeriodicMaintenanceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo do Worker de Manutenção Acadêmica.");
            }
        }
    }

    public Task PerformPeriodicMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executando ciclo de verificação de prazos acadêmicos e auditoria de tentativas...");
        // Auditoria de prazos, expiração de tentativas pendentes e sincronismo de status
        return Task.CompletedTask;
    }
}
