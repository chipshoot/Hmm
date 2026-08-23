using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// A read-only "wallet" card: a titled, grouped list of labelled rows, each
    /// referencing a piece of some note. Persisted as one HmmNote's content
    /// under the Hmm.CheatsheetMan.Cheatsheet catalog.
    /// </summary>
    public class CheatsheetCard
    {
        /// <summary>The backing note's local int id. Not part of the card JSON.</summary>
        public int NoteId { get; set; }

        /// <summary>The owning author's id. Not part of the card JSON.</summary>
        public int AuthorId { get; set; }

        public int SchemaVersion { get; set; } = CheatsheetConstant.CurrentSchemaVersion;

        /// <summary>
        /// Stable v4 UUID minted once at create time and never regenerated on
        /// edit. Also the note's subject, so it must not track the mutable,
        /// non-unique <see cref="Title"/>.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = CheatsheetConstant.DefaultWalletGroup;

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = CheatsheetConstant.DefaultTemplateId;

        /// <summary>
        /// Stored verbatim. The server never gates, encrypts or rejects on this
        /// flag - it is a client-side UI concern.
        /// </summary>
        public bool Protected { get; set; }

        public IList<CheatsheetRow> Rows { get; set; } = new List<CheatsheetRow>();

        /// <summary>
        /// Every card property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();

        public static string GetNoteSubject(string cardId)
            => $"{CheatsheetConstant.CheatsheetSubjectPrefix}{cardId}";
    }
}
