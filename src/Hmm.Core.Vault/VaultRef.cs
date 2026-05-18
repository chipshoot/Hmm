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
