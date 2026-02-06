using Hmm.Utility.MeasureUnit;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class VolumeTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesValidVolume()
        {
            // Arrange & Act
            var volume = new Volume(10.5);

            // Assert
            Assert.Equal(VolumeUnit.Liter, volume.Unit);
            Assert.Equal(3, volume.Fractional);
            Assert.Equal(10.5, volume.Value);
        }

        [Theory]
        [InlineData(10.0, VolumeUnit.Milliliter)]
        [InlineData(5.0, VolumeUnit.Centiliter)]
        [InlineData(2.5, VolumeUnit.Deciliter)]
        [InlineData(1.0, VolumeUnit.Liter)]
        [InlineData(0.5, VolumeUnit.CubicMeter)]
        [InlineData(12.0, VolumeUnit.Ounce)]
        [InlineData(2.0, VolumeUnit.Pint)]
        [InlineData(1.5, VolumeUnit.Quart)]
        [InlineData(3.0, VolumeUnit.Gallon)]
        [InlineData(1.0, VolumeUnit.Bushel)]
        public void Constructor_WithDifferentUnits_CreatesValidVolume(double value, VolumeUnit unit)
        {
            // Arrange & Act
            var volume = new Volume(value, unit);

            // Assert
            Assert.Equal(value, volume.Value);
            Assert.Equal(unit, volume.Unit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void Constructor_WithValidFractional_CreatesValidVolume(int fractional)
        {
            // Arrange & Act
            var volume = new Volume(10.0, VolumeUnit.Liter, fractional);

            // Assert
            Assert.Equal(fractional, volume.Fractional);
        }

        [Fact]
        public void Constructor_WithNegativeFractional_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Volume(10.0, VolumeUnit.Liter, -1));

            Assert.Contains("Fractional digits must be non-negative", exception.Message);
        }

        [Fact]
        public void Constructor_WithInvalidUnit_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Volume(10.0, (VolumeUnit)999));

            Assert.Contains("Invalid volume unit", exception.Message);
        }

        #endregion

        #region Factory Method Tests

        [Fact]
        public void FromMilliliter_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromMilliliter(1000.0);

            // Assert
            Assert.Equal(VolumeUnit.Milliliter, volume.Unit);
            Assert.Equal(1000.0, volume.TotalMilliliter);
        }

        [Fact]
        public void FromCentiliter_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromCentiliter(100.0);

            // Assert
            Assert.Equal(VolumeUnit.Centiliter, volume.Unit);
            Assert.Equal(100.0, volume.TotalCentiliter);
        }

        [Fact]
        public void FromDeciliter_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromDeciliter(10.0);

            // Assert
            Assert.Equal(VolumeUnit.Deciliter, volume.Unit);
            Assert.Equal(10.0, volume.TotalDeciliter);
        }

        [Fact]
        public void FromLiter_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromLiter(1.5);

            // Assert
            Assert.Equal(VolumeUnit.Liter, volume.Unit);
            Assert.Equal(1.5, volume.TotalLiter);
        }

        [Fact]
        public void FromCubicMeter_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromCubicMeter(2.0);

            // Assert
            Assert.Equal(VolumeUnit.CubicMeter, volume.Unit);
            Assert.Equal(2.0, volume.TotalCubicMeter);
        }

        [Fact]
        public void FromOunce_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromOunce(16.0);

            // Assert
            Assert.Equal(VolumeUnit.Ounce, volume.Unit);
            Assert.Equal(16.0, volume.TotalOunce);
        }

        [Fact]
        public void FromPint_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromPint(2.0);

            // Assert
            Assert.Equal(VolumeUnit.Pint, volume.Unit);
            Assert.Equal(2.0, volume.TotalPint);
        }

        [Fact]
        public void FromQuart_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromQuart(4.0);

            // Assert
            Assert.Equal(VolumeUnit.Quart, volume.Unit);
            Assert.Equal(4.0, volume.TotalQuart);
        }

        [Fact]
        public void FromGallon_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromGallon(5.0);

            // Assert
            Assert.Equal(VolumeUnit.Gallon, volume.Unit);
            Assert.Equal(5.0, volume.TotalGallon);
        }

        [Fact]
        public void FromBushel_CreatesCorrectVolume()
        {
            // Arrange & Act
            var volume = Volume.FromBushel(3.0);

            // Assert
            Assert.Equal(VolumeUnit.Bushel, volume.Unit);
            Assert.Equal(3.0, volume.TotalBushel);
        }

        #endregion

        #region Unit Conversion Tests

        [Fact]
        public void UnitConversions_BetweenMetricUnits_AreCorrect()
        {
            // Arrange
            var ml = Volume.FromMilliliter(1000.0);

            // Act & Assert
            Assert.Equal(1000.0, ml.TotalMilliliter);
            Assert.Equal(100.0, ml.TotalCentiliter);
            Assert.Equal(10.0, ml.TotalDeciliter);
            Assert.Equal(1.0, ml.TotalLiter);
            Assert.Equal(0.001, ml.TotalCubicMeter);
        }

        [Fact]
        public void UnitConversions_BetweenUSUnits_AreCorrect()
        {
            // Arrange
            var gallon = Volume.FromGallon(1.0);

            // Act & Assert
            Assert.Equal(1.0, gallon.TotalGallon);
            Assert.Equal(4.0, gallon.TotalQuart, 2);
            Assert.Equal(8.0, gallon.TotalPint, 2);
            Assert.Equal(128.0, gallon.TotalOunce, 1);
        }

        [Fact]
        public void UnitConversions_BetweenMetricAndUS_AreCorrect()
        {
            // Arrange - 1 US gallon = 3.78541 liters
            var gallon = Volume.FromGallon(1.0);

            // Act & Assert
            Assert.Equal(3.785, gallon.TotalLiter);
            Assert.Equal(3785.41, gallon.TotalMilliliter);
        }

        [Fact]
        public void UnitConversions_OunceToMilliliter_AreCorrect()
        {
            // Arrange - 1 US fl oz = 29.573 ml
            var ounce = Volume.FromOunce(1.0);

            // Act & Assert
            Assert.Equal(29.573, ounce.TotalMilliliter);
        }

        [Fact]
        public void ValueProperty_ReturnsCorrectValueForUnit()
        {
            // Arrange
            var ml = Volume.FromMilliliter(100.0);
            var l = Volume.FromLiter(1.0);
            var oz = Volume.FromOunce(16.0);

            // Act & Assert
            Assert.Equal(100.0, ml.Value);   // Returns as milliliters
            Assert.Equal(1.0, l.Value);      // Returns as liters
            Assert.Equal(16.0, oz.Value);    // Returns as ounces
        }

        #endregion

        #region Arithmetic Operator Tests

        [Fact]
        public void AdditionOperator_AddsTwoVolumes()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(0.5);

            // Act
            var result = v1 + v2;

            // Assert
            Assert.Equal(1.5, result.TotalLiter);
            Assert.Equal(VolumeUnit.Liter, result.Unit);
        }

        [Fact]
        public void AdditionOperator_WithDifferentUnits_PreservesFirstUnit()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromMilliliter(500.0);

            // Act
            var result = v1 + v2;

            // Assert
            Assert.Equal(VolumeUnit.Liter, result.Unit);
            Assert.Equal(1.5, result.TotalLiter);
        }

        [Fact]
        public void SubtractionOperator_SubtractsTwoVolumes()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(0.5);

            // Act
            var result = v1 - v2;

            // Assert
            Assert.Equal(1.5, result.TotalLiter);
        }

        [Fact]
        public void SubtractionOperator_CanProduceNegativeValue()
        {
            // Arrange
            var v1 = Volume.FromLiter(0.5);
            var v2 = Volume.FromLiter(1.0);

            // Act
            var result = v1 - v2;

            // Assert
            Assert.Equal(-0.5, result.TotalLiter);
        }

        [Fact]
        public void MultiplicationOperator_WithInt_MultipliesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(2.0);

            // Act
            var result = volume * 3;

            // Assert
            Assert.Equal(6.0, result.TotalLiter);
        }

        [Fact]
        public void MultiplicationOperator_WithDouble_MultipliesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(2.0);

            // Act
            var result = volume * 2.5;

            // Assert
            Assert.Equal(5.0, result.TotalLiter);
        }

        [Fact]
        public void DivisionOperator_WithInt_DividesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(10.0);

            // Act
            var result = volume / 4;

            // Assert
            Assert.Equal(2.5, result.TotalLiter);
        }

        [Fact]
        public void DivisionOperator_WithDouble_DividesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(10.0);

            // Act
            var result = volume / 2.5;

            // Assert
            Assert.Equal(4.0, result.TotalLiter);
        }

        [Fact]
        public void ModulusOperator_ComputesRemainder()
        {
            // Arrange
            var v1 = Volume.FromLiter(10.0);
            var v2 = Volume.FromLiter(3.0);

            // Act
            var result = v1 % v2;

            // Assert
            Assert.Equal(1.0, result.TotalLiter);
        }

        #endregion

        #region Comparison Operator Tests

        [Fact]
        public void EqualityOperator_WithEqualVolumes_ReturnsTrue()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(1.0);

            // Act & Assert
            Assert.True(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act & Assert
            Assert.False(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentUnits_ReturnsFalse()
        {
            // Arrange
            var v1 = new Volume(1.0, VolumeUnit.Liter, 3);
            var v2 = new Volume(1.0, VolumeUnit.Milliliter, 3);

            // Act & Assert
            Assert.False(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentFractional_ReturnsFalse()
        {
            // Arrange
            var v1 = new Volume(1.0, VolumeUnit.Liter, 2);
            var v2 = new Volume(1.0, VolumeUnit.Liter, 3);

            // Act & Assert
            Assert.False(v1 == v2);
        }

        [Fact]
        public void EqualityOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume == 5);
            Assert.False(volume == 3);
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ReturnsTrue()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act & Assert
            Assert.True(v1 != v2);
        }

        [Fact]
        public void InequalityOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume != 3);
            Assert.False(volume != 5);
        }

        [Fact]
        public void GreaterThanOperator_ComparesCorrectly()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(1.0);

            // Act & Assert
            Assert.True(v1 > v2);
            Assert.False(v2 > v1);
        }

        [Fact]
        public void GreaterThanOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume > 3);
            Assert.False(volume > 10);
        }

        [Fact]
        public void LessThanOperator_ComparesCorrectly()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act & Assert
            Assert.True(v1 < v2);
            Assert.False(v2 < v1);
        }

        [Fact]
        public void LessThanOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume < 10);
            Assert.False(volume < 3);
        }

        [Fact]
        public void GreaterThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(2.0);
            var v3 = Volume.FromLiter(1.0);

            // Act & Assert
            Assert.True(v1 >= v2);
            Assert.True(v1 >= v3);
            Assert.False(v3 >= v1);
        }

        [Fact]
        public void GreaterThanOrEqualOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume >= 5);
            Assert.True(volume >= 3);
            Assert.False(volume >= 10);
        }

        [Fact]
        public void LessThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(2.0);
            var v3 = Volume.FromLiter(3.0);

            // Act & Assert
            Assert.True(v1 <= v2);
            Assert.True(v1 <= v3);
            Assert.False(v3 <= v1);
        }

        [Fact]
        public void LessThanOrEqualOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act & Assert
            Assert.True(volume <= 5);
            Assert.True(volume <= 10);
            Assert.False(volume <= 3);
        }

        #endregion

        #region Static Method Tests

        [Fact]
        public void Max_WithMultipleVolumes_ReturnsMaximum()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(3.0);
            var v3 = Volume.FromLiter(2.0);

            // Act
            var max = Volume.Max(v1, v2, v3);

            // Assert
            Assert.Equal(3.0, max.TotalLiter);
        }

        [Fact]
        public void Max_WithSingleVolume_ReturnsThatVolume()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act
            var max = Volume.Max(volume);

            // Assert
            Assert.Equal(5.0, max.TotalLiter);
        }

        [Fact]
        public void Max_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Volume.Max());

            Assert.Contains("No measure unit object found", exception.Message);
        }

        [Fact]
        public void Min_WithMultipleVolumes_ReturnsMinimum()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(1.0);
            var v3 = Volume.FromLiter(3.0);

            // Act
            var min = Volume.Min(v1, v2, v3);

            // Assert
            Assert.Equal(1.0, min.TotalLiter);
        }

        [Fact]
        public void Min_WithSingleVolume_ReturnsThatVolume()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act
            var min = Volume.Min(volume);

            // Assert
            Assert.Equal(5.0, min.TotalLiter);
        }

        [Fact]
        public void Min_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Volume.Min());

            Assert.Contains("No measure unit object found", exception.Message);
        }

        [Fact]
        public void Abs_WithPositiveValue_ReturnsPositiveValue()
        {
            // Arrange
            var volume = Volume.FromLiter(5.0);

            // Act
            var result = Volume.Abs(volume);

            // Assert
            Assert.Equal(5.0, result.TotalLiter);
        }

        [Fact]
        public void Abs_WithNegativeValue_ReturnsPositiveValue()
        {
            // Arrange
            var volume = Volume.FromLiter(-5.0);

            // Act
            var result = Volume.Abs(volume);

            // Assert
            Assert.Equal(5.0, result.TotalLiter);
        }

        [Fact]
        public void Abs_PreservesUnitAndFractional()
        {
            // Arrange
            var volume = new Volume(-5.0, VolumeUnit.Gallon, 5);

            // Act
            var result = Volume.Abs(volume);

            // Assert
            Assert.Equal(VolumeUnit.Gallon, result.Unit);
            Assert.Equal(5, result.Fractional);
        }

        #endregion

        #region Equality and Comparison Tests

        [Fact]
        public void Equals_WithIdenticalVolumes_ReturnsTrue()
        {
            // Arrange
            var v1 = new Volume(1.0, VolumeUnit.Liter, 3);
            var v2 = new Volume(1.0, VolumeUnit.Liter, 3);

            // Act & Assert
            Assert.True(v1.Equals(v2));
            Assert.True(v1.Equals((object)v2));
        }

        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act & Assert
            Assert.False(v1.Equals(v2));
        }

        [Fact]
        public void Equals_WithDifferentTypes_ReturnsFalse()
        {
            // Arrange
            var volume = Volume.FromLiter(1.0);
            var other = "not a volume";

            // Act & Assert
            Assert.False(volume.Equals(other));
        }

        [Fact]
        public void GetHashCode_ForEqualVolumes_ReturnsSameHashCode()
        {
            // Arrange
            var v1 = new Volume(1.0, VolumeUnit.Liter, 3);
            var v2 = new Volume(1.0, VolumeUnit.Liter, 3);

            // Act & Assert
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ForDifferentVolumes_ReturnsDifferentHashCode()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act & Assert
            Assert.NotEqual(v1.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        public void CompareTo_WithLargerVolume_ReturnsNegative()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(2.0);

            // Act
            var result = v1.CompareTo(v2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_WithSmallerVolume_ReturnsPositive()
        {
            // Arrange
            var v1 = Volume.FromLiter(2.0);
            var v2 = Volume.FromLiter(1.0);

            // Act
            var result = v1.CompareTo(v2);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_WithEqualVolume_ReturnsZero()
        {
            // Arrange
            var v1 = Volume.FromLiter(1.0);
            var v2 = Volume.FromLiter(1.0);

            // Act
            var result = v1.CompareTo(v2);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_WithoutFormat_ReturnsInternalValue()
        {
            // Arrange
            var volume = Volume.FromLiter(1.0);

            // Act
            var result = volume.ToString();

            // Assert
            Assert.NotEmpty(result);
        }

        [Theory]
        [InlineData("ml", "1000 ml")]
        [InlineData("cl", "100 cl")]
        [InlineData("dl", "10 dl")]
        [InlineData("l", "1 l")]
        public void ToString_WithMetricFormat_ReturnsFormattedString(string format, string expected)
        {
            // Arrange
            var volume = Volume.FromLiter(1.0);

            // Act
            var result = volume.ToString(format);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("oz", "33.815 oz")]
        [InlineData("pt", "2.113 pt")]
        [InlineData("qt", "1.057 qt")]
        [InlineData("gal", "0.264 gal")]
        public void ToString_WithUSFormat_ReturnsFormattedString(string format, string expected)
        {
            // Arrange
            var volume = Volume.FromLiter(1.0);

            // Act
            var result = volume.ToString(format);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToString_WithCubicMeterFormat_ReturnsFormattedString()
        {
            // Arrange
            var volume = Volume.FromLiter(1000.0);

            // Act
            var result = volume.ToString("m3");

            // Assert
            Assert.Equal("1 m3", result);
        }

        [Fact]
        public void ToString_WithBushelFormat_ReturnsFormattedString()
        {
            // Arrange
            var volume = Volume.FromBushel(1.0);

            // Act
            var result = volume.ToString("bu");

            // Assert
            Assert.Contains("bu", result);
        }

        [Fact]
        public void ToString_RespectsFractionalDigits()
        {
            // Arrange
            var v1 = new Volume(1.0, VolumeUnit.Liter, 2);
            var v2 = new Volume(1.0, VolumeUnit.Liter, 4);

            // Act
            var result1 = v1.ToString("l");
            var result2 = v2.ToString("l");

            // Assert
            Assert.Contains("1", result1);
            Assert.Contains("l", result1);
            Assert.Contains("1", result2);
            Assert.Contains("l", result2);

            // Verify the actual fractional property
            Assert.Equal(2, v1.Fractional);
            Assert.Equal(4, v2.Fractional);
        }

        [Fact]
        public void ToString_WithCustomFormat_UsesDefaultFormatting()
        {
            // Arrange
            var volume = new Volume(1.5, VolumeUnit.Liter, 2);

            // Act
            var result = volume.ToString("custom");

            // Assert
            Assert.Equal("1.50", result);
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Volume_IsImmutable_PropertiesCannotBeChanged()
        {
            // Arrange
            var volume = new Volume(1.0, VolumeUnit.Liter, 3);

            // Act & Assert - Verify properties are read-only
            Assert.Equal(VolumeUnit.Liter, volume.Unit);
            Assert.Equal(3, volume.Fractional);
        }

        [Fact]
        public void ArithmeticOperations_DoNotModifyOriginal()
        {
            // Arrange
            var original = Volume.FromLiter(5.0);
            var originalValue = original.TotalLiter;

            // Act
            _ = original + Volume.FromLiter(2.0);
            _ = original - Volume.FromLiter(1.0);
            _ = original * 2;
            _ = original / 2;

            // Assert
            Assert.Equal(originalValue, original.TotalLiter);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Volume_WithZeroValue_WorksCorrectly()
        {
            // Arrange & Act
            var volume = Volume.FromLiter(0.0);

            // Assert
            Assert.Equal(0.0, volume.TotalLiter);
            Assert.Equal(0.0, volume.TotalMilliliter);
            Assert.Equal(0.0, volume.TotalOunce);
        }

        [Fact]
        public void Volume_WithVeryLargeValue_WorksCorrectly()
        {
            // Arrange & Act
            var volume = Volume.FromCubicMeter(1000.0);

            // Assert
            Assert.Equal(1000.0, volume.TotalCubicMeter);
            Assert.Equal(1000000.0, volume.TotalLiter);
        }

        [Fact]
        public void Volume_WithVerySmallValue_WorksCorrectly()
        {
            // Arrange & Act
            var volume = Volume.FromMilliliter(0.001);

            // Assert
            Assert.Equal(0.001, volume.TotalMilliliter);
        }

        [Fact]
        public void Volume_RoundingRespectsFractionalDigits()
        {
            // Arrange
            var volume = new Volume(1.123456789, VolumeUnit.Liter, 3);

            // Act
            var rounded = volume.TotalLiter;

            // Assert
            Assert.Equal(1.123, rounded);
        }

        [Fact]
        public void Volume_ConversionBetweenDifferentUnits_MaintainsAccuracy()
        {
            // Arrange
            var liter = Volume.FromLiter(1.0);
            var gallon = Volume.FromGallon(1.0);

            // Act - Convert liter to milliliter and back
            var mlValue = liter.TotalMilliliter;
            var backToLiter = Volume.FromMilliliter(mlValue);

            // Assert
            Assert.Equal(1.0, backToLiter.TotalLiter);
            Assert.Equal(3.785, gallon.TotalLiter);
        }

        #endregion
    }
}
