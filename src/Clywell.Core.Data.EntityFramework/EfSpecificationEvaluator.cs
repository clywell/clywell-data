namespace Clywell.Core.Data.EntityFramework;

/// <summary>
/// Evaluates <see cref="ISpecification{T}"/> instances against EF Core <see cref="IQueryable{T}"/> sources.
/// </summary>
/// <remarks>
/// <para>
/// Translates specification criteria, ordering, includes (expression-based and string-based),
/// paging, and read-only hints into EF Core LINQ expressions.
/// </para>
/// <para>
/// A singleton <see cref="Default"/> instance is provided for convenience. Custom evaluators
/// can be registered via DI when additional evaluation logic is needed.
/// </para>
/// </remarks>
public class EfSpecificationEvaluator : ISpecificationEvaluator
{
    /// <summary>
    /// Gets the default singleton instance of <see cref="EfSpecificationEvaluator"/>.
    /// </summary>
    public static EfSpecificationEvaluator Default { get; } = new();

    /// <inheritdoc />
    public IQueryable<T> Evaluate<T>(IQueryable<T> source, ISpecification<T> specification, bool evaluatePagingAndOrdering = true)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        var query = source;

        // Apply read-only hint
        if (specification.IsReadOnly)
        {
            query = query.AsNoTracking();
        }

        // Apply filter criteria (AND'd together)
        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        // Apply string-based includes
        foreach (var includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        // Apply expression-based includes (with ThenInclude support)
        query = ApplyIncludes(query, specification);

        if (evaluatePagingAndOrdering)
        {
            // Apply ordering
            query = ApplyOrdering(query, specification);

            // Apply paging
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }

    /// <inheritdoc />
    public IQueryable<TResult> Evaluate<T, TResult>(IQueryable<T> source, ISpecification<T, TResult> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);

        // Apply filtering, ordering, includes first
        var query = Evaluate(source, (ISpecification<T>)specification);

        // Apply projection
        if (specification.Selector is null)
        {
            throw new InvalidOperationException(
                $"Specification {specification.GetType().Name} must define a selector (call Select() in constructor).");
        }

        return query.Select(specification.Selector);
    }

    // ============================================================
    // Private Helpers
    // ============================================================

    private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.OrderExpressions.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var orderExpression in specification.OrderExpressions)
        {
            if (orderedQuery is null)
            {
                orderedQuery = orderExpression.Descending
                    ? query.OrderByDescending(orderExpression.KeySelector)
                    : query.OrderBy(orderExpression.KeySelector);
            }
            else
            {
                orderedQuery = orderExpression.Descending
                    ? orderedQuery.ThenByDescending(orderExpression.KeySelector)
                    : orderedQuery.ThenBy(orderExpression.KeySelector);
            }
        }

        return orderedQuery ?? query;
    }

    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        // Build dot-separated include paths from expression-based includes.
        // Groups Include + subsequent ThenInclude expressions into full navigation paths
        // and uses the string-based Include overload for EF Core compatibility.
        string? currentPath = null;

        foreach (var includeExpression in specification.IncludeExpressions)
        {
            var memberName = GetMemberName(includeExpression.Expression);
            if (memberName is null)
            {
                continue;
            }

            if (includeExpression.Type == IncludeType.Include)
            {
                // Flush the previous path
                if (currentPath is not null)
                {
                    query = query.Include(currentPath);
                }

                currentPath = memberName;
            }
            else if (includeExpression.Type == IncludeType.ThenInclude && currentPath is not null)
            {
                currentPath = $"{currentPath}.{memberName}";
            }
        }

        // Flush the last path
        if (currentPath is not null)
        {
            query = query.Include(currentPath);
        }

        return query;
    }

    private static string? GetMemberName(System.Linq.Expressions.LambdaExpression lambdaExpression)
    {
        var body = lambdaExpression.Body;

        // Unwrap Convert/ConvertChecked nodes (for value type navigation properties)
        if (body is System.Linq.Expressions.UnaryExpression unary &&
            (unary.NodeType == System.Linq.Expressions.ExpressionType.Convert ||
             unary.NodeType == System.Linq.Expressions.ExpressionType.ConvertChecked))
        {
            body = unary.Operand;
        }

        if (body is System.Linq.Expressions.MemberExpression member)
        {
            return member.Member.Name;
        }

        return null;
    }
}
