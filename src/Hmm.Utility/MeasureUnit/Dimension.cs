using Hmm.Utility.HmmNoteContentMap;
using System;
using System.ComponentModel;
using System.Globalization;
namespace Hmm.Utility.MeasureUnit
{
    /// <summary>
    /// Help class to help convert between in and mm
    /// <remarks>
    /// The value of dimension internally saved as mm/1000 and can be convert to cm, m, inch and feet
    /// The default dimension unit is <see cref="DimensionUnit.Millimetre" /> so when we new a
    /// dimension object then we are setting the internal unit to millimeter and the value will adjusted
    /// by unit parameter of constructor.
    /// you can also get a <see cref="Dimension" /> object from five static method, e.g.
    /// <code>
    /// var width1 = Dimension.FromMeter(35.0);
    /// var width2 = Dimension.FromCentimeter(0.035);
    /// var width3 = Dimension.FromInch(20);
    /// var width4 = Dimension.FromFeet(20);
    /// </code>
    /// this can setup a dimension without manually indicate dimension's unit
    /// you can also get the converted dimension value from five static method, e.g.
    /// <code>
    /// var width1 = Dimension.TotalMillimeter;
    /// var width2 = Dimension.TotalCentimeter;
    /// var width3 = Dimension.TotalMeter;
    /// var width4 = Dimension.TotalInch;
    /// var width5 = Dimension.TotalFeet;
    /// </code>
    /// this can right value without adjust dimension's unit
    /// </remarks>
    /// </summary>
    [ImmutableObject(true)]
    [NoteSerializerInstructor(true)]
    public readonly struct Dimension : IMeasureUnit<Dimension, DimensionUnit>
    {
        #region private fields

        // Internal representation: microns (1/1000 of a millimeter)
        private const long MicronsPerMillimetre = 1000;
        private const long MicronsPerCentimetre = 10000;
        private const long MicronsPerMetre = 1000000;
        private const long MicronsPerKilometre = 1000000000;
        private const long MicronsPerInch = 25400;
        private const long MicronsPerFoot = MicronsPerInch * 12;

        private readonly long _value;
        private readonly int _fractional;

        #endregion private fields

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Dimension"/> structure.
        /// </summary>
        /// <param name="value">The value of dimension, this will be adjusted to convert internal value based on unit.</param>
        /// <param name="unit">The unit of dimension.</param>
        /// <param name="fractional">The fractional digits for rounding (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when fractional is negative or unit is invalid.</exception>
        public Dimension(double value, DimensionUnit unit = DimensionUnit.Millimetre, int fractional = 3)
        {
            if (fractional < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fractional), "Fractional digits must be non-negative");
            }

            if (!Enum.IsDefined(typeof(DimensionUnit), unit))
            {
                throw new ArgumentOutOfRangeException(nameof(unit), $"Invalid dimension unit: {unit}");
            }

            _value = InternalValue(value, unit);
            _fractional = fractional;
            Unit = unit;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Dimension"/> structure from being created.
        /// <remarks>
        /// the constructor is only used by override operators
        /// </remarks>
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="fractional">The fractional.</param>
        private Dimension(long value, DimensionUnit unit, int fractional)
        {
            _value = value;
            Unit = unit;
            _fractional = fractional;
        }

        #endregion constructor

        #region public properties

        /// <summary>
        /// Gets the actual amount of the dimension.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        [NoteContent]
        public double Value
        {
            get
            {
                switch (Unit)
                {
                    case DimensionUnit.Millimetre:
                        return TotalMillimetre;

                    case DimensionUnit.Centimetre:
                        return TotalCentimetre;

                    case DimensionUnit.Metre:
                        return TotalMetre;

                    case DimensionUnit.Kilometre:
                        return TotalKilometre;

                    case DimensionUnit.Inch:
                        return TotalInch;

                    case DimensionUnit.Feet:
                        return TotalFeet;

                    default:
                        return TotalMillimetre;
                }
            }
        }

        [NoteContent]
        public DimensionUnit Unit { get; }

        public int Fractional => _fractional;

        public double TotalMillimetre => Math.Round(_value / (double)MicronsPerMillimetre, Fractional);

        public double TotalCentimetre => Math.Round(_value / (double)MicronsPerCentimetre, Fractional);

        public double TotalMetre => Math.Round(_value / (double)MicronsPerMetre, Fractional);

        public double TotalKilometre => Math.Round(_value / (double)MicronsPerKilometre, Fractional);

        public double TotalInch => Math.Round(_value / (double)MicronsPerInch, Fractional);

        public double TotalFeet => Math.Round(_value / (double)MicronsPerFoot, Fractional);

        #endregion public properties

        #region public methods

        public static Dimension FromMillimeter(double value)
        {
            return new Dimension(value);
        }

        public static Dimension FromCentimeter(double value)
        {
            return new Dimension(value, DimensionUnit.Centimetre);
        }

        public static Dimension FromMeter(double value)
        {
            return new Dimension(value, DimensionUnit.Metre);
        }

        public static Dimension FromKilometer(double value)
        {
            return new Dimension(value, DimensionUnit.Kilometre);
        }

