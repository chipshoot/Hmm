namespace Hmm.Core.Vault;

/// <summary>
/// Bound from <c>AttachmentSettings</c> in <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// Defaults shipped here match the values written into the design
/// doc's "Storage policy" section. Production overrides live in
/// the Hmm.ServiceApi configuration (rendered into the Docker
/// container's environment).
/// </remarks>
public sealed class AttachmentSettings
{
    /// <summary>
    /// Filesystem root for the vault. In Docker production this is
    /// the mounted volume <c>/var/lib/hmm-vault</c>. The store
    /// appends <c>/{authorId}/{relativePath}</c> when writing —
    /// so the per-author namespace is enforced by the store, not
    /// by the caller.
    /// </summary>
    public string RootDir { get; set; } = "/var/lib/hmm-vault";

    /// <summary>
    /// Max upload size in bytes. Server returns 413 above this.
    /// Default 8 MB matches the client-side picker cap.
    /// </summary>
    public long MaxBytes { get; set; } = 8L * 1024 * 1024;

    /// <summary>
    /// Accepted MIME types. v1 allow-list mirrors the Flutter
    /// picker / schema. Server returns 415 for others.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/heic",
        "image/webp",
    };

    /// <summary>
    /// Max original-image dimension on the long edge. Images
    /// larger than this are downsized server-side (Phase 5 via
    /// SkiaSharp); smaller ones pass through unchanged.
    /// </summary>
    public int MaxLongEdgePixels { get; set; } = 4096;
}
