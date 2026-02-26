namespace Clywell.Core.Data;

/// <summary>
/// Represents a unit of work for coordinating persistence of changes
/// across one or more repositories.
/// </summary>
/// <remarks>
/// <para>
/// The unit of work pattern ensures that all repository changes within a single
/// business operation are committed atomically via <see cref="SaveChangesAsync"/>.
/// </para>
/// <para>
/// For explicit transaction control, use <see cref="BeginTransactionAsync"/> to
/// obtain an <see cref="IDataTransaction"/>. For most use cases, the CQRS pipeline
/// behavior will manage transactions automatically.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
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
    /// <remarks>
    /// Prefer using the CQRS pipeline transaction behavior over explicit transactions
    /// unless you need cross-repository coordination within a single handler.
    /// </remarks>
    Task<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
