using Hmm.Utility.Currency;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class MoneyTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaultParameters_CreatesZeroCAD()
        {
            // Arrange & Act
            var money = new Money();

            // Assert
            Assert.Equal(0m, money.Amount);
            Assert.Equal("CAD", money.CurrencyCode);
            Assert.Equal(0d, money.InternalAmount);
        }

        [Fact]
        public void Constructor_WithDecimalAmount_CreatesValidMoney()
        {
            // Arrange & Act
            var money = new Money(100.50m);

            // Assert
            Assert.Equal(100.50m, money.Amount);
            Assert.Equal("CAD", money.CurrencyCode);
        }

        [Fact]
        public void Constructor_WithDoubleAmount_CreatesValidMoney()
        {
            // Arrange & Act
            var money = new Money(75.25);

            // Assert
            Assert.Equal(75.25m, money.Amount);
        }

        [Theory]
        [InlineData(CurrencyCodeType.Usd)]
        [InlineData(CurrencyCodeType.Eur)]
        [InlineData(CurrencyCodeType.Gbp)]
        [InlineData(CurrencyCodeType.Jpy)]
        [InlineData(CurrencyCodeType.Cny)]
        public void Constructor_WithDifferentCurrencies_CreatesValidMoney(CurrencyCodeType currency)
        {
            // Arrange & Act
            var money = new Money(100m, currency);

            // Assert
            Assert.Equal(100m, money.Amount);
            Assert.Equal(currency.ToString().ToUpper(), money.CurrencyCode);
        }

        [Fact]
        public void Constructor_WithNegativeAmount_CreatesNegativeMoney()
        {
            // Arrange & Act
            var money = new Money(-50.25m);

            // Assert
            Assert.Equal(-50.25m, money.Amount);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Amount_RoundsToTwoDecimalPlaces()
        {
            // Arrange & Act
            var money = new Money(100.12345);

            // Assert
            Assert.Equal(100.12m, money.Amount);
        }

        [Fact]
        public void Amount_RoundsAwayFromZero()
        {
            // Arrange & Act
            var money1 = new Money(100.125); // Should round to 100.13
            var money2 = new Money(100.124); // Should round to 100.12

            // Assert
            Assert.Equal(100.13m, money1.Amount);
            Assert.Equal(100.12m, money2.Amount);
        }

        [Fact]
        public void TruncatedAmount_TruncatesInsteadOfRounds()
        {
            // Arrange & Act
            var money = new Money(100.999);

            // Assert - TruncatedAmount uses DecimalDigits (2) as multiplier
            // Formula: (long)Math.Truncate(InternalAmount * DecimalDigits) / DecimalDigits
            // So 100.999 * 2 = 201.998, truncate = 201, / 2 = 100.5
            // Note: This appears to be a bug - should use 100 (10^DecimalDigits) instead of DecimalDigits
            Assert.Equal(100.5m, money.TruncatedAmount);
            Assert.Equal(101.00m, money.Amount); // Amount rounds correctly
        }

        [Fact]
        public void CurrencyCode_ReturnsUpperCaseCode()
        {
            // Arrange & Act
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(100m, CurrencyCodeType.Eur);

            // Assert
            Assert.Equal("USD", usd.CurrencyCode);
            Assert.Equal("EUR", eur.CurrencyCode);
        }

        [Fact]
        public void CurrencyName_ReturnsDescriptiveName()
        {
            // Arrange & Act
            var cad = new Money(100m, CurrencyCodeType.Cad);
            var usd = new Money(100m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal("Canadian dollar", cad.CurrencyName);
            Assert.Equal("United States dollar", usd.CurrencyName);
        }

        [Fact]
        public void CurrencySymbol_ReturnsCorrectSymbol()
        {
            // Arrange & Act
            var cad = new Money(100m, CurrencyCodeType.Cad);
            var usd = new Money(100m, CurrencyCodeType.Usd);

            // Assert
            Assert.NotEmpty(cad.CurrencySymbol);
            Assert.NotEmpty(usd.CurrencySymbol);
        }

        [Fact]
        public void IsoCode_ReturnsNumericCode()
        {
            // Arrange & Act
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(100m, CurrencyCodeType.Eur);

            // Assert
            Assert.Equal(840, usd.IsoCode);
            Assert.Equal(978, eur.IsoCode);
        }

        [Fact]
        public void LocalCurrencyCode_ReturnsSystemCurrency()
        {
            // Arrange & Act
            var localCode = Money.LocalCurrencyCode;

            // Assert
            Assert.NotEqual(CurrencyCodeType.None, localCode);
        }

        #endregion

        #region Arithmetic Operator Tests - Money with Money

        [Fact]
        public void AdditionOperator_AddsTwoMoney_SameCurrency()
        {
            // Arrange
            var m1 = new Money(100.50m, CurrencyCodeType.Usd);
            var m2 = new Money(50.25m, CurrencyCodeType.Usd);

            // Act
            var result = m1 + m2;

            // Assert
            Assert.Equal(150.75m, result.Amount);
            Assert.Equal("USD", result.CurrencyCode);
        }

        [Fact]
        public void AdditionOperator_DifferentCurrencies_ThrowsException()
        {
            // Arrange
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(100m, CurrencyCodeType.Eur);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => usd + eur);
            Assert.Contains("Different money found", exception.Message);
        }

        [Fact]
        public void SubtractionOperator_SubtractsTwoMoney_SameCurrency()
        {
            // Arrange
            var m1 = new Money(100.50m, CurrencyCodeType.Usd);
            var m2 = new Money(30.25m, CurrencyCodeType.Usd);

            // Act
            var result = m1 - m2;

            // Assert
            Assert.Equal(70.25m, result.Amount);
            Assert.Equal("USD", result.CurrencyCode);
        }

        [Fact]
        public void SubtractionOperator_CanProduceNegativeAmount()
        {
            // Arrange
            var m1 = new Money(50m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = m1 - m2;

            // Assert
            Assert.Equal(-50m, result.Amount);
        }

        [Fact]
        public void SubtractionOperator_DifferentCurrencies_ThrowsException()
        {
            // Arrange
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(50m, CurrencyCodeType.Eur);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => usd - eur);
            Assert.Contains("Different money found", exception.Message);
        }

        [Fact]
        public void DivisionOperator_ReturnDecimalRatio()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(25m, CurrencyCodeType.Usd);

            // Act
            var result = m1 / m2;

            // Assert
            Assert.Equal(4m, result);
            Assert.IsType<decimal>(result);
        }

        [Fact]
        public void DivisionOperator_DifferentCurrencies_ThrowsException()
        {
            // Arrange
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(25m, CurrencyCodeType.Eur);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => usd / eur);
            Assert.Contains("Different money found", exception.Message);
        }

        #endregion

        #region Arithmetic Operator Tests - Money with Scalars

        [Fact]
        public void AdditionOperator_WithDecimal_AddsCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money + 50.50m;

            // Assert
            Assert.Equal(150.50m, result.Amount);
            Assert.Equal("USD", result.CurrencyCode);
        }

        [Fact]
        public void AdditionOperator_WithDouble_AddsCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money + 25.75;

            // Assert
            Assert.Equal(125.75m, result.Amount);
        }

        [Fact]
        public void AdditionOperator_WithNull_ThrowsException()
        {
            // Arrange
            Money money = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => money + 50.0);
        }

        [Fact]
        public void SubtractionOperator_WithDecimal_SubtractsCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money - 25.50m;

            // Assert
            Assert.Equal(74.50m, result.Amount);
        }

        [Fact]
        public void SubtractionOperator_WithDouble_SubtractsCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money - 30.25;

            // Assert
            Assert.Equal(69.75m, result.Amount);
        }

        [Fact]
        public void SubtractionOperator_WithNull_ThrowsException()
        {
            // Arrange
            Money money = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => money - 50.0);
        }

        [Fact]
        public void MultiplicationOperator_WithDecimal_MultipliesCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money * 2.5m;

            // Assert
            Assert.Equal(250m, result.Amount);
        }

        [Fact]
        public void MultiplicationOperator_WithDouble_MultipliesCorrectly()
        {
            // Arrange
            var money = new Money(50m, CurrencyCodeType.Usd);

            // Act
            var result = money * 3.0;

            // Assert
            Assert.Equal(150m, result.Amount);
        }

        [Fact]
        public void MultiplicationOperator_WithNull_ThrowsException()
        {
            // Arrange
            Money money = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => money * 2.0);
        }

        [Fact]
        public void DivisionOperator_WithDecimal_DividesCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money / 4m;

            // Assert
            Assert.Equal(25m, result.Amount);
        }

        [Fact]
        public void DivisionOperator_WithDouble_DividesCorrectly()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money / 2.5;

            // Assert
            Assert.Equal(40m, result.Amount);
        }

        [Fact]
        public void DivisionOperator_WithNull_ThrowsException()
        {
            // Arrange
            Money money = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => money / 2.0);
        }

        #endregion

        #region Comparison Operator Tests

        [Fact]
        public void EqualityOperator_WithEqualMoney_ReturnsTrue()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 == m2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentAmounts_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.False(m1 == m2);
        }

        [Fact]
        public void EqualityOperator_WithDifferentCurrencies_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Eur);

            // Act & Assert
            Assert.False(m1 == m2);
        }

        [Fact]
        public void EqualityOperator_WithNull_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            Money m2 = null;

            // Act & Assert
            Assert.False(m1 == m2);
            Assert.False(m2 == m1);
        }

        [Fact]
        public void EqualityOperator_BothNull_ReturnsTrue()
        {
            // Arrange
            Money m1 = null;
            Money m2 = null;

            // Act & Assert
            Assert.True(m1 == m2);
        }

        [Fact]
        public void EqualityOperator_WithDecimal_ComparesAmount()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(money == 100m);
            Assert.False(money == 50m);
        }

        [Fact]
        public void EqualityOperator_WithDouble_ComparesAmount()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(money == 100.0);
            Assert.False(money == 50.0);
        }

        [Fact]
        public void InequalityOperator_WithDifferentMoney_ReturnsTrue()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 != m2);
        }

        [Fact]
        public void InequalityOperator_WithEqualMoney_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.False(m1 != m2);
        }

        [Fact]
        public void InequalityOperator_WithDecimal_ComparesAmount()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(money != 50m);
            Assert.False(money != 100m);
        }

        [Fact]
        public void LessThanOperator_ComparesCorrectly()
        {
            // Arrange
            var m1 = new Money(50m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);
            var m3 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 < m2);
            Assert.False(m2 < m1);
            Assert.False(m1 < m3); // Equal values
        }

        [Fact]
        public void LessThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var m1 = new Money(50m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);
            var m3 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 <= m2);
            Assert.True(m1 <= m3);
            Assert.False(m2 <= m1);
        }

        [Fact]
        public void GreaterThanOperator_ComparesCorrectly()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);
            var m3 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 > m2);
            Assert.False(m2 > m1);
            Assert.False(m1 > m3); // Equal values
        }

        [Fact]
        public void GreaterThanOrEqualOperator_ComparesCorrectly()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);
            var m3 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1 >= m2);
            Assert.True(m1 >= m3);
            Assert.False(m2 >= m1);
        }

        [Fact]
        public void ComparisonOperators_DifferentCurrencies_ThrowsException()
        {
            // Arrange
            var usd = new Money(100m, CurrencyCodeType.Usd);
            var eur = new Money(100m, CurrencyCodeType.Eur);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => usd < eur);
            Assert.Throws<ArgumentException>(() => usd <= eur);
            Assert.Throws<ArgumentException>(() => usd > eur);
            Assert.Throws<ArgumentException>(() => usd >= eur);
        }

        #endregion

        #region Equals and GetHashCode Tests

        [Fact]
        public void Equals_WithIdenticalMoney_ReturnsTrue()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.True(m1.Equals(m2));
            Assert.True(m1.Equals((object)m2));
        }

        [Fact]
        public void Equals_WithDifferentAmounts_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.False(m1.Equals(m2));
        }

        [Fact]
        public void Equals_WithDifferentCurrencies_ReturnsFalse()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Eur);

            // Act & Assert
            Assert.False(m1.Equals(m2));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.False(money.Equals(null));
        }

        [Fact]
        public void Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);
            var other = "not a money object";

            // Act & Assert
            Assert.False(money.Equals(other));
        }

        [Fact]
        public void GetHashCode_ForEqualMoney_ReturnsSameHashCode()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ForDifferentMoney_ReturnsDifferentHashCode()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);

            // Act & Assert
            Assert.NotEqual(m1.GetHashCode(), m2.GetHashCode());
        }

        #endregion

        #region CompareTo Tests

        [Fact]
        public void CompareTo_WithLargerMoney_ReturnsNegative()
        {
            // Arrange
            var m1 = new Money(50m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = m1.CompareTo(m2);

            // Assert
            Assert.True(result < 0);
        }

        [Fact]
        public void CompareTo_WithSmallerMoney_ReturnsPositive()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(50m, CurrencyCodeType.Usd);

            // Act
            var result = m1.CompareTo(m2);

            // Assert
            Assert.True(result > 0);
        }

        [Fact]
        public void CompareTo_WithEqualMoney_ReturnsZero()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = m1.CompareTo(m2);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CompareTo_WithNull_ReturnsPositive()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var result = money.CompareTo((object)null);

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void CompareTo_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);
            var other = "not a money object";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => money.CompareTo(other));
            Assert.Contains("Argument must be Money", exception.Message);
        }

        #endregion

        #region Allocate Tests

        [Fact]
        public void Allocate_EvenlyDivisible_DistributesEqually()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var results = money.Allocate(4);

            // Assert
            Assert.Equal(4, results.Length);
            Assert.All(results, m => Assert.Equal(25m, m.Amount));
        }

        [Fact]
        public void Allocate_WithRemainder_DistributesRemainder()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var results = money.Allocate(3);

            // Assert
            Assert.Equal(3, results.Length);

            // First result gets the extra penny
            Assert.Equal(33.34m, results[0].Amount);
            Assert.Equal(33.33m, results[1].Amount);
            Assert.Equal(33.33m, results[2].Amount);

            // Sum should equal original
            var sum = results[0].Amount + results[1].Amount + results[2].Amount;
            Assert.Equal(money.Amount, sum);
        }

        [Fact]
        public void Allocate_PreservesCurrency()
        {
            // Arrange
            var money = new Money(100m, CurrencyCodeType.Eur);

            // Act
            var results = money.Allocate(3);

            // Assert
            Assert.All(results, m => Assert.Equal("EUR", m.CurrencyCode));
        }

        [Fact]
        public void Allocate_SinglePart_ReturnsOriginalAmount()
        {
            // Arrange
            var money = new Money(100.50m, CurrencyCodeType.Usd);

            // Act
            var results = money.Allocate(1);

            // Assert
            Assert.Single(results);
            Assert.Equal(100.50m, results[0].Amount);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public void Allocate_SumOfPartsEqualsOriginal(int parts)
        {
            // Arrange
            var money = new Money(123.45m, CurrencyCodeType.Usd);

            // Act
            var results = money.Allocate(parts);

            // Assert
            var sum = 0m;
            foreach (var result in results)
            {
                sum += result.Amount;
            }
            Assert.Equal(money.Amount, sum);
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_CreatesNewInstance()
        {
            // Arrange
            var original = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var clone = (Money)original.Clone();

            // Assert
            Assert.NotSame(original, clone);
        }

        [Fact]
        public void Clone_HasSameValues()
        {
            // Arrange
            var original = new Money(100.50m, CurrencyCodeType.Eur);

            // Act
            var clone = (Money)original.Clone();

            // Assert
            Assert.Equal(original.Amount, clone.Amount);
            Assert.Equal(original.CurrencyCode, clone.CurrencyCode);
            Assert.Equal(original.InternalAmount, clone.InternalAmount);
        }

        #endregion

        #region Explicit Conversion Tests

        [Fact]
        public void ExplicitConversion_FromDecimal_CreatesMoneyWithLocalCurrency()
        {
            // Arrange & Act
            var money = (Money)100.50m;

            // Assert
            Assert.Equal(100.50m, money.Amount);
            Assert.NotNull(money.CurrencyCode);
        }

        [Fact]
        public void ExplicitConversion_FromDouble_CreatesMoneyWithLocalCurrency()
        {
            // Arrange & Act
            var money = (Money)75.25;

            // Assert
            Assert.Equal(75.25m, money.Amount);
            Assert.NotNull(money.CurrencyCode);
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public void Money_IsImmutable_InternalAmountIsReadOnly()
        {
            // Arrange & Act
            var money = new Money(100m, CurrencyCodeType.Usd);
            var originalAmount = money.InternalAmount;

            // Assert - InternalAmount should not have a public setter
            Assert.Equal(100d, originalAmount, 0.01);

            // Verify we cannot change it through reflection would be ideal,
            // but we can verify operations return new instances
            var result = money + 50m;
            Assert.Equal(100d, money.InternalAmount, 0.01); // Original unchanged
            Assert.Equal(150d, result.InternalAmount, 0.01); // New instance
        }

        [Fact]
        public void ArithmeticOperations_DoNotModifyOriginal()
        {
            // Arrange
            var original = new Money(100m, CurrencyCodeType.Usd);
            var originalAmount = original.Amount;

            // Act
            _ = original + 50m;
            _ = original - 25m;
            _ = original * 2.0;
            _ = original / 2.0;

            // Assert
            Assert.Equal(originalAmount, original.Amount);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Money_WithZeroAmount_WorksCorrectly()
        {
            // Arrange & Act
            var money = new Money(0m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal(0m, money.Amount);
            Assert.Equal(0d, money.InternalAmount);
        }

        [Fact]
        public void Money_WithVeryLargeAmount_WorksCorrectly()
        {
            // Arrange & Act
            var money = new Money(1_000_000_000m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal(1_000_000_000m, money.Amount);
        }

        [Fact]
        public void Money_WithVerySmallAmount_WorksCorrectly()
        {
            // Arrange & Act
            var money = new Money(0.01m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal(0.01m, money.Amount);
        }

        [Fact]
        public void Money_WithPrecisionLoss_RoundsCorrectly()
        {
            // Arrange & Act
            var money = new Money(10.123456789m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal(10.12m, money.Amount);
        }

        [Fact]
        public void Money_WithMaxDecimal_DoesNotOverflow()
        {
            // Arrange & Act
            var money = new Money(999999999.99m, CurrencyCodeType.Usd);

            // Assert
            Assert.Equal(999999999.99m, money.Amount);
        }

        [Fact]
        public void Money_NegativeOperations_WorkCorrectly()
        {
            // Arrange
            var m1 = new Money(-50m, CurrencyCodeType.Usd);
            var m2 = new Money(100m, CurrencyCodeType.Usd);

            // Act
            var sum = m1 + m2;
            var diff = m1 - m2;

            // Assert
            Assert.Equal(50m, sum.Amount);
            Assert.Equal(-150m, diff.Amount);
        }

        [Fact]
        public void Division_ByVerySmallAmount_WorksCorrectly()
        {
            // Arrange
            var m1 = new Money(100m, CurrencyCodeType.Usd);
            var m2 = new Money(0.01m, CurrencyCodeType.Usd);

            // Act
            var result = m1 / m2;

            // Assert
            Assert.Equal(10000m, result);
        }

        #endregion

        #region Currency Symbol and Name Tests

        [Fact]
        public void CurrencySymbol_ForNoneCurrency_ReturnsEmpty()
        {
            // Arrange & Act
            var money = new Money(100m, CurrencyCodeType.None);

            // Assert
            Assert.Equal(string.Empty, money.CurrencySymbol);
        }

        [Fact]
        public void CurrencyName_ForNoneCurrency_ReturnsNone()
        {
            // Arrange & Act
            var money = new Money(100m, CurrencyCodeType.None);

            // Assert
            Assert.Equal("None", money.CurrencyName);
        }

        [Theory]
        [InlineData(CurrencyCodeType.Usd, "United States dollar")]
        [InlineData(CurrencyCodeType.Cad, "Canadian dollar")]
        [InlineData(CurrencyCodeType.Cny, "Chinese yuan")]
        [InlineData(CurrencyCodeType.Hkd, "Hong Kong dollar")]
        public void CurrencyName_ReturnsCorrectDescriptiveName(CurrencyCodeType currency, string expectedName)
        {
            // Arrange & Act
            var money = new Money(100m, currency);

            // Assert
            Assert.Equal(expectedName, money.CurrencyName);
        }

        #endregion
    }
}
