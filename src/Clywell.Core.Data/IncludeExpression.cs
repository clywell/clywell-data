namespace Clywell.Core.Data;

/// <summary>
/// Represents a navigation property include expression for eager loading.
/// </summary>
/// <param name="Expression">The lambda expression selecting the navigation property.</param>
/// <param name="PreviousExpression">
/// The previous include expression for chained (ThenInclude) loading.
/// <see langword="null"/> for top-level includes.
/// </param>
/// <param name="Type">The type of include (top-level or chained).</param>
public sealed record IncludeExpression(
    LambdaExpression Expression,
    LambdaExpression? PreviousExpression,
    IncludeType Type);

/// <summary>
/// Specifies the type of include operation.
/// </summary>
public enum IncludeType
{
    /// <summary>A top-level include (e.g., Include(x => x.Orders)).</summary>
    Include,

    /// <summary>A chained include (e.g., ThenInclude(o => o.Items)).</summary>
    ThenInclude,
}
