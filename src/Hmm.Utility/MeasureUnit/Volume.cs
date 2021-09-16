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
    public struct Volume : IComparable<Volume>
    {
        private const string ErrorMsg = "No volume object found";

        // The value which is 1/1000 milliliter
        private readonly long _value;

        private int _fractional;

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Volume"/> structure.
        /// </summary>
        /// <param name="value">The value of volume, this will be adjusted to convert internal value based on unit.</param>
        /// <param name="unit">The unit of volume.</param>
        /// <param name="fractional">The fractional.</param>
        public Volume(double value, VolumeUnit unit = VolumeUnit.Liter, int fractional = 3)
        {
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
        public VolumeUnit Unit { get; set; }

        public int Fractional
        {
            get => _fractional;

            set
            {
                if (value >= 0)
                {
                    _fractional = value;
                }
            }
        }

        public double TotalMilliliter => Math.Round(_value / 1000.0, Fractional);

        public double TotalCentiliter => Math.Round(_value / 10000.0, Fractional);

        public double TotalDeciliter => Math.Round(_value / 100000.0, Fractional);

        public double TotalLiter => Math.Round(_value / 1000000.0, Fractional);

        public double TotalCubicMeter => Math.Round(_value / 1000000000.0, Fractional);

        public double TotalOunce => Math.Round(_value / 29573.5, Fractional);

        public double TotalPint => Math.Round(_value / 473176.0, Fractional);

        public double TotalQuart => Math.Round(_value / 946353.0, Fractional);

        public double TotalGallon => Math.Round(_value / 3785410.0, Fractional);

        public double TotalBushel => Math.Round(_value / 35239100.0, Fractional);

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
            return new Volume(Math.Abs(x.Value), x.Unit, x.Fractional);
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
            return new Volume(newValue, x.Unit);
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
            return x == new Volume(y, x.Unit);
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

        public string ToString(string format = null)
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

                case "qal":
                    result = $"{TotalGallon} qal";
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

        public static Volume FromCubicOunce(double value)
        {
            return new Volume(value, VolumeUnit.Ounce);
        }

        public static Volume FromCubicPint(double value)
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
            throw new NotImplementedException();
        }

        #endregion implementation of interface IComparable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Volume && Equals((Volume)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        #endregion override public methods of System.ValueType

        #region private methods

        private static long InternalValue(double value, VolumeUnit unit)
        {
            switch (unit)
            {
                case VolumeUnit.Milliliter:
                    return (long)Math.Round(value * 1000, 0);

                case VolumeUnit.Centiliter:
                    return (long)Math.Round(value * 10000.0, 0);

                case VolumeUnit.Deciliter:
                    return (long)Math.Round(value * 100000.0, 0);

                case VolumeUnit.Liter:
                    return (long)Math.Round(value * 1000000.0, 0);

                case VolumeUnit.CubicMeter:
                    return (long)Math.Round(value * 1000000000.0, 0);

                case VolumeUnit.Ounce:
                    return (long)Math.Round(value * 29573.5, 0);

                case VolumeUnit.Pint:
                    return (long)Math.Round(value * 473176.0, 0);

                case VolumeUnit.Quart:
                    return (long)Math.Round(value * 946353.0, 0);

                case VolumeUnit.Gallon:
                    return (long)Math.Round(value * 3785410.0, 0);

                case VolumeUnit.Bushel:
                    return (long)Math.Round(value * 35239100.0, 0);

                default:
                    throw new NotImplementedException(nameof(VolumeUnit));
            }
        }

        #endregion private methods
    }
}