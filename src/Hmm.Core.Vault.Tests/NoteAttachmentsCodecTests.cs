using System.Text.Json;

namespace Hmm.Core.Vault.Tests;

/// <summary>
/// Mirrors the Flutter-side <c>attachment_ref_codec_test.dart</c>
/// so wire payloads round-trip byte-for-byte between client and
/// server. Adds server-only checks (vault-kind enforcement, schema
/// validation) that don't apply on the Flutter side.
/// </summary>
public class NoteAttachmentsCodecTests
{
    private static VaultRef SampleRef(string path = "attachments/note-1/a.jpg", long size = 100)
        => new()
        {
            Path = path,
            ContentType = "image/jpeg",
            ByteSize = size,
        };

    public class RoundTrip
    {
        [Fact]
        public void Encode_of_empty_returns_null()
        {
            Assert.Null(NoteAttachmentsCodec.Encode(NoteAttachments.Empty));
            Assert.Null(NoteAttachmentsCodec.Encode(null));
        }

        [Fact]
        public void Decode_of_null_or_empty_returns_empty()
        {
            Assert.True(NoteAttachmentsCodec.Decode(null).IsEmpty);
            Assert.True(NoteAttachmentsCodec.Decode("").IsEmpty);
        }

        [Fact]
        public void Primary_only_round_trips()
        {
            var primary = SampleRef("attachments/note-7/a.jpg", 100);
            var payload = new NoteAttachments(primary);

            var encoded = NoteAttachmentsCodec.Encode(payload);
            Assert.NotNull(encoded);
            var decoded = NoteAttachmentsCodec.Decode(encoded);

            Assert.Equal(payload, decoded);
            Assert.Equal(primary, decoded.PrimaryImage);
            Assert.Empty(decoded.Images);
        }

        [Fact]
        public void Gallery_only_round_trips_and_preserves_order()
        {
            var images = new List<VaultRef>
            {
                SampleRef("attachments/note-1/a.jpg", 1),
                SampleRef("attachments/note-1/b.jpg", 2),
                SampleRef("attachments/note-1/c.jpg", 3),
            };
            var payload = new NoteAttachments(images: images);

            var decoded = NoteAttachmentsCodec.Decode(
                NoteAttachmentsCodec.Encode(payload));

            Assert.Null(decoded.PrimaryImage);
            Assert.Equal(images.Count, decoded.Images.Count);
            for (int i = 0; i < images.Count; i++)
            {
                Assert.Equal(images[i], decoded.Images[i]);
            }
        }

        [Fact]
        public void Mixed_primary_and_gallery_round_trips()
        {
            var payload = new NoteAttachments(
                primaryImage: SampleRef("attachments/note-5/primary.jpg", 200),
                images: new List<VaultRef>
                {
                    SampleRef("attachments/note-5/extra-1.jpg", 50),
                    SampleRef("attachments/note-5/extra-2.jpg", 75),
                });

            var decoded = NoteAttachmentsCodec.Decode(
                NoteAttachmentsCodec.Encode(payload));

            Assert.Equal(payload, decoded);
        }

        [Fact]
        public void Encoded_shape_matches_design_doc()
        {
            // Verifies key set + lowercase camelCase property names.
            // The Flutter codec emits the same shape.
            var payload = new NoteAttachments(
                primaryImage: SampleRef("attachments/note-5/p.jpg", 100));
            var encoded = NoteAttachmentsCodec.Encode(payload);

            using var doc = JsonDocument.Parse(encoded!);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("primaryImage", out var primary));
            Assert.Equal("vault", primary.GetProperty("kind").GetString());
            Assert.Equal("attachments/note-5/p.jpg",
                primary.GetProperty("path").GetString());
            Assert.True(root.TryGetProperty("images", out var images));
            Assert.Equal(JsonValueKind.Array, images.ValueKind);
            Assert.Equal(0, images.GetArrayLength());
        }

