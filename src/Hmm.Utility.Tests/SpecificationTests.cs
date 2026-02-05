using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Specification;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class SpecificationTests
    {
        // Test entity for specification tests
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        #region Specification<T> Tests

        [Fact]
        public void Specification_IsSatisfiedBy_ReturnsTrueWhenConditionMet()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.Age >= 18);
            var entity = new TestEntity { Age = 25 };

            // Act
            var result = spec.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Specification_IsSatisfiedBy_ReturnsFalseWhenConditionNotMet()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.Age >= 18);
            var entity = new TestEntity { Age = 15 };

            // Act
            var result = spec.IsSatisfiedBy(entity);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Specification_ToExpression_ReturnsUsableExpression()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.Name == "Test");
            var entities = new List<TestEntity>
            {
                new() { Name = "Test" },
                new() { Name = "Other" },
                new() { Name = "Test" }
            }.AsQueryable();

            // Act
            var expression = spec.ToExpression();
            var filtered = entities.Where(expression).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, e => Assert.Equal("Test", e.Name));
        }

        [Fact]
        public void Specification_Constructor_ThrowsOnNullExpression()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Specification<TestEntity>(null));
        }

        [Fact]
        public void Specification_ImplicitConversionFromExpression_Works()
        {
            // Arrange & Act
            System.Linq.Expressions.Expression<Func<TestEntity, bool>> expr = e => e.IsActive;
            Specification<TestEntity> spec = expr;

            // Assert
            Assert.True(spec.IsSatisfiedBy(new TestEntity { IsActive = true }));
            Assert.False(spec.IsSatisfiedBy(new TestEntity { IsActive = false }));
        }

        #endregion

        #region AndSpecification Tests

        [Fact]
        public void AndSpecification_IsSatisfiedBy_ReturnsTrueWhenBothConditionsMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 18);
            var spec2 = new Specification<TestEntity>(e => e.IsActive);
            var combined = spec1.And(spec2);
            var entity = new TestEntity { Age = 25, IsActive = true };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AndSpecification_IsSatisfiedBy_ReturnsFalseWhenFirstConditionNotMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 18);
            var spec2 = new Specification<TestEntity>(e => e.IsActive);
            var combined = spec1.And(spec2);
            var entity = new TestEntity { Age = 15, IsActive = true };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AndSpecification_IsSatisfiedBy_ReturnsFalseWhenSecondConditionNotMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 18);
            var spec2 = new Specification<TestEntity>(e => e.IsActive);
            var combined = spec1.And(spec2);
            var entity = new TestEntity { Age = 25, IsActive = false };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AndSpecification_ToExpression_CanBeUsedWithLinq()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 18);
            var spec2 = new Specification<TestEntity>(e => e.IsActive);
            var combined = spec1.And(spec2);
            var entities = new List<TestEntity>
            {
                new() { Age = 25, IsActive = true },
                new() { Age = 15, IsActive = true },
                new() { Age = 25, IsActive = false },
                new() { Age = 30, IsActive = true }
            }.AsQueryable();

            // Act
            var filtered = entities.Where(combined.ToExpression()).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, e => Assert.True(e.Age >= 18 && e.IsActive));
        }

        #endregion

        #region OrSpecification Tests

        [Fact]
        public void OrSpecification_IsSatisfiedBy_ReturnsTrueWhenFirstConditionMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 65);
            var spec2 = new Specification<TestEntity>(e => e.Age < 18);
            var combined = spec1.Or(spec2);
            var entity = new TestEntity { Age = 70 };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OrSpecification_IsSatisfiedBy_ReturnsTrueWhenSecondConditionMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 65);
            var spec2 = new Specification<TestEntity>(e => e.Age < 18);
            var combined = spec1.Or(spec2);
            var entity = new TestEntity { Age = 10 };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OrSpecification_IsSatisfiedBy_ReturnsFalseWhenNeitherConditionMet()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 65);
            var spec2 = new Specification<TestEntity>(e => e.Age < 18);
            var combined = spec1.Or(spec2);
            var entity = new TestEntity { Age = 30 };

            // Act
            var result = combined.IsSatisfiedBy(entity);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void OrSpecification_ToExpression_CanBeUsedWithLinq()
        {
            // Arrange
            var spec1 = new Specification<TestEntity>(e => e.Age >= 65);
            var spec2 = new Specification<TestEntity>(e => e.Age < 18);
            var combined = spec1.Or(spec2);
            var entities = new List<TestEntity>
            {
                new() { Age = 70 },
                new() { Age = 10 },
                new() { Age = 30 },
                new() { Age = 75 }
            }.AsQueryable();

            // Act
            var filtered = entities.Where(combined.ToExpression()).ToList();

            // Assert
            Assert.Equal(3, filtered.Count);
        }

        #endregion

        #region NotSpecification Tests

        [Fact]
        public void NotSpecification_IsSatisfiedBy_ReturnsTrueWhenConditionNotMet()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.IsActive);
            var notSpec = spec.Not();
            var entity = new TestEntity { IsActive = false };

            // Act
            var result = notSpec.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void NotSpecification_IsSatisfiedBy_ReturnsFalseWhenConditionMet()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.IsActive);
            var notSpec = spec.Not();
            var entity = new TestEntity { IsActive = true };

            // Act
            var result = notSpec.IsSatisfiedBy(entity);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void NotSpecification_ToExpression_CanBeUsedWithLinq()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.IsActive);
            var notSpec = spec.Not();
            var entities = new List<TestEntity>
            {
                new() { IsActive = true },
                new() { IsActive = false },
                new() { IsActive = false }
            }.AsQueryable();

            // Act
            var filtered = entities.Where(notSpec.ToExpression()).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, e => Assert.False(e.IsActive));
        }

        #endregion

        #region Complex Chaining Tests

        [Fact]
        public void Specifications_CanBeChainedComplexly()
        {
            // Arrange - Adult AND (Active OR VIP name)
            var isAdult = new Specification<TestEntity>(e => e.Age >= 18);
            var isActive = new Specification<TestEntity>(e => e.IsActive);
            var isVip = new Specification<TestEntity>(e => e.Name == "VIP");
            var combined = isAdult.And(isActive.Or(isVip));

            var entities = new List<TestEntity>
            {
                new() { Age = 25, IsActive = true, Name = "Regular" },   // Adult + Active = YES
                new() { Age = 25, IsActive = false, Name = "VIP" },      // Adult + VIP = YES
                new() { Age = 15, IsActive = true, Name = "Regular" },   // Not adult = NO
                new() { Age = 25, IsActive = false, Name = "Regular" },  // Adult but not active/VIP = NO
            }.AsQueryable();

            // Act
            var filtered = entities.Where(combined.ToExpression()).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public void Specifications_DoubleNotReturnsOriginal()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.IsActive);
            var doubleNot = spec.Not().Not();
            var entity = new TestEntity { IsActive = true };

            // Act
            var result = doubleNot.IsSatisfiedBy(entity);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region IQueryable Extension Tests

        [Fact]
        public void Satisfying_Extension_FiltersQueryableUsingSpecification()
        {
            // Arrange
            var spec = new Specification<TestEntity>(e => e.Age >= 21);
            var entities = new List<TestEntity>
            {
                new() { Id = 1, Age = 25 },
                new() { Id = 2, Age = 18 },
                new() { Id = 3, Age = 30 }
            }.AsQueryable();

            // Act
            var filtered = entities.Satisfying(spec).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
            Assert.Contains(filtered, e => e.Id == 1);
            Assert.Contains(filtered, e => e.Id == 3);
        }

        #endregion

        #region IsActivatedSpecification Tests

        private class ActivatableEntity : Entity, IActivatable
        {
            [StringLength(100)]
            public string Name { get; set; }
            public bool IsActivated { get; set; }
        }

        [Fact]
        public void IsActivatedSpecification_SatisfiedByActivatedEntity()
        {
            // Arrange
            var spec = new IsActivatedSpecification<ActivatableEntity>();
            var entity = new ActivatableEntity { IsActivated = true };

            // Act & Assert
            Assert.True(spec.IsSatisfiedBy(entity));
        }

        [Fact]
        public void IsActivatedSpecification_NotSatisfiedByDeactivatedEntity()
        {
            // Arrange
            var spec = new IsActivatedSpecification<ActivatableEntity>();
            var entity = new ActivatableEntity { IsActivated = false };

            // Act & Assert
            Assert.False(spec.IsSatisfiedBy(entity));
        }

        [Fact]
        public void IsActivatedSpecification_ToExpression_FiltersDeactivated()
        {
            // Arrange
            var spec = new IsActivatedSpecification<ActivatableEntity>();
            var entities = new List<ActivatableEntity>
            {
                new() { Id = 1, IsActivated = true },
                new() { Id = 2, IsActivated = false },
                new() { Id = 3, IsActivated = true }
            }.AsQueryable();

            // Act
            var filtered = entities.Where(spec.ToExpression()).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, e => Assert.True(e.IsActivated));
        }

        [Fact]
        public void IsActivatedSpecification_ComposesWithAndSpecification()
        {
            // Arrange
            var isActivated = new IsActivatedSpecification<ActivatableEntity>();
            var hasName = new Specification<ActivatableEntity>(e => e.Name == "Test");
            var combined = isActivated.And(hasName);

            // Act & Assert
            Assert.True(combined.IsSatisfiedBy(new ActivatableEntity { IsActivated = true, Name = "Test" }));
            Assert.False(combined.IsSatisfiedBy(new ActivatableEntity { IsActivated = true, Name = "Other" }));
            Assert.False(combined.IsSatisfiedBy(new ActivatableEntity { IsActivated = false, Name = "Test" }));
        }

        #endregion

        #region NameMatchSpecification Tests

        private class NamedEntity : Entity
        {
            [StringLength(200)]
            public string Name { get; set; }
            public bool IsActivated { get; set; }
        }

        [Fact]
        public void NameMatchSpecification_MatchesByNameCaseInsensitive()
        {
            // Arrange
            var spec = new NameMatchSpecification<NamedEntity>(e => e.Name, "test");

            // Act & Assert
            Assert.True(spec.IsSatisfiedBy(new NamedEntity { Name = "Test" }));
            Assert.True(spec.IsSatisfiedBy(new NamedEntity { Name = "TEST" }));
            Assert.True(spec.IsSatisfiedBy(new NamedEntity { Name = "test" }));
            Assert.False(spec.IsSatisfiedBy(new NamedEntity { Name = "other" }));
        }

        [Fact]
        public void NameMatchSpecification_WithAdditionalFilter_CombinesBothConditions()
        {
            // Arrange
            var spec = new NameMatchSpecification<NamedEntity>(
                e => e.Name,
                "test",
                e => e.IsActivated);

            // Act & Assert
            Assert.True(spec.IsSatisfiedBy(new NamedEntity { Name = "Test", IsActivated = true }));
            Assert.False(spec.IsSatisfiedBy(new NamedEntity { Name = "Test", IsActivated = false }));
            Assert.False(spec.IsSatisfiedBy(new NamedEntity { Name = "Other", IsActivated = true }));
        }

        [Fact]
        public void NameMatchSpecification_ToExpression_WorksWithLinq()
        {
            // Arrange
            var spec = new NameMatchSpecification<NamedEntity>(e => e.Name, "target");
            var entities = new List<NamedEntity>
            {
                new() { Id = 1, Name = "Target" },
                new() { Id = 2, Name = "Other" },
                new() { Id = 3, Name = "TARGET" }
            }.AsQueryable();

            // Act
            var filtered = entities.Where(spec.ToExpression()).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public void NameMatchSpecification_ComposesWithIdExclusion()
        {
            // Arrange - name match AND id != 5 (simulates update validation)
            var nameSpec = new NameMatchSpecification<NamedEntity>(e => e.Name, "test");
            var excludeId = new Specification<NamedEntity>(e => e.Id != 5);
            var combined = nameSpec.And(excludeId);

            var entities = new List<NamedEntity>
            {
                new() { Id = 5, Name = "Test" },   // excluded by ID
                new() { Id = 10, Name = "Test" },   // matches
                new() { Id = 15, Name = "Other" }   // excluded by name
            }.AsQueryable();

            // Act
            var filtered = entities.Where(combined.ToExpression()).ToList();

            // Assert
            Assert.Single(filtered);
            Assert.Equal(10, filtered[0].Id);
        }

        #endregion
    }
}
