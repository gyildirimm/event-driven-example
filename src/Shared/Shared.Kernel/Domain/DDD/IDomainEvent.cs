namespace Shared.Kernel.Domain.DDD;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

public abstract class EntityDomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}