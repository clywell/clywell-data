using Clywell.Primitives;
namespace Clywell.Core.Data;

/// <summary>
/// Abstract base class for domain entities that can raise domain events.
/// Inherit from this instead of implementing <see cref="IEntity{TId}"/> directly.
/// </summary>
/// <typeparam name="TId">The type of the entity's primary key.</typeparam>
public abstract class Entity<TId> : IEntity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public abstract TId Id { get; protected set; }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Raises a domain event by adding it to the entity's event collection.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}