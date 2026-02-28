namespace Clywell.Core.Data;

/// <summary>
/// Marks an entity as auditable, capturing who created and last modified it and when.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on any entity that requires an audit trail of
/// creation and modification. The Infrastructure layer (EF Core) populates
/// these properties automatically via a <c>SaveChanges</c> interceptor.
/// </para>
/// <para>
/// All timestamps are stored as UTC.
/// </para>
/// </remarks>
public interface IAuditable
{
    /// <summary>Gets or sets the UTC timestamp when the entity was created.</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who created the entity.</summary>
    string? CreatedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the entity was last modified.</summary>
    DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who last modified the entity.</summary>
    string? UpdatedBy { get; set; }
}
