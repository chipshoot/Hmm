using Hmm.Utility.HmmNoteContentMap;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Hmm.Utility.MeasureUnit
{
    /// <summary>
    /// Help class to help convert between ml and ounce
    /// <remarks>
    /// The value of dimension internally saved as ml/1000 and can be convert to liter, cubicMeter, ounce and gallon
    /// The default volume unit is <see cref="VolumeUnit.Liter" /> so when we new a
    /// volume object then we are setting the internal unit to ml and the value will adjusted
    /// by unit parameter of constructor.
    /// you can also get a <see cref="Volume" /> object from five static method, e.g.
    /// <code>
    /// var vol = Volume.FromLiter(35.0);
    /// var vol2 = Volume.FromMilliliter(0.035);
    /// var vol3 = Volume.FromOunce(20);
    /// var vol4 = Volume.FromGallon(20);
    /// </code>
    /// this can setup a volume without manually indicate volume's unit
    /// you can also get the converted volume value from five static method, e.g.
    /// <code>
    /// var vol1 = Volume.TotalLiter;
    /// var vol2 = Volume.TotalMilliliter;
    /// var vol3 = Volume.TotalOunce;
    /// var vol5 = Volume.TotalGallon;
    /// </code>
    /// this can right value without adjust volume's unit
    /// </remarks>
    /// </summary>
    [ImmutableObject(true)]
    [NoteSerializerInstructor(true)]
    public readonly struct Volume : IComparable<Volume>, IEquatable<Volume>
    {
        private const string ErrorMsg = "No volume object found";

        // Internal representation: microliters (1/1000 of a milliliter)
        private const long MicrolitersPerMilliliter = 1000;
        private const long MicrolitersPerCentiliter = 10000;
        private const long MicrolitersPerDeciliter = 100000;
        private const long MicrolitersPerLiter = 1000000;
        private const long MicrolitersPerCubicMeter = 1000000000;
        private const long MicrolitersPerOunce = 29573;      // US fluid ounce = 29.573 ml
        private const long MicrolitersPerPint = 473176;      // US pint = 473.176 ml
        private const long MicrolitersPerQuart = 946353;     // US quart = 946.353 ml
        private const long MicrolitersPerGallon = 3785410;   // US gallon = 3785.41 ml
        private const long MicrolitersPerBushel = 35239100;  // US bushel = 35239.1 ml

        private readonly long _value;
        private readonly int _fractional;

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Volume"/> structure.
        /// </summary>
        /// <param name="value">The value of volume, this will be adjusted to convert internal value based on unit.</param>
        /// <param name="unit">The unit of volume.</param>
        /// <param name="fractional">The fractional digits for rounding (must be non-negative).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when fractional is negative or unit is invalid.</exception>
        public Volume(double value, VolumeUnit unit = VolumeUnit.Liter, int fractional = 3)
        {
            if (fractional < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fractional), "Fractional digits must be non-negative");
            }

            if (!Enum.IsDefined(typeof(VolumeUnit), unit))
            {
                throw new ArgumentOutOfRangeException(nameof(unit), $"Invalid volume unit: {unit}");
            }

            _value = InternalValue(value, unit);
            _fractional = fractional;
            Unit = unit;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Volume"/> structure from being created.
        /// <remarks>
        /// the constructor is only used by override operators
        /// </remarks>
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="fractional">The fractional.</param>
        private Volume(long value, VolumeUnit unit, int fractional)
        {
            _value = value;
            Unit = unit;
            _fractional = fractional;
        }

        #endregion constructor

        #region public properties

        /// <summary>
        /// Gets the actual amount of the volume.
        /// </summary>
        /// <value>
        /// The volume.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [NoteContent]
        public double Value
        {
            get
            {
                switch (Unit)
                {
                    case VolumeUnit.Milliliter:
                        return TotalMilliliter;

                    case VolumeUnit.Centiliter:
                        return TotalCentiliter;

                    case VolumeUnit.Deciliter:
                        return TotalDeciliter;

                    case VolumeUnit.Liter:
                        return TotalLiter;

                    case VolumeUnit.CubicMeter:
                        return TotalCubicMeter;

                    case VolumeUnit.Ounce:
                        return TotalOunce;

                    case VolumeUnit.Pint:
                        return TotalPint;

                    case VolumeUnit.Quart:
                        return TotalQuart;

                    case VolumeUnit.Gallon:
                        return TotalGallon;

                    case VolumeUnit.Bushel:
                        return TotalBushel;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [NoteContent]
        public VolumeUnit Unit { get; }

        public int Fractional => _fractional;

        public double TotalMilliliter => Math.Round(_value / (double)MicrolitersPerMilliliter, Fractional);

        public double TotalCentiliter => Math.Round(_value / (double)MicrolitersPerCentiliter, Fractional);

        public double TotalDeciliter => Math.Round(_value / (double)MicrolitersPerDeciliter, Fractional);

        public double TotalLiter => Math.Round(_value / (double)MicrolitersPerLiter, Fractional);

        public double TotalCubicMeter => Math.Round(_value / (double)MicrolitersPerCubicMeter, Fractional);

        public double TotalOunce => Math.Round(_value / (double)MicrolitersPerOunce, Fractional);

        public double TotalPint => Math.Round(_value / (double)MicrolitersPerPint, Fractional);

        public double TotalQuart => Math.Round(_value / (double)MicrolitersPerQuart, Fractional);

        public double TotalGallon => Math.Round(_value / (double)MicrolitersPerGallon, Fractional);

        public double TotalBushel => Math.Round(_value / (double)MicrolitersPerBushel, Fractional);

        public static Volume Max(params Volume[] items)
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var max = items.Aggregate((i1, i2) => i1 > i2 ? i1 : i2);

            return max;
        }

        public static Volume Min(params Volume[] items)
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var min = items.Aggregate((i1, i2) => i1 < i2 ? i1 : i2);

            return min;
        }

        public static Volume Abs(Volume x)
        {
            return new Volume(Math.Abs(x._value), x.Unit, x.Fractional);
        }

        #endregion public properties

        #region override operators

        public static Volume operator +(Volume x, Volume y)
        {
            var newValue = x._value + y._value;
            return new Volume(newValue, x.Unit, x.Fractional);
        }

        public static Volume operator -(Volume x, Volume y)
        {
            var newValue = x._value - y._value;
            return new Volume(newValue, x.Unit, x.Fractional);
        }

        public static Volume operator *(Volume x, int y)
        {
            var newValue = x._value * y;
            return new Volume(newValue, x.Unit, x.Fractional);
        }

        public static Volume operator *(Volume x, double y)
        {
            var newValue = x._value * y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Volume(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Volume operator /(Volume x, int y)
        {
            var newValue = (double)x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Volume(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Volume operator /(Volume x, double y)
        {
            var newValue = x._value / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Volume(newValueAsLong, x.Unit, x.Fractional);
        }

        public static Volume operator %(Volume x, Volume y)
        {
            var newValue = (double)x._value % y._value;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Volume(newValueAsLong, x.Unit, x.Fractional);
        }

        public static bool operator !=(Volume x, Volume y)
        {
            return !x.Equals(y);
        }

        public static bool operator !=(Volume x, int y)
        {
            return !x.Equals(new Volume(y, x.Unit));
        }

        public static bool operator ==(Volume x, Volume y)
        {
            return x.Equals(y);
        }

        public static bool operator ==(Volume x, int y)
        {
            return x.Equals(new Volume(y, x.Unit));
        }

        public static bool operator >(Volume x, Volume y)
        {
            return x._value > y._value;
        }

        public static bool operator >(Volume x, int y)
        {
            return x > new Volume(y, x.Unit);
        }

        public static bool operator <(Volume x, Volume y)
        {
            return x._value < y._value;
        }

        public static bool operator <(Volume x, int y)
        {
            return x < new Volume(y, x.Unit);
        }

        public static bool operator >=(Volume x, Volume y)
        {
            return x == y || x > y;
        }

        public static bool operator >=(Volume x, int y)
        {
            return x == y || x > y;
        }

        public static bool operator <=(Volume x, Volume y)
        {
            return x == y || x < y;
        }

        public static bool operator <=(Volume x, int y)
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
                case "ml":
                    result = $"{TotalMilliliter} ml";
                    break;

                case "cl":
                    result = $"{TotalCentiliter} cl";
                    break;

                case "dl":
                    result = $"{TotalDeciliter} dl";
                    break;

                case "l":
                    result = $"{TotalLiter} l";
                    break;

                case "m3":
                    result = $"{TotalCubicMeter} m3";
                    break;

                case "oz":
                    result = $"{TotalOunce} oz";
                    break;

                case "pt":
                    result = $"{TotalPint} pt";
                    break;

                case "qt":
                    result = $"{TotalQuart} qt";
                    break;

                case "gal":
                    result = $"{TotalGallon} gal";
                    break;

                case "bu":
                    result = $"{TotalBushel} bu";
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

        public static Volume FromMilliliter(double value)
        {
            return new Volume(value, VolumeUnit.Milliliter);
        }

        public static Volume FromCentiliter(double value)
        {
            return new Volume(value, VolumeUnit.Centiliter);
        }

        public static Volume FromDeciliter(double value)
        {
            return new Volume(value, VolumeUnit.Deciliter);
        }

        public static Volume FromLiter(double value)
        {
            return new Volume(value);
        }

        public static Volume FromCubicMeter(double value)
        {
            return new Volume(value, VolumeUnit.CubicMeter);
        }

        public static Volume FromOunce(double value)
        {
            return new Volume(value, VolumeUnit.Ounce);
        }

        public static Volume FromPint(double value)
        {
            return new Volume(value, VolumeUnit.Pint);
        }

        public static Volume FromQuart(double value)
        {
            return new Volume(value, VolumeUnit.Quart);
        }

        public static Volume FromGallon(double value)
        {
            return new Volume(value, VolumeUnit.Gallon);
        }

        public static Volume FromBushel(double value)
        {
            return new Volume(value, VolumeUnit.Bushel);
        }

        #endregion public methods

        #region implementation of interface IComparable

        public int CompareTo(Volume other)
        {
            var localValue = _value;
            return localValue.CompareTo(other._value);
        }

        #endregion implementation of interface IComparable

        #region implementation of interface IEquatable

        public bool Equals(Volume other)
        {
            return _value == other._value && Unit == other.Unit && Fractional == other.Fractional;
        }

        #endregion implementation of interface IEquatable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Volume volume && Equals(volume);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, Unit, Fractional);
        }

        #endregion override public methods of System.ValueType

        #region private methods

        private static long InternalValue(double value, VolumeUnit unit)
        {
            switch (unit)
            {
                case VolumeUnit.Milliliter:
                    return (long)Math.Round(value * MicrolitersPerMilliliter, 0);

                case VolumeUnit.Centiliter:
                    return (long)Math.Round(value * MicrolitersPerCentiliter, 0);

                case VolumeUnit.Deciliter:
                    return (long)Math.Round(value * MicrolitersPerDeciliter, 0);

                case VolumeUnit.Liter:
                    return (long)Math.Round(value * MicrolitersPerLiter, 0);

                case VolumeUnit.CubicMeter:
                    return (long)Math.Round(value * MicrolitersPerCubicMeter, 0);

                case VolumeUnit.Ounce:
                    return (long)Math.Round(value * MicrolitersPerOunce, 0);

                case VolumeUnit.Pint:
                    return (long)Math.Round(value * MicrolitersPerPint, 0);

                case VolumeUnit.Quart:
                    return (long)Math.Round(value * MicrolitersPerQuart, 0);

                case VolumeUnit.Gallon:
                    return (long)Math.Round(value * MicrolitersPerGallon, 0);

                case VolumeUnit.Bushel:
                    return (long)Math.Round(value * MicrolitersPerBushel, 0);

                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, "Invalid volume unit");
            }
        }

        #endregion private methods
    }
}