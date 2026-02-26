namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TEntity, TId}"/> providing full CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity's identifier type.</typeparam>
/// <remarks>
/// <para>
/// Extends <see cref="EfReadRepository{TEntity, TId}"/> with write operations backed by
/// the EF Core change tracker. Changes are not persisted until
/// <see cref="IDataContext.SaveChangesAsync"/> is called.
/// </para>
/// </remarks>
public class EfRepository<TEntity, TId> : EfReadRepository<TEntity, TId>, IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    /// <param name="specificationEvaluator">The specification evaluator for translating specs to LINQ.</param>
    public EfRepository(DbContext dbContext, ISpecificationEvaluator specificationEvaluator)
        : base(dbContext, specificationEvaluator)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity, TId}"/> class
    /// using the default specification evaluator.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public EfRepository(DbContext dbContext)
        : base(dbContext, EfSpecificationEvaluator.Default)
    {
    }

    // ============================================================
    // Overrides — tracked for write repos
    // ============================================================

    /// <inheritdoc />
    /// <remarks>
    /// Returns a tracked entity (no detach), since entities retrieved via
    /// a write repository are expected to be modified and saved.
    /// </remarks>
    public override async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }

    // ============================================================
    // Write Operations
    // ============================================================

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entry.Entity;
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }
}
