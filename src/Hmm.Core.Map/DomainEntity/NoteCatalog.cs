using System.ComponentModel.DataAnnotations;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DomainEntity
{
    public class NoteCatalog : HasDefaultEntity
    {
        [MaxLength(200)]
        public string Name { get; set; }

        public NoteContentFormatType Type { get; set; }

        public string Schema { get; set; }
    }
}