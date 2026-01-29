using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests.ValidatorTests
{
    public class UniqueNameValidationHelperTests
    {
        private readonly Mock<IRepository<AuthorDao>> _mockAuthorRepo;
        private readonly Mock<IRepository<NoteCatalogDao>> _mockCatalogRepo;
        private readonly Mock<ICompositeEntityRepository<TagDao, HmmNoteDao>> _mockTagRepo;

        public UniqueNameValidationHelperTests()
        {
            _mockAuthorRepo = new Mock<IRepository<AuthorDao>>();
            _mockCatalogRepo = new Mock<IRepository<NoteCatalogDao>>();
            _mockTagRepo = new Mock<ICompositeEntityRepository<TagDao, HmmNoteDao>>();
        }

        [Fact]
        public async Task NewEntity_WithUniqueName_ReturnsTrue()
        {
            // Arrange
            var newEntityId = 0;
            var uniqueName = "NewUniqueAuthor";

            // Entity doesn't exist (new entity)
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.NotFound("Not found"));

            // No existing authors with this name
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.NotFound("No results"));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                newEntityId,
                uniqueName,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task NewEntity_WithDuplicateName_ReturnsFalse()
        {
            // Arrange
            var newEntityId = 0;
            var duplicateName = "ExistingAuthor";

            // Entity doesn't exist (new entity)
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.NotFound("Not found"));

            // Existing author with this name found
            var existingAuthors = new PageList<AuthorDao>(
                new List<AuthorDao> { new AuthorDao { Id = 5, AccountName = duplicateName, IsActivated = true } },
                1, 1, 10);
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.Ok(existingAuthors));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                newEntityId,
                duplicateName,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistingEntity_UpdatingWithSameName_ReturnsTrue()
        {
            // Arrange
            var existingEntityId = 5;
            var sameName = "ExistingAuthor";

            // Entity exists
            var existingAuthor = new AuthorDao { Id = existingEntityId, AccountName = sameName, IsActivated = true };
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(existingEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.Ok(existingAuthor));

            // No OTHER authors with this name (only the entity being updated has it)
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.NotFound("No conflicts"));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                existingEntityId,
                sameName,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistingEntity_UpdatingToConflictingName_ReturnsFalse()
        {
            // Arrange
            var existingEntityId = 5;
            var conflictingName = "OtherAuthor";

            // Entity exists
            var existingAuthor = new AuthorDao { Id = existingEntityId, AccountName = "OriginalName", IsActivated = true };
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(existingEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.Ok(existingAuthor));

            // Another author with the conflicting name exists
            var conflictingAuthors = new PageList<AuthorDao>(
                new List<AuthorDao> { new AuthorDao { Id = 10, AccountName = conflictingName, IsActivated = true } },
                1, 1, 10);
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.Ok(conflictingAuthors));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                existingEntityId,
                conflictingName,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrWhitespaceName_ReturnsFalse(string name)
        {
            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                0,
                name,
                dao => dao.AccountName,
                null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CaseInsensitiveNameComparison_DetectsDuplicates()
        {
            // Arrange
            var newEntityId = 0;
            var uppercaseName = "TESTAUTHOR";

            // Entity doesn't exist (new entity)
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.NotFound("Not found"));

            // Existing author with lowercase name found
            var existingAuthors = new PageList<AuthorDao>(
                new List<AuthorDao> { new AuthorDao { Id = 5, AccountName = "testauthor", IsActivated = true } },
                1, 1, 10);
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.Ok(existingAuthors));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                newEntityId,
                uppercaseName,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert - Should detect duplicate despite case difference
            Assert.False(result);
        }

        [Fact]
        public async Task NoteCatalog_WithoutIsActivatedFilter_Works()
        {
            // Arrange - NoteCatalog doesn't have IsActivated filter
            var newEntityId = 0;
            var uniqueName = "NewCatalog";

            // Entity doesn't exist (new entity)
            _mockCatalogRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<NoteCatalogDao>.NotFound("Not found"));

            // No existing catalogs with this name
            _mockCatalogRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<NoteCatalogDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalogDao>>.NotFound("No results"));

            // Act - Note: additionalFilter is null for NoteCatalog
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<NoteCatalog, NoteCatalogDao>(
                _mockCatalogRepo.Object,
                newEntityId,
                uniqueName,
                dao => dao.Name,
                additionalFilter: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Tag_WithCompositeRepository_Works()
        {
            // Arrange
            var newEntityId = 0;
            var uniqueName = "NewTag";

            // Entity doesn't exist (new entity)
            _mockTagRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<TagDao>.NotFound("Not found"));

            // No existing tags with this name
            _mockTagRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<TagDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<TagDao>>.NotFound("No results"));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync(
                _mockTagRepo.Object,
                newEntityId,
                uniqueName,
                dao => dao.Name,
                dao => dao.IsActivated);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TrimsWhitespace_FromName()
        {
            // Arrange
            var newEntityId = 0;
            var nameWithWhitespace = "  TestName  ";

            // Entity doesn't exist (new entity)
            _mockAuthorRepo.Setup(r => r.GetEntityAsync(newEntityId))
                .ReturnsAsync(ProcessingResult<AuthorDao>.NotFound("Not found"));

            // Existing author with trimmed name found
            var existingAuthors = new PageList<AuthorDao>(
                new List<AuthorDao> { new AuthorDao { Id = 5, AccountName = "testname", IsActivated = true } },
                1, 1, 10);
            _mockAuthorRepo.Setup(r => r.GetEntitiesAsync(It.IsAny<Expression<Func<AuthorDao, bool>>>(), null))
                .ReturnsAsync(ProcessingResult<PageList<AuthorDao>>.Ok(existingAuthors));

            // Act
            var result = await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _mockAuthorRepo.Object,
                newEntityId,
                nameWithWhitespace,
                dao => dao.AccountName,
                dao => dao.IsActivated);

            // Assert - Should detect duplicate after trimming whitespace
            Assert.False(result);
        }
    }
}
