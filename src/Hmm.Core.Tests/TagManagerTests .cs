using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class TagManagerTests : CoreTestFixtureBase
    {
        private readonly TagManager _tagManager;

        public TagManagerTests()
        {
            _tagManager = new TagManager(TagRepository, Mapper, LookupRepository);
        }

        [Fact]
        public async Task Can_Get_Tag_By_Id()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "tag1",
                Description = "Testing tag",
                IsActivated = true
            };
            var newTagResult = await _tagManager.CreateAsync(tag);
            Assert.True(newTagResult.Success);
            Assert.NotNull(newTagResult.Value);
            Assert.True(newTagResult.Value.Id >= 0, "newTag.Id is greater than or equal to 0");

            // Act
            var retrievedTagResult = await _tagManager.GetTagByIdAsync(newTagResult.Value.Id);

            // Assert
            Assert.True(retrievedTagResult.Success);
            Assert.NotNull(retrievedTagResult.Value);
            Assert.Equal(newTagResult.Value.Id, retrievedTagResult.Value.Id);
            Assert.Equal(tag.Name, retrievedTagResult.Value.Name);
            Assert.Equal(tag.Description, retrievedTagResult.Value.Description);
            Assert.Equal(tag.IsActivated, retrievedTagResult.Value.IsActivated);
        }

        [Fact]
        public async Task Can_Get_Tag_By_Name()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "tag1",
                Description = "Testing tag",
                IsActivated = true
            };
            var newTagResult = await _tagManager.CreateAsync(tag);
            Assert.True(newTagResult.Success);
            Assert.NotNull(newTagResult.Value);
            Assert.True(newTagResult.Value.Id >= 0, "newTag.Id is greater than or equal to 0");

            // Act
            var retrievedTagResult = await _tagManager.GetTagByNameAsync("tag1");

            // Assert
            Assert.True(retrievedTagResult.Success);
            Assert.NotNull(retrievedTagResult.Value);
            Assert.Equal(newTagResult.Value.Id, retrievedTagResult.Value.Id);
            Assert.Equal(tag.Name, retrievedTagResult.Value.Name);
            Assert.Equal(tag.Description, retrievedTagResult.Value.Description);
            Assert.Equal(tag.IsActivated, retrievedTagResult.Value.IsActivated);
        }

        [Fact]
        public async Task Can_Get_Tags()
        {
            // Act
            var tagsResult = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.True(tagsResult.Success);
            Assert.True(tagsResult.Value.Count >= 1, "authors.Count >= 1");
        }

        [Fact]
        public async Task Can_Get_Tag_With_Query()
        {
            // Act
            var tagsResult = await _tagManager.GetEntitiesAsync(t => t.Name == "ComputerPeripheral");

            // Assert
            Assert.True(tagsResult.Success);
            Assert.Single(tagsResult.Value);
        }

        [Fact]
        public async Task Cannot_Get_DeActivated_Tag()
        {
            // Arrange
            var tagResult = await _tagManager.GetTagByIdAsync(100);
            Assert.NotNull(tagResult.Value);

            // Act
            await _tagManager.DeActivateAsync(100);
            tagResult = await _tagManager.GetTagByIdAsync(100);

            // Assert
            Assert.Null(tagResult.Value);
        }

        [Fact]
        public async Task Cannot_Get_DeActivated_Tag_ByQuery()
        {
            // Arrange
            var tagResult = await _tagManager.GetTagByNameAsync("GasLog");
            Assert.NotNull(tagResult.Value);

            // Act
            await _tagManager.DeActivateAsync(tagResult.Value.Id);
            tagResult = await _tagManager.GetTagByNameAsync("GasLog");

            // Assert
            Assert.Null(tagResult.Value);
        }

        [Fact]
        public async Task Get_TagList_Does_Not_Contain_Deactivated_Tag()
        {
            // Arrange
            var tagsResult = await _tagManager.GetEntitiesAsync();
            var tagNumber = tagsResult.Value.Count;
            var tagResult = await _tagManager.GetTagByIdAsync(100);
            Assert.NotNull(tagResult.Value);
            await _tagManager.DeActivateAsync(100);

            // Act
            tagsResult = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.Equal(tagNumber - 1, tagsResult.Value.Count);
        }

        [Fact]
        public async Task Can_Add_Valid_Tag()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "tag1",
                Description = "Testing tag",
                IsActivated = true
            };

            // Act
            var newTagResult = await _tagManager.CreateAsync(tag);

            // Assert
            Assert.True(newTagResult.Success);
            Assert.NotNull(newTagResult.Value);
            Assert.True(newTagResult.Value.Id >= 0, "newTag.Id is greater to 0");
        }

        [Fact]
        public async Task Cannot_Add_Invalid_Tag()
        {
            // Arrange

            var tag = new Tag
            {
                Name = GetRandomString(255),
                IsActivated = true,
                Description = "Test invalid tag"
            };

            // Act
            var newTagResult = await _tagManager.CreateAsync(tag);

            // Assert
            Assert.False(newTagResult.Success);
            Assert.Equal("Name : 'Name' must be between 1 and 200 characters. You entered 255 characters.", newTagResult.Messages.First()?.Message);
            Assert.Null(newTagResult.Value);
        }

        [Fact]
        public async Task Can_Update_Valid_Tag()
        {
            // Arrange
            var tag = new Tag
            {
                Name = "testTag2",
                IsActivated = true,
                Description = "Test update tag"
            };
            var result = await _tagManager.CreateAsync(tag);
            Assert.True(result.Value.Id > 0, "tag.Id is greater then 0");

            //   Act
            var savedTagsResult = await _tagManager.GetEntitiesAsync();
            var savedTag = savedTagsResult.Value.FirstOrDefault(a => a.Id == result.Value.Id);
            Assert.NotNull(savedTag);
            savedTag.Description = "Updated tag";
            var updatedTagResult = await _tagManager.UpdateAsync(savedTag);

            //  Assert
            Assert.NotNull(updatedTagResult.Value);
            Assert.True(updatedTagResult.Success);
            Assert.Empty(updatedTagResult.Messages);
        }

        [Fact]
        public async Task Cannot_Update_InValid_Tag()
        {
            //    Arrange
            var tag = new Tag
            {
                Name = "jfang2",
                IsActivated = true,
                Description = "Sample Tag"
            };
            await _tagManager.CreateAsync(tag);
            Assert.True(tag.Id > 0, "newTag.Id is greater then 0");

            //   Act
            tag.Name = GetRandomString(255);
            var newTagResult = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(newTagResult.Success);
            Assert.Null(newTagResult.Value);
        }

        [Fact]
        public async Task Cannot_Update_Not_Exists_Tag()
        {
            // Arrange - no id
            var tag = new Tag
            {
                Name = "jfang2",
                IsActivated = true,
                Description = "Sample author"
            };

            //   Act
            var newTagResult = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(newTagResult.Success);
            Assert.Null(newTagResult.Value);

            // Arrange - id not exist
            tag = new Tag
            {
                Id = 20000,
                Name = "jfang2",
                IsActivated = true,
                Description = "Author with non exists id"
            };

            // Act
            newTagResult = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(newTagResult.Success);
            Assert.Null(newTagResult.Value);
        }

        [Fact]
        public async Task Can_Deactivate_Tag()
        {
            // Arrange
            var tagsResult = await _tagManager.GetEntitiesAsync();
            var tag = tagsResult.Value.FirstOrDefault();
            Assert.NotNull(tag);
            Assert.True(tag.IsActivated);

            // Act
            await _tagManager.DeActivateAsync(tag.Id);
            var updatedTagResult = await _tagManager.GetTagByIdAsync(tag.Id);

            // Assert
            Assert.True(updatedTagResult.Success);
            Assert.Empty(updatedTagResult.Messages);
            Assert.Null(updatedTagResult.Value);
        }

        [Fact]
        public async Task Cannot_Get_Deactivated_Tag()
        {
            // Arrange
            var tagsResult = await _tagManager.GetEntitiesAsync();
            var tagNum = tagsResult.Value.Count;
            var tag = tagsResult.Value.FirstOrDefault();
            Assert.NotNull(tag);
            Assert.True(tag.IsActivated);

            // Act
            await _tagManager.DeActivateAsync(tag.Id);
            var updatedTagResult = await _tagManager.GetTagByIdAsync(tag.Id);
            tagsResult = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.True(updatedTagResult.Success);
            Assert.Empty(updatedTagResult.Messages);
            Assert.Null(updatedTagResult.Value);
            Assert.Equal(tagNum - 1, tagsResult.Value.Count);
        }

        [Fact]
        public async Task Can_Check_Tag_Exists()
        {
            // Arrange
            var tagsResult = await _tagManager.GetEntitiesAsync();
            var tag = tagsResult.Value.FirstOrDefault();

            Assert.NotNull(tag);

            // Act
            var result = await _tagManager.IsTagExistsAsync(tag.Id);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, false)]
        public async Task Check_Tag_Exists_With_Invalid_Id_Get_False(int id, bool expectResult)
        {
            // Arrange
            // Act
            var result = await _tagManager.IsTagExistsAsync(id);

            // Act & Assert
            Assert.Equal(result, expectResult);
        }
    }
}