using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to update an existing author.
    /// </summary>
    public class ApiAuthorForUpdate : ApiEntity
    {
        public string Role { get; set; }

        [Required(ErrorMessage = "Account name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Account name must be between 1 and 100 characters")]
        public string AccountName { get; set; }

        public ApiContact ContactInfo { get; set; }

        public bool IsActivated { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        public string? Bio { get; set; }

        [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
        public string? AvatarUrl { get; set; }

        [StringLength(100, ErrorMessage = "Time zone cannot exceed 100 characters")]
        public string? TimeZone { get; set; }
    }
}