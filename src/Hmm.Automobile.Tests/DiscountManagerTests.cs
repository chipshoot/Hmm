using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class DiscountManagerTests : AutoTestFixtureBase
    {
        private IAutoEntityManager<GasDiscount> _manager;

        public DiscountManagerTests()
        {
            SetupDevEnv();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidDiscount_ReturnsCreatedEntity()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Program = "Costco membership",
                Amount = new Money(0.6m),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount",
                IsActive = true
            };

            // Act
            var result = await _manager.CreateAsync(discount);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            Assert.Equal("Costco membership", result.Value.Program);
            Assert.Equal(TestDefaultAuthor.Id, result.Value.AuthorId);
        }

        [Fact]
        public async Task CreateAsync_WithNullEntity_ReturnsInvalidResult()
        {
            // Act
            var result = await _manager.CreateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidEntity_ReturnsValidationError()
        {
            // Arrange - missing required fields
            var discount = new GasDiscount
            {
                Program = "", // Empty - invalid
                Amount = new Money(0.6m)
            };

            // Act
            var result = await _manager.CreateAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidDiscount_ReturnsUpdatedEntity()
        {
            // Arrange
            var discounts = await SetupEnvironmentAsync();
            var discount = discounts.FirstOrDefault();
            Assert.NotNull(discount);

            // Act
            discount.Program = "Petro-Canada";
            var result = await _manager.UpdateAsync(discount);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Petro-Canada", result.Value.Program);
        }

        [Fact]
        public async Task UpdateAsync_WithNullEntity_ReturnsInvalidResult()
        {
            // Act
            var result = await _manager.UpdateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentEntity_ReturnsNotFound()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Id = 99999, // Non-existent
                Program = "Test",
                Amount = new Money(0.5m)
            };

            // Act
            var result = await _manager.UpdateAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllProperties()
        {
            // Arrange
            var discounts = await SetupEnvironmentAsync();
            var discount = discounts.First();

            // Act
            discount.Program = "Updated Program";
            discount.Amount = new Money(1.5m);
            discount.Comment = "Updated Comment";
            discount.DiscountType = GasDiscountType.Flat;
            discount.IsActive = false;

            var result = await _manager.UpdateAsync(discount);

            // Assert
            Assert.True(result.Success);
            var updated = result.Value;
            Assert.Equal("Updated Program", updated.Program);
            Assert.Equal(1.5m, updated.Amount.Amount);
            Assert.Equal("Updated Comment", updated.Comment);
            Assert.Equal(GasDiscountType.Flat, updated.DiscountType);
            Assert.False(updated.IsActive);
        }

        #endregion

        #region GetEntitiesAsync Tests

        [Fact]
        public async Task GetEntitiesAsync_ReturnsAllDiscounts()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            var discounts = result.Value.ToList();
            Assert.Equal(2, discounts.Count);
        }

        [Fact]
        public async Task GetEntitiesAsync_WithPagination_ReturnsPagedResults()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters
            {
                PageNumber = 1,
                PageSize = 1
            });

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.CurrentPage);
        }

        #endregion

        #region GetEntityByIdAsync Tests

        [Fact]
        public async Task GetEntityByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var discounts = await SetupEnvironmentAsync();
            var expectedDiscount = discounts.First();

            // Act
            var result = await _manager.GetEntityByIdAsync(expectedDiscount.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedDiscount.Id, result.Value.Id);
            Assert.Equal(expectedDiscount.Program, result.Value.Program);
        }

        [Fact]
        public async Task GetEntityByIdAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntityByIdAsync(99999);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region IsEntityOwnerAsync Tests

        [Fact]
        public async Task IsEntityOwnerAsync_WithOwnedEntity_ReturnsTrue()
        {
            // Arrange
            var discounts = await SetupEnvironmentAsync();
            var discount = discounts.First();

            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(discount.Id);

            // Assert
            Assert.True(isOwner);
        }

        [Fact]
        public async Task IsEntityOwnerAsync_WithNonExistentEntity_ReturnsFalse()
        {
            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(99999);

            // Assert
            Assert.False(isOwner);
        }

        #endregion

        #region Properties Tests

        [Fact]
        public void NoteSerializer_ReturnsValidSerializer()
        {
            // Assert
            Assert.NotNull(_manager.NoteSerializer);
        }

        [Fact]
        public void Validator_ReturnsValidValidator()
        {
            // Assert
            Assert.NotNull(_manager.Validator);
        }

        [Fact]
        public void AuthorProvider_ReturnsValidAuthor()
        {
            // Assert
            Assert.NotNull(_manager.AuthorProvider);
            Assert.NotNull(_manager.AuthorProvider.CachedAuthor);
            Assert.Equal(TestDefaultAuthor.AccountName, _manager.AuthorProvider.CachedAuthor.AccountName);
        }

        #endregion

        private async Task<List<GasDiscount>> SetupEnvironmentAsync()
        {
            var discounts = new List<GasDiscount>();

            var discount1 = new GasDiscount
            {
                Program = "Costco membership",
                Amount = new Money(0.6m),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount",
                IsActive = true
            };
            var result1 = await _manager.CreateAsync(discount1);
            Assert.True(result1.Success);
            discounts.Add(result1.Value);

            var discount2 = new GasDiscount
            {
                Program = "Petro-Canada membership",
                Amount = new Money(0.2m),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test Discount 2",
                IsActive = true
            };
            var result2 = await _manager.CreateAsync(discount2);
            Assert.True(result2.Success);
            discounts.Add(result2.Value);

            return discounts;
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            var noteSerializer = new GasDiscountJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<GasDiscount>());

            var noteManager = CreateNoteManager();

            _manager = new DiscountManager(
                noteSerializer,
                new GasDiscountValidator(LookupRepository),
                noteManager,
                LookupRepository,
                CreateMockAuthorProvider());
        }

        private IHmmNoteManager CreateNoteManager()
        {
            var mockNoteManager = new Mock<IHmmNoteManager>();
            var notes = new List<HmmNote>();
            var noteIdCounter = 1;

            // Setup CreateAsync
            mockNoteManager.Setup(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    note.Id = noteIdCounter++;
                    notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            // Setup UpdateAsync
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    var existing = notes.FirstOrDefault(n => n.Id == note.Id);
                    if (existing != null)
                    {
                        notes.Remove(existing);
                        notes.Add(note);
                        return ProcessingResult<HmmNote>.Ok(note);
                    }
                    return ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            // Setup GetNoteByIdAsync
            mockNoteManager.Setup(m => m.GetNoteByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync((int id, bool includeDeleted) =>
                {
                    var note = notes.FirstOrDefault(n => n.Id == id);
                    return note != null
                        ? ProcessingResult<HmmNote>.Ok(note)
                        : ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            // Setup GetNotesAsync
            mockNoteManager.Setup(m => m.GetNotesAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((
                    System.Linq.Expressions.Expression<Func<HmmNote, bool>> query,
                    bool includeDeleted,
                    ResourceCollectionParameters para) =>
                {
                    var filtered = notes.AsQueryable().Where(query).ToList();
                    var pageList = PageList<HmmNote>.Create(filtered.AsQueryable(), para?.PageNumber ?? 1, para?.PageSize ?? 20);
                    return ProcessingResult<PageList<HmmNote>>.Ok(pageList);
                });

            return mockNoteManager.Object;
        }
    }
}
