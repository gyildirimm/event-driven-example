using Shared.Kernel.Domain.DDD;

namespace Shared.Kernel.Domain;

public abstract partial class Entity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    protected Entity(TId id) : base()
    {
        Id = id;
    }
    
    protected void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id!.Equals(default(TId)) || other.Id!.Equals(default(TId)))
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Partial class to handle domain events
/// </summary>
/// <typeparam name="TId"></typeparam>
public abstract partial class Entity<TId> : IEntity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public IReadOnlyList<IDomainEvent> GetDomainEvents()
    {
        return (IReadOnlyList<IDomainEvent>)DomainEvents;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}


public abstract class Entity : Entity<Guid>
{
    protected Entity() : base()
    {
        Id = Guid.NewGuid();
    }

    protected Entity(Guid id) : base(id)
    {
    }
}