using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests;

/// <summary>
/// Phase 2b: location round-trips, and a PUT that omits location fields
/// must preserve the stored value (controller maps DTO onto loaded note).
/// </summary>
public class ApiNoteLocationMappingTests : CoreTestFixtureBase
{
    private static HmmNote Existing() => new()
    {
        Id = 1, Subject = "s", Content = "{}",
        Author = new Author { Id = 1 }, Catalog = new NoteCatalog { Id = 1 },
        Latitude = 47.6, Longitude = -122.3, LocationLabel = "Seattle, WA",
    };

    [Fact]
    public void Update_with_null_location_preserves_stored_value()
    {
        var note = Existing();
        var dto = new ApiNoteForUpdate { Subject = "s2" }; // location omitted

        ApiMapper.Map(dto, note);

        Assert.Equal(47.6, note.Latitude);
        Assert.Equal(-122.3, note.Longitude);
        Assert.Equal("Seattle, WA", note.LocationLabel);
    }

    [Fact]
    public void Update_with_location_overwrites()
    {
        var note = Existing();
        var dto = new ApiNoteForUpdate
        {
            Subject = "s2", Latitude = 1.0, Longitude = 2.0, LocationLabel = "X",
        };

        ApiMapper.Map(dto, note);

        Assert.Equal(1.0, note.Latitude);
        Assert.Equal(2.0, note.Longitude);
        Assert.Equal("X", note.LocationLabel);
    }

    [Fact]
    public void Create_maps_location_through()
    {
        var dto = new ApiNoteForCreate
        {
            Subject = "s", AuthorId = 1, CatalogId = 1,
            Latitude = 10.0, Longitude = 20.0, LocationLabel = "Y",
        };

        var note = ApiMapper.Map<ApiNoteForCreate, HmmNote>(dto);

        Assert.Equal(10.0, note.Latitude);
        Assert.Equal(20.0, note.Longitude);
        Assert.Equal("Y", note.LocationLabel);
    }
}
