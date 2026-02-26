namespace Clywell.Core.Data;

/// <summary>
/// Represents a unit of work that coordinates persistence across repositories,
/// analogous to how <c>DbContext</c> combines the Unit of Work and Repository patterns.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="Repository{TEntity, TId}"/> to obtain a repository for any entity type
/// (similar to <c>DbContext.Set&lt;T&gt;()</c>), then call <see cref="SaveChangesAsync"/>
/// to persist all pending changes atomically.
/// </para>
/// <para>
/// For explicit transaction control, use <see cref="BeginTransactionAsync"/> to
/// obtain an <see cref="IDataTransaction"/>.
/// </para>
/// <example>
/// <code>
/// public class CreateOrderHandler(IDataContext dataContext)
/// {
///     public async Task Handle(CreateOrderCommand command, CancellationToken ct)
///     {
///         var repo = dataContext.Repository&lt;Order, Guid&gt;();
///         await repo.AddAsync(new Order(command.CustomerId), ct);
///         await dataContext.SaveChangesAsync(ct);
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public interface IDataContext
{
    /// <summary>
    /// Gets a repository for the specified entity type, analogous to <c>DbContext.Set&lt;T&gt;()</c>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity's identifier type.</typeparam>
    /// <returns>A repository instance scoped to this unit of work.</returns>
    IRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class, IEntity<TId>
        where TId : notnull;

    /// <summary>
    /// Persists all pending changes to the underlying data store.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the data store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit database transaction for fine-grained control.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="IDataTransaction"/> that must be committed or rolled back.</returns>
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
