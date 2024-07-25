// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;

namespace Hmm.Core.Map.Tests;

public class NoteMappingTests
{
    private readonly IMapper _mapper;

    public NoteMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HmmMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Can_Map_NoteDao_To_Note()
    {
        // Arrange
        var noteDao = SampleDataGenerator.GetNoteDao();

        // Act
        var note = _mapper.Map<HmmNote>(noteDao);

        // Assert
        Assert.NotNull(note);
        Assert.Equal("ComputerPeripheral", note.Subject);
        Assert.False(note.IsDeleted);
    }

    [Fact]
    public void Can_Map_Note_To_NoteDao()
    {
        // Arrange
        var note = SampleDataGenerator.GetNote();

        // Act
        var noteDao = _mapper.Map<HmmNoteDao>(note);

        // Assert
        Assert.NotNull(noteDao);
        Assert.Equal("SystemConfiguration", noteDao.Subject);
        Assert.False(noteDao.IsDeleted);
    }
}