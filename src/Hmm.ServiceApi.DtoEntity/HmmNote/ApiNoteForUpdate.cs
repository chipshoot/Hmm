namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteForUpdate : ApiEntity
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public string Description { get; set; }
    }
}