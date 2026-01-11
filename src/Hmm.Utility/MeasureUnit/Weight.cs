using Hmm.Utility.HmmNoteContentMap;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Hmm.Utility.MeasureUnit
{
    /// <summary>
    /// Help class to help convert between lb and kg
    /// <remarks>
    /// The value of weight internally saved as milligrams and can be convert to g, kg and lb
    /// The default weight unit is <see cref="WeightUnit.Kilogram" /> so when we new a
    /// weight object then we are setting the internal unit to milligrams and the value will be adjusted
    /// by unit parameter of constructor.
    /// You can also get a <see cref="Weight" /> object from static factory methods, e.g.
    /// <code>
    /// var weight1 = Weight.FromGrams(35.0);
    /// var weight2 = Weight.FromKilograms(0.035);
    /// var weight3 = Weight.FromPounds(34.0);
    /// </code>
    /// This can setup a weight without manually indicating weight's unit.
    /// You can also get the converted weight value from properties, e.g.
    /// <code>
    /// var w1 = weight.TotalGrams;
    /// var w2 = weight.TotalKilograms;
    /// var w3 = weight.TotalPounds;
    /// </code>
    /// This can get the right value without adjusting weight's unit.
    /// </remarks>
    /// </summary>
    [ImmutableObject(true)]
    [NoteSerializerInstructor(true)]
    public readonly struct Weight : IEquatable<Weight>, IComparable<Weight>
    {
        private const string ErrorMsg = "No weight object found";

        // Internal representation: milligrams (for better precision with decimal weights)
        private const long MilligramsPerGram = 1000;
        private const long MilligramsPerKilogram = 1000000;
        private const double MilligramsPerPound = 453592.37;  // 1 lb = 453.59237 g

        private readonly long _value;
        private readonly int _fractional;

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Weight"/> structure.
        /// </summary>
        /// <param name="value">The value of weight, this will be adjusted to convert internal value based on unit.</param>
        /// <param name="unit">The unit of weight.</param>
        /// <param name="fractional">The fractional digits for rounding (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when fractional is negative or unit is invalid.</exception>
        public Weight(double value, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)
        {
            if (fractional < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fractional), "Fractional digits must be non-negative");
            }

            if (!Enum.IsDefined(typeof(WeightUnit), unit))
            {
                throw new ArgumentOutOfRangeException(nameof(unit), $"Invalid weight unit: {unit}");
            }

            _value = InternalValue(value, unit);
            _fractional = fractional;
            Unit = unit;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Weight"/> structure from being created.
        /// <remarks>
        /// The constructor is only used by override operators
        /// </remarks>
        /// </summary>
        /// <param name="value">The value in milligrams.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="fractional">The fractional.</param>
        private Weight(long value, WeightUnit unit, int fractional)
        {
            _value = value;
            Unit = unit;
            _fractional = fractional;
        }

        #endregion constructor

        #region public properties

        /// <summary>
        /// Gets the actual amount of the weight.
        /// </summary>
        /// <value>
        /// The weight.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [NoteContent]
        public double Value
        {
            get
            {
                switch (Unit)
                {
                    case WeightUnit.Gram:
                        return TotalGrams;

                    case WeightUnit.Kilogram:
                        return TotalKilograms;

                    case WeightUnit.Pound:
                        return TotalPounds;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [NoteContent]
        public WeightUnit Unit { get; }

        public int Fractional => _fractional;

        public double TotalGrams => Math.Round(_value / (double)MilligramsPerGram, Fractional);

        public double TotalKilograms => Math.Round(_value / (double)MilligramsPerKilogram, Fractional);

        public double TotalPounds => Math.Round(_value / MilligramsPerPound, Fractional);

        public static Weight Max(params Weight[] items)
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var max = items.Aggregate((i1, i2) => i1 > i2 ? i1 : i2);

            return max;
        }

        public static Weight Min(params Weight[] items)
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var min = items.Aggregate((i1, i2) => i1 < i2 ? i1 : i2);

            return min;
        }

        public static Weight Abs(Weight x)
        {
            return new Weight(Math.Abs(x._value), x.Unit, x.Fractional);
        }

        #endregion public properties

        #region override operators

        public static Weight operator +(Weight x, Weight y)
        {
            var newValue = x._value + y._value;
            return new Weight(newValue, x.Unit, x.Fractional);
        }

        public static Weight operator -(Weight x, Weight y)
        {
            var newValue = x._value - y._value;
            return new Weight(newValue, x.Unit, x.Fractional);
        }

        public static Weight operator *(Weight x, int y)
        {
            var newValue = x._value * y;
            return new Weight(newValue, x.Unit, x.Fractional);
        }

        public static Weight operator *(Weight x, double y)
        {
            var newValue = x._value * y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Weight operator /(Weight x, int y)
        {
            var newValue = (double)x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Weight operator /(Weight x, double y)
        {
            var newValue = x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Weight operator %(Weight x, Weight y)
        {
            var newValue = (double)x._value % y._value;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong, x.Unit, x.Fractional);
        }

        public static bool operator !=(Weight x, Weight y)
        {
            return !x.Equals(y);
        }

        public static bool operator !=(Weight x, int y)
        {
            return !x.Equals(new Weight(y, x.Unit));
        }

        public static bool operator ==(Weight x, Weight y)
        {
            return x.Equals(y);
        }

        public static bool operator ==(Weight x, int y)
        {
            return x.Equals(new Weight(y, x.Unit));
        }

        public static bool operator >(Weight x, Weight y)
        {
            return x._value > y._value;
        }

        public static bool operator >(Weight x, int y)
        {
            return x > new Weight(y, x.Unit);
        }

        public static bool operator <(Weight x, Weight y)
        {
            return x._value < y._value;
        }

        public static bool operator <(Weight x, int y)
        {
            return x < new Weight(y, x.Unit);
        }

        public static bool operator >=(Weight x, Weight y)
        {
            return x == y || x > y;
        }

        public static bool operator >=(Weight x, int y)
        {
            return x == y || x > y;
        }

        public static bool operator <=(Weight x, Weight y)
        {
            return x == y || x < y;
        }

        public static bool operator <=(Weight x, int y)
        {
            return x == y || x < y;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                var localvalue = _value;
                return localvalue.ToString(CultureInfo.InvariantCulture);
            }

            var fmt = format.Trim().ToLower(CultureInfo.InvariantCulture);

            string result;
            switch (fmt)
            {
                case "g":
                    result = $"{TotalGrams} g";
                    break;

                case "kg":
                    result = $"{TotalKilograms} kg";
                    break;

                case "lb":
                    result = $"{TotalPounds} lb";
                    break;

                case "all":
                    result = $"{TotalPounds} lb / {TotalKilograms} kg";
                    break;

                default:
                    var pSpecifier = $"F{Fractional}";
                    result = Value.ToString(pSpecifier, CultureInfo.InvariantCulture);
                    break;
            }

            return result;
        }

        #endregion override operators

        #region public methods

        public static Weight FromGrams(double value)
        {
            return new Weight(value, WeightUnit.Gram);
        }

        public static Weight FromKilograms(double value)
        {
            return new Weight(value, WeightUnit.Kilogram);
        }

        public static Weight FromPounds(double value)
        {
            return new Weight(value, WeightUnit.Pound);
        }

        #endregion public methods

        #region implementation of interface IComparable

        public int CompareTo(Weight other)
        {
            var localValue = _value;
            return localValue.CompareTo(other._value);
        }

        #endregion implementation of interface IComparable

        #region implementation of interface IEquatable

        public bool Equals(Weight other)
        {
            return _value == other._value && Unit == other.Unit && Fractional == other.Fractional;
        }

        #endregion implementation of interface IEquatable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Weight weight && Equals(weight);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, Unit, Fractional);
        }

        #endregion override public methods of System.ValueType

        #region private methods

        private static long InternalValue(double value, WeightUnit unit)
        {
            switch (unit)
            {
                case WeightUnit.Gram:
                    return (long)Math.Round(value * MilligramsPerGram, 0);

                case WeightUnit.Kilogram:
                    return (long)Math.Round(value * MilligramsPerKilogram, 0);

                case WeightUnit.Pound:
                    return (long)Math.Round(value * MilligramsPerPound, 0);

                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, "Invalid weight unit");
            }
        }

        #endregion private methods
    }
}
