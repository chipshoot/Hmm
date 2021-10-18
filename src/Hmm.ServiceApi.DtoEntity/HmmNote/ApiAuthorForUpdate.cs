namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiAuthorForUpdate : ApiEntity
    {
        public string Role { get; set; }

        public string AccountName { get; set; }

        public bool IsActivated { get; set; }

        public string Description { get; set; }
    }
}