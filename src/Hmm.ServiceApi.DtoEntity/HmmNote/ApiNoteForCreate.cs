// Ignore Spelling: Dto

using System;
using System.Collections.Generic;
using Hmm.Core.Vault;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to create a new note.
    /// </summary>
    public class ApiNoteForCreate : ApiEntity
    {
        /// <summary>
        /// Optional client-supplied identity. Sync clients
        /// generate this offline so a note can carry a stable id
        /// before the server ever sees it. Server-side manager
        /// assigns a fresh Guid when this is null/empty.
        /// </summary>
        public string? Uuid { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public int AuthorId { get; set; }

        public int CatalogId { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Optional user-chosen note date. Null ⇒ server defaults to now.
        /// </summary>
        public DateTime? NoteDate { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationLabel { get; set; }

        /// <summary>
        /// Optional headline image for the note. Must be a
        /// <c>vault</c>-kind ref by the time it reaches the API
        /// (client rewrites other kinds during upload).
        /// </summary>
        public VaultRef? PrimaryImage { get; set; }

        /// <summary>
        /// Optional gallery — zero or more refs. Must not contain
        /// <see cref="PrimaryImage"/>.
        /// </summary>
        public IList<VaultRef> Images { get; set; } = new List<VaultRef>();
    }
}
