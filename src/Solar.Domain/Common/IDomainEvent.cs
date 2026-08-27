namespace Solar.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
