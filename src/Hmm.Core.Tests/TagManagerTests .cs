using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            var mapper = config.CreateMapper();
            _tagManager = new TagManager(TagRepository, mapper);
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
            var newTag = await _tagManager.CreateAsync(tag);
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.NotNull(newTag);
            Assert.True(newTag.Id >= 0, "newTag.Id is greater than or equal to 0");

            // Act
            var retrievedTag = await _tagManager.GetTagByIdAsync(newTag.Id);

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.NotNull(retrievedTag);
            Assert.Equal(newTag.Id, retrievedTag.Id);
            Assert.Equal(tag.Name, retrievedTag.Name);
            Assert.Equal(tag.Description, retrievedTag.Description);
            Assert.Equal(tag.IsActivated, retrievedTag.IsActivated);
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
            var newTag = await _tagManager.CreateAsync(tag);
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.NotNull(newTag);
            Assert.True(newTag.Id >= 0, "newTag.Id is greater than or equal to 0");

            // Act
            var retrievedTag = await _tagManager.GetTagByNameAsync("tag1");

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.NotNull(retrievedTag);
            Assert.Equal(newTag.Id, retrievedTag.Id);
            Assert.Equal(tag.Name, retrievedTag.Name);
            Assert.Equal(tag.Description, retrievedTag.Description);
            Assert.Equal(tag.IsActivated, retrievedTag.IsActivated);
        }

        [Fact]
        public async Task Can_Get_Tags()
        {
            // Act
            var tags = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.True(tags.Count >= 1, "authors.Count >= 1");
        }

        [Fact]
        public async Task Can_Get_Tag_With_Query()
        {
            // Act
            var tags = await _tagManager.GetEntitiesAsync(a => a.Name == "ComputerPeripheral");

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.Single(tags);
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
            var newTag = await _tagManager.CreateAsync(tag);

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.NotNull(newTag);
            Assert.True(newTag.Id >= 0, "newTag.Id is greater to 0");
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
            var newTag = await _tagManager.CreateAsync(tag);

            // Assert
            Assert.False(_tagManager.ProcessResult.Success);
            Assert.Equal("Name : 'Name' must be between 1 and 200 characters. You entered 255 characters.", _tagManager.ProcessResult.MessageList.First()?.Message);
            Assert.Null(newTag);
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
            Assert.True(result.Id > 0, "tag.Id is greater then 0");

            //   Act
            var savedTags = await _tagManager.GetEntitiesAsync();
            var savedTag = savedTags.FirstOrDefault(a => a.Id == result.Id);
            Assert.NotNull(savedTag);
            savedTag.Description = "Updated tag";
            var updatedTag = await _tagManager.UpdateAsync(savedTag);

            //  Assert
            Assert.NotNull(updatedTag);
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.Empty(_tagManager.ProcessResult.MessageList);
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
            var newTag = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(_tagManager.ProcessResult.Success);
            //Assert.True(_tagManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("Tag is invalid"));
            Assert.Null(newTag);
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
            var newTag = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(_tagManager.ProcessResult.Success);
            Assert.Null(newTag);

            // Arrange - id not exist
            _tagManager.ProcessResult.Rest();
            tag = new Tag
            {
                Id = 20000,
                Name = "jfang2",
                IsActivated = true,
                Description = "Author with non exists id"
            };

            // Act
            newTag = await _tagManager.UpdateAsync(tag);

            //  Assert
            Assert.False(_tagManager.ProcessResult.Success);
            Assert.Null(newTag);
        }

        [Fact]
        public async Task Can_Deactivate_Tag()
        {
            // Arrange
            var tags = await _tagManager.GetEntitiesAsync();
            var tag = tags.FirstOrDefault();
            Assert.NotNull(tag);
            Assert.True(tag.IsActivated);

            // Act
            await _tagManager.DeActivateAsync(tag.Id);
            var updatedTag = await _tagManager.GetTagByIdAsync(tag.Id);

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.Empty(_tagManager.ProcessResult.MessageList);
            Assert.Null(updatedTag);
        }

        [Fact]
        public async Task Cannot_Get_Deactivated_Tag()
        {
            // Arrange
            var tags = await _tagManager.GetEntitiesAsync();
            var tagNum = tags.Count;
            var tag = tags.FirstOrDefault();
            Assert.NotNull(tag);
            Assert.True(tag.IsActivated);

            // Act
            await _tagManager.DeActivateAsync(tag.Id);
            var updatedTag = await _tagManager.GetTagByIdAsync(tag.Id);
            tags = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.True(_tagManager.ProcessResult.Success);
            Assert.Empty(_tagManager.ProcessResult.MessageList);
            Assert.Null(updatedTag);
            Assert.Equal(tagNum-1, tags.Count);
        }

        [Fact]
        public async Task Can_Check_Tag_Exists()
        {
            // Arrange
            var tags = await _tagManager.GetEntitiesAsync();
            var tag = tags.FirstOrDefault();
            Assert.NotNull(tag);

            // Act
            var result = await _tagManager.TagExistsAsync(tag.Id);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, false)]
        public async Task Check_Tag_Exists_With_Invalid_Id_Get_False(int id, bool expectResult)
        {
            // Arrange
            // Act
            var result = await _tagManager.TagExistsAsync(id);

            // Act & Assert
            Assert.Equal(result, expectResult);
        }
    }
}