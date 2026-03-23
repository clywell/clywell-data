using Clywell.Primitives;

namespace Clywell.Core.Data;

/// <summary>
/// Exposes collected domain events on a domain entity.
/// Used by interceptors to dispatch events without knowing the entity's key type.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the domain events raised since the last clear.</summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears all collected domain events after they have been dispatched.</summary>
    void ClearDomainEvents();
}