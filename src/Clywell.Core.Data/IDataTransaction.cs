namespace Clywell.Core.Data;

/// <summary>
/// Represents an abstraction over a database transaction.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts away the specific transaction implementation
/// (e.g., <c>IDbContextTransaction</c> in EF Core) so the Application layer
/// can manage transactions without referencing infrastructure packages.
/// </para>
/// <para>
/// Implements <see cref="IAsyncDisposable"/> — use <c>await using</c> to ensure
/// the transaction is disposed even if an exception occurs. An uncommitted
/// transaction will be rolled back on disposal.
/// </para>
/// </remarks>
public interface IDataTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction, persisting all changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, discarding all changes.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
