namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// Wraps a <see cref="DbContext"/> to provide transactional persistence.
/// Register as a scoped service to share the same context across repositories
/// within a single request/handler.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EfUnitOfWork"/> class.
/// </remarks>
/// <param name="dbContext">The EF Core database context.</param>
internal sealed class EfUnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        return new EfDataTransaction(transaction);
    }
}
