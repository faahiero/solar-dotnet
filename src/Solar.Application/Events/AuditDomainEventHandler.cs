using Microsoft.Extensions.Logging;
using Solar.Application.Common;
using Solar.Domain.Events;

namespace Solar.Application.Events;

public class AuditDomainEventHandler : 
    IDomainEventHandler<GradeUpdatedDomainEvent>,
    IDomainEventHandler<UserBlacklistedDomainEvent>,
    IDomainEventHandler<ExamAttemptCompletedDomainEvent>
{
    private readonly ILogger<AuditDomainEventHandler> _logger;

    public AuditDomainEventHandler(ILogger<AuditDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(GradeUpdatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: Nota atualizada para usuário {UserId} na alocação {AllocationId}. Nova Média: {FinalGrade}, Situação: {Situation}",
            domainEvent.UserId, domainEvent.AllocationId, domainEvent.FinalGrade, domainEvent.Situation);
        return Task.CompletedTask;
    }

    public Task HandleAsync(UserBlacklistedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "DomainEvent: CPF {Cpf} inserido na blacklist. Motivo: {Reason}",
            domainEvent.Cpf, domainEvent.Reason);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ExamAttemptCompletedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: Tentativa de avaliação {AttemptId} concluída pelo usuário {UserId}. Nota: {Score}, Aprovado: {Passed}",
            domainEvent.AttemptId, domainEvent.UserId, domainEvent.Score, domainEvent.Passed);
        return Task.CompletedTask;
    }
}
