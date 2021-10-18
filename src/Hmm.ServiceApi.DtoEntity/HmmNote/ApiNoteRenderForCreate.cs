namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteRenderForCreate
    {
        public string Name { get; set; }

        public string Namespace { get; set; }

        public bool IsDefault { get; set; }

        public string Description { get; set; }
    }
}