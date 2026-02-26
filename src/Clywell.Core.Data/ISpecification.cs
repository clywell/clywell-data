namespace Clywell.Core.Data;

/// <summary>
/// Defines a specification that encapsulates query criteria, ordering, paging,
/// includes, and behavioral flags for an entity query.
/// </summary>
/// <typeparam name="T">The entity type the specification applies to.</typeparam>
/// <remarks>
/// <para>
/// Specifications are composable query objects that describe <em>what</em> data to retrieve
/// without prescribing <em>how</em> to retrieve it. The infrastructure layer (EF Core)
/// evaluates specifications using <see cref="ISpecificationEvaluator"/> to produce
/// efficient database queries.
/// </para>
/// <para>
/// Prefer subclassing <see cref="Specification{T}"/> over implementing this interface
/// directly, as the base class provides a fluent builder API.
/// </para>
/// </remarks>
public interface ISpecification<T>
    where T : class
{
    /// <summary>Gets the filter criteria to apply (AND'd together).</summary>
    IReadOnlyList<Expression<Func<T, bool>>> Criteria { get; }

    /// <summary>Gets the ordering expressions to apply, in priority order.</summary>
    IReadOnlyList<OrderExpression<T>> OrderExpressions { get; }

    /// <summary>Gets the include expressions for eager loading.</summary>
    IReadOnlyList<IncludeExpression> IncludeExpressions { get; }

    /// <summary>Gets the string-based include paths for eager loading.</summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>Gets the number of items to skip (for paging). <see langword="null"/> if not set.</summary>
    int? Skip { get; }

    /// <summary>Gets the maximum number of items to take (for paging). <see langword="null"/> if not set.</summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether the query should be evaluated as read-only (no change tracking).
    /// </summary>
    bool IsReadOnly { get; }
}

/// <summary>
/// Defines a specification with a projection from <typeparamref name="T"/> to <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
/// <remarks>
/// Use this interface when you want to select a subset of entity properties
/// (e.g., projecting to a DTO) directly in the database query for optimal performance.
/// </remarks>
public interface ISpecification<T, TResult> : ISpecification<T>
    where T : class
{
    /// <summary>
    /// Gets the projection expression that maps the entity to the result type.
    /// </summary>
    Expression<Func<T, TResult>>? Selector { get; }
}
