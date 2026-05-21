namespace Hmm.Core.Map.Migration
{
    /// <summary>
    /// Server-computed counts + per-record errors from a migration
    /// upload or replace. Returned directly on the HTTP response
    /// and also folded into <c>MigrationLog.RecordCounts</c>.
    /// </summary>
    public sealed class MigrationUploadResult
    {
        public int NotesPersisted { get; init; }

        public int NotesFailed { get; init; }

        public int VaultFilesPersisted { get; init; }

        public long VaultBytes { get; init; }

        public System.Collections.Generic.IList<MigrationRecordError> Errors { get; init; }
            = new System.Collections.Generic.List<MigrationRecordError>();
    }

    public sealed class MigrationRecordError
    {
        /// <summary>
        /// Index of the offending record in the request's
        /// <c>notes</c> array, or -1 when the error doesn't tie to
        /// a specific record (e.g. a malformed vault blob).
        /// </summary>
        public required int Index { get; init; }

        public required string Message { get; init; }
    }
}
