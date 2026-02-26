namespace Clywell.Core.Data;

/// <summary>
/// Provides read-only data access operations for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type. Must implement <see cref="IEntity{TId}"/>.</typeparam>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
/// <remarks>
/// <para>
/// This interface is intended to be referenced from the Application layer without
/// introducing any dependency on Entity Framework Core. All queries are expressed
/// through <see cref="ISpecification{T}"/> objects, ensuring query logic is
/// encapsulated, testable, and provider-independent.
/// </para>
/// </remarks>
public interface IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    // ============================================================
    // Identity Queries
    // ============================================================

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity if found; otherwise <see langword="null"/>.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    // ============================================================
    // Specification Queries
    // ============================================================

    /// <summary>
    /// Retrieves all entities matching the given specification.
    /// </summary>
    /// <param name="specification">The specification defining filter, ordering, includes, and paging.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities matching the given projection specification.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="specification">The specification defining filter, ordering, includes, paging, and projection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of projected results.</returns>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities (no filtering).
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first entity matching the specification, or <see langword="null"/> if none match.
    /// </summary>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The first matching entity, or <see langword="null"/>.</returns>
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specification.
    /// </summary>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity matches the specification.
    /// </summary>
    /// <param name="specification">The specification to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if at least one entity matches; otherwise <see langword="false"/>.</returns>
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
}
