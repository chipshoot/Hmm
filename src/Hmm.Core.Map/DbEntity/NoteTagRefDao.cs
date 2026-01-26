// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Map.DbEntity;

/// <summary>
/// Join table entity for the many-to-many relationship between Notes and Tags.
/// This class intentionally does NOT inherit from Entity because:
/// 1. It's a pure join table, not a domain entity with business meaning
/// 2. It doesn't need the Description property from Entity base class
/// 3. It's managed through navigation properties on HmmNoteDao and TagDao
/// 4. EF Core manages its lifecycle through the relationship configuration
/// </summary>
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