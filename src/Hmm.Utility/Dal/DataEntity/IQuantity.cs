using System;

namespace Hmm.Utility.Dal.DataEntity
{
    /// <summary>
    /// Common interface for value types that represent a quantity with a unit.
    /// Provides the shared contract for both class-based (Money) and struct-based
    /// (Dimension, Volume, Weight) value objects in the system.
    ///
    /// This is the root abstraction for the "value with unit" pattern. More specialized
    /// interfaces like IMeasureUnit extend this with domain-specific members.
    /// </summary>
    /// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
    /// <typeparam name="TUnit">The unit enum type for this quantity.</typeparam>
    public interface IQuantity<TSelf, TUnit> : IEquatable<TSelf>, IComparable<TSelf>
        where TUnit : struct, Enum
    {
        /// <summary>
        /// Gets the unit of this quantity.
        /// </summary>
        TUnit Unit { get; }
    }
}
