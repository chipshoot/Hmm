using Hmm.Utility.Dal.DataEntity;
using System.ComponentModel.DataAnnotations;
using Hmm.Core.Map.DbEntity;

namespace Hmm.Core.DomainEntity
{
    public class NoteCatalog : HasDefaultEntity
    {
        [MaxLength(200)]
        public string Name { get; set; }

        public NoteContentFormatType Type { get; set; }

        public string Schema { get; set; }
    }
}