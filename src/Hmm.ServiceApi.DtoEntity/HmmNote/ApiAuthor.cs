using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Represents an author in API responses.
    /// </summary>
    public class ApiAuthor : ApiEntity
    {
        public int Id { get; set; }

        public string AccountName { get; set; }

        public ApiContact ContactInfo { get; set; }

        public string Role { get; set; }

        public bool IsActivated { get; set; }

        public string Description { get; set; }

        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }

        public string? TimeZone { get; set; }
    }
}