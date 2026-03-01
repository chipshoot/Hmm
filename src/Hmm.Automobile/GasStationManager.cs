using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Manager for gas station entities. Uses base class for common CRUD operations.
    /// Only UpdateAsync is overridden to handle GasStation-specific property copying.
    /// </summary>
    public class GasStationManager : EntityManagerBase<GasStation>
    {
        public GasStationManager(
            INoteSerializer<GasStation> noteSerializer,
            IHmmValidator<GasStation> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
            : base(validator, noteManager, lookupRepo, authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<GasStation> NoteSerializer { get; }

        // GetEntitiesAsync - uses base class implementation
        // GetEntityByIdAsync - uses base class implementation
        // CreateAsync - uses base class implementation

        public override Task<ProcessingResult<GasStation>> UpdateAsync(GasStation entity, bool commitChanges = true)
        {
            return UpdateEntityAsync(
                entity,
                "Cannot find gas station in data source",
                (existing, updated) =>
                {
                    existing.Name = updated.Name;
                    existing.Address = updated.Address;
                    existing.City = updated.City;
                    existing.State = updated.State;
                    existing.Country = updated.Country;
                    existing.ZipCode = updated.ZipCode;
                    existing.Description = updated.Description;
                    existing.IsActive = updated.IsActive;
                },
                commitChanges);
        }

        /// <summary>
        /// Gets active gas stations only.
        /// </summary>
        public virtual async Task<ProcessingResult<PageList<GasStation>>> GetActiveStationsAsync(
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var allStationsResult = await GetEntitiesAsync(resourceCollectionParameters);
            if (!allStationsResult.Success)
            {
                return allStationsResult;
            }

            var activeStations = allStationsResult.Value.Where(s => s.IsActive).ToList();
            var result = new PageList<GasStation>(
                activeStations,
                activeStations.Count,
                allStationsResult.Value.CurrentPage,
                allStationsResult.Value.PageSize);

            return ProcessingResult<PageList<GasStation>>.Ok(result);
        }

        /// <summary>
        /// Finds a gas station by name.
        /// </summary>
        /// <param name="name">The station name to search for.</param>
        /// <returns>ProcessingResult containing the station if found.</returns>
        public virtual async Task<ProcessingResult<GasStation>> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ProcessingResult<GasStation>.Invalid("Station name cannot be empty");
            }

            var stationsResult = await GetEntitiesAsync();
            if (!stationsResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(stationsResult.ErrorMessage, stationsResult.ErrorType);
            }

            var station = stationsResult.Value.FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return station != null
                ? ProcessingResult<GasStation>.Ok(station)
                : ProcessingResult<GasStation>.NotFound($"Gas station '{name}' not found");
        }
    }
}
