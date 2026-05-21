using System.Collections.Generic;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests;

/// <summary>
/// AutoMapper coverage for the Phase 6c additions: PrimaryImage /
/// Images on the API DTOs round-trip through <c>ApiMappingProfile</c>
/// against the <c>HmmNote</c> domain entity.
/// </summary>
public class ApiNoteAttachmentsMappingTests : CoreTestFixtureBase
{
    private static VaultRef Ref(
        string path,
        string contentType = "image/jpeg",
        long byteSize = 100)
        => new() { Path = path, ContentType = contentType, ByteSize = byteSize };

    private static HmmNote MakeNote(VaultRef? primary, IList<VaultRef> images)
        => new()
        {
            Id = 1,
            Subject = "test",
            Content = "{}",
            Author = new Author { Id = 1 },
            Catalog = new NoteCatalog { Id = 1 },
            PrimaryImage = primary,
            Images = images,
        };

    [Fact]
    public void HmmNote_to_ApiNote_carries_primary_and_gallery()
    {
        var primary = Ref("attachments/note-1/p.jpg", "image/png", 500);
        var gallery = new List<VaultRef>
        {
            Ref("attachments/note-1/a.jpg"),
            Ref("attachments/note-1/b.jpg", "image/webp", 10),
        };
        var note = MakeNote(primary, gallery);

        var api = ApiMapper.Map<HmmNote, ApiNote>(note);

        Assert.Equal(primary, api.PrimaryImage);
        Assert.Equal(2, api.Images.Count);
        Assert.Equal(gallery[0], api.Images[0]);
        Assert.Equal(gallery[1], api.Images[1]);
    }

    [Fact]
    public void HmmNote_to_ApiNote_with_no_attachments_yields_null_and_empty()
    {
        var note = MakeNote(primary: null, images: new List<VaultRef>());

        var api = ApiMapper.Map<HmmNote, ApiNote>(note);

        Assert.Null(api.PrimaryImage);
        Assert.Empty(api.Images);
    }

    [Fact]
    public void ApiNoteForCreate_to_HmmNote_carries_attachments()
    {
        var primary = Ref("attachments/note-9/main.jpg");
        var dto = new ApiNoteForCreate
        {
            Subject = "s",
            Content = "{}",
            AuthorId = 1,
            CatalogId = 1,
            PrimaryImage = primary,
            Images = new List<VaultRef>
            {
                Ref("attachments/note-9/x.jpg", "image/heic", 20),
            },
        };

        var note = ApiMapper.Map<ApiNoteForCreate, HmmNote>(dto);

        Assert.Equal(primary, note.PrimaryImage);
        Assert.Single(note.Images);
        Assert.Equal("attachments/note-9/x.jpg", note.Images[0].Path);
        Assert.Equal("image/heic", note.Images[0].ContentType);
    }

    [Fact]
    public void ApiNoteForUpdate_to_HmmNote_carries_attachments()
    {
        var dto = new ApiNoteForUpdate
        {
            Subject = "s",
            Content = "{}",
            PrimaryImage = Ref("attachments/note-2/p.jpg"),
            Images = new List<VaultRef>(),
        };

        var note = ApiMapper.Map<ApiNoteForUpdate, HmmNote>(dto);

        Assert.NotNull(note.PrimaryImage);
        Assert.Equal("attachments/note-2/p.jpg", note.PrimaryImage!.Path);
        Assert.Empty(note.Images);
    }

    [Fact]
    public void HmmNote_to_ApiNoteForCreate_carries_attachments()
    {
        var primary = Ref("attachments/note-4/p.jpg");
        var note = MakeNote(primary, new List<VaultRef>
        {
            Ref("attachments/note-4/a.jpg"),
        });

        var dto = ApiMapper.Map<HmmNote, ApiNoteForCreate>(note);

        Assert.Equal(primary, dto.PrimaryImage);
        Assert.Single(dto.Images);
    }
}
