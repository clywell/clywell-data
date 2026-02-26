namespace Clywell.Core.Data;

/// <summary>
/// Base class for building query specifications with a fluent API.
/// </summary>
/// <typeparam name="T">The entity type the specification applies to.</typeparam>
/// <remarks>
/// <para>
/// Subclass this type and define your criteria in the constructor using the
/// protected builder methods (<see cref="Where"/>, <see cref="OrderBy"/>,
/// <see cref="Include{TProperty}"/>, <see cref="ApplyPaging"/>).
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public sealed class ActiveTicketsByTenantSpec : Specification&lt;Ticket&gt;
/// {
///     public ActiveTicketsByTenantSpec(Guid tenantId, int page, int pageSize)
///     {
///         Where(t =&gt; t.TenantId == tenantId);
///         Where(t =&gt; t.Status == TicketStatus.Active);
///         OrderByDescending(t =&gt; t.CreatedAtUtc);
///         ApplyPaging((page - 1) * pageSize, pageSize);
///         AsReadOnly();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class Specification<T> : ISpecification<T>
    where T : class
{
    private readonly List<Expression<Func<T, bool>>> _criteria = [];
    private readonly List<OrderExpression<T>> _orderExpressions = [];
    private readonly List<IncludeExpression> _includeExpressions = [];
    private readonly List<string> _includeStrings = [];

    // ============================================================
    // ISpecification<T> Implementation
    // ============================================================

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, bool>>> Criteria => _criteria.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<OrderExpression<T>> OrderExpressions => _orderExpressions.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<IncludeExpression> IncludeExpressions => _includeExpressions.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; private set; }

    // ============================================================
    // Fluent Builder Methods (Protected)
    // ============================================================

    /// <summary>
    /// Adds a filter criterion. Multiple criteria are AND'd together.
    /// </summary>
    /// <param name="predicate">The filter predicate expression.</param>
    protected void Where(Expression<Func<T, bool>> predicate)
    {
        _criteria.Add(predicate);
    }

    /// <summary>
    /// Adds an ascending ordering expression.
    /// </summary>
    /// <param name="keySelector">The expression selecting the ordering key.</param>
    protected void OrderBy(Expression<Func<T, object?>> keySelector)
    {
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: false));
    }

    /// <summary>
    /// Adds a descending ordering expression.
    /// </summary>
    /// <param name="keySelector">The expression selecting the ordering key.</param>
    protected void OrderByDescending(Expression<Func<T, object?>> keySelector)
    {
        _orderExpressions.Add(new OrderExpression<T>(keySelector, Descending: true));
    }

    /// <summary>
    /// Adds a navigation property to eagerly load.
    /// </summary>
    /// <typeparam name="TProperty">The navigation property type.</typeparam>
    /// <param name="includeExpression">The expression selecting the navigation property.</param>
    /// <returns>An <see cref="IIncludeBuilder{T, TProperty}"/> for chaining ThenInclude calls.</returns>
    protected IIncludeBuilder<T, TProperty> Include<TProperty>(Expression<Func<T, TProperty>> includeExpression)
    {
        _includeExpressions.Add(new IncludeExpression(includeExpression, null, IncludeType.Include));
        return new IncludeBuilder<T, TProperty>(this);
    }

    /// <summary>
    /// Adds a navigation property to eagerly load (for collections).
    /// </summary>
    /// <typeparam name="TProperty">The collection element type.</typeparam>
    /// <param name="includeExpression">The expression selecting the collection navigation property.</param>
    /// <returns>An <see cref="IIncludeBuilder{T, TProperty}"/> for chaining ThenInclude calls.</returns>
    protected IIncludeBuilder<T, TProperty> IncludeCollection<TProperty>(Expression<Func<T, IEnumerable<TProperty>>> includeExpression)
    {
        _includeExpressions.Add(new IncludeExpression(includeExpression, null, IncludeType.Include));
        return new IncludeBuilder<T, TProperty>(this);
    }

    /// <summary>
    /// Adds a string-based include path for eager loading.
    /// </summary>
    /// <param name="includePath">The dot-separated navigation path (e.g., "Orders.Items").</param>
    protected void Include(string includePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(includePath);
        _includeStrings.Add(includePath);
    }

    /// <summary>
    /// Applies paging to the specification.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The maximum number of items to take.</param>
    protected void ApplyPaging(int skip, int take)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Marks the specification as read-only, hinting the infrastructure to
    /// disable change tracking (e.g., AsNoTracking in EF Core).
    /// </summary>
    protected void AsReadOnly()
    {
        IsReadOnly = true;
    }

    // ============================================================
    // Internal: ThenInclude support
    // ============================================================

    internal void AddThenInclude(IncludeExpression includeExpression)
    {
        _includeExpressions.Add(includeExpression);
    }
}

/// <summary>
/// Provides a fluent API for chaining ThenInclude calls after an Include.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
/// <typeparam name="TPreviousProperty">The previously included property type.</typeparam>
public interface IIncludeBuilder<T, TPreviousProperty>
    where T : class
{
    /// <summary>
    /// Chains an additional navigation property to eagerly load from the previously included property.
    /// </summary>
    /// <typeparam name="TProperty">The navigation property type to include.</typeparam>
    /// <param name="thenIncludeExpression">The expression selecting the nested navigation property.</param>
    /// <returns>An <see cref="IIncludeBuilder{T, TProperty}"/> for further chaining.</returns>
    IIncludeBuilder<T, TProperty> ThenInclude<TProperty>(Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression);

    /// <summary>
    /// Chains an additional collection navigation property from the previously included property.
    /// </summary>
    /// <typeparam name="TProperty">The collection element type.</typeparam>
    /// <param name="thenIncludeExpression">The expression selecting the nested collection navigation.</param>
    /// <returns>An <see cref="IIncludeBuilder{T, TProperty}"/> for further chaining.</returns>
    IIncludeBuilder<T, TProperty> ThenIncludeCollection<TProperty>(Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> thenIncludeExpression);
}

/// <summary>
/// Internal implementation of the ThenInclude fluent builder.
/// </summary>
internal sealed class IncludeBuilder<T, TPreviousProperty> : IIncludeBuilder<T, TPreviousProperty>
    where T : class
{
    private readonly Specification<T> _specification;

    internal IncludeBuilder(Specification<T> specification)
    {
        _specification = specification;
    }

    /// <inheritdoc />
    public IIncludeBuilder<T, TProperty> ThenInclude<TProperty>(Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression)
    {
        ArgumentNullException.ThrowIfNull(thenIncludeExpression);

        var previousExpression = _specification.IncludeExpressions[^1].Expression;
        _specification.AddThenInclude(new IncludeExpression(thenIncludeExpression, previousExpression, IncludeType.ThenInclude));
        return new IncludeBuilder<T, TProperty>(_specification);
    }

    /// <inheritdoc />
    public IIncludeBuilder<T, TProperty> ThenIncludeCollection<TProperty>(Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> thenIncludeExpression)
    {
        ArgumentNullException.ThrowIfNull(thenIncludeExpression);

        var previousExpression = _specification.IncludeExpressions[^1].Expression;
        _specification.AddThenInclude(new IncludeExpression(thenIncludeExpression, previousExpression, IncludeType.ThenInclude));
        return new IncludeBuilder<T, TProperty>(_specification);
    }
}
