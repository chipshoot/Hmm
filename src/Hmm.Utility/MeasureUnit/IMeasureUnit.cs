using System;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Utility.MeasureUnit
{
    /// <summary>
    /// Specialized interface for measure unit value types (Dimension, Volume, Weight).
    /// Extends IQuantity with measure-specific members: numeric value access,
    /// fractional digit control, and formatted string output.
    ///
    /// All implementors are readonly structs with:
    /// - Internal storage as a high-precision long (microns, microliters, milligrams)
    /// - Unit conversion via TotalXxx properties
    /// - Full operator overloads (+, -, *, /, %, ==, !=, &gt;, &lt;, &gt;=, &lt;=)
    /// - IEquatable&lt;T&gt; equality by (internal value, unit, fractional)
    /// - IComparable&lt;T&gt; comparison by internal value
    /// </summary>
    /// <typeparam name="TSelf">The implementing struct type (CRTP pattern).</typeparam>
    /// <typeparam name="TUnit">The unit enum type for this measure.</typeparam>
    public interface IMeasureUnit<TSelf, TUnit> : IQuantity<TSelf, TUnit>
        where TSelf : struct, IMeasureUnit<TSelf, TUnit>
        where TUnit : struct, Enum
    {
        /// <summary>
        /// Gets the value in the current unit.
        /// </summary>
        double Value { get; }

        /// <summary>
        /// Gets the number of fractional digits used for rounding.
        /// </summary>
        int Fractional { get; }

        /// <summary>
        /// Formats the value as a string with the specified format code.
        /// </summary>
        string ToString(string format);
    }
}
