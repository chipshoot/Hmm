using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiTagForUpdate
    {
        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tag name must be between 1 and 100 characters")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }
}