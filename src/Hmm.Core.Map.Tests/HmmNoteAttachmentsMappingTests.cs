// Ignore Spelling: Dao

using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hmm.Core.Map.Tests;

/// <summary>
/// Round-trip coverage for the Phase 6b mapping: the JSON
/// <c>attachments</c> column on <c>HmmNoteDao</c> ↔ <c>PrimaryImage</c>
/// + <c>Images</c> on the <c>HmmNote</c> domain entity.
/// </summary>
public class HmmNoteAttachmentsMappingTests
{
    private readonly IMapper _mapper;

    public HmmNoteAttachmentsMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HmmMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    private static HmmNoteDao MinimalDao(string? attachments = null) => new()
    {
        Id = 1,
        Subject = "test",
        Content = "{}",
        Attachments = attachments,
        CreateDate = DateTime.UtcNow,
        LastModifiedDate = DateTime.UtcNow,
    };

    private static HmmNote MinimalNote(
        VaultRef? primary = null,
        IList<VaultRef>? images = null) => new()
    {
        Id = 1,
        Subject = "test",
        Content = "{}",
        CreateDate = DateTime.UtcNow,
        LastModifiedDate = DateTime.UtcNow,
        PrimaryImage = primary,
        Images = images ?? new List<VaultRef>(),
    };

    private static VaultRef Sample(
        string path = "attachments/note-1/a.jpg", long size = 100)
        => new() { Path = path, ContentType = "image/jpeg", ByteSize = size };

    // ----- DAO → Domain -----

    [Fact]
    public void Null_attachments_column_maps_to_no_primary_image_and_empty_images()
    {
        var dao = MinimalDao(attachments: null);

        var note = _mapper.Map<HmmNote>(dao);

        Assert.Null(note.PrimaryImage);
        Assert.Empty(note.Images);
    }

    [Fact]
    public void Files_in_the_column_map_to_domain_Files()
    {
        const string json =
            "{\"images\":[],\"files\":[{\"kind\":\"vault\",\"path\":\"attachments/n/r.pdf\",\"contentType\":\"application/pdf\",\"byteSize\":3}]}";
        var dao = MinimalDao(attachments: json);

        var note = _mapper.Map<HmmNote>(dao);

        Assert.Single(note.Files);
        Assert.Equal("application/pdf", note.Files[0].ContentType);
        Assert.Equal("attachments/n/r.pdf", note.Files[0].Path);
    }

    [Fact]
    public void Domain_Files_encode_back_into_the_column()
    {
        var pdf = new VaultRef
        {
            Path = "attachments/n/r.pdf",
            ContentType = "application/pdf",
            ByteSize = 3,
        };
        var note = MinimalNote();
        note.Files = new List<VaultRef> { pdf };

        var dao = _mapper.Map<HmmNoteDao>(note);

        Assert.NotNull(dao.Attachments);
        Assert.Contains("r.pdf", dao.Attachments!);
    }

    [Fact]
    public void Empty_string_attachments_maps_to_no_primary_image_and_empty_images()
    {
        var dao = MinimalDao(attachments: string.Empty);

        var note = _mapper.Map<HmmNote>(dao);

        Assert.Null(note.PrimaryImage);
        Assert.Empty(note.Images);
    }

    [Fact]
    public void Primary_only_attachments_column_round_trips()
    {
        var primary = Sample("attachments/note-7/p.jpg", 200);
        var encoded = NoteAttachmentsCodec.Encode(new NoteAttachments(primary));
        var dao = MinimalDao(attachments: encoded);

        var note = _mapper.Map<HmmNote>(dao);

        Assert.Equal(primary, note.PrimaryImage);
        Assert.Empty(note.Images);
    }

    [Fact]
    public void Gallery_attachments_round_trip_and_preserve_order()
    {
        var images = new List<VaultRef>
        {
            Sample("attachments/note-1/a.jpg", 1),
            Sample("attachments/note-1/b.jpg", 2),
            Sample("attachments/note-1/c.jpg", 3),
        };
        var encoded = NoteAttachmentsCodec.Encode(new NoteAttachments(images: images));
        var dao = MinimalDao(attachments: encoded);

        var note = _mapper.Map<HmmNote>(dao);

        Assert.Null(note.PrimaryImage);
        Assert.Equal(images.Count, note.Images.Count);
        for (int i = 0; i < images.Count; i++)
        {
            Assert.Equal(images[i], note.Images[i]);
        }
    }

    // ----- Domain → DAO -----

    [Fact]
    public void Empty_domain_attachments_encode_to_null_column()
    {
        var note = MinimalNote();

        var dao = _mapper.Map<HmmNoteDao>(note);

        // Empty payload encodes to null so the column stores SQL NULL,
        // not "{}".
        Assert.Null(dao.Attachments);
    }

    [Fact]
    public void Domain_primary_image_encodes_to_attachments_column_JSON()
    {
        var primary = Sample("attachments/note-9/main.jpg", 500);
        var note = MinimalNote(primary: primary);

        var dao = _mapper.Map<HmmNoteDao>(note);

        Assert.NotNull(dao.Attachments);
        // The column should be a valid wrapper with our primary ref.
        var decoded = NoteAttachmentsCodec.Decode(dao.Attachments);
        Assert.Equal(primary, decoded.PrimaryImage);
        Assert.Empty(decoded.Images);
    }

    [Fact]
    public void Domain_gallery_round_trips_through_DAO_column_to_a_fresh_HmmNote()
    {
        var primary = Sample("attachments/note-5/primary.jpg", 100);
        var gallery = new List<VaultRef>
        {
            Sample("attachments/note-5/x.jpg", 10),
            Sample("attachments/note-5/y.jpg", 20),
        };
        var original = MinimalNote(primary, gallery);

        // Round-trip through the DAO.
        var dao = _mapper.Map<HmmNoteDao>(original);
        var restored = _mapper.Map<HmmNote>(dao);

        Assert.Equal(primary, restored.PrimaryImage);
        Assert.Equal(gallery.Count, restored.Images.Count);
        for (int i = 0; i < gallery.Count; i++)
        {
            Assert.Equal(gallery[i], restored.Images[i]);
        }
    }
}
