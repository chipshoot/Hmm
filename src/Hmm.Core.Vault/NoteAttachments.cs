namespace Hmm.Core.Vault;

/// <summary>
/// Value of the per-note <c>attachments</c> JSON column on
/// <c>Notes</c>. Mirrors the Flutter-side <c>NoteAttachments</c>
/// codec output (see Phase 9 on the Flutter side) so wire payloads
/// round-trip byte-for-byte between client and server.
/// </summary>
/// <remarks>
/// The two slots are <strong>disjoint</strong> — a photo lives in
/// exactly one of them, never both. Promoting a gallery image to
/// primary moves it out of <see cref="Images"/>. Disjointness is
/// enforced by the constructor; trying to construct a
/// <see cref="NoteAttachments"/> with a duplicate ref throws
/// <see cref="ArgumentException"/>.
/// </remarks>
public sealed class NoteAttachments
{
    /// <summary>
    /// An empty payload — equivalent to a SQL-NULL
    /// <c>Notes.attachments</c> column value. Use this rather than
    /// <c>new NoteAttachments()</c> for clarity.
    /// </summary>
    public static readonly NoteAttachments Empty = new();

    /// <summary>
    /// The note's headline image, or <c>null</c> when none is set.
    /// Disjoint with <see cref="Images"/>.
    /// </summary>
    public VaultRef? PrimaryImage { get; }

    /// <summary>
    /// Gallery (zero or more). Does not include
    /// <see cref="PrimaryImage"/> when one is set.
    /// </summary>
    public IReadOnlyList<VaultRef> Images { get; }

    /// <summary>
    /// Non-image attachments (PDF now, audio later). Rendered by content
    /// type. Independent of <see cref="Images"/>.
    /// </summary>
    public IReadOnlyList<VaultRef> Files { get; }

    public NoteAttachments(
        VaultRef? primaryImage = null,
        IList<VaultRef>? images = null,
        IList<VaultRef>? files = null)
    {
        images ??= Array.Empty<VaultRef>();
        files ??= Array.Empty<VaultRef>();
        if (primaryImage != null)
        {
            foreach (var img in images)
            {
                if (Equals(img, primaryImage))
                {
                    throw new ArgumentException(
                        "NoteAttachments: primaryImage may not also appear in images",
                        nameof(images));
                }
            }
        }
        PrimaryImage = primaryImage;
        Images = images.ToList().AsReadOnly();
        Files = files.ToList().AsReadOnly();
    }

    /// <summary>
    /// True when all slots are empty — same shape SQL NULL would
    /// produce.
    /// </summary>
    public bool IsEmpty =>
        PrimaryImage == null && Images.Count == 0 && Files.Count == 0;

    public bool IsNotEmpty => !IsEmpty;

    public override bool Equals(object? obj)
    {
        if (obj is not NoteAttachments other) return false;
        if (!Equals(PrimaryImage, other.PrimaryImage)) return false;
        if (Images.Count != other.Images.Count) return false;
        for (int i = 0; i < Images.Count; i++)
        {
            if (!Equals(Images[i], other.Images[i])) return false;
        }
        if (Files.Count != other.Files.Count) return false;
        for (int i = 0; i < Files.Count; i++)
        {
            if (!Equals(Files[i], other.Files[i])) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(PrimaryImage);
        foreach (var img in Images) hash = HashCode.Combine(hash, img);
        foreach (var f in Files) hash = HashCode.Combine(hash, f);
        return hash;
    }
}
