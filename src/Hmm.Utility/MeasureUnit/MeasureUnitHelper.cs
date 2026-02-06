using System;
using System.Linq;

namespace Hmm.Utility.MeasureUnit
{
    /// <summary>
    /// Shared implementations for measure unit operations, eliminating duplication
    /// across Dimension, Volume, and Weight structs.
    /// </summary>
    public static class MeasureUnitHelper
    {
        /// <summary>
        /// Returns the maximum value from a params array of measure units.
        /// </summary>
        public static T Max<T>(T[] items) where T : struct, IComparable<T>
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "No measure unit object found");
            }

            return items.Aggregate((i1, i2) => i1.CompareTo(i2) >= 0 ? i1 : i2);
        }

        /// <summary>
        /// Returns the minimum value from a params array of measure units.
        /// </summary>
        public static T Min<T>(T[] items) where T : struct, IComparable<T>
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "No measure unit object found");
            }

            return items.Aggregate((i1, i2) => i1.CompareTo(i2) <= 0 ? i1 : i2);
        }
    }
}
