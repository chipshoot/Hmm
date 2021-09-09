using System;

namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// The base interface of all entity in domain
    /// </summary>
    /// <typeparam name="TIdentity">The type of the identity.</typeparam>
    public interface IGenericEntity<TIdentity> : IEquatable<IGenericEntity<TIdentity>>
    {
        /// <summary>
        /// Gets the id of the domain entity.
        /// </summary>
        TIdentity Id { get; }
    }
}