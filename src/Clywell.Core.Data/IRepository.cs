namespace Clywell.Core.Data;

/// <summary>
/// Provides full CRUD data access operations for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type. Must implement <see cref="IEntity{TId}"/>.</typeparam>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
/// <remarks>
/// <para>
/// Extends <see cref="IReadRepository{TEntity, TId}"/> with write operations (Add, Update, Remove).
/// Changes are not persisted until <see cref="IDataContext.SaveChangesAsync"/> is called.
/// </para>
/// <para>
/// This interface is intended to be referenced from the Application layer. The Infrastructure
/// layer provides the EF Core implementation via <c>EfRepository&lt;TEntity, TId&gt;</c>.
/// </para>
/// </remarks>
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    // ============================================================
    // Write Operations
    // ============================================================

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The added entity (with any generated values populated).</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entity as modified in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marks multiple entities as modified in the repository.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Marks an entity for removal from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Marks multiple entities for removal from the repository.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    void RemoveRange(IEnumerable<TEntity> entities);
}
