using Hmm.Core.Map.DbEntity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Map.DomainEntity;

public class NoteTagRef
{
    [Column("id")]
    public int Id { get; set; }

    [Column("noteid")]
    public int NoteId { get; set; }

    public HmmNoteDao Note { get; set; }

    [Column("tagid")]
    public int TagId { get; set; }

    public Tag Tag { get; set; }
}