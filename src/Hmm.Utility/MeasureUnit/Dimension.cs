using Hmm.Utility.HmmNoteContentMap;
using Hmm.Utility.Misc;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

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
    public struct Dimension : IComparable<Dimension>, IMeasureXmlSerializable<Dimension>
    {
        #region private fields

        private const string ErrorMsg = "No dimension object found";
        private const double InternalUnitPerInch = 25400;
        private readonly long _value;
        private int _fractional;

        #endregion private fields

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Dimension"/> structure.
        /// </summary>
        /// <param name="value">The value of dimension, this will be adjusted to convert internal value based on unit.</param>
        /// <param name="unit">The unit of dimension.</param>
        /// <param name="fractional">The fractional.</param>
        public Dimension(double value, DimensionUnit unit = DimensionUnit.Millimetre, int fractional = 3)
        {
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
        public DimensionUnit Unit { get; set; }

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

        public double TotalMillimetre => Math.Round(_value / 1000.0, Fractional);

        public double TotalCentimetre => Math.Round(_value / 10000.0, Fractional);

        public double TotalMetre => Math.Round(_value / 100000.0, Fractional);

        public double TotalKilometre => Math.Round(_value / 100000000.0, Fractional);

        public double TotalInch => Math.Round(_value / InternalUnitPerInch, Fractional);

        public double TotalFeet => Math.Round((_value / InternalUnitPerInch) / 12.0, Fractional);

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

        public static Dimension Max(params Dimension[] items)
        {
            if (items.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var max = items.Aggregate((i1, i2) => i1 > i2 ? i1 : i2);

            return max;
        }

        public static Dimension Min(params Dimension[] items)
        {
            if (!items.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(items), ErrorMsg);
            }

            var min = items.Aggregate((i1, i2) => i1 < i2 ? i1 : i2);

            return min;
        }

        public static Dimension Abs(Dimension x)
        {
            return new Dimension(Math.Abs(x.Value), x.Unit, x.Fractional);
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
            return new Dimension(newValue, x.Unit);
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
            return x == new Dimension(y, x.Unit);
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

        public string ToString(string format = null)
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
            return _value == other._value;
        }

        #endregion implementation of interface IEquatable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Dimension && Equals((Dimension)obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        #endregion override public methods of System.ValueType

        #region private methods

        private static long InternalValue(double value, DimensionUnit unit)
        {
            switch (unit)
            {
                case DimensionUnit.Millimetre:
                    return (long)Math.Round(value * 1000.0, 0);

                case DimensionUnit.Centimetre:
                    return (long)Math.Round(value * 10000.0, 0);

                case DimensionUnit.Metre:
                    return (long)Math.Round(value * 100000.0, 0);

                case DimensionUnit.Kilometre:
                    return (long)Math.Round(value * 100000000.0, 0);

                case DimensionUnit.Inch:
                    return (long)Math.Round(value * InternalUnitPerInch, 0);

                case DimensionUnit.Feet:
                    return (long)Math.Round(value * InternalUnitPerInch * 12, 0);

                default:
                    return (long)Math.Round(value * 10.0, 0);
            }
        }

        #endregion private methods

        #region implementation of interface IMeasureXmlSerializable

        public XElement Measure2Xml(XNamespace ns)
        {
            if (ns != null)
            {
                return new XElement(ns + "Dimension",
                    new XElement(ns + "Value", Value),
                    new XElement(ns + "Unit", Unit));
            }

            return new XElement("Dimension",
                new XElement("Value", Value),
                new XElement("Unit", Unit));
        }

        public Dimension Xml2Measure(XElement xmlContent)
        {
            var ns = xmlContent.GetDefaultNamespace();
            var doc = new XDocument(xmlContent);
            var root = GetXElement("Dimension", doc, ns);
            if (root == null)
            {
                throw new ArgumentException("The XML element does not contains Dimension element");
            }

            var dv = GetXElement("Value", root, ns);
            if (!double.TryParse(dv?.Value, out var value))
            {
                throw new ArgumentException("The XML element does not contains valid value element");
            }

            var unit = GetXElement("Unit", root, ns);
            if (string.IsNullOrEmpty(unit?.Value))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            if (!Enum.TryParse(unit.Value, true, out DimensionUnit unitType))
            {
                throw new ArgumentException("The XML element does not contains unit element");
            }

            var dim = new Dimension(value, unitType);

            return dim;
        }

        #endregion implementation of interface IMeasureXmlSerializable

        #region private methods

        private static XElement GetXElement(string ename, XContainer content, XNamespace ns)
        {
            return ns != null ? content?.Element(ns + ename) : content?.Element(ename);
        }

        #endregion private methods
    }
}