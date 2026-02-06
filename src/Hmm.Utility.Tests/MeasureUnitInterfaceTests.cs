using Hmm.Utility.Currency;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.MeasureUnit;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    /// <summary>
    /// Tests that Dimension, Volume, Weight, and Money all conform to the
    /// IQuantity/IMeasureUnit interface hierarchy, and that MeasureUnitHelper
    /// correctly provides shared Max/Min logic.
    /// </summary>
    public class MeasureUnitInterfaceTests
    {
        #region IQuantity Conformance Tests

        [Fact]
        public void Dimension_Implements_IQuantity()
        {
            IQuantity<Dimension, DimensionUnit> qty = new Dimension(10.0, DimensionUnit.Centimetre);

            Assert.Equal(DimensionUnit.Centimetre, qty.Unit);
        }

        [Fact]
        public void Volume_Implements_IQuantity()
        {
            IQuantity<Volume, VolumeUnit> qty = new Volume(5.0, VolumeUnit.Liter);

            Assert.Equal(VolumeUnit.Liter, qty.Unit);
        }

        [Fact]
        public void Weight_Implements_IQuantity()
        {
            IQuantity<Weight, WeightUnit> qty = new Weight(2.5, WeightUnit.Kilogram);

            Assert.Equal(WeightUnit.Kilogram, qty.Unit);
        }

        [Fact]
        public void Money_Implements_IQuantity()
        {
            IQuantity<Money, CurrencyCodeType> qty = new Money(100m, CurrencyCodeType.Usd);

            Assert.Equal(CurrencyCodeType.Usd, qty.Unit);
        }

        [Fact]
        public void Money_IQuantity_Unit_MatchesCurrencyProperty()
        {
            var money = new Money(50m, CurrencyCodeType.Eur);
            IQuantity<Money, CurrencyCodeType> qty = money;

            Assert.Equal(money.Currency, qty.Unit);
        }

        [Fact]
        public void Money_IQuantity_Equality_WorksThroughInterface()
        {
            var money1 = new Money(100m, CurrencyCodeType.Usd);
            var money2 = new Money(100m, CurrencyCodeType.Usd);
            IQuantity<Money, CurrencyCodeType> qty1 = money1;
            IQuantity<Money, CurrencyCodeType> qty2 = money2;

            // IEquatable<Money> inherited through IQuantity
            Assert.True(money1.Equals(money2));
            Assert.True(qty1.Equals((object)qty2));
        }

        [Fact]
        public void Money_IQuantity_CompareTo_WorksThroughInterface()
        {
            var money1 = new Money(50m, CurrencyCodeType.Usd);
            var money2 = new Money(100m, CurrencyCodeType.Usd);
            IQuantity<Money, CurrencyCodeType> qty1 = money1;
            IQuantity<Money, CurrencyCodeType> qty2 = money2;

            // IComparable<Money> inherited through IQuantity
            Assert.True(money1.CompareTo(money2) < 0);
        }

        #endregion

        #region IMeasureUnit Conformance Tests

        [Fact]
        public void Dimension_Implements_IMeasureUnit()
        {
            IMeasureUnit<Dimension, DimensionUnit> unit = new Dimension(10.0, DimensionUnit.Centimetre);

            Assert.Equal(10.0, unit.Value);
            Assert.Equal(DimensionUnit.Centimetre, unit.Unit);
            Assert.Equal(3, unit.Fractional);
        }

        [Fact]
        public void Volume_Implements_IMeasureUnit()
        {
            IMeasureUnit<Volume, VolumeUnit> unit = new Volume(5.0, VolumeUnit.Liter);

            Assert.Equal(5.0, unit.Value);
            Assert.Equal(VolumeUnit.Liter, unit.Unit);
            Assert.Equal(3, unit.Fractional);
        }

        [Fact]
        public void Weight_Implements_IMeasureUnit()
        {
            IMeasureUnit<Weight, WeightUnit> unit = new Weight(2.5, WeightUnit.Kilogram);

            Assert.Equal(2.5, unit.Value);
            Assert.Equal(WeightUnit.Kilogram, unit.Unit);
            Assert.Equal(3, unit.Fractional);
        }

        #endregion

        #region MeasureUnitHelper.Max Tests

        [Fact]
        public void Max_Dimension_ReturnsLargest()
        {
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(150.0);
            var d3 = Dimension.FromMillimeter(100.0);

            var max = Dimension.Max(d1, d2, d3);

            Assert.Equal(150.0, max.TotalMillimetre);
        }

        [Fact]
        public void Max_Volume_ReturnsLargest()
        {
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(3.0);
            var v3 = Volume.FromLiter(2.0);

            var max = Volume.Max(v1, v2, v3);

            Assert.Equal(3.0, max.TotalLiter);
        }

        [Fact]
        public void Max_Weight_ReturnsLargest()
        {
            var w1 = Weight.FromKilograms(1.0);
            var w2 = Weight.FromKilograms(5.0);
            var w3 = Weight.FromKilograms(3.0);

            var max = Weight.Max(w1, w2, w3);

            Assert.Equal(5.0, max.TotalKilograms);
        }

        [Fact]
        public void Max_SingleItem_ReturnsThatItem()
        {
            var d = Dimension.FromMillimeter(42.0);
            Assert.Equal(42.0, Dimension.Max(d).TotalMillimetre);
        }

        [Fact]
        public void Max_EmptyArray_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Dimension.Max());
            Assert.Throws<ArgumentOutOfRangeException>(() => Volume.Max());
            Assert.Throws<ArgumentOutOfRangeException>(() => Weight.Max());
        }

        #endregion

        #region MeasureUnitHelper.Min Tests

        [Fact]
        public void Min_Dimension_ReturnsSmallest()
        {
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(150.0);
            var d3 = Dimension.FromMillimeter(100.0);

            var min = Dimension.Min(d1, d2, d3);

            Assert.Equal(50.0, min.TotalMillimetre);
        }

        [Fact]
        public void Min_Volume_ReturnsSmallest()
        {
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(3.0);
            var v3 = Volume.FromLiter(2.0);

            var min = Volume.Min(v1, v2, v3);

            Assert.Equal(1.0, min.TotalLiter);
        }

        [Fact]
        public void Min_Weight_ReturnsSmallest()
        {
            var w1 = Weight.FromKilograms(1.0);
            var w2 = Weight.FromKilograms(5.0);
            var w3 = Weight.FromKilograms(3.0);

            var min = Weight.Min(w1, w2, w3);

            Assert.Equal(1.0, min.TotalKilograms);
        }

        [Fact]
        public void Min_SingleItem_ReturnsThatItem()
        {
            var v = Volume.FromLiter(7.0);
            Assert.Equal(7.0, Volume.Min(v).TotalLiter);
        }

        [Fact]
        public void Min_EmptyArray_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Dimension.Min());
            Assert.Throws<ArgumentOutOfRangeException>(() => Volume.Min());
            Assert.Throws<ArgumentOutOfRangeException>(() => Weight.Min());
        }

        #endregion

        #region Generic Usage via Interface

        [Fact]
        public void CanUseGenericMethodWithMeasureUnit()
        {
            var d = new Dimension(100.0, DimensionUnit.Millimetre);
            var v = new Volume(5.0, VolumeUnit.Liter);
            var w = new Weight(2.0, WeightUnit.Kilogram);

            Assert.Equal("100 mm", FormatUnit<Dimension, DimensionUnit>(d, "mm"));
            Assert.Equal("5 l", FormatUnit<Volume, VolumeUnit>(v, "l"));
            Assert.Equal("2 kg", FormatUnit<Weight, WeightUnit>(w, "kg"));
        }

        private static string FormatUnit<TSelf, TUnit>(TSelf unit, string format)
            where TSelf : struct, IMeasureUnit<TSelf, TUnit>
            where TUnit : struct, Enum
        {
            return unit.ToString(format);
        }

        #endregion
    }
}
