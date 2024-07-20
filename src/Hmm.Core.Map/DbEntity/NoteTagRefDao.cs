// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Map.DbEntity;

public class NoteTagRefDao
{
    [Column("id")]
    public int Id { get; set; }

    [Column("noteid")]
    public int NoteId { get; set; }
    public HmmNoteDao Note { get; set; }

    [Column("tagid")]
    public int TagId { get; set; }
    public TagDao Tag { get; set; }
}