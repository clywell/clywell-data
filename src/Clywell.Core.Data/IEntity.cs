namespace Clywell.Core.Data;

/// <summary>
/// Defines a base entity contract with a typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity's unique identifier.</typeparam>
/// <remarks>
/// All domain entities should implement this interface to participate in the
/// repository and specification infrastructure. The identifier type is typically
/// <see cref="System.Guid"/> but can be any equatable type.
/// </remarks>
public interface IEntity<TId>
    where TId : notnull
{
    /// <summary>Gets the unique identifier of the entity.</summary>
    TId Id { get; }
}
