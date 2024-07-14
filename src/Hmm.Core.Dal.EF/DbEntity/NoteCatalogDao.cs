// Ignore Spelling: Dao

using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.DataEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Dal.EF.DbEntity;

public class NoteCatalogDao : HasDefaultEntity
{
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; }

    [Column("schema")]
    public string Schema { get; set; }
    
    [Column("format")]
    public NoteContentFormatType FormatType { get; set; }
}