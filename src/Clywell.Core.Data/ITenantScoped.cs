namespace Clywell.Core.Data;

/// <summary>
/// Marks an entity as belonging to a specific tenant, enabling data isolation
/// and multi-tenancy enforcement at the infrastructure layer.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on any entity that must be scoped to a tenant.
/// The Infrastructure layer applies a global EF Core query filter using
/// <see cref="TenantId"/> to ensure data isolation between tenants.
/// </para>
/// <para>
/// The <see cref="TenantId"/> is typically populated from the ambient
/// <c>ITenantContext</c> (resolved from the JWT claim) before the entity
/// is persisted.
/// </para>
/// </remarks>
public interface ITenantScoped
{
    /// <summary>Gets or sets the identifier of the tenant that owns this entity.</summary>
    Guid TenantId { get; set; }
}