        public static Dimension FromInch(double value)
        {
            return new Dimension(value, DimensionUnit.Inch);
        }

        public static Dimension FromFeet(double value)
        {
            return new Dimension(value, DimensionUnit.Feet);
        }

        public static Dimension Max(params Dimension[] items) => MeasureUnitHelper.Max(items);

        public static Dimension Min(params Dimension[] items) => MeasureUnitHelper.Min(items);

        public static Dimension Abs(Dimension x)
        {
            return new Dimension(Math.Abs(x._value), x.Unit, x.Fractional);
        }

        #endregion public methods

        #region override operators

        public static Dimension operator +(Dimension x, Dimension y)
        {
            var newValue = x._value + y._value;
            return new Dimension(newValue, x.Unit, x.Fractional);
        }

        public static Dimension operator -(Dimension x, Dimension y)
        {
            var newValue = x._value - y._value;
            return new Dimension(newValue, x.Unit, x.Fractional);
        }

        public static Dimension operator *(Dimension x, int y)
        {
            var newValue = x._value * y;
            return new Dimension(newValue, x.Unit, x.Fractional);
        }

        public static Dimension operator *(Dimension x, double y)
        {
            var newValue = x._value * y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Dimension(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Dimension operator /(Dimension x, int y)
        {
            var newValue = (double)x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Dimension(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Dimension operator /(Dimension x, double y)
        {
            var newValue = x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Dimension(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Dimension operator %(Dimension x, Dimension y)
        {
            var newValue = (double)x._value % y._value;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Dimension(newValueAsLong, x.Unit, x.Fractional);
        }

        public static bool operator !=(Dimension x, Dimension y)
        {
            return !x.Equals(y);
        }

        public static bool operator !=(Dimension x, int y)
        {
            return !x.Equals(new Dimension(y, x.Unit));
        }

        public static bool operator ==(Dimension x, Dimension y)
        {
            return x.Equals(y);
        }

        public static bool operator ==(Dimension x, int y)
        {
            return x.Equals(new Dimension(y, x.Unit));
        }

        public static bool operator >(Dimension x, Dimension y)
        {
            return x._value > y._value;
        }

        public static bool operator >(Dimension x, int y)
        {
            return x > new Dimension(y, x.Unit);
        }

        public static bool operator <(Dimension x, Dimension y)
        {
            return x._value < y._value;
        }

        public static bool operator <(Dimension x, int y)
        {
            return x < new Dimension(y, x.Unit);
        }

        public static bool operator >=(Dimension x, Dimension y)
        {
            return x == y || x > y;
        }

        public static bool operator >=(Dimension x, int y)
        {
            return x == y || x > y;
        }

        public static bool operator <=(Dimension x, Dimension y)
        {
            return x == y || x < y;
        }

        public static bool operator <=(Dimension x, int y)
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
                var localValue = _value;
                return localValue.ToString(CultureInfo.InvariantCulture);
            }

            var fmt = format.Trim().ToLower(CultureInfo.InvariantCulture);

            string result;
            switch (fmt)
            {
                case "mm":
                    result = $"{TotalMillimetre} mm";
                    break;

                case "cm":
                    result = $"{TotalCentimetre} cm";
                    break;

                case "m":
                    result = $"{TotalMetre} m";
                    break;

                case "in":
                    result = $"{TotalInch} in";
                    break;

                case "ft":
                    result = $"{TotalFeet} ft";
                    break;

                case "mm/in":
                    result = $"{TotalMillimetre} mm / {TotalInch} in";
                    break;

                default:
                    var pSpecifier = $"F{Fractional}";
                    result = Value.ToString(pSpecifier, CultureInfo.InvariantCulture);
                    break;
            }

            return result;
        }

        #endregion override operators

        #region implementation of interface IComparable

        public int CompareTo(Dimension other)
        {
            var localvalue = _value;
            return localvalue.CompareTo(other._value);
        }

        #endregion implementation of interface IComparable

        #region implementation of interface IEquatable

        public bool Equals(Dimension other)
        {
            return _value == other._value && Unit == other.Unit && Fractional == other.Fractional;
        }

        #endregion implementation of interface IEquatable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Dimension dimension && Equals(dimension);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, Unit, Fractional);
        }

        #endregion override public methods of System.ValueType

        #region private methods

        private static long InternalValue(double value, DimensionUnit unit)
        {
            switch (unit)
            {
                case DimensionUnit.Millimetre:
                    return (long)Math.Round(value * MicronsPerMillimetre, 0);

                case DimensionUnit.Centimetre:
                    return (long)Math.Round(value * MicronsPerCentimetre, 0);

                case DimensionUnit.Metre:
                    return (long)Math.Round(value * MicronsPerMetre, 0);

                case DimensionUnit.Kilometre:
                    return (long)Math.Round(value * MicronsPerKilometre, 0);

                case DimensionUnit.Inch:
                    return (long)Math.Round(value * MicronsPerInch, 0);

                case DimensionUnit.Feet:
                    return (long)Math.Round(value * MicronsPerFoot, 0);

                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, "Invalid dimension unit");
            }
        }

        #endregion private methods
    }
}