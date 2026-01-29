namespace Hmm.Utility.Dal.DataEntity;

/// <summary>
/// Interface for entities that support soft deletion via an IsActivated flag.
/// Entities implementing this interface can be deactivated instead of being permanently deleted.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity is activated.
    /// When false, the entity is considered soft-deleted and should be excluded from normal queries.
    /// </summary>
    bool IsActivated { get; set; }
}
