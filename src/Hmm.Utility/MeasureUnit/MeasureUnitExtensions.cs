using Hmm.Utility.Currency;

namespace Hmm.Utility.MeasureUnit
{
    public static class MeasureUnitExtensions
    {
        #region Dimension

        public static Dimension GetMillimeter(this double value)
        {
            return new Dimension(value);
        }

        public static Dimension GetCentimeter(this double value)
        {
            return new Dimension(value, DimensionUnit.Centimetre);
        }

        public static Dimension GetMeter(this double value)
        {
            return new Dimension(value, DimensionUnit.Metre);
        }

        public static Dimension GetKilometer(this double value)
        {
            return new Dimension(value, DimensionUnit.Kilometre);
        }

        public static Dimension GetInch(this double value)
        {
            return new Dimension(value, DimensionUnit.Inch);
        }

        public static Dimension GetFeet(this double value)
        {
            return new Dimension(value, DimensionUnit.Feet);
        }

        public static Dimension GetMillimeter(this int value)
        {
            return new Dimension(value);
        }

        public static Dimension GetCentimeter(this int value)
        {
            return new Dimension(value, DimensionUnit.Centimetre);
        }

        public static Dimension GetMeter(this int value)
        {
            return new Dimension(value, DimensionUnit.Metre);
        }

        public static Dimension GetKilometer(this int value)
        {
            return new Dimension(value, DimensionUnit.Kilometre);
        }

        public static Dimension GetInch(this int value)
        {
            return new Dimension(value, DimensionUnit.Inch);
        }

        public static Dimension GetFeet(this int value)
        {
            return new Dimension(value, DimensionUnit.Feet);
        }

        #endregion Dimension

        #region Volume

        public static Volume GetMilliliter(this double value)
        {
            return new Volume(value, VolumeUnit.Milliliter);
        }

        public static Volume GetCentiliter(this double value)
        {
            return new Volume(value, VolumeUnit.Centiliter);
        }

        public static Volume GetDeciliter(this double value)
        {
            return new Volume(value, VolumeUnit.Deciliter);
        }

        public static Volume GetLiter(this double value)
        {
            return new Volume(value);
        }

        public static Volume GetCubicMeter(this double value)
        {
            return new Volume(value, VolumeUnit.CubicMeter);
        }

        public static Volume GetCubicOunce(this double value)
        {
            return new Volume(value, VolumeUnit.Ounce);
        }

        public static Volume GetCubicPint(this double value)
        {
            return new Volume(value, VolumeUnit.Pint);
        }

        public static Volume GetQuart(this double value)
        {
            return new Volume(value, VolumeUnit.Quart);
        }

        public static Volume GetGallon(this double value)
        {
            return new Volume(value, VolumeUnit.Gallon);
        }

        public static Volume GetBushel(this double value)
        {
            return new Volume(value, VolumeUnit.Bushel);
        }

        #endregion Volume

        #region Money

        public static Money GetCad(this decimal amount)
        {
            return new Money(amount, CurrencyCodeType.Cad);
        }

        #endregion
    }
}