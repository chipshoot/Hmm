namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteRender : ApiEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public bool IsDefault { get; set; }

        public string Description { get; set; }
    }
}