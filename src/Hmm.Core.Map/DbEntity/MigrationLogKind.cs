namespace Hmm.Core.Map.DbEntity
{
    /// <summary>
    /// What triggered a <c>MigrationLog</c> row.
    /// Mirrors <c>docs/multi-device-cloud-sync.md</c> §"MigrationLog".
    /// </summary>
    public enum MigrationLogKind
    {
        /// <summary>
        /// Free → Paid bulk push from a local-mode device.
        /// </summary>
        UploadFromLocal = 0,

        /// <summary>
        /// Paid → Local snapshot, or Lapsed export. Bytes are
        /// streamed back to the device as a zip.
        /// </summary>
        ExportToLocal = 1,

        /// <summary>
        /// Server-side wipe-then-upload, used by the "Replace cloud"
        /// branch of the upgrade flow.
        /// </summary>
        CloudReplaced = 2,

        /// <summary>
        /// Lapsed-state delete (subscription expired, retention
        /// window passed). Not surfaced yet — listed here so the
        /// enum agrees with the design doc.
        /// </summary>
        LapsedDelete = 3,
    }
}
