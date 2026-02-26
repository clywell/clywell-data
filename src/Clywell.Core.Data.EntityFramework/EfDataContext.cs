using System.Collections.Concurrent;

namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core implementation of <see cref="IDataContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Wraps a <see cref="DbContext"/> to provide repository access and transactional persistence,
/// analogous to how <c>DbContext</c> exposes <c>DbSet&lt;T&gt;</c> via <c>Set&lt;T&gt;()</c>.
/// </para>
/// <para>
/// Repository instances are cached per entity type for the lifetime of the data context.
/// Register as a scoped service to share the same context across all repository access
/// within a single request/handler.
/// </para>
/// </remarks>
internal sealed class EfDataContext(DbContext dbContext, ISpecificationEvaluator specificationEvaluator) : IDataContext
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ISpecificationEvaluator _specificationEvaluator = specificationEvaluator ?? throw new ArgumentNullException(nameof(specificationEvaluator));
    private readonly ConcurrentDictionary<Type, object> _repositories = new();

    /// <inheritdoc />
    public IRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        return (IRepository<TEntity, TId>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new EfRepository<TEntity, TId>(_dbContext, _specificationEvaluator));
    }

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
