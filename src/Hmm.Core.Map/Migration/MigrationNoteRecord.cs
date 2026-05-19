namespace Hmm.Core.Map.Migration
{
    /// <summary>
    /// Per-note payload inside a migration envelope. Mirrors the
    /// columns the server actually owns — subject, content, catalog
    /// (by name), tags (by name), timestamps, and the raw
    /// <c>attachments</c> JSON column value. Author is implicit
    /// (resolved from the JWT subject).
    /// </summary>
    /// <remarks>
    /// Kept deliberately separate from <c>HmmNote</c> so the wire
    /// shape doesn't drift when the domain model gains new
    /// concerns. The migration manager translates this record into
    /// an <c>HmmNote</c> + tags + catalog FK before persistence.
    /// </remarks>
    public sealed class MigrationNoteRecord
    {
        public string Subject { get; init; } = string.Empty;

        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Catalog name (not Id). The server resolves it; a missing
        /// catalog yields a per-record error rather than a global
        /// failure so other records still upload.
        /// </summary>
        public string CatalogName { get; init; } = string.Empty;

        /// <summary>
        /// Optional. Tags are upserted by name — unknown names
        /// become new <c>Tag</c> rows.
        /// </summary>
        public System.Collections.Generic.IList<string> TagNames { get; init; }
            = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Raw <c>Notes.attachments</c> JSON. Either null or a value
        /// the server can round-trip through
        /// <c>NoteAttachmentsCodec</c> — the manager runs the codec
        /// on every record and rejects bad payloads as a per-record
        /// error.
        /// </summary>
        public string? AttachmentsJson { get; init; }

        public string? Description { get; init; }

        public System.DateTime CreateDate { get; init; }

        public System.DateTime LastModifiedDate { get; init; }

        public bool IsDeleted { get; init; }
    }
}
