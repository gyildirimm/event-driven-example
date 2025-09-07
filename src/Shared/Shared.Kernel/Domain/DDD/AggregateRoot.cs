namespace Shared.Kernel.Domain.DDD;

public abstract class AggregateRoot<TId> : Entity<TId>, IEntity<TId>
{
    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }
}

public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }
}