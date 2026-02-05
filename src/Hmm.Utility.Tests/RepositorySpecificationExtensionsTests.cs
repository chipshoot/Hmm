using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Specification;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Utility.Tests
{
    public class RepositorySpecificationExtensionsTests
    {
        private class TestEntity : Entity
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        private class FakeRepository : IRepository<TestEntity>
        {
            public Expression<Func<TestEntity, bool>> LastQuery { get; private set; }
            public ResourceCollectionParameters LastParameters { get; private set; }
            private readonly PageList<TestEntity> _data;

            public FakeRepository(PageList<TestEntity> data = null)
            {
                _data = data ?? new PageList<TestEntity>();
            }

            public Task<ProcessingResult<PageList<TestEntity>>> GetEntitiesAsync(
                Expression<Func<TestEntity, bool>> query = null,
                ResourceCollectionParameters resourceCollectionParameters = null)
            {
                LastQuery = query;
                LastParameters = resourceCollectionParameters;
                return Task.FromResult(ProcessingResult<PageList<TestEntity>>.Ok(_data));
            }

            public Task<ProcessingResult<TestEntity>> GetEntityAsync(int id) =>
                Task.FromResult(ProcessingResult<TestEntity>.Ok(new TestEntity { Id = id }));

            public Task<ProcessingResult<TestEntity>> AddAsync(TestEntity entity) =>
                Task.FromResult(ProcessingResult<TestEntity>.Ok(entity));

            public Task<ProcessingResult<TestEntity>> UpdateAsync(TestEntity entity) =>
                Task.FromResult(ProcessingResult<TestEntity>.Ok(entity));

            public Task<ProcessingResult<Unit>> DeleteAsync(TestEntity entity) =>
                Task.FromResult(ProcessingResult<Unit>.Ok(Unit.Value));

            public void Flush() { }
        }

        [Fact]
        public async Task GetEntitiesAsync_WithSpecification_PassesExpressionToRepository()
        {
            // Arrange
            var repo = new FakeRepository();
            var spec = new Specification<TestEntity>(e => e.Age >= 18);

            // Act
            var result = await repo.GetEntitiesAsync(spec);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(repo.LastQuery);
        }

        [Fact]
        public async Task GetEntitiesAsync_WithSpecification_ExpressionFiltersCorrectly()
        {
            // Arrange
            var repo = new FakeRepository();
            var spec = new Specification<TestEntity>(e => e.Name == "Alice");

            // Act
            await repo.GetEntitiesAsync(spec);

            // Assert - verify the expression is functionally equivalent
            var compiledQuery = repo.LastQuery.Compile();
            Assert.True(compiledQuery(new TestEntity { Name = "Alice" }));
            Assert.False(compiledQuery(new TestEntity { Name = "Bob" }));
        }

        [Fact]
        public async Task GetEntitiesAsync_WithCombinedSpecification_PassesComposedExpression()
        {
            // Arrange
            var repo = new FakeRepository();
            var isAdult = new Specification<TestEntity>(e => e.Age >= 18);
            var isActive = new Specification<TestEntity>(e => e.IsActive);
            var combined = isAdult.And(isActive);

            // Act
            await repo.GetEntitiesAsync(combined);

            // Assert
            var compiledQuery = repo.LastQuery.Compile();
            Assert.True(compiledQuery(new TestEntity { Age = 25, IsActive = true }));
            Assert.False(compiledQuery(new TestEntity { Age = 25, IsActive = false }));
            Assert.False(compiledQuery(new TestEntity { Age = 15, IsActive = true }));
        }

        [Fact]
        public async Task GetEntitiesAsync_WithSpecificationAndPagination_PassesBothArguments()
        {
            // Arrange
            var repo = new FakeRepository();
            var spec = new Specification<TestEntity>(e => e.IsActive);
            var parameters = new ResourceCollectionParameters
            {
                PageNumber = 2,
                PageSize = 10
            };

            // Act
            await repo.GetEntitiesAsync(spec, parameters);

            // Assert
            Assert.NotNull(repo.LastQuery);
            Assert.Equal(2, repo.LastParameters.PageNumber);
            Assert.Equal(10, repo.LastParameters.PageSize);
        }

        [Fact]
        public async Task GetEntitiesAsync_WithNegatedSpecification_PassesNegatedExpression()
        {
            // Arrange
            var repo = new FakeRepository();
            var isActive = new Specification<TestEntity>(e => e.IsActive);
            var isInactive = isActive.Not();

            // Act
            await repo.GetEntitiesAsync(isInactive);

            // Assert
            var compiledQuery = repo.LastQuery.Compile();
            Assert.True(compiledQuery(new TestEntity { IsActive = false }));
            Assert.False(compiledQuery(new TestEntity { IsActive = true }));
        }

        [Fact]
        public async Task GetEntitiesAsync_WithOrSpecification_PassesOrExpression()
        {
            // Arrange
            var repo = new FakeRepository();
            var isSenior = new Specification<TestEntity>(e => e.Age >= 65);
            var isMinor = new Specification<TestEntity>(e => e.Age < 18);
            var combined = isSenior.Or(isMinor);

            // Act
            await repo.GetEntitiesAsync(combined);

            // Assert
            var compiledQuery = repo.LastQuery.Compile();
            Assert.True(compiledQuery(new TestEntity { Age = 70 }));
            Assert.True(compiledQuery(new TestEntity { Age = 10 }));
            Assert.False(compiledQuery(new TestEntity { Age = 30 }));
        }

        [Fact]
        public async Task GetEntitiesAsync_WithoutPagination_DefaultsToNull()
        {
            // Arrange
            var repo = new FakeRepository();
            var spec = new Specification<TestEntity>(e => e.IsActive);

            // Act
            await repo.GetEntitiesAsync(spec);

            // Assert
            Assert.Null(repo.LastParameters);
        }
    }
}
