using Clywell.Primitives;

namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// Dispatches collected domain events to their registered handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all provided domain events to their corresponding handlers.
    /// </summary>
    /// <param name="domainEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DispatchAsync(
        IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}