namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiAuthorForCreate : ApiEntity
    {
        public string AccountNumber { get; set; }

        public string Role { get; set; }

        public bool IsActivated { get; set; }

        public string Description { get; set; }
    }
}