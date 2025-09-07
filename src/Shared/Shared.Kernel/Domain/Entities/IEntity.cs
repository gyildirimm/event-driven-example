using Shared.Kernel.Domain.DDD;

namespace Shared.Kernel.Domain;

public interface IEntity<TId>
{
    TId Id { get; }
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
    
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void RemoveDomainEvent(IDomainEvent domainEvent);
    
    IReadOnlyList<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}