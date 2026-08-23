namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Wire-contract constants shared with the Flutter client
    /// (lib/features/cheatsheet/data/cheatsheet_codec.dart and
    /// lib/core/data/local/local_cheatsheet_repository.dart). Changing any
    /// value here breaks interoperability with already-persisted cards.
    /// </summary>
    public static class CheatsheetConstant
    {
        /// <summary>
        /// Three-segment name so the client's CatalogPalette.domainKeyFor
        /// groups cheatsheets as their own domain.
        /// </summary>
        public const string CheatsheetCatalogName = "Hmm.CheatsheetMan.Cheatsheet";

        /// <summary>Key the card object sits under inside note.content.</summary>
        public const string CheatsheetContentKey = "Cheatsheet";

        /// <summary>
        /// The note subject is an identity, never a label: "Cheatsheet:{cardId}".
        /// </summary>
        public const string CheatsheetSubjectPrefix = "Cheatsheet:";

        /// <summary>Current persisted card shape. Client: CheatsheetCodec.currentSchemaVersion.</summary>
        public const int CurrentSchemaVersion = 1;

        public const string DefaultWalletGroup = "Ungrouped";

        public const string DefaultTemplateId = "blank";

        // valueAction tokens - stored verbatim, never parsed into an enum.
        public const string ValueActionNone = "none";
        public const string ValueActionCall = "call";
        public const string ValueActionMap = "map";

        // source kind tokens - stored verbatim, never parsed into an enum.
        public const string SourceKindField = "field";
        public const string SourceKindSection = "section";
        public const string SourceKindWhole = "whole";
    }
}
