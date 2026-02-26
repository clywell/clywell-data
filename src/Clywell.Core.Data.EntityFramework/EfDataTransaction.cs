namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core implementation of <see cref="IDataTransaction"/>.
/// </summary>
/// <remarks>
/// Wraps an <see cref="IDbContextTransaction"/>. An uncommitted transaction
/// is rolled back when disposed.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EfDataTransaction"/> class.
/// </remarks>
/// <param name="transaction">The EF Core database transaction.</param>
internal sealed class EfDataTransaction(IDbContextTransaction transaction) : IDataTransaction
{
    private bool _disposed;

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }
    }
}