        [Fact]
        public void Optional_originalName_is_preserved_when_set_and_omitted_when_null()
        {
            var withName = new NoteAttachments(
                new VaultRef
                {
                    Path = "attachments/note-1/a.jpg",
                    ContentType = "image/jpeg",
                    ByteSize = 10,
                    OriginalName = "vacation.jpg",
                });
            var roundTrippedWithName = NoteAttachmentsCodec.Decode(
                NoteAttachmentsCodec.Encode(withName));
            Assert.Equal("vacation.jpg",
                roundTrippedWithName.PrimaryImage!.OriginalName);

            // When null, serialise omits the key (CamelCase + ignore-null)
            var withoutName = new NoteAttachments(
                new VaultRef
                {
                    Path = "attachments/note-1/b.jpg",
                    ContentType = "image/jpeg",
                    ByteSize = 10,
                });
            var encoded = NoteAttachmentsCodec.Encode(withoutName);
            Assert.DoesNotContain("originalName", encoded);
        }
    }

    public class Validation
    {
        [Fact]
        public void Validate_returns_null_for_valid_payload()
        {
            var encoded = NoteAttachmentsCodec.Encode(
                new NoteAttachments(SampleRef()));
            Assert.Null(NoteAttachmentsCodec.Validate(encoded));
        }

        [Fact]
        public void Validate_returns_null_for_null_or_empty()
        {
            Assert.Null(NoteAttachmentsCodec.Validate(null));
            Assert.Null(NoteAttachmentsCodec.Validate(""));
        }

        [Fact]
        public void Validate_rejects_malformed_JSON()
        {
            var error = NoteAttachmentsCodec.Validate("{not-json");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_rejects_non_object_root()
        {
            var error = NoteAttachmentsCodec.Validate("[1,2,3]");
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_rejects_disallowed_content_type()
        {
            // image/bmp is not in the schema's enum.
            var bad = """
            { "primaryImage": { "kind":"vault","path":"a.bmp","contentType":"image/bmp","byteSize":1 } }
            """;
            var error = NoteAttachmentsCodec.Validate(bad);
            Assert.NotNull(error);
        }

        [Fact]
        public void Validate_rejects_missing_required_byteSize_on_vault_ref()
        {
            // vault refs require byteSize; the schema enforces this.
            var bad = """
            { "primaryImage": { "kind":"vault","path":"a.jpg","contentType":"image/jpeg" } }
            """;
            var error = NoteAttachmentsCodec.Validate(bad);
            Assert.NotNull(error);
        }
    }

    public class RejectsNonVaultKinds
    {
        [Fact]
        public void Decode_rejects_phasset_kind()
        {
            // The schema permits phasset (so the Flutter side can
            // parse client-stored payloads), but the .NET codec
            // rejects it server-side. Free → Paid migration is
            // supposed to rewrite these refs before upload.
            var phasset = """
            { "primaryImage": { "kind":"phasset","id":"ABC","contentType":"image/heic" } }
            """;
            var ex = Assert.Throws<FormatException>(
                () => NoteAttachmentsCodec.Decode(phasset));
            Assert.Contains("vault", ex.Message);
        }

        [Fact]
        public void Decode_rejects_cloudFile_kind()
        {
            var cloud = """
            { "primaryImage": { "kind":"cloudFile","provider":"oneDrive","path":"x.jpg","contentType":"image/jpeg" } }
            """;
            Assert.Throws<FormatException>(
                () => NoteAttachmentsCodec.Decode(cloud));
        }
    }

    public class PathValidation
    {
        [Fact]
        public void Decode_rejects_vault_ref_with_parent_segment()
        {
            var bad = """
            { "primaryImage": { "kind":"vault","path":"../escape.jpg","contentType":"image/jpeg","byteSize":1 } }
            """;
            var ex = Assert.Throws<FormatException>(
                () => NoteAttachmentsCodec.Decode(bad));
            Assert.Contains("path", ex.Message);
        }

        [Fact]
        public void Decode_rejects_vault_ref_with_backslash()
        {
            var bad = """
            { "primaryImage": { "kind":"vault","path":"a\\b.jpg","contentType":"image/jpeg","byteSize":1 } }
            """;
            Assert.Throws<FormatException>(
                () => NoteAttachmentsCodec.Decode(bad));
        }
    }

    public class Disjointness
    {
        [Fact]
        public void Constructor_rejects_primary_appearing_in_images()
        {
            var dup = SampleRef("attachments/note-1/dup.jpg");
            Assert.Throws<ArgumentException>(
                () => new NoteAttachments(dup, new List<VaultRef> { dup }));
        }

        [Fact]
        public void Decode_surfaces_disjointness_as_FormatException()
        {
            // A buggy older client could in theory emit a payload
            // that duplicates the primary in the gallery. Codec
            // must reject so a single error type covers all
            // "bad input."
            var refJson = """
            { "kind":"vault","path":"attachments/note-1/dup.jpg","contentType":"image/jpeg","byteSize":1 }
            """;
            var payload = $$"""
            { "primaryImage": {{refJson}}, "images": [ {{refJson}} ] }
            """;
            Assert.Throws<FormatException>(
                () => NoteAttachmentsCodec.Decode(payload));
        }
    }
}
