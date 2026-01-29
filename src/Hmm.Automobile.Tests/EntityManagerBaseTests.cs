using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    /// <summary>
    /// Tests for EntityManagerBase common functionality.
    /// Verifies that the base class correctly provides common CRUD operations
    /// to derived managers (AutomobileManager, DiscountManager, GasStationManager).
    /// </summary>
    public class EntityManagerBaseTests
    {
        private readonly Mock<INoteSerializer<AutomobileInfo>> _mockSerializer;
        private readonly Mock<IHmmValidator<AutomobileInfo>> _mockValidator;
        private readonly Mock<IHmmNoteManager> _mockNoteManager;
        private readonly Mock<IEntityLookup> _mockLookup;
        private readonly Mock<IAuthorProvider> _mockAuthorProvider;

        public EntityManagerBaseTests()
        {
            _mockSerializer = new Mock<INoteSerializer<AutomobileInfo>>();
            _mockValidator = new Mock<IHmmValidator<AutomobileInfo>>();
            _mockNoteManager = new Mock<IHmmNoteManager>();
            _mockLookup = new Mock<IEntityLookup>();
            _mockAuthorProvider = new Mock<IAuthorProvider>();

            // Default author setup
            var author = new Author { Id = 1, AccountName = "testuser" };
            _mockAuthorProvider.Setup(p => p.CachedAuthor).Returns(author);
            _mockAuthorProvider.Setup(p => p.GetAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Ok(author));
        }

        [Fact]
        public void Constructor_NullValidator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutomobileManager(
                _mockSerializer.Object,
                null,
                _mockNoteManager.Object,
                _mockLookup.Object,
                _mockAuthorProvider.Object));
        }

        [Fact]
        public void Constructor_NullNoteManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutomobileManager(
                _mockSerializer.Object,
                _mockValidator.Object,
                null,
                _mockLookup.Object,
                _mockAuthorProvider.Object));
        }

        [Fact]
        public void Constructor_NullLookupRepo_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutomobileManager(
                _mockSerializer.Object,
                _mockValidator.Object,
                _mockNoteManager.Object,
                null,
                _mockAuthorProvider.Object));
        }

        [Fact]
        public void Constructor_NullAuthorProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutomobileManager(
                _mockSerializer.Object,
                _mockValidator.Object,
                _mockNoteManager.Object,
                _mockLookup.Object,
                null));
        }

        [Fact]
        public void Constructor_NullNoteSerializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AutomobileManager(
                null,
                _mockValidator.Object,
                _mockNoteManager.Object,
                _mockLookup.Object,
                _mockAuthorProvider.Object));
        }

        [Fact]
        public async Task CreateAsync_NullEntity_ReturnsInvalid()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var result = await manager.CreateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_ValidationFails_ReturnsInvalid()
        {
            // Arrange
            var manager = CreateManager();
            var entity = new AutomobileInfo { Maker = "Honda" };

            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Invalid("Validation failed"));

            // Act
            var result = await manager.CreateAsync(entity);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Validation", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_NullEntity_ReturnsInvalid()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var result = await manager.UpdateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IsEntityOwnerAsync_EntityNotFound_ReturnsFalse()
        {
            // Arrange
            var manager = CreateManager();
            SetupCatalogLookup();
            SetupNoteNotFound();

            // Act
            var result = await manager.IsEntityOwnerAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Validator_ReturnsInjectedValidator()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert
            Assert.Same(_mockValidator.Object, manager.Validator);
        }

        [Fact]
        public void AuthorProvider_ReturnsInjectedProvider()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert
            Assert.Same(_mockAuthorProvider.Object, manager.AuthorProvider);
        }

        [Fact]
        public void NoteSerializer_ReturnsInjectedSerializer()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert
            Assert.Same(_mockSerializer.Object, manager.NoteSerializer);
        }

        private AutomobileManager CreateManager()
        {
            return new AutomobileManager(
                _mockSerializer.Object,
                _mockValidator.Object,
                _mockNoteManager.Object,
                _mockLookup.Object,
                _mockAuthorProvider.Object);
        }

        private void SetupNoteNotFound()
        {
            _mockNoteManager.Setup(m => m.GetNoteByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<HmmNote>.NotFound("Not found"));
        }

        private void SetupCatalogLookup()
        {
            var catalog = new NoteCatalog { Id = 1, Name = "AutomobileInfo" };
            var catalogList = new PageList<NoteCatalog>(new[] { catalog }, 1, 1, 10);
            _mockLookup.Setup(l => l.GetEntitiesAsync<NoteCatalog>(
                    It.IsAny<System.Linq.Expressions.Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(catalogList));
        }
    }
}
