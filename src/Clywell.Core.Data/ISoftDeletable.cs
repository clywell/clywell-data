namespace Clywell.Core.Data;

/// <summary>
/// Marks an entity as supporting soft deletion — records are flagged as deleted
/// rather than physically removed from the data store.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on any entity that must be recoverable or must
/// retain its data for audit/compliance reasons after logical deletion.
/// </para>
/// <para>
/// The Infrastructure layer applies a global EF Core query filter that
/// excludes entities where <see cref="IsDeleted"/> is <see langword="true"/>,
/// making soft-deleted records transparent to normal queries.
/// </para>
/// <para>
/// All timestamps are stored as UTC.
/// </para>
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the entity was soft-deleted.</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Gets or sets the identifier of the user who soft-deleted the entity.</summary>
    string? DeletedBy { get; set; }
}
