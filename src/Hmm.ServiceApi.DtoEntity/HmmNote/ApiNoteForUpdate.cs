using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Hmm.Core.Vault;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to update an existing note.
    /// </summary>
    public class ApiNoteForUpdate : ApiEntity
    {
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Subject must be between 1 and 200 characters")]
        public string Subject { get; set; }

        public string Content { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        /// <summary>
        /// Carried through on update so a sync push can refresh a
        /// note without losing its identity. Server preserves an
        /// existing value when null/empty; otherwise the supplied
        /// value wins (sync push owns the truth).
        /// </summary>
        public string? Uuid { get; set; }

        /// <summary>
        /// Replacement headline image, or <c>null</c> to clear it.
        /// </summary>
        public VaultRef? PrimaryImage { get; set; }

        /// <summary>
        /// Replacement gallery — an empty list clears it. Must not
        /// contain <see cref="PrimaryImage"/>.
        /// </summary>
        public IList<VaultRef> Images { get; set; } = new List<VaultRef>();
    }
}
