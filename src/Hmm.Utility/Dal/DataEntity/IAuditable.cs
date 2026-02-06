using System;

namespace Hmm.Utility.Dal.DataEntity;

/// <summary>
/// Interface for entities that support audit tracking with creation and modification metadata.
/// Entities implementing this interface record who created/modified them and when.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the UTC date and time when this entity was created.
    /// </summary>
    DateTime CreateDate { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when this entity was last modified.
    /// </summary>
    DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets the account name of the user who created this entity.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the account name of the user who last modified this entity.
    /// </summary>
    string LastModifiedBy { get; set; }
}
