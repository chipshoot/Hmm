namespace Hmm.Core.Vault;

/// <summary>
/// Server-side reference to one attachment stored in the vault.
/// </summary>
/// <remarks>
/// The .NET side only knows about <c>vault</c>-kind attachment refs;
/// the wire-level tagged-union (<c>vault</c> / <c>phasset</c> /
/// <c>cloudFile</c>) is a Flutter concept. Free → Paid migration
/// rewrites every non-vault ref into a <see cref="VaultRef"/>
/// before upload, so by the time a payload reaches the API it's
/// already vault-shaped. The codec rejects an explicit
/// non-<c>vault</c> <c>kind</c> with a 400 — defense in depth.
///
/// See <c>docs/attachments-design.md</c> and
/// <c>src/Hmm.Core/Schemas/NoteAttachments.schema.json</c> for the
/// full design + JSON shape.
/// </remarks>
public sealed record VaultRef
{
    /// <summary>
    /// Tagged-union discriminator. Always <c>"vault"</c> for this
    /// record — present so JSON serialisation of this type carries
    /// the same shape the Flutter <c>AttachmentRefCodec</c> expects
    /// (it reads the <c>kind</c> field to dispatch between
    /// <c>vault</c> / <c>phasset</c> / <c>cloudFile</c>; missing
    /// <c>kind</c> throws a <c>FormatException</c>).
    /// </summary>
    /// <remarks>
    /// Get-only with a constant value — System.Text.Json includes it
    /// on every emitted VaultRef, the internal codec
    /// (<c>NoteAttachmentsCodec</c>) was already writing
    /// <c>kind = "vault"</c> manually so this is consistent across
    /// the wire surfaces.
    /// </remarks>
    public string Kind => "vault";

    /// <summary>
    /// Vault relative path, in POSIX form. Validated via
    /// <see cref="VaultPathUtil.Validate(string)"/>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Original filename at pick time, if known. Best-effort
    /// metadata for the UI; not used in storage addressing.
    /// </summary>
    public string? OriginalName { get; init; }

    /// <summary>
    /// MIME type. v1 allow-list: image/jpeg, image/png, image/heic,
    /// image/webp. See <c>docs/attachments-design.md</c> "Storage
    /// policy".
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes. Required on <c>vault</c> kind (always
    /// known at upload time — the server measures it).
    /// </summary>
    public required long ByteSize { get; init; }
}
