namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// EF Core implementation of <see cref="IReadRepository{TEntity, TId}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity's identifier type.</typeparam>
/// <remarks>
/// <para>
/// Provides read-only data access backed by an EF Core <see cref="DbContext"/>.
/// All queries are evaluated as no-tracking by default for optimal read performance.
/// </para>
/// <para>
/// Supports two query styles:
/// <list type="bullet">
///   <item><description>Identity lookup via <see cref="GetByIdAsync"/></description></item>
///   <item><description>Specification-based queries via <see cref="ListAsync(ISpecification{TEntity}, CancellationToken)"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EfReadRepository{TEntity, TId}"/> class.
/// </remarks>
/// <param name="dbContext">The EF Core database context.</param>
/// <param name="specificationEvaluator">The specification evaluator for translating specs to LINQ.</param>
public class EfReadRepository<TEntity, TId>(DbContext dbContext, ISpecificationEvaluator specificationEvaluator) : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ISpecificationEvaluator _specificationEvaluator = specificationEvaluator ?? throw new ArgumentNullException(nameof(specificationEvaluator));

    /// <summary>
    /// Initializes a new instance of the <see cref="EfReadRepository{TEntity, TId}"/> class
    /// using the default specification evaluator.
    /// </summary>
    /// <param name="dbContext">The EF Core database context.</param>
    public EfReadRepository(DbContext dbContext)
        : this(dbContext, EfSpecificationEvaluator.Default)
    {
    }

    /// <summary>Gets the underlying <see cref="DbContext"/> for derived classes.</summary>
    protected DbContext DbContext => _dbContext;

    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for the entity type.</summary>
    protected DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    /// <summary>Gets the specification evaluator.</summary>
    protected ISpecificationEvaluator SpecificationEvaluator => _specificationEvaluator;

    // ============================================================
    // Identity Queries
    // ============================================================

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken)
            .ConfigureAwait(false);
    }

    // ============================================================
    // Specification Queries
    // ============================================================

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification, evaluatePagingAndOrdering: false);
        return await query.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification, evaluatePagingAndOrdering: false);
        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    // ============================================================
    // Protected Helpers
    // ============================================================

    /// <summary>
    /// Applies a specification to the entity DbSet, returning a filtered queryable.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="evaluatePagingAndOrdering">
    /// Whether to include paging and ordering. Set to <see langword="false"/> for
    /// count/any operations that don't need ordering or paging.
    /// </param>
    /// <returns>The queryable with the specification applied.</returns>
    protected IQueryable<TEntity> ApplySpecification(
        ISpecification<TEntity> specification,
        bool evaluatePagingAndOrdering = true)
    {
        return _specificationEvaluator.Evaluate(DbSet.AsQueryable(), specification, evaluatePagingAndOrdering);
    }

    /// <summary>
    /// Applies a projection specification to the entity DbSet.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="specification">The projection specification to apply.</param>
    /// <returns>The projected queryable with the specification applied.</returns>
    protected IQueryable<TResult> ApplySpecification<TResult>(ISpecification<TEntity, TResult> specification)
    {
        return _specificationEvaluator.Evaluate(DbSet.AsQueryable(), specification);
    }
}
