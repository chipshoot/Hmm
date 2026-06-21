using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NJsonSchema;

namespace Hmm.Core.Vault;

/// <summary>
/// JSON codec for the <c>Notes.attachments</c> column value. Mirrors
/// the Flutter <c>NoteAttachmentsCodec</c> in
/// <c>lib/core/data/attachments/attachment_ref_codec.dart</c> — the
/// wire shape must agree byte-for-byte. Adds schema validation on
/// the server side because the API is the trust boundary between
/// clients and storage.
/// </summary>
/// <remarks>
/// <para>
/// Server-side, only <c>kind: "vault"</c> refs are accepted. The
/// Flutter client rewrites every <c>phasset</c> / <c>cloudFile</c>
/// ref into a <c>vault</c> ref before upload (the Free → Paid
/// migration step in Phase 18), so the API never sees the other
/// kinds. Anything else is rejected at the codec layer with a clear
/// message.
/// </para>
/// <para>
/// The schema in <c>Schemas/NoteAttachments.schema.json</c> covers
/// all three kinds (so the same file can drive client + server
/// validation). The codec adds a vault-only check on top.
/// </para>
/// </remarks>
public static class NoteAttachmentsCodec
{
    private const string AllowedKind = "vault";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Lazy<JsonSchema> Schema = new(LoadSchema);

