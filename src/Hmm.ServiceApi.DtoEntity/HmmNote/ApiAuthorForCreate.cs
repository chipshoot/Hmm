namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to create a new author.
    /// </summary>
    public class ApiAuthorForCreate : ApiEntity
    {
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