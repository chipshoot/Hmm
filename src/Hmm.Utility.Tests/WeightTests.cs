using Hmm.Utility.MeasureUnit;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class WeightTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesValidWeight()
        {
            // Arrange & Act
            var weight = new Weight(10.5);

            // Assert
            Assert.Equal(WeightUnit.Kilogram, weight.Unit);
            Assert.Equal(3, weight.Fractional);
            Assert.Equal(10.5, weight.Value);
        }

        [Theory]
        [InlineData(100.0, WeightUnit.Gram)]
        [InlineData(5.0, WeightUnit.Kilogram)]
        [InlineData(2.5, WeightUnit.Pound)]
        public void Constructor_WithDifferentUnits_CreatesValidWeight(double value, WeightUnit unit)
        {
            // Arrange & Act
            var weight = new Weight(value, unit);

            // Assert
            Assert.Equal(value, weight.Value);
            Assert.Equal(unit, weight.Unit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void Constructor_WithValidFractional_CreatesValidWeight(int fractional)
        {
            // Arrange & Act
            var weight = new Weight(10.0, WeightUnit.Kilogram, fractional);

            // Assert
            Assert.Equal(fractional, weight.Fractional);
        }

        [Fact]
        public void Constructor_WithNegativeFractional_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Weight(10.0, WeightUnit.Kilogram, -1));

            Assert.Contains("Fractional digits must be non-negative", exception.Message);
        }

        [Fact]
        public void Constructor_WithInvalidUnit_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Weight(10.0, (WeightUnit)999));

            Assert.Contains("Invalid weight unit", exception.Message);
        }

        #endregion

        #region Factory Method Tests

        [Fact]
        public void FromGrams_CreatesCorrectWeight()
        {
            // Arrange & Act
            var weight = Weight.FromGrams(1000.0);

            // Assert
            Assert.Equal(WeightUnit.Gram, weight.Unit);
            Assert.Equal(1000.0, weight.TotalGrams);
        }

        [Fact]
        public void FromKilograms_CreatesCorrectWeight()
        {
            // Arrange & Act
            var weight = Weight.FromKilograms(2.5);

            // Assert
            Assert.Equal(WeightUnit.Kilogram, weight.Unit);
            Assert.Equal(2.5, weight.TotalKilograms);
        }

        [Fact]
        public void FromPounds_CreatesCorrectWeight()
        {
            // Arrange & Act
            var weight = Weight.FromPounds(10.0);

            // Assert
            Assert.Equal(WeightUnit.Pound, weight.Unit);
            Assert.Equal(10.0, weight.TotalPounds);
        }

        #endregion

        #region Unit Conversion Tests

        [Fact]
        public void UnitConversions_BetweenMetricUnits_AreCorrect()
        {
            // Arrange
            var grams = Weight.FromGrams(1000.0);

            // Act & Assert
            Assert.Equal(1000.0, grams.TotalGrams);
            Assert.Equal(1.0, grams.TotalKilograms);
        }

        [Fact]
        public void UnitConversions_BetweenMetricAndImperial_AreCorrect()
        {
            // Arrange - 1 pound = 453.59237 grams
            var pound = Weight.FromPounds(1.0);

            // Act & Assert
            Assert.Equal(1.0, pound.TotalPounds);
            Assert.Equal(453.592, pound.TotalGrams);
            Assert.Equal(0.454, pound.TotalKilograms);
        }

        [Fact]
        public void UnitConversions_PoundsToKilograms_AreCorrect()
        {
            // Arrange - 2.2046 pounds ≈ 1 kilogram
            var kg = Weight.FromKilograms(1.0);

            // Act & Assert
            Assert.Equal(2.205, kg.TotalPounds);
        }

        [Fact]
        public void ValueProperty_ReturnsCorrectValueForUnit()
        {
            // Arrange
            var grams = Weight.FromGrams(500.0);
            var kg = Weight.FromKilograms(2.0);
            var lb = Weight.FromPounds(5.0);

            // Act & Assert
            Assert.Equal(500.0, grams.Value);  // Returns as grams
            Assert.Equal(2.0, kg.Value);       // Returns as kilograms
            Assert.Equal(5.0, lb.Value);       // Returns as pounds
        }

        #endregion

        #region Arithmetic Operator Tests

        [Fact]
        public void AdditionOperator_AddsTwoWeights()
        {
            // Arrange
            var w1 = Weight.FromKilograms(2.0);
            var w2 = Weight.FromKilograms(1.5);

            // Act
            var result = w1 + w2;

            // Assert
            Assert.Equal(3.5, result.TotalKilograms);
            Assert.Equal(WeightUnit.Kilogram, result.Unit);
        }

        [Fact]
        public void AdditionOperator_WithDifferentUnits_PreservesFirstUnit()
        {
            // Arrange
            var w1 = Weight.FromKilograms(1.0);
            var w2 = Weight.FromGrams(500.0);

            // Act
            var result = w1 + w2;

            // Assert
            Assert.Equal(WeightUnit.Kilogram, result.Unit);
            Assert.Equal(1.5, result.TotalKilograms);
        }

        [Fact]
        public void SubtractionOperator_SubtractsTwoWeights()
        {
            // Arrange
            var w1 = Weight.FromKilograms(3.0);
            var w2 = Weight.FromKilograms(1.0);

            // Act
            var result = w1 - w2;

            // Assert
            Assert.Equal(2.0, result.TotalKilograms);
        }

        [Fact]
        public void SubtractionOperator_CanProduceNegativeValue()
        {
            // Arrange
            var w1 = Weight.FromKilograms(1.0);
            var w2 = Weight.FromKilograms(2.0);

            // Act
            var result = w1 - w2;

            // Assert
            Assert.Equal(-1.0, result.TotalKilograms);
        }

        [Fact]
        public void MultiplicationOperator_WithInt_MultipliesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(2.0);

            // Act
            var result = weight * 3;

            // Assert
            Assert.Equal(6.0, result.TotalKilograms);
        }

        [Fact]
        public void MultiplicationOperator_WithDouble_MultipliesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(2.0);

            // Act
            var result = weight * 2.5;

            // Assert
            Assert.Equal(5.0, result.TotalKilograms);
        }

        [Fact]
        public void DivisionOperator_WithInt_DividesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(10.0);

            // Act
            var result = weight / 4;

            // Assert
            Assert.Equal(2.5, result.TotalKilograms);
        }

        [Fact]
        public void DivisionOperator_WithDouble_DividesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(10.0);

            // Act
            var result = weight / 2.5;

            // Assert
            Assert.Equal(4.0, result.TotalKilograms);
        }

        [Fact]
        public void ModulusOperator_ComputesRemainder()
        {
            // Arrange
            var w1 = Weight.FromKilograms(10.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act
            var result = w1 % w2;

            // Assert
            Assert.Equal(1.0, result.TotalKilograms);
        }

        #endregion

        #region Comparison Operator Tests

        [Fact]
        public void EqualityOperator_WithEqualWeights_ReturnsTrue()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(w1 == w2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.False(w1 == w2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentUnits_ReturnsFalse()
        {
            // Arrange
            var w1 = new Weight(1.0, WeightUnit.Kilogram, 3);
            var w2 = new Weight(1.0, WeightUnit.Gram, 3);

            // Act & Assert
            Assert.False(w1 == w2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentFractional_ReturnsFalse()
        {
            // Arrange
            var w1 = new Weight(1.0, WeightUnit.Kilogram, 2);
            var w2 = new Weight(1.0, WeightUnit.Kilogram, 3);

            // Act & Assert
            Assert.False(w1 == w2);
        }

        [Fact]
        public void EqualityOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight == 5);
            Assert.False(weight == 3);
        }

        [Fact]
        public void InequalityOperator_WithDifferentValues_ReturnsTrue()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.True(w1 != w2);
        }

        [Fact]
        public void InequalityOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight != 3);
            Assert.False(weight != 5);
        }

        [Fact]
        public void GreaterThanOperator_ComparesCorrectly()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.True(w1 > w2);
            Assert.False(w2 > w1);
        }

        [Fact]
        public void GreaterThanOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight > 3);
            Assert.False(weight > 10);
        }

        [Fact]
        public void LessThanOperator_ComparesCorrectly()
        {
            // Arrange
            var w1 = Weight.FromKilograms(3.0);
            var w2 = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(w1 < w2);
            Assert.False(w2 < w1);
        }

        [Fact]
        public void LessThanOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight < 10);
            Assert.False(weight < 3);
        }

        [Fact]
        public void GreaterThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(5.0);
            var w3 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.True(w1 >= w2);
            Assert.True(w1 >= w3);
            Assert.False(w3 >= w1);
        }

        [Fact]
        public void GreaterThanOrEqualOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight >= 5);
            Assert.True(weight >= 3);
            Assert.False(weight >= 10);
        }

        [Fact]
        public void LessThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(5.0);
            var w3 = Weight.FromKilograms(7.0);

            // Act & Assert
            Assert.True(w1 <= w2);
            Assert.True(w1 <= w3);
            Assert.False(w3 <= w1);
        }

        [Fact]
        public void LessThanOrEqualOperator_WithInt_ComparesCorrectly()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act & Assert
            Assert.True(weight <= 5);
            Assert.True(weight <= 10);
            Assert.False(weight <= 3);
        }

        #endregion

        #region Static Method Tests

        [Fact]
        public void Max_WithMultipleWeights_ReturnsMaximum()
        {
            // Arrange
            var w1 = Weight.FromKilograms(2.0);
            var w2 = Weight.FromKilograms(5.0);
            var w3 = Weight.FromKilograms(3.0);

            // Act
            var max = Weight.Max(w1, w2, w3);

            // Assert
            Assert.Equal(5.0, max.TotalKilograms);
        }

        [Fact]
        public void Max_WithSingleWeight_ReturnsThatWeight()
        {
            // Arrange
            var weight = Weight.FromKilograms(10.0);

            // Act
            var max = Weight.Max(weight);

            // Assert
            Assert.Equal(10.0, max.TotalKilograms);
        }

        [Fact]
        public void Max_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Weight.Max());

            Assert.Contains("No weight object found", exception.Message);
        }

        [Fact]
        public void Min_WithMultipleWeights_ReturnsMinimum()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(2.0);
            var w3 = Weight.FromKilograms(3.0);

            // Act
            var min = Weight.Min(w1, w2, w3);

            // Assert
            Assert.Equal(2.0, min.TotalKilograms);
        }

        [Fact]
        public void Min_WithSingleWeight_ReturnsThatWeight()
        {
            // Arrange
            var weight = Weight.FromKilograms(10.0);

            // Act
            var min = Weight.Min(weight);

            // Assert
            Assert.Equal(10.0, min.TotalKilograms);
        }

        [Fact]
        public void Min_WithEmptyArray_ThrowsArgumentOutOfRangeException()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Weight.Min());

            Assert.Contains("No weight object found", exception.Message);
        }

        [Fact]
        public void Abs_WithPositiveValue_ReturnsPositiveValue()
        {
            // Arrange
            var weight = Weight.FromKilograms(10.0);

            // Act
            var result = Weight.Abs(weight);

            // Assert
            Assert.Equal(10.0, result.TotalKilograms);
        }

        [Fact]
        public void Abs_WithNegativeValue_ReturnsPositiveValue()
        {
            // Arrange
            var weight = Weight.FromKilograms(-10.0);

            // Act
            var result = Weight.Abs(weight);

            // Assert
            Assert.Equal(10.0, result.TotalKilograms);
        }

        [Fact]
        public void Abs_PreservesUnitAndFractional()
        {
            // Arrange
            var weight = new Weight(-5.0, WeightUnit.Pound, 5);

            // Act
            var result = Weight.Abs(weight);

            // Assert
            Assert.Equal(WeightUnit.Pound, result.Unit);
            Assert.Equal(5, result.Fractional);
        }

        #endregion

        #region Equality and Comparison Tests

        [Fact]
        public void Equals_WithIdenticalWeights_ReturnsTrue()
        {
            // Arrange
            var w1 = new Weight(5.0, WeightUnit.Kilogram, 3);
            var w2 = new Weight(5.0, WeightUnit.Kilogram, 3);

            // Act & Assert
            Assert.True(w1.Equals(w2));
            Assert.True(w1.Equals((object)w2));
        }

        [Fact]
        public void Equals_WithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.False(w1.Equals(w2));
        }

        [Fact]
        public void Equals_WithDifferentTypes_ReturnsFalse()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);
            var other = "not a weight";

            // Act & Assert
            Assert.False(weight.Equals(other));
        }

        [Fact]
        public void GetHashCode_ForEqualWeights_ReturnsSameHashCode()
        {
            // Arrange
            var w1 = new Weight(5.0, WeightUnit.Kilogram, 3);
            var w2 = new Weight(5.0, WeightUnit.Kilogram, 3);

            // Act & Assert
            Assert.Equal(w1.GetHashCode(), w2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ForDifferentWeights_ReturnsDifferentHashCode()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act & Assert
            Assert.NotEqual(w1.GetHashCode(), w2.GetHashCode());
        }

        [Fact]
        public void CompareTo_WithLargerWeight_ReturnsNegative()
        {
            // Arrange
            var w1 = Weight.FromKilograms(3.0);
            var w2 = Weight.FromKilograms(5.0);

            // Act
            var result = w1.CompareTo(w2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_WithSmallerWeight_ReturnsPositive()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(3.0);

            // Act
            var result = w1.CompareTo(w2);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_WithEqualWeight_ReturnsZero()
        {
            // Arrange
            var w1 = Weight.FromKilograms(5.0);
            var w2 = Weight.FromKilograms(5.0);

            // Act
            var result = w1.CompareTo(w2);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_WithoutFormat_ReturnsInternalValue()
        {
            // Arrange
            var weight = Weight.FromKilograms(5.0);

            // Act
            var result = weight.ToString();

            // Assert
            Assert.NotEmpty(result);
        }

        [Theory]
        [InlineData("g", "1000 g")]
        [InlineData("kg", "1 kg")]
        public void ToString_WithMetricFormat_ReturnsFormattedString(string format, string expected)
        {
            // Arrange
            var weight = Weight.FromKilograms(1.0);

            // Act
            var result = weight.ToString(format);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToString_WithPoundFormat_ReturnsFormattedString()
        {
            // Arrange
            var weight = Weight.FromPounds(10.0);

            // Act
            var result = weight.ToString("lb");

            // Assert
            Assert.Equal("10 lb", result);
        }

        [Fact]
        public void ToString_WithAllFormat_ReturnsFormattedString()
        {
            // Arrange
            var weight = Weight.FromKilograms(1.0);

            // Act
            var result = weight.ToString("all");

            // Assert
            Assert.Contains("lb", result);
            Assert.Contains("kg", result);
            Assert.Contains("/", result);
            Assert.Contains("2.205", result); // 1 kg ≈ 2.205 lb
            Assert.Contains("1", result);     // 1 kg
        }

        [Fact]
        public void ToString_RespectsFractionalDigits()
        {
            // Arrange
            var w1 = new Weight(1.0, WeightUnit.Kilogram, 2);
            var w2 = new Weight(1.0, WeightUnit.Kilogram, 4);

            // Act
            var result1 = w1.ToString("kg");
            var result2 = w2.ToString("kg");

            // Assert
            Assert.Contains("1", result1);
            Assert.Contains("kg", result1);
            Assert.Contains("1", result2);
            Assert.Contains("kg", result2);

            // Verify the actual fractional property
            Assert.Equal(2, w1.Fractional);
            Assert.Equal(4, w2.Fractional);
        }

        [Fact]
        public void ToString_WithCustomFormat_UsesDefaultFormatting()
        {
            // Arrange
            var weight = new Weight(2.5, WeightUnit.Kilogram, 2);

            // Act
            var result = weight.ToString("custom");

            // Assert
            Assert.Equal("2.50", result);
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Weight_IsImmutable_PropertiesCannotBeChanged()
        {
            // Arrange
            var weight = new Weight(5.0, WeightUnit.Kilogram, 3);

            // Act & Assert - Verify properties are read-only
            Assert.Equal(WeightUnit.Kilogram, weight.Unit);
            Assert.Equal(3, weight.Fractional);
        }

        [Fact]
        public void ArithmeticOperations_DoNotModifyOriginal()
        {
            // Arrange
            var original = Weight.FromKilograms(10.0);
            var originalValue = original.TotalKilograms;

            // Act
            _ = original + Weight.FromKilograms(5.0);
            _ = original - Weight.FromKilograms(2.0);
            _ = original * 2;
            _ = original / 2;

            // Assert
            Assert.Equal(originalValue, original.TotalKilograms);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Weight_WithZeroValue_WorksCorrectly()
        {
            // Arrange & Act
            var weight = Weight.FromKilograms(0.0);

            // Assert
            Assert.Equal(0.0, weight.TotalKilograms);
            Assert.Equal(0.0, weight.TotalGrams);
            Assert.Equal(0.0, weight.TotalPounds);
        }

        [Fact]
        public void Weight_WithVeryLargeValue_WorksCorrectly()
        {
            // Arrange & Act
            var weight = Weight.FromKilograms(1000000.0);

            // Assert
            Assert.Equal(1000000.0, weight.TotalKilograms);
            Assert.Equal(1000000000.0, weight.TotalGrams);
        }

        [Fact]
        public void Weight_WithVerySmallValue_WorksCorrectly()
        {
            // Arrange & Act
            var weight = Weight.FromGrams(0.001);

            // Assert
            Assert.Equal(0.001, weight.TotalGrams);
        }

        [Fact]
        public void Weight_RoundingRespectsFractionalDigits()
        {
            // Arrange
            var weight = new Weight(1.123456789, WeightUnit.Kilogram, 3);

            // Act
            var rounded = weight.TotalKilograms;

            // Assert
            Assert.Equal(1.123, rounded);
        }

        [Fact]
        public void Weight_ConversionBetweenDifferentUnits_MaintainsAccuracy()
        {
            // Arrange
            var kg = Weight.FromKilograms(1.0);
            var lb = Weight.FromPounds(1.0);

            // Act - Convert kg to grams and back
            var gramsValue = kg.TotalGrams;
            var backToKg = Weight.FromGrams(gramsValue);

            // Assert
            Assert.Equal(1.0, backToKg.TotalKilograms);
            Assert.Equal(453.592, lb.TotalGrams);
        }

        [Fact]
        public void Weight_DecimalPrecision_WorksCorrectly()
        {
            // Arrange & Act - Test that milligram precision works
            var weight = Weight.FromGrams(1.234);

            // Assert
            Assert.Equal(1.234, weight.TotalGrams);
            Assert.Equal(0.001, weight.TotalKilograms);
        }

        [Fact]
        public void Weight_NegativeValue_WorksCorrectly()
        {
            // Arrange & Act
            var weight = Weight.FromKilograms(-5.0);

            // Assert
            Assert.Equal(-5.0, weight.TotalKilograms);
            Assert.Equal(-5000.0, weight.TotalGrams);
        }

        #endregion

        #region Operator Preserves Metadata Tests

        [Fact]
        public void AdditionOperator_PreservesUnitAndFractional()
        {
            // Arrange
            var w1 = new Weight(5.0, WeightUnit.Pound, 5);
            var w2 = Weight.FromPounds(3.0);

            // Act
            var result = w1 + w2;

            // Assert
            Assert.Equal(WeightUnit.Pound, result.Unit);
            Assert.Equal(5, result.Fractional);
        }

        [Fact]
        public void SubtractionOperator_PreservesUnitAndFractional()
        {
            // Arrange
            var w1 = new Weight(5.0, WeightUnit.Gram, 4);
            var w2 = Weight.FromGrams(2.0);

            // Act
            var result = w1 - w2;

            // Assert
            Assert.Equal(WeightUnit.Gram, result.Unit);
            Assert.Equal(4, result.Fractional);
        }

        [Fact]
        public void MultiplicationOperator_PreservesUnitAndFractional()
        {
            // Arrange
            var weight = new Weight(5.0, WeightUnit.Kilogram, 2);

            // Act
            var result = weight * 3;

            // Assert
            Assert.Equal(WeightUnit.Kilogram, result.Unit);
            Assert.Equal(2, result.Fractional);
        }

        [Fact]
        public void DivisionOperator_PreservesUnitAndFractional()
        {
            // Arrange
            var weight = new Weight(10.0, WeightUnit.Pound, 6);

            // Act
            var result = weight / 2;

            // Assert
            Assert.Equal(WeightUnit.Pound, result.Unit);
            Assert.Equal(6, result.Fractional);
        }

        #endregion
    }
}
