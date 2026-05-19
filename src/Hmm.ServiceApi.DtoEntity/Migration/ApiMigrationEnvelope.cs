// Ignore Spelling: Dto

using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Migration
{
    /// <summary>
    /// Top-level migration request DTO carried as the
    /// <c>manifest</c> field of a multipart upload. Vault bytes
    /// travel as separate file parts; the controller maps these
    /// into <c>MigrationEnvelope</c> + <c>MigrationVaultBlob</c>
    /// for the manager.
    /// </summary>
    public class ApiMigrationEnvelope
    {
        public string? DeviceIdentifier { get; set; }

        /// <summary>
        /// Optional client-supplied counts JSON (e.g.
        /// <c>{ "resolvedPhAssets": 5, "resolvedCloudFiles": 2,
        /// "unresolvedRefs": 1 }</c>) — the server preserves
        /// non-conflicting keys in the audit row.
        /// </summary>
        public string? ClientRecordCounts { get; set; }

        public IList<ApiMigrationNoteRecord> Notes { get; set; }
            = new List<ApiMigrationNoteRecord>();
    }

    public class ApiMigrationNoteRecord
    {
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        /// <summary>Catalog name; the server resolves the FK.</summary>
        public string CatalogName { get; set; } = string.Empty;

        public IList<string> TagNames { get; set; } = new List<string>();

        /// <summary>
        /// Raw <c>Notes.attachments</c> JSON string, or null.
        /// </summary>
        public string? AttachmentsJson { get; set; }

        public string? Description { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
