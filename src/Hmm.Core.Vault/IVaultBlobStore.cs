namespace Hmm.Core.Vault;

/// <summary>
/// Storage abstraction for attachment bytes. One per-author vault
/// namespace; the implementation owns the per-author scoping (e.g.
/// prefixing with <c>/{authorId}/</c> under the configured root).
/// </summary>
/// <remarks>
/// Mirrors the Flutter-side <c>IVaultStore</c> contract so both
/// sides have the same mental model. See
/// <c>docs/attachments-design.md</c> for the design and
/// <c>docs/attachments-path-spec.md</c> for the relative-path rules
/// every method enforces via
/// <see cref="VaultPathUtil.Validate(string)"/>.
///
/// Contract notes:
/// <list type="bullet">
///   <item>Every method that takes a <paramref name="relativePath"/>
///   validates it; invalid paths throw <see cref="ArgumentException"/>.</item>
///   <item><see cref="PutBytesAsync"/> is atomic — a reader can
///   never see a half-written file. The filesystem impl writes to
///   a temp file under the same parent and renames into place.</item>
///   <item><see cref="DeleteAsync"/> is idempotent — deleting a
///   path that doesn't exist succeeds silently.</item>
///   <item><see cref="ListAsync"/> returns entries whose
///   <see cref="VaultEntry.RelativePath"/> is equal to or nested
///   under the given prefix; an empty string means "list everything
///   under the per-author vault root."</item>
/// </list>
/// </remarks>
public interface IVaultBlobStore
{
    /// <summary>
    /// Write <paramref name="bytes"/> at
    /// <paramref name="relativePath"/> for the given author.
    /// </summary>
    /// <param name="authorId">Per-author scoping key — the JWT subject.</param>
    /// <param name="relativePath">Vault relative path (POSIX form).</param>
    /// <param name="bytes">Payload.</param>
    /// <param name="contentType">
    /// Advisory MIME — the filesystem impl ignores it; an HTTP-backed
    /// impl would use it as a response Content-Type cache value.
    /// </param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task PutBytesAsync(
        int authorId,
        string relativePath,
        ReadOnlyMemory<byte> bytes,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read the bytes at <paramref name="relativePath"/> for the
    /// given author, or null if the file doesn't exist.
    /// </summary>
    Task<byte[]?> GetBytesAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a file exists at
    /// <paramref name="relativePath"/> for the given author.
    /// </summary>
    Task<bool> ExistsAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete the file at <paramref name="relativePath"/> for the
    /// given author. Succeeds silently if it doesn't exist.
    /// </summary>
    Task DeleteAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerate every file under <paramref name="prefix"/> in the
    /// given author's vault. An empty prefix lists everything under
    /// the per-author root.
    /// </summary>
    Task<IReadOnlyList<VaultEntry>> ListAsync(
        int authorId,
        string prefix,
        CancellationToken cancellationToken = default);
}
