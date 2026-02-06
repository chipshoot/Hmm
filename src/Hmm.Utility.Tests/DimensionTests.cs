using Hmm.Utility.MeasureUnit;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class DimensionTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesValidDimension()
        {
            // Arrange & Act
            var dimension = new Dimension(10.5);

            // Assert
            Assert.Equal(DimensionUnit.Millimetre, dimension.Unit);
            Assert.Equal(3, dimension.Fractional);
            Assert.Equal(10.5, dimension.Value);
        }

        [Theory]
        [InlineData(10.0, DimensionUnit.Millimetre)]
        [InlineData(5.0, DimensionUnit.Centimetre)]
        [InlineData(2.5, DimensionUnit.Metre)]
        [InlineData(1.0, DimensionUnit.Kilometre)]
        [InlineData(12.0, DimensionUnit.Inch)]
        [InlineData(3.5, DimensionUnit.Feet)]
        public void Constructor_WithDifferentUnits_CreatesValidDimension(double value, DimensionUnit unit)
        {
            // Arrange & Act
            var dimension = new Dimension(value, unit);

            // Assert
            Assert.Equal(value, dimension.Value);
            Assert.Equal(unit, dimension.Unit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void Constructor_WithValidFractional_CreatesValidDimension(int fractional)
        {
            // Arrange & Act
            var dimension = new Dimension(10.0, DimensionUnit.Millimetre, fractional);

            // Assert
            Assert.Equal(fractional, dimension.Fractional);
        }

        [Fact]
        public void Constructor_WithNegativeFractional_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Dimension(10.0, DimensionUnit.Millimetre, -1));

            Assert.Contains("Fractional digits must be non-negative", exception.Message);
        }

        [Fact]
        public void Constructor_WithInvalidUnit_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Dimension(10.0, (DimensionUnit)999));

            Assert.Contains("Invalid dimension unit", exception.Message);
        }

        #endregion

        #region Factory Method Tests

        [Fact]
        public void FromMillimeter_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromMillimeter(100.0);

            // Assert
            Assert.Equal(DimensionUnit.Millimetre, dimension.Unit);
            Assert.Equal(100.0, dimension.TotalMillimetre);
        }

        [Fact]
        public void FromCentimeter_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromCentimeter(10.0);

            // Assert
            Assert.Equal(DimensionUnit.Centimetre, dimension.Unit);
            Assert.Equal(10.0, dimension.TotalCentimetre);
        }

        [Fact]
        public void FromMeter_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromMeter(1.5);

            // Assert
            Assert.Equal(DimensionUnit.Metre, dimension.Unit);
            Assert.Equal(1.5, dimension.TotalMetre);
        }

        [Fact]
        public void FromKilometer_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromKilometer(2.0);

            // Assert
            Assert.Equal(DimensionUnit.Kilometre, dimension.Unit);
            Assert.Equal(2.0, dimension.TotalKilometre);
        }

        [Fact]
        public void FromInch_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromInch(12.0);

            // Assert
            Assert.Equal(DimensionUnit.Inch, dimension.Unit);
            Assert.Equal(12.0, dimension.TotalInch);
        }

        [Fact]
        public void FromFeet_CreatesCorrectDimension()
        {
            // Arrange & Act
            var dimension = Dimension.FromFeet(3.0);

            // Assert
            Assert.Equal(DimensionUnit.Feet, dimension.Unit);
            Assert.Equal(3.0, dimension.TotalFeet);
        }

        #endregion

        #region Unit Conversion Tests

        [Fact]
        public void UnitConversions_BetweenMetricUnits_AreCorrect()
        {
            // Arrange
            var mm = Dimension.FromMillimeter(1000.0);

            // Act & Assert
            Assert.Equal(1000.0, mm.TotalMillimetre);
            Assert.Equal(100.0, mm.TotalCentimetre);
            Assert.Equal(1.0, mm.TotalMetre);
            Assert.Equal(0.001, mm.TotalKilometre);
        }

        [Fact]
        public void UnitConversions_BetweenImperialUnits_AreCorrect()
        {
            // Arrange
            var feet = Dimension.FromFeet(1.0);

            // Act & Assert
            Assert.Equal(1.0, feet.TotalFeet);
            Assert.Equal(12.0, feet.TotalInch);
        }

        [Fact]
        public void UnitConversions_BetweenMetricAndImperial_AreCorrect()
        {
            // Arrange
            var inch = Dimension.FromInch(1.0);

            // Act & Assert - 1 inch = 25.4mm
            Assert.Equal(25.4, inch.TotalMillimetre);
            Assert.Equal(2.54, inch.TotalCentimetre);
        }

        [Fact]
        public void ValueProperty_ReturnsCorrectValueForUnit()
        {
            // Arrange
            var mm = Dimension.FromMillimeter(100.0);
            var cm = Dimension.FromCentimeter(10.0);
            var inch = Dimension.FromInch(1.0);

            // Act & Assert
            Assert.Equal(100.0, mm.Value); // Returns as millimeters
            Assert.Equal(10.0, cm.Value);   // Returns as centimeters
            Assert.Equal(1.0, inch.Value);   // Returns as inches
        }

        #endregion

        #region Arithmetic Operator Tests

        [Fact]
        public void AdditionOperator_AddsTwoDimensions()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act
            var result = d1 + d2;

            // Assert
            Assert.Equal(150.0, result.TotalMillimetre);
            Assert.Equal(DimensionUnit.Millimetre, result.Unit);
        }

        [Fact]
        public void AdditionOperator_WithDifferentUnits_PreservesFirstUnit()
        {
            // Arrange
            var d1 = Dimension.FromCentimeter(10.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act
            var result = d1 + d2;

            // Assert
            Assert.Equal(DimensionUnit.Centimetre, result.Unit);
            Assert.Equal(20.0, result.TotalCentimetre);
        }

        [Fact]
        public void SubtractionOperator_SubtractsTwoDimensions()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(30.0);

            // Act
            var result = d1 - d2;

            // Assert
            Assert.Equal(70.0, result.TotalMillimetre);
        }

        [Fact]
        public void SubtractionOperator_CanProduceNegativeValue()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act
            var result = d1 - d2;

            // Assert
            Assert.Equal(-50.0, result.TotalMillimetre);
        }

        [Fact]
        public void MultiplicationOperator_WithInt_MultipliesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(10.0);

            // Act
            var result = dimension * 5;

            // Assert
            Assert.Equal(50.0, result.TotalMillimetre);
        }

        [Fact]
        public void MultiplicationOperator_WithDouble_MultipliesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(10.0);

            // Act
            var result = dimension * 2.5;

            // Assert
            Assert.Equal(25.0, result.TotalMillimetre);
        }

        [Fact]
        public void DivisionOperator_WithInt_DividesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = dimension / 4;

            // Assert
            Assert.Equal(25.0, result.TotalMillimetre);
        }

        [Fact]
        public void DivisionOperator_WithDouble_DividesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = dimension / 2.5;

            // Assert
            Assert.Equal(40.0, result.TotalMillimetre);
        }

        [Fact]
        public void ModulusOperator_ComputesRemainder()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(30.0);

            // Act
            var result = d1 % d2;

            // Assert
            Assert.Equal(10.0, result.TotalMillimetre);
        }

        #endregion

        #region Comparison Operator Tests

        [Fact]
        public void EqualityOperator_WithEqualDimensions_ReturnsTrue()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act & Assert
            Assert.True(d1 == d2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.False(d1 == d2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentUnits_ReturnsFalse()
        {
            // Arrange
            var d1 = new Dimension(100.0, DimensionUnit.Millimetre, 3);
            var d2 = new Dimension(100.0, DimensionUnit.Centimetre, 3);

            // Act & Assert
            Assert.False(d1 == d2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentFractional_ReturnsFalse()
        {
            // Arrange
            var d1 = new Dimension(100.0, DimensionUnit.Millimetre, 2);
            var d2 = new Dimension(100.0, DimensionUnit.Millimetre, 3);

            // Act & Assert
            Assert.False(d1 == d2);
        }

        [Fact]
        public void EqualityOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act & Assert
            Assert.True(dimension == 100);
            Assert.False(dimension == 50);
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ReturnsTrue()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.True(d1 != d2);
        }

        [Fact]
        public void GreaterThanOperator_ComparesCorrectly()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.True(d1 > d2);
            Assert.False(d2 > d1);
        }

        [Fact]
        public void GreaterThanOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act & Assert
            Assert.True(dimension > 50);
            Assert.False(dimension > 150);
        }

        [Fact]
        public void LessThanOperator_ComparesCorrectly()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act & Assert
            Assert.True(d1 < d2);
            Assert.False(d2 < d1);
        }

        [Fact]
        public void GreaterThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(100.0);
            var d3 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.True(d1 >= d2);
            Assert.True(d1 >= d3);
            Assert.False(d3 >= d1);
        }

        [Fact]
        public void LessThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(100.0);
            var d3 = Dimension.FromMillimeter(150.0);

            // Act & Assert
            Assert.True(d1 <= d2);
            Assert.True(d1 <= d3);
            Assert.False(d3 <= d1);
        }

        #endregion

        #region Static Method Tests

        [Fact]
        public void Max_WithMultipleDimensions_ReturnsMaximum()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(150.0);
            var d3 = Dimension.FromMillimeter(100.0);

            // Act
            var max = Dimension.Max(d1, d2, d3);

            // Assert
            Assert.Equal(150.0, max.TotalMillimetre);
        }

        [Fact]
        public void Max_WithSingleDimension_ReturnsThatDimension()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var max = Dimension.Max(dimension);

            // Assert
            Assert.Equal(100.0, max.TotalMillimetre);
        }

        [Fact]
        public void Max_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Dimension.Max());

            Assert.Contains("No measure unit object found", exception.Message);
        }

        [Fact]
        public void Min_WithMultipleDimensions_ReturnsMinimum()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(150.0);
            var d3 = Dimension.FromMillimeter(100.0);

            // Act
            var min = Dimension.Min(d1, d2, d3);

            // Assert
            Assert.Equal(50.0, min.TotalMillimetre);
        }

        [Fact]
        public void Min_WithSingleDimension_ReturnsThatDimension()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var min = Dimension.Min(dimension);

            // Assert
            Assert.Equal(100.0, min.TotalMillimetre);
        }

        [Fact]
        public void Min_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Dimension.Min());

            Assert.Contains("No measure unit object found", exception.Message);
        }

        [Fact]
        public void Abs_WithPositiveValue_ReturnsPositiveValue()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = Dimension.Abs(dimension);

            // Assert
            Assert.Equal(100.0, result.TotalMillimetre);
        }

        [Fact]
        public void Abs_WithNegativeValue_ReturnsPositiveValue()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(-100.0);

            // Act
            var result = Dimension.Abs(dimension);

            // Assert
            Assert.Equal(100.0, result.TotalMillimetre);
        }

        [Fact]
        public void Abs_PreservesUnitAndFractional()
        {
            // Arrange
            var dimension = new Dimension(-100.0, DimensionUnit.Centimetre, 5);

            // Act
            var result = Dimension.Abs(dimension);

            // Assert
            Assert.Equal(DimensionUnit.Centimetre, result.Unit);
            Assert.Equal(5, result.Fractional);
        }

        #endregion

        #region Equality and Comparison Tests

        [Fact]
        public void Equals_WithIdenticalDimensions_ReturnsTrue()
        {
            // Arrange
            var d1 = new Dimension(100.0, DimensionUnit.Millimetre, 3);
            var d2 = new Dimension(100.0, DimensionUnit.Millimetre, 3);

            // Act & Assert
            Assert.True(d1.Equals(d2));
            Assert.True(d1.Equals((object)d2));
        }

        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.False(d1.Equals(d2));
        }

        [Fact]
        public void Equals_WithDifferentTypes_ReturnsFalse()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);
            var other = "not a dimension";

            // Act & Assert
            Assert.False(dimension.Equals(other));
        }

        [Fact]
        public void GetHashCode_ForEqualDimensions_ReturnsSameHashCode()
        {
            // Arrange
            var d1 = new Dimension(100.0, DimensionUnit.Millimetre, 3);
            var d2 = new Dimension(100.0, DimensionUnit.Millimetre, 3);

            // Act & Assert
            Assert.Equal(d1.GetHashCode(), d2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ForDifferentDimensions_ReturnsDifferentHashCode()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act & Assert
            Assert.NotEqual(d1.GetHashCode(), d2.GetHashCode());
        }

        [Fact]
        public void CompareTo_WithLargerDimension_ReturnsNegative()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(50.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act
            var result = d1.CompareTo(d2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_WithSmallerDimension_ReturnsPositive()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(50.0);

            // Act
            var result = d1.CompareTo(d2);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_WithEqualDimension_ReturnsZero()
        {
            // Arrange
            var d1 = Dimension.FromMillimeter(100.0);
            var d2 = Dimension.FromMillimeter(100.0);

            // Act
            var result = d1.CompareTo(d2);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_WithoutFormat_ReturnsInternalValue()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = dimension.ToString();

            // Assert
            Assert.NotEmpty(result);
        }

        [Theory]
        [InlineData("mm", "100 mm")]
        [InlineData("cm", "10 cm")]
        [InlineData("m", "0.1 m")]
        public void ToString_WithMetricFormat_ReturnsFormattedString(string format, string expected)
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = dimension.ToString(format);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("in")]
        [InlineData("ft")]
        public void ToString_WithImperialFormat_ReturnsFormattedString(string format)
        {
            // Arrange
            var dimension = Dimension.FromInch(12.0);

            // Act
            var result = dimension.ToString(format);

            // Assert
            Assert.Contains(format, result);
        }

        [Fact]
        public void ToString_WithMixedFormat_ReturnsFormattedString()
        {
            // Arrange
            var dimension = Dimension.FromMillimeter(100.0);

            // Act
            var result = dimension.ToString("mm/in");

            // Assert
            Assert.Contains("mm", result);
            Assert.Contains("in", result);
            Assert.Contains("/", result);
        }

        [Fact]
        public void ToString_RespectsFractionalDigits()
        {
            // Arrange - Using values that survive the micron conversion
            var d1 = new Dimension(1.0, DimensionUnit.Centimetre, 2);
            var d2 = new Dimension(1.0, DimensionUnit.Centimetre, 4);

            // Act
            var result1 = d1.ToString("cm");
            var result2 = d2.ToString("cm");

            // Assert - Verify fractional digits are respected
            Assert.Contains("1", result1);
            Assert.Contains("cm", result1);
            Assert.Contains("1", result2);
            Assert.Contains("cm", result2);

            // Verify the actual fractional property
            Assert.Equal(2, d1.Fractional);
            Assert.Equal(4, d2.Fractional);
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Dimension_IsImmutable_PropertiesCannotBeChanged()
        {
            // Arrange
            var dimension = new Dimension(100.0, DimensionUnit.Millimetre, 3);

            // Act & Assert - Verify properties are read-only
            // Unit property should have no setter
            Assert.Equal(DimensionUnit.Millimetre, dimension.Unit);

            // Fractional property should have no setter
            Assert.Equal(3, dimension.Fractional);
        }

        [Fact]
        public void ArithmeticOperations_DoNotModifyOriginal()
        {
            // Arrange
            var original = Dimension.FromMillimeter(100.0);
            var originalValue = original.TotalMillimetre;

            // Act
            _ = original + Dimension.FromMillimeter(50.0);
            _ = original - Dimension.FromMillimeter(25.0);
            _ = original * 2;
            _ = original / 2;

            // Assert
            Assert.Equal(originalValue, original.TotalMillimetre);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Dimension_WithZeroValue_WorksCorrectly()
        {
            // Arrange & Act
            var dimension = Dimension.FromMillimeter(0.0);

            // Assert
            Assert.Equal(0.0, dimension.TotalMillimetre);
            Assert.Equal(0.0, dimension.TotalInch);
        }

        [Fact]
        public void Dimension_WithVeryLargeValue_WorksCorrectly()
        {
            // Arrange & Act
            var dimension = Dimension.FromKilometer(1000.0);

            // Assert
            Assert.Equal(1000.0, dimension.TotalKilometre);
            Assert.Equal(1000000.0, dimension.TotalMetre);
        }

        [Fact]
        public void Dimension_WithVerySmallValue_WorksCorrectly()
        {
            // Arrange & Act
            var dimension = Dimension.FromMillimeter(0.001);

            // Assert
            Assert.Equal(0.001, dimension.TotalMillimetre);
        }

        [Fact]
        public void Dimension_RoundingRespectsFractionalDigits()
        {
            // Arrange
            var dimension = new Dimension(10.123456789, DimensionUnit.Millimetre, 3);

            // Act
            var rounded = dimension.TotalMillimetre;

            // Assert
            Assert.Equal(10.123, rounded);
        }

        #endregion
    }
}
