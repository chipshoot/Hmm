using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Manager for automobile entities. Uses base class for common CRUD operations.
    /// Only UpdateAsync is overridden to handle AutomobileInfo-specific property copying.
    /// </summary>
    public class AutomobileManager : EntityManagerBase<AutomobileInfo>
    {
        public AutomobileManager(
            INoteSerializer<AutomobileInfo> noteSerializer,
            IHmmValidator<AutomobileInfo> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
            : base(validator, noteManager, lookupRepo, authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<AutomobileInfo> NoteSerializer { get; }

        // GetEntitiesAsync - uses base class implementation
        // GetEntityByIdAsync - uses base class implementation
        // CreateAsync - uses base class implementation

        public override Task<ProcessingResult<AutomobileInfo>> UpdateAsync(AutomobileInfo entity, bool commitChanges = true)
        {
            return UpdateEntityAsync(
                entity,
                "Cannot find automobile in data source",
                (existing, updated) =>
                {
                    // Overlay every user-mutable field from the
                    // incoming update onto the persisted entity.
                    // The previous version of this lambda copied
                    // only 7 fields, silently dropping Notes text,
                    // VIN, fuel info, registration / insurance,
                    // service dates, and (Phase 12.5) the photo
                    // refs. System fields (Id, AuthorId,
                    // CreatedDate) stay on `existing` because the
                    // wire DTO doesn't carry them.
                    existing.VIN = updated.VIN;
                    existing.Maker = updated.Maker;
                    existing.Brand = updated.Brand;
                    existing.Model = updated.Model;
                    existing.Trim = updated.Trim;
                    existing.Year = updated.Year;
                    existing.Color = updated.Color;
                    existing.Plate = updated.Plate;

                    existing.EngineType = updated.EngineType;
                    existing.FuelType = updated.FuelType;
                    existing.FuelTankCapacity = updated.FuelTankCapacity;
                    existing.CityMPG = updated.CityMPG;
                    existing.HighwayMPG = updated.HighwayMPG;
                    existing.CombinedMPG = updated.CombinedMPG;

                    existing.MeterReading = updated.MeterReading;
                    existing.PurchaseMeterReading = updated.PurchaseMeterReading;
                    existing.PurchaseDate = updated.PurchaseDate;
                    existing.PurchasePrice = updated.PurchasePrice;
                    existing.OwnershipStatus = updated.OwnershipStatus;

                    existing.IsActive = updated.IsActive;
                    existing.SoldDate = updated.SoldDate;
                    existing.SoldMeterReading = updated.SoldMeterReading;
                    existing.SoldPrice = updated.SoldPrice;

                    existing.RegistrationExpiryDate = updated.RegistrationExpiryDate;
                    existing.InsuranceExpiryDate = updated.InsuranceExpiryDate;
                    existing.InsuranceProvider = updated.InsuranceProvider;
                    existing.InsurancePolicyNumber = updated.InsurancePolicyNumber;

                    existing.LastServiceDate = updated.LastServiceDate;
                    existing.LastServiceMeterReading = updated.LastServiceMeterReading;
                    existing.NextServiceDueDate = updated.NextServiceDueDate;
                    existing.NextServiceDueMeterReading = updated.NextServiceDueMeterReading;

                    existing.Notes = updated.Notes;

                    // Phase 12.5: attachment refs ride through the
                    // underlying note's attachments column (handled
                    // by AutomobileJsonNoteSerialize.GetNote
                    // copying these onto note.PrimaryImage /
                    // note.Images before NoteManager persists).
                    existing.PrimaryImage = updated.PrimaryImage;
                    existing.Images = updated.Images ?? new System.Collections.Generic.List<Hmm.Core.Vault.VaultRef>();
                },
                commitChanges);
        }
    }
}