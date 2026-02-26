namespace Clywell.Core.Data;

/// <summary>
/// Evaluates an <see cref="ISpecification{T}"/> against an <see cref="IQueryable{T}"/>
/// to produce a filtered, ordered, paged, and projected query.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by the EF Core layer (<c>EfSpecificationEvaluator</c>)
/// which translates specification criteria, ordering, includes, and paging into
/// EF Core LINQ expressions.
/// </para>
/// <para>
/// The Application layer does not call this directly — it is used internally
/// by repository implementations.
/// </para>
/// </remarks>
public interface ISpecificationEvaluator
{
    /// <summary>
    /// Applies the specification to the given queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source to evaluate against.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="evaluatePagingAndOrdering">
    /// Whether to include ordering and paging. Pass <see langword="false"/> for
    /// aggregate operations (Count, Any) where ordering and paging are irrelevant.
    /// </param>
    /// <returns>The queryable with all specification criteria applied.</returns>
    IQueryable<T> Evaluate<T>(IQueryable<T> source, ISpecification<T> specification, bool evaluatePagingAndOrdering = true)
        where T : class;

    /// <summary>
    /// Applies the specification with projection to the given queryable source.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source to evaluate against.</param>
    /// <param name="specification">The specification with projection to apply.</param>
    /// <returns>The projected queryable with all specification criteria applied.</returns>
    IQueryable<TResult> Evaluate<T, TResult>(IQueryable<T> source, ISpecification<T, TResult> specification)
        where T : class;
}
