namespace Clywell.Core.Data;

/// <summary>
/// Represents an ordering expression with direction.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <param name="KeySelector">The expression selecting the ordering key.</param>
/// <param name="Descending">Whether the ordering is descending.</param>
public sealed record OrderExpression<T>(
    Expression<Func<T, object?>> KeySelector,
    bool Descending)
    where T : class;
