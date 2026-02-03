using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// JSON serializer for GasDiscount entities.
    /// Handles serialization of GasDiscount domain objects to/from JSON stored in HmmNote.Content.
    /// </summary>
    public class GasDiscountJsonNoteSerialize : EntityJsonNoteSerializeBase<GasDiscount>
    {
        private readonly INoteCatalogProvider _catalogProvider;

        public GasDiscountJsonNoteSerialize(
            INoteCatalogProvider catalogProvider,
            ILogger<GasDiscount> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);

            _catalogProvider = catalogProvider;
        }

        public override Task<ProcessingResult<GasDiscount>> GetEntity(HmmNote note)
        {
            try
            {
                var (discountElement, document, error) = GetEntityRoot(note, AutomobileConstant.GasDiscountRecordSubject);
                if (!discountElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<GasDiscount>.Fail(
                        error ?? "Failed to parse gas discount from note",
                        ErrorCategory.MappingError));
                }

                var discountJson = discountElement.Value;

                // Parse enum with default
                Enum.TryParse<GasDiscountType>(GetStringProperty(discountJson, "discountType"), out var discountType);

                // Parse Amount (Money)
                Money amount = null;
                if (discountJson.TryGetProperty("amount", out var amountElement))
                {
                    amount = JsonSerializer.Deserialize<Money>(amountElement.GetRawText(), JsonOptions);
                }

                var discount = new GasDiscount
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,
                    Program = GetStringProperty(discountJson, "program"),
                    Amount = amount,
                    DiscountType = discountType,
                    IsActive = GetBoolProperty(discountJson, "isActive", true),
                    Comment = GetStringProperty(discountJson, "comment")
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<GasDiscount>.Ok(discount));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ProcessingResult<GasDiscount>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(GasDiscount entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var discountData = new Dictionary<string, object>
                {
                    ["program"] = entity.Program ?? string.Empty,
                    ["amount"] = entity.Amount,
                    ["discountType"] = entity.DiscountType.ToString(),
                    ["isActive"] = entity.IsActive,
                    ["comment"] = entity.Comment ?? string.Empty
                };

                // Create the full note structure
                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.GasDiscountRecordSubject] = discountData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing GasDiscount to JSON");
                return string.Empty;
            }
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync(NoteCatalogType.GasDiscount);
        }

        // Note: Helper methods (GetBoolProperty, GetStringProperty, etc.) are inherited from DefaultJsonNoteSerializer<T>
    }
}
