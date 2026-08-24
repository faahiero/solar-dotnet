using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Solar.Domain.Enums;
using Solar.Infrastructure.Persistence;

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

    public async Task PerformPeriodicMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executando ciclo de verificação de prazos acadêmicos e auditoria periódica...");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();

        // 1. Auditoria de tentativas de avaliação pendentes
        var pendingExams = await db.ExamUserAttempts
            .Where(e => !e.Complete && e.Start.HasValue && e.Start.Value < DateTime.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);

        if (pendingExams.Count > 0)
        {
            foreach (var attempt in pendingExams)
            {
                attempt.Complete = true;
                attempt.End = attempt.Start!.Value.AddHours(2);
                attempt.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Auditoria concluiu {Count} tentativas de provas expiradas automaticamente.", pendingExams.Count);
        }

        // 2. Estatística de alocações ativas
        var activeAllocationsCount = await db.Allocations
            .CountAsync(a => a.Status == AllocationStatus.Activated, cancellationToken);

        _logger.LogInformation("Manutenção concluída. Total de matrículas ativas auditadas: {Count}", activeAllocationsCount);
    }
}
