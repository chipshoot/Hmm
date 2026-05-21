namespace Hmm.Core.Map.Migration
{
    /// <summary>
    /// Top-level migration request shape (excluding the vault byte
    /// stream, which travels alongside as separate multipart parts).
    /// </summary>
    public sealed class MigrationEnvelope
    {
        /// <summary>
        /// Client-supplied device identifier — install UUID,
        /// hostname, anything stable. Optional; stored verbatim in
        /// <c>MigrationLog.DeviceIdentifier</c>.
        /// </summary>
        public string? DeviceIdentifier { get; init; }

        /// <summary>
        /// Optional client-side counts (e.g. resolvedPhAssets,
        /// resolvedCloudFiles, unresolvedRefs from the Free → Paid
        /// resolution step). The server merges its own vault-side
        /// counts into this before writing the audit row.
        /// </summary>
        public string? ClientRecordCounts { get; init; }

        public System.Collections.Generic.IList<MigrationNoteRecord> Notes { get; init; }
            = new System.Collections.Generic.List<MigrationNoteRecord>();
    }
}
