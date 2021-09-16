using Hmm.Utility.HmmNoteContentMap;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Hmm.Utility.MeasureUnit
{
    ///  <summary>
    ///  Help class to help convert between lb and kg
    ///  <remarks>
    ///  The value of weight internally saved as grams and can be convert to kg and lb
    ///  The only way to get <see cref="T:Hmm.Utility.MeasureUnit.Weight" /> object if from four static method, e.g.
    ///   <code>
    ///      var weight1 = Weight.FromGrams(35.0);
    ///      var weight2 = Weight.FromKilograms(0.035);
    ///      var weight3 = Weight.FromPonds(34.0);
    ///   </code>
    ///  </remarks>
    ///  </summary>
    [ImmutableObject(true)]
    [NoteSerializerInstructor(true)]
    public struct Weight : IEquatable<Weight>, IComparable<Weight>
    {
        #region private fields

        private const double GramsPerPound = 453.59237038037829803270366517422;

        private readonly long _valueInG;

        private int _fractional;

        #endregion private fields

        #region constructor

        private Weight(long valueInG, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)
        {
            _valueInG = valueInG;
            Unit = unit;
            _fractional = fractional;
        }

        #endregion constructor

        #region public properties

        public double TotalGrams => Math.Round((double)_valueInG, Fractional);

        public double TotalKilograms => Math.Round(_valueInG / 1000.0, Fractional);

        public double TotalPounds => Math.Round(_valueInG / GramsPerPound, Fractional);

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
                        return TotalKilograms;
                }
            }
        }

        [NoteContent]
        public WeightUnit Unit { get; set; }

        public int Fractional
        {
            get => _fractional;

            set
            {
                if (value > 0)
                {
                    _fractional = value;
                }
            }
        }

        #endregion public properties

        #region public methods

        public static Weight FromGrams(double value)
        {
            var g = (long)value;

            return new Weight(g, WeightUnit.Gram);
        }

        public static Weight FromKilograms(int value)
        {
            var g = (long)Math.Round(value * 1000.0, 0);
            return new Weight(g);
        }

        public static Weight FromKilograms(long value)
        {
            var g = (long)Math.Round(value * 1000.0, 0);
            return new Weight(g);
        }

        public static Weight FromKilograms(double value)
        {
            var g = (long)Math.Round(value * 1000.0, 0);
            return new Weight(g);
        }

        public static Weight FromPounds(double value)
        {
            var grams = value * GramsPerPound;

            var g = (long)Math.Round(grams, 0);

            return new Weight(g, WeightUnit.Pound);
        }

        #endregion public methods

        #region override operators

        public static Weight operator -(Weight x, Weight y)
        {
            var newValue = x._valueInG - y._valueInG;
            return new Weight(newValue);
        }

        public static bool operator !=(Weight x, Weight y)
        {
            return !x.Equals(y);
        }

        public static Weight operator *(Weight x, int y)
        {
            var newValue = x._valueInG * y;
            return new Weight(newValue);
        }

        public static Weight operator *(Weight x, double y)
        {
            var newValue = x._valueInG * y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong);
        }

        public static Weight operator /(Weight x, int y)
        {
            var newValue = (double)x._valueInG / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong);
        }

        public static Weight operator /(Weight x, double y)
        {
            var newValue = x._valueInG / y;
            var newValueAsLong = (long)Math.Round(newValue, 0);

            return new Weight(newValueAsLong);
        }

        public static Weight operator +(Weight x, Weight y)
        {
            var newValue = x._valueInG + y._valueInG;
            return new Weight(newValue);
        }

        public static bool operator ==(Weight x, Weight y)
        {
            return x.Equals(y);
        }

        public static bool operator >(Weight x, Weight y)
        {
            return x._valueInG > y._valueInG;
        }

        public static bool operator >(Weight x, int y)
        {
            return x > new Weight(y, x.Unit);
        }

        public static bool operator <(Weight x, Weight y)
        {
            return x._valueInG < y._valueInG;
        }

        public static bool operator <(Weight x, int y)
        {
            return x < new Weight(y, x.Unit);
        }

        public string ToString(string format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                var localValue = _valueInG;
                return localValue.ToString(CultureInfo.InvariantCulture);
            }

            var fmt = format.Trim().ToLower(CultureInfo.InvariantCulture);
            string result;
            switch (fmt)
            {
                case "lb":
                    result = $"{TotalPounds} lb";
                    break;

                case "g":
                    result = $"{TotalGrams} g";
                    break;

                case "kg":
                    result = $"{TotalKilograms} kg";
                    break;

                case "all":
                    result = $"{TotalPounds} lbs / {TotalPounds} kg";
                    break;

                default:
                    var localValue = _valueInG;
                    result = localValue.ToString(CultureInfo.InvariantCulture);
                    break;
            }

            return result;
        }

        #endregion override operators

        #region implementation of interface IComparable

        public int CompareTo(Weight other)
        {
            var localValue = _valueInG;
            return localValue.CompareTo(other._valueInG);
        }

        #endregion implementation of interface IComparable

        #region implementation of interface IEquatable

        public bool Equals(Weight other)
        {
            return _valueInG == other._valueInG;
        }

        #endregion implementation of interface IEquatable

        #region override public methods of System.ValueType

        public override bool Equals(object obj)
        {
            return obj is Weight weight && Equals(weight);
        }

        public override int GetHashCode()
        {
            return _valueInG.GetHashCode();
        }

        #endregion override public methods of System.ValueType
    }
}