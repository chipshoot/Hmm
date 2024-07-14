using Hmm.Utility.Dal.DataEntity;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Core.DomainEntity
{
    public class NoteCatalog : HasDefaultEntity
    {
        [MaxLength(200)]
        public string Name { get; set; }

        public NoteContentFormatType Type { get; set; }

        public string Schema { get; set; }
    }

    public enum NoteContentFormatType
    {
        PlainText,
        Xml,
        Json,
        Markdown
    }
}