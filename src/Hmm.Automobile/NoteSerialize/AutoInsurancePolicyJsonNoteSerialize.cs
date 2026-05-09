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
    /// JSON serializer for AutoInsurancePolicy. Round-trips the policy to/from
    /// HmmNote.Content under the AutoInsurancePolicy catalog and subject prefix.
    /// </summary>
    public class AutoInsurancePolicyJsonNoteSerialize : EntityJsonNoteSerializeBase<AutoInsurancePolicy>
    {
        private readonly INoteCatalogProvider _catalogProvider;

        public AutoInsurancePolicyJsonNoteSerialize(
            INoteCatalogProvider catalogProvider,
            ILogger<AutoInsurancePolicy> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);
            _catalogProvider = catalogProvider;
        }

        public override Task<ProcessingResult<AutoInsurancePolicy>> GetEntity(HmmNote note)
        {
            try
            {
                var (policyElement, document, error) = GetEntityRoot(note, AutomobileConstant.AutoInsurancePolicyRecordSubject);
                if (!policyElement.HasValue || document == null)
                {
                    return Task.FromResult(ProcessingResult<AutoInsurancePolicy>.Fail(
                        error ?? "Failed to parse insurance policy from note",
                        ErrorCategory.MappingError));
                }

                var policyJson = policyElement.Value;

                Money premium = null;
                if (policyJson.TryGetProperty("premium", out var premiumElement))
                {
                    premium = JsonSerializer.Deserialize<Money>(premiumElement.GetRawText(), JsonOptions);
                }

                var coverage = new List<CoverageItem>();
                if (policyJson.TryGetProperty("coverage", out var coverageElement) &&
                    coverageElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in coverageElement.EnumerateArray())
                    {
                        coverage.Add(new CoverageItem
                        {
                            Type = GetStringProperty(item, "type", string.Empty),
                            Limit = item.TryGetProperty("limit", out var limit) ? limit.GetDecimal() : 0m,
                            Deductible = item.TryGetProperty("deductible", out var ded) ? ded.GetDecimal() : 0m,
                            Currency = GetStringProperty(item, "currency", string.Empty)
                        });
                    }
                }

                decimal? deductible = null;
                if (policyJson.TryGetProperty("deductible", out var policyDeductibleElement) &&
                    policyDeductibleElement.ValueKind != JsonValueKind.Null)
                {
                    deductible = policyDeductibleElement.GetDecimal();
                }

                var policy = new AutoInsurancePolicy
                {
                    Id = note.Id,
                    AuthorId = note.Author.Id,
                    AutomobileId = GetIntProperty(policyJson, "automobileId"),
                    Provider = GetStringProperty(policyJson, "provider", string.Empty),
                    PolicyNumber = GetStringProperty(policyJson, "policyNumber", string.Empty),
                    EffectiveDate = GetDateTimeProperty(policyJson, "effectiveDate"),
                    ExpiryDate = GetDateTimeProperty(policyJson, "expiryDate"),
                    Premium = premium,
                    Deductible = deductible,
                    Coverage = coverage,
                    Notes = GetStringProperty(policyJson, "notes", string.Empty),
                    IsActive = GetBoolProperty(policyJson, "isActive", true),
                    CreatedDate = GetDateTimeProperty(policyJson, "createdDate"),
                    LastModifiedDate = GetDateTimeProperty(policyJson, "lastModifiedDate"),
                    IsDeleted = note.IsDeleted
                };

                document.Dispose();
                return Task.FromResult(ProcessingResult<AutoInsurancePolicy>.Ok(policy));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing AutoInsurancePolicy from note");
                return Task.FromResult(ProcessingResult<AutoInsurancePolicy>.FromException(ex));
            }
        }

        public override string GetNoteSerializationText(AutoInsurancePolicy entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            try
            {
                var coverageList = new List<object>();
                if (entity.Coverage != null)
                {
                    foreach (var c in entity.Coverage)
                    {
                        coverageList.Add(new
                        {
                            type = c.Type ?? string.Empty,
                            limit = c.Limit,
                            deductible = c.Deductible,
                            currency = c.Currency ?? string.Empty
                        });
                    }
                }

                var policyData = new Dictionary<string, object>
                {
                    ["automobileId"] = entity.AutomobileId,
                    ["provider"] = entity.Provider ?? string.Empty,
                    ["policyNumber"] = entity.PolicyNumber ?? string.Empty,
                    ["effectiveDate"] = entity.EffectiveDate.ToString("o"),
                    ["expiryDate"] = entity.ExpiryDate.ToString("o"),
                    ["premium"] = entity.Premium,
                    ["deductible"] = entity.Deductible,
                    ["coverage"] = coverageList,
                    ["notes"] = entity.Notes ?? string.Empty,
                    ["isActive"] = entity.IsActive,
                    ["createdDate"] = entity.CreatedDate.ToString("o"),
                    ["lastModifiedDate"] = entity.LastModifiedDate.ToString("o")
                };

                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>
                        {
                            [AutomobileConstant.AutoInsurancePolicyRecordSubject] = policyData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing AutoInsurancePolicy to JSON");
                return string.Empty;
            }
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync(NoteCatalogType.AutoInsurancePolicy);
        }
    }
}
