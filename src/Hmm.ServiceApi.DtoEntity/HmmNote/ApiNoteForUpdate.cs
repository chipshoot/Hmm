using System.ComponentModel.DataAnnotations;

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
    }
}