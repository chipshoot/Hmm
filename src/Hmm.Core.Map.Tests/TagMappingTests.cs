// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;

namespace Hmm.Core.Map.Tests;

public class TagMappingTests
{
    private readonly IMapper _mapper;

    public TagMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HmmMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Can_Map_TagDao_To_Tag()
    {
        // Arrange
        var tagDao = SampleDataGenerator.GetTagDao();

        // Act
        var tag = _mapper.Map<Tag>(tagDao);

        // Assert
        Assert.NotNull(tag);
        Assert.Equal("ComputerPeripheral", tag.Name);
        Assert.True(tag.IsActivated);
    }

    [Fact]
    public void Can_Map_Tag_To_TagDao()
    {
        // Arrange
        var tag = SampleDataGenerator.GetTag();

        // Act
        var tagDao = _mapper.Map<TagDao>(tag);

        // Assert
        Assert.NotNull(tagDao);
        Assert.Equal("SystemConfiguration", tagDao.Name);
        Assert.True(tagDao.IsActivated);
    }
}