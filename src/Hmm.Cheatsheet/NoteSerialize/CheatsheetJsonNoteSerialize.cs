using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.NoteSerializer;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;

namespace Hmm.Cheatsheet.NoteSerialize
{
    /// <summary>
    /// Reads and writes a <see cref="CheatsheetCard"/> as the JSON content of an
    /// HmmNote, in the exact shape the Flutter client persists:
    /// { "note": { "content": { "Cheatsheet": { ... } } } }.
    ///
    /// Losslessness rule: a JSON property is consumed into a typed field ONLY
    /// when it is present with the expected JSON type. Unknown keys, and known
    /// keys carrying an unexpected type, are cloned into ExtraFields and
    /// re-emitted verbatim. A row that cannot be modelled at all is kept whole
    /// in CheatsheetRow.RawJson. This mirrors - and extends - the client's
    /// unreadableRows handling: a save must never destroy data this version did
    /// not understand.
    /// </summary>
    public class CheatsheetJsonNoteSerialize : DefaultJsonNoteSerializer<CheatsheetCard>
    {
        private const string KeySchemaVersion = "schemaVersion";
        private const string KeyId = "id";
        private const string KeyTitle = "title";
        private const string KeyWalletGroup = "walletGroup";
        private const string KeyTags = "tags";
        private const string KeyTemplateId = "templateId";
        private const string KeyProtected = "protected";
        private const string KeyRows = "rows";
        private const string KeyLabel = "label";
        private const string KeyValueAction = "valueAction";
        private const string KeyOpenSource = "openSource";
        private const string KeySource = "source";
        private const string KeyNoteUuid = "noteUuid";
        private const string KeyKind = "kind";
        private const string KeyLocator = "locator";

        private readonly ICheatsheetCatalogProvider _catalogProvider;

        public CheatsheetJsonNoteSerialize(
            ICheatsheetCatalogProvider catalogProvider,
            ILogger<CheatsheetCard> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);

            _catalogProvider = catalogProvider;
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync();
        }

        public override Task<ProcessingResult<CheatsheetCard>> GetEntity(HmmNote note)
        {
            if (note == null)
            {
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    "Null note found when trying to deserialize cheatsheet card from note",
                    ErrorCategory.NotFound));
            }

            if (string.IsNullOrEmpty(note.Content))
            {
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    "Empty note content found",
                    ErrorCategory.MappingError));
            }

            try
            {
                using var document = JsonDocument.Parse(note.Content);
                if (!TryGetCardElement(document, out var cardJson))
                {
                    return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                        $"Missing '{CheatsheetConstant.CheatsheetContentKey}' element in note content JSON",
                        ErrorCategory.MappingError));
                }

                var card = ReadCard(cardJson);
                card.NoteId = note.Id;
                card.AuthorId = note.Author?.Id ?? 0;

                if (string.IsNullOrEmpty(card.Id))
                {
                    // The subject is the identity of record; content may lag it.
                    card.Id = SubjectToCardId(note.Subject);
                }

                return Task.FromResult(ProcessingResult<CheatsheetCard>.Ok(card));
            }
            catch (JsonException ex)
            {
                Logger?.LogError(ex, "JSON parsing error while deserializing cheatsheet card");
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    $"Invalid JSON format: {ex.Message}",
                    ErrorCategory.MappingError));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing cheatsheet card from note");
                return Task.FromResult(ProcessingResult<CheatsheetCard>.FromException(ex));
            }
        }
        public override Task<ProcessingResult<HmmNote>> GetNote(in CheatsheetCard entity)
        {
            if (entity == null)
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Null entity found when trying to serialize cheatsheet card to note",
                    ErrorCategory.NotFound));
            }

            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Cheatsheet card id is required to build the note subject",
                    ErrorCategory.MappingError));
            }

            var content = GetNoteSerializationText(entity);
            if (string.IsNullOrEmpty(content))
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Failed to serialize cheatsheet card content to JSON",
                    ErrorCategory.MappingError));
            }

            var note = new HmmNote
            {
                Id = entity.NoteId,
                Subject = CheatsheetCard.GetNoteSubject(entity.Id),
                Content = content,
                Catalog = Catalog
            };

            return Task.FromResult(ProcessingResult<HmmNote>.Ok(note));
        }

        public override string GetNoteSerializationText(CheatsheetCard entity)
        {
            if (entity == null)
            {
                Logger?.LogWarning("Null cheatsheet card provided for serialization");
                return string.Empty;
            }

            try
            {
                // Insertion order is the wire order; extras are copied last so a
                // preserved original always wins over a fabricated default.
                var cardData = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    [KeySchemaVersion] = entity.SchemaVersion,
                    [KeyId] = entity.Id ?? string.Empty,
                    [KeyTitle] = entity.Title ?? string.Empty,
                    [KeyWalletGroup] = entity.WalletGroup ?? CheatsheetConstant.DefaultWalletGroup,
                    [KeyTags] = entity.Tags ?? new List<string>(),
                    [KeyTemplateId] = entity.TemplateId ?? CheatsheetConstant.DefaultTemplateId,
                    [KeyProtected] = entity.Protected,
                    [KeyRows] = (entity.Rows ?? new List<CheatsheetRow>()).Select(WriteRow).ToList()
                };

                CopyExtras(entity.ExtraFields, cardData);

                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            [CheatsheetConstant.CheatsheetContentKey] = cardData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing cheatsheet card to JSON");
                return string.Empty;
            }
        }

        private static object WriteRow(CheatsheetRow row)
        {
            if (row == null)
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            // A row this version could not model is re-emitted byte-for-byte,
            // in its original position.
            if (row.RawJson.HasValue)
            {
                return row.RawJson.Value;
            }

            var rowData = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [KeyLabel] = row.Label ?? string.Empty,
                [KeyValueAction] = row.ValueAction ?? CheatsheetConstant.ValueActionNone,
                [KeyOpenSource] = row.OpenSource
            };

            if (row.Source != null)
            {
                var sourceData = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    [KeyNoteUuid] = row.Source.NoteUuid ?? string.Empty,
                    [KeyKind] = row.Source.Kind ?? CheatsheetConstant.SourceKindWhole
                };

                // JsonSerializer's WhenWritingNull only applies to POCO members,
                // not dictionary entries, so omit the key explicitly.
                if (row.Source.Locator != null)
                {
                    sourceData[KeyLocator] = row.Source.Locator;
                }

                CopyExtras(row.Source.ExtraFields, sourceData);
                rowData[KeySource] = sourceData;
            }

            CopyExtras(row.ExtraFields, rowData);
            return rowData;
        }

        private static void CopyExtras(
            IDictionary<string, JsonElement> extras,
            IDictionary<string, object> target)
        {
            if (extras == null)
            {
                return;
            }

            foreach (var extra in extras)
            {
                target[extra.Key] = extra.Value;
            }
        }


        private static string SubjectToCardId(string subject)
        {
            if (string.IsNullOrEmpty(subject) ||
                !subject.StartsWith(CheatsheetConstant.CheatsheetSubjectPrefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return subject.Substring(CheatsheetConstant.CheatsheetSubjectPrefix.Length);
        }

        private static bool TryGetCardElement(JsonDocument document, out JsonElement cardJson)
        {
            cardJson = default;

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("note", out var noteElement) ||
                noteElement.ValueKind != JsonValueKind.Object ||
                !noteElement.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (contentElement.TryGetProperty(CheatsheetConstant.CheatsheetContentKey, out cardJson) &&
                cardJson.ValueKind == JsonValueKind.Object)
            {
                return true;
            }

            // Tolerate a camelCase writer, the way EntityJsonNoteSerializeBase does.
            return contentElement.TryGetProperty("cheatsheet", out cardJson) &&
                   cardJson.ValueKind == JsonValueKind.Object;
        }

        private static CheatsheetCard ReadCard(JsonElement cardJson)
        {
            var consumed = new HashSet<string>(StringComparer.Ordinal);

            var card = new CheatsheetCard
            {
                SchemaVersion = ReadInt(cardJson, KeySchemaVersion, CheatsheetConstant.CurrentSchemaVersion, consumed),
                Id = ReadString(cardJson, KeyId, consumed) ?? string.Empty,
                Title = ReadString(cardJson, KeyTitle, consumed) ?? string.Empty,
                WalletGroup = ReadString(cardJson, KeyWalletGroup, consumed) ?? CheatsheetConstant.DefaultWalletGroup,
                TemplateId = ReadString(cardJson, KeyTemplateId, consumed) ?? CheatsheetConstant.DefaultTemplateId,
                Protected = ReadBool(cardJson, KeyProtected, false, consumed),
                Tags = ReadStringList(cardJson, KeyTags, consumed),
                Rows = ReadRows(cardJson, consumed)
            };

            card.ExtraFields = ReadExtras(cardJson, consumed);
            return card;
        }

        private static IList<CheatsheetRow> ReadRows(JsonElement cardJson, HashSet<string> consumed)
        {
            var rows = new List<CheatsheetRow>();

            if (!cardJson.TryGetProperty(KeyRows, out var rowsJson) ||
                rowsJson.ValueKind != JsonValueKind.Array)
            {
                // Not an array: leave it unconsumed so it survives in ExtraFields.
                return rows;
            }

            consumed.Add(KeyRows);
            foreach (var rowJson in rowsJson.EnumerateArray())
            {
                rows.Add(ReadRow(rowJson));
            }

            return rows;
        }

        private static CheatsheetRow ReadRow(JsonElement rowJson)
        {
            if (rowJson.ValueKind != JsonValueKind.Object)
            {
                return new CheatsheetRow { RawJson = rowJson.Clone() };
            }

            // A non-object, non-null "source" is exactly the case the client
            // treats as an unreadable row. Keep the whole row rather than
            // guessing at a repair.
            if (rowJson.TryGetProperty(KeySource, out var probeSource) &&
                probeSource.ValueKind != JsonValueKind.Object &&
                probeSource.ValueKind != JsonValueKind.Null)
            {
                return new CheatsheetRow { RawJson = rowJson.Clone() };
            }

            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var row = new CheatsheetRow
            {
                Label = ReadString(rowJson, KeyLabel, consumed) ?? string.Empty,
                ValueAction = ReadString(rowJson, KeyValueAction, consumed) ?? CheatsheetConstant.ValueActionNone,
                OpenSource = ReadBool(rowJson, KeyOpenSource, true, consumed)
            };

            if (rowJson.TryGetProperty(KeySource, out var sourceJson) &&
                sourceJson.ValueKind == JsonValueKind.Object)
            {
                consumed.Add(KeySource);
                row.Source = ReadSource(sourceJson);
            }
            else if (rowJson.TryGetProperty(KeySource, out var nullSource) &&
                     nullSource.ValueKind == JsonValueKind.Null)
            {
                // Explicit null means unbound; nothing to preserve.
                consumed.Add(KeySource);
            }

            row.ExtraFields = ReadExtras(rowJson, consumed);
            return row;
        }

        private static CheatsheetSource ReadSource(JsonElement sourceJson)
        {
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var source = new CheatsheetSource
            {
                NoteUuid = ReadString(sourceJson, KeyNoteUuid, consumed) ?? string.Empty,
                Kind = ReadString(sourceJson, KeyKind, consumed) ?? CheatsheetConstant.SourceKindWhole,
                Locator = ReadString(sourceJson, KeyLocator, consumed)
            };

            source.ExtraFields = ReadExtras(sourceJson, consumed);
            return source;
        }

        private static IDictionary<string, JsonElement> ReadExtras(JsonElement element, HashSet<string> consumed)
        {
            var extras = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            foreach (var property in element.EnumerateObject())
            {
                if (consumed.Contains(property.Name))
                {
                    continue;
                }

                // Clone: the owning JsonDocument is disposed before this
                // dictionary escapes the serializer.
                extras[property.Name] = property.Value.Clone();
            }

            return extras;
        }

        private static string ReadString(JsonElement element, string name, HashSet<string> consumed)
        {
            if (element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                consumed.Add(name);
                return property.GetString();
            }

            return null;
        }

        private static bool ReadBool(JsonElement element, string name, bool defaultValue, HashSet<string> consumed)
        {
            if (element.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                {
                    consumed.Add(name);
                    return true;
                }

                if (property.ValueKind == JsonValueKind.False)
                {
                    consumed.Add(name);
                    return false;
                }
            }

            return defaultValue;
        }

        private static int ReadInt(JsonElement element, string name, int defaultValue, HashSet<string> consumed)
        {
            // TryGetInt32, not TryGetDouble-then-cast: the latter accepted 1.7 and
            // silently narrowed it to 1, discarding what the client sent. Every
            // other reader here consumes only on an exact type match and leaves
            // anything else to ExtraFields; this one was the exception.
            if (element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt32(out var value))
            {
                consumed.Add(name);
                return value;
            }

            return defaultValue;
        }

        private static IList<string> ReadStringList(JsonElement element, string name, HashSet<string> consumed)
        {
            var values = new List<string>();

            if (!element.TryGetProperty(name, out var property) ||
                property.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    // Mixed array: do not consume, so the original survives
                    // verbatim in ExtraFields.
                    return new List<string>();
                }

                values.Add(item.GetString());
            }

            consumed.Add(name);
            return values;
        }
    }
}
