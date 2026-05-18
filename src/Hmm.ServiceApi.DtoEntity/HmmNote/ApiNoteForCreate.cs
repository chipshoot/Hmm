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
        public string Subject { get; set; }

        public string Content { get; set; }

        public int AuthorId { get; set; }

        public int CatalogId { get; set; }

        public string Description { get; set; }

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
