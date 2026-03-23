using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core interceptor that dispatches domain events after <c>SaveChangesAsync</c> succeeds.
/// Registers as a singleton; resolves <see cref="IDomainEventDispatcher"/> per invocation via
/// <see cref="IServiceScopeFactory"/> to avoid captive-dependency issues.
/// </summary>
/// <remarks>
/// This interceptor performs in-process (non-transactional) dispatch only.
/// For the transactional outbox pattern use <c>OutboxSaveChangesInterceptor</c>
/// from <c>Clywell.Core.Messaging</c>.
/// </remarks>
public sealed class DomainEventDispatchInterceptor(IServiceScopeFactory scopeFactory)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var entitiesWithEvents = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        using var scope = scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        foreach (var entity in entitiesWithEvents)
        {
            await dispatcher.DispatchAsync(entity.DomainEvents, cancellationToken);
            entity.ClearDomainEvents();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}