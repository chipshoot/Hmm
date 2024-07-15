// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity;

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