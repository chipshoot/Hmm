// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;

namespace Hmm.Core.Map.Tests;

public class NoteCatalogMappingTests
{
    private readonly IMapper _mapper;

    public NoteCatalogMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HmmMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Can_Map_NoteCatalogDao_To_NoteCatalog()
    {
        // Arrange
        var catalogDao = SampleDataGenerator.GetCatalogDao();

        // Act
        var catalog = _mapper.Map<NoteCatalog>(catalogDao);

        // Assert
        Assert.NotNull(catalog);
        Assert.Equal("Diary", catalog.Name);
        Assert.Equal("Testing note catalog", catalog.Description);
        Assert.Empty(catalog.Schema);
        Assert.Equal(DomainEntity.NoteContentFormatType.Markdown, catalog.Type);
        Assert.False(catalog.IsDefault);
    }

    [Fact]
    public void Can_Map_NoteCatalog_To_NoteCatalogDao()
    {
        // Arrange
        var catalog = SampleDataGenerator.GetCatalog();

        // Act
        var catalogDao = _mapper.Map<NoteCatalogDao>(catalog);

        // Assert
        Assert.NotNull(catalogDao);
        Assert.Equal("DiaryNote", catalogDao.Name);
        Assert.Equal("This is a testing catalog", catalogDao.Description);
        Assert.Empty(catalogDao.Schema);
    }
}