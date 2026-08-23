using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// One labelled line of a cheatsheet card. A row may be unbound
    /// (<see cref="Source"/> is null).
    /// </summary>
    public class CheatsheetRow
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// "none" | "call" | "map" - see <see cref="CheatsheetConstant"/>.
        /// Verbatim string, not an enum; see <see cref="CheatsheetSource.Kind"/>.
        /// </summary>
        public string ValueAction { get; set; } = CheatsheetConstant.ValueActionNone;

        /// <summary>Whether the client offers "open the source note".</summary>
        public bool OpenSource { get; set; } = true;

        /// <summary>Null = unbound.</summary>
        public CheatsheetSource Source { get; set; }

        /// <summary>
        /// Every row property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();

        /// <summary>
        /// The whole row, kept verbatim, when this version cannot model it at
        /// all (not a JSON object, or a "source" that is not an object).
        /// Mirrors the Flutter codec's unreadableRows: saving rewrites the whole
        /// card, so a row dropped on read would be erased by the next unrelated
        /// edit. Emitted untouched, in its original position.
        /// </summary>
        public JsonElement? RawJson { get; set; }

        public bool IsUnreadable => RawJson.HasValue;
    }
}