    /// <summary>
    /// Decode the value of <c>Notes.attachments</c>. <c>null</c> or
    /// empty input returns <see cref="NoteAttachments.Empty"/>.
    /// Throws <see cref="FormatException"/> on schema violations,
    /// unknown kinds, or malformed JSON.
    /// </summary>
    public static NoteAttachments Decode(string? json)
    {
        if (string.IsNullOrEmpty(json)) return NoteAttachments.Empty;

        var schemaErrors = Validate(json);
        if (schemaErrors != null)
        {
            throw new FormatException(
                $"NoteAttachments: schema validation failed — {schemaErrors}");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        VaultRef? primary = null;
        if (root.TryGetProperty("primaryImage", out var primaryElement)
            && primaryElement.ValueKind != JsonValueKind.Null)
        {
            primary = DecodeVaultRef(primaryElement, "primaryImage");
        }

        var images = new List<VaultRef>();
        if (root.TryGetProperty("images", out var imagesElement)
            && imagesElement.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (var item in imagesElement.EnumerateArray())
            {
                images.Add(DecodeVaultRef(item, $"images[{i++}]"));
            }
        }

        var files = new List<VaultRef>();
        if (root.TryGetProperty("files", out var filesElement)
            && filesElement.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (var item in filesElement.EnumerateArray())
            {
                files.Add(DecodeVaultRef(item, $"files[{i++}]"));
            }
        }

        try
        {
            return new NoteAttachments(primary, images, files);
        }
        catch (ArgumentException ex)
        {
            // Surface disjointness as FormatException so callers
            // handle one error type for "bad input."
            throw new FormatException($"NoteAttachments: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Encode to a JSON string suitable for the column. Returns
    /// <c>null</c> when the payload is empty — the caller should
    /// store SQL NULL in that case.
    /// </summary>
    public static string? Encode(NoteAttachments? value)
    {
        if (value == null || value.IsEmpty) return null;

        var primary = value.PrimaryImage != null
            ? VaultRefToJson(value.PrimaryImage)
            : null;
        var images = value.Images
            .Select(VaultRefToJson)
            .ToList();
        var files = value.Files
            .Select(VaultRefToJson)
            .ToList();

        // Order-preserving dictionary (System.Text.Json keeps insertion
        // order). `files` is omitted when empty so existing images-only
        // payloads encode byte-identically to before Phase 3a.
        var dict = new Dictionary<string, object?>
        {
            ["primaryImage"] = primary,
            ["images"] = images,
        };
        if (files.Count > 0) dict["files"] = files;
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    /// <summary>
    /// Validate the raw column value against
    /// <c>NoteAttachments.schema.json</c>. Returns <c>null</c> on
    /// success, a human-readable error message on failure. Use this
    /// at the API boundary so unparsable / schema-violating payloads
    /// are rejected before hitting the DB.
    /// </summary>
    public static string? Validate(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        // Parse with System.Text.Json first so malformed input
        // surfaces as a JsonException we control, rather than
        // bubbling Newtonsoft.Json's JsonReaderException up from
        // NJsonSchema (which uses Newtonsoft internally).
        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return $"invalid JSON: {ex.Message}";
        }

        var errors = Schema.Value.Validate(json);
        if (errors.Count == 0) return null;
        return string.Join("; ", errors.Select(e =>
            $"{e.Path}: {e.Kind}"));
    }

    // ------------------------------------------------------------
    // Private — VaultRef shape mapping
    // ------------------------------------------------------------

    private static VaultRef DecodeVaultRef(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException(
                $"{path}: expected object, got {element.ValueKind}");
        }

        if (!element.TryGetProperty("kind", out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String)
        {
            throw new FormatException($"{path}: missing \"kind\"");
        }
        var kind = kindElement.GetString();
        if (kind != AllowedKind)
        {
            // Belt-and-braces: the schema's oneOf permits all three
            // kinds, but the .NET side only accepts vault. Tell the
            // caller why.
            throw new FormatException(
                $"{path}: kind \"{kind}\" not allowed on the server " +
                "(only \"vault\" — the client should have rewritten " +
                "phasset / cloudFile refs during Free → Paid upload).");
        }

        string? pathVal = TryGetString(element, "path");
        if (string.IsNullOrEmpty(pathVal))
        {
            throw new FormatException($"{path}: missing \"path\"");
        }
        // Reuse the same path-spec validator the storage layer uses
        // so paths agree across the wire.
        try
        {
            VaultPathUtil.Validate(pathVal);
        }
        catch (ArgumentException ex)
        {
            throw new FormatException(
                $"{path}.path: {ex.Message}");
        }

        var contentType = TryGetString(element, "contentType")
            ?? throw new FormatException(
                $"{path}: missing \"contentType\"");
        if (!element.TryGetProperty("byteSize", out var byteSizeElement))
        {
            throw new FormatException($"{path}: missing \"byteSize\"");
        }
        if (!byteSizeElement.TryGetInt64(out var byteSize) || byteSize < 0)
        {
            throw new FormatException(
                $"{path}.byteSize: expected a non-negative integer");
        }

        return new VaultRef
        {
            Path = pathVal,
            OriginalName = TryGetString(element, "originalName"),
            ContentType = contentType,
            ByteSize = byteSize,
        };
    }

    private static string? TryGetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Null) return null;
        if (prop.ValueKind != JsonValueKind.String) return null;
        return prop.GetString();
    }

    private static object VaultRefToJson(VaultRef r)
    {
        // Same key set the schema describes; OriginalName is omitted
        // when null thanks to DefaultIgnoreCondition.
        if (r.OriginalName == null)
        {
            return new
            {
                kind = "vault",
                path = r.Path,
                contentType = r.ContentType,
                byteSize = r.ByteSize,
            };
        }
        return new
        {
            kind = "vault",
            path = r.Path,
            originalName = r.OriginalName,
            contentType = r.ContentType,
            byteSize = r.ByteSize,
        };
    }

    private static JsonSchema LoadSchema()
    {
        var assembly = typeof(NoteAttachmentsCodec).Assembly;
        // Embedded resource name = "{AssemblyName}.{folder}.{file}"
        // i.e. "Hmm.Core.Vault.Schemas.NoteAttachments.schema.json".
        const string resourceName =
            "Hmm.Core.Vault.Schemas.NoteAttachments.schema.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found: {resourceName}. " +
                "Check the csproj's <EmbeddedResource Include=\"Schemas\\*.schema.json\" />.");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSchema.FromJsonAsync(json).GetAwaiter().GetResult();
    }
}
