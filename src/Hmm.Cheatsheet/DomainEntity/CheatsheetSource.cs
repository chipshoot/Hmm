using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// A reference to a piece of a note. The referenced note is addressed by
    /// <see cref="NoteUuid"/> - the cross-device-stable identity - never by the
    /// local int note id, which differs per device.
    /// </summary>
    public class CheatsheetSource
    {
        public string NoteUuid { get; set; } = string.Empty;

        /// <summary>
        /// "field" | "section" | "whole" - see <see cref="CheatsheetConstant"/>.
        /// Kept as a verbatim string: parsing to an enum would silently rewrite
        /// an unknown token to a default on the next save.
        /// </summary>
        public string Kind { get; set; } = CheatsheetConstant.SourceKindWhole;

        /// <summary>field -> dotted JSON path; section -> heading text; whole -> null.</summary>
        public string Locator { get; set; }

        /// <summary>
        /// Every source property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();
    }
}
