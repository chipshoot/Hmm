namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteCatalogForCreate
    {
        public string Name { get; set; }

        public NoteContentFormatType FormatType { get; set; }

        public string Schema { get; set; }

        public bool IsDefault { get; set; }

        public string Description { get; set; }
    }

    public enum NoteContentFormatType
    {
        PlainText,
        Xml,
        Json,
        Markdown
    }
}