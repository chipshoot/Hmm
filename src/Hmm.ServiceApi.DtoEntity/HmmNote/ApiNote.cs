using System;
using System.Collections.Generic;
using Hmm.Core.Vault;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Represents a note in API responses.
    /// </summary>
    public class ApiNote : ApiEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Cross-device-stable identity. The wire identity sync
        /// clients key on; <see cref="Id"/> remains the internal
        /// FK target. Auto-assigned by the server when the
        /// creating call doesn't supply one.
        /// </summary>
        public string? Uuid { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public int AuthorId { get; set; }

        public int CatalogId { get; set; }

        /// <summary>
        /// Resolved catalog name. Read-only projection — present so
        /// sync clients can match a note to its catalog by the
        /// stable name (catalog names are unique across the table;
        /// ids aren't stable cross-device). Server-derived from
        /// <c>Note.Catalog.Name</c>; ignored on writes.
        /// </summary>
        public string? CatalogName { get; set; }

        public string Description { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public string CreatedBy { get; set; }

        public string LastModifiedBy { get; set; }

        /// <summary>
        /// Headline image for the note, or <c>null</c> when none is set.
        /// Server-side only <c>vault</c>-kind refs are accepted; the
        /// client rewrites phasset / cloudFile into vault refs before
        /// upload (Phase 18 migration).
        /// </summary>
        public VaultRef? PrimaryImage { get; set; }

        /// <summary>
        /// Gallery — zero or more refs. Disjoint with
        /// <see cref="PrimaryImage"/>.
        /// </summary>
        public IList<VaultRef> Images { get; set; } = new List<VaultRef>();
    }
}
