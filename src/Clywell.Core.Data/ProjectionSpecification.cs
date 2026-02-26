namespace Clywell.Core.Data;

/// <summary>
/// Base class for building query specifications with projection support.
/// </summary>
/// <typeparam name="T">The source entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
/// <remarks>
/// <para>
/// Extends <see cref="Specification{T}"/> with a <see cref="Select"/> method that defines
/// how entities are projected to the result type. The projection is evaluated in the
/// database query (SQL SELECT), avoiding unnecessary data transfer.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// public sealed class TicketSummarySpec : Specification&lt;Ticket, TicketSummaryDto&gt;
/// {
///     public TicketSummarySpec(Guid tenantId)
///     {
///         Where(t =&gt; t.TenantId == tenantId);
///         OrderByDescending(t =&gt; t.CreatedAtUtc);
///         Select(t =&gt; new TicketSummaryDto(t.Id, t.Title, t.Status));
///         AsReadOnly();
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class Specification<T, TResult> : Specification<T>, ISpecification<T, TResult>
    where T : class
{
    /// <inheritdoc />
    public Expression<Func<T, TResult>>? Selector { get; private set; }

    /// <summary>
    /// Sets the projection expression for mapping entities to the result type.
    /// </summary>
    /// <param name="selector">The projection expression.</param>
    protected void Select(Expression<Func<T, TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Selector = selector;
    }
}
