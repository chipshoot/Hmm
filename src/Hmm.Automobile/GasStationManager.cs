using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class GasStationManager : EntityManagerBase<GasStation>
    {
        public GasStationManager(
            INoteSerializer<GasStation> noteSerializer,
            IHmmValidator<GasStation> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo)
            : base(validator, noteManager, lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<GasStation> NoteSerializer { get; }

        public override async Task<ProcessingResult<PageList<GasStation>>> GetEntitiesAsync(
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notesResult = await GetNotesAsync(new GasStation(), null, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<GasStation>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var stationTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });

                var stations = await Task.WhenAll(stationTasks);
                var stationList = stations.Where(station => station != null);

                var result = new PageList<GasStation>(stationList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<GasStation>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<GasStation>>.FromException(ex);
            }
        }

        public override async Task<ProcessingResult<GasStation>> GetEntityByIdAsync(int id)
        {
            var noteResult = await GetNoteAsync(id, new GasStation());
            if (!noteResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await NoteSerializer.GetEntity(noteResult.Value);
        }

        public override async Task<ProcessingResult<GasStation>> CreateAsync(GasStation entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasStation>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<GasStation>.Invalid(validationResult.ErrorMessage);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(createdNoteResult.Value.Id);
        }

        public override async Task<ProcessingResult<GasStation>> UpdateAsync(GasStation entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasStation>.Invalid("Entity cannot be null");
            }

            var curStationResult = await GetEntityByIdAsync(entity.Id);
            if (!curStationResult.Success)
            {
                return ProcessingResult<GasStation>.NotFound("Cannot find gas station in data source");
            }

            var curStation = curStationResult.Value;
            curStation.Name = entity.Name;
            curStation.Address = entity.Address;
            curStation.City = entity.City;
            curStation.State = entity.State;
            curStation.ZipCode = entity.ZipCode;
            curStation.Description = entity.Description;
            curStation.IsActive = entity.IsActive;

            var noteResult = await NoteSerializer.GetNote(curStation);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var updatedNoteResult = await NoteManager.UpdateAsync(note);
            if (!updatedNoteResult.Success)
            {
                return ProcessingResult<GasStation>.Fail(updatedNoteResult.ErrorMessage, updatedNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(curStation.Id);
        }

        /// <summary>
        /// Gets active gas stations only.
        /// </summary>
        public async Task<ProcessingResult<PageList<GasStation>>> GetActiveStationsAsync(
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
        public async Task<ProcessingResult<GasStation>> GetByNameAsync(string name)
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
