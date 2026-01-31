using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteCatalogForUpdate
    {
        [Required(ErrorMessage = "Catalog name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Catalog name must be between 1 and 100 characters")]
        public string Name { get; set; }

        public NoteContentFormatType FormatType { get; set; }

        public string Schema { get; set; }

        public bool IsDefault { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }
}