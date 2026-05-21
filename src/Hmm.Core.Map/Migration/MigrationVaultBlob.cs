namespace Hmm.Core.Map.Migration
{
    /// <summary>
    /// One vault file inside a migration upload request. The
    /// controller materialises these from the multipart stream
    /// (one per file part) and hands them to the manager.
    /// </summary>
    /// <remarks>
    /// Bytes live in memory for the lifetime of one request — fine
    /// at the v1 8 MB cap per file, ~tens of files in a typical
    /// migration. Switch to a streaming shape if either limit
    /// changes.
    /// </remarks>
    public sealed class MigrationVaultBlob
    {
        /// <summary>
        /// Vault relative path, POSIX form. The manager validates
        /// it via <c>VaultPathUtil.Validate</c> before persisting.
        /// </summary>
        public required string RelativePath { get; init; }

        /// <summary>
        /// Content-Type from the multipart part header. The manager
        /// rejects values outside <c>AttachmentSettings.AllowedContentTypes</c>.
        /// </summary>
        public required string ContentType { get; init; }

        public required byte[] Bytes { get; init; }
    }
}
