using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Manager for gas log entities. Overrides CreateAsync to handle automobile meter reading updates.
    /// Uses base class for GetEntitiesAsync and GetEntityByIdAsync.
    /// </summary>
    public class GasLogManager : EntityManagerBase<GasLog>, IGasLogManager
    {
        private readonly IDateTimeProvider _dateProvider;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;

        public GasLogManager(
            INoteSerializer<GasLog> noteSerializer,
            IHmmValidator<GasLog> validator,
            IHmmNoteManager noteManager,
            IAutoEntityManager<AutomobileInfo> autoManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider,
            IDateTimeProvider dateProvider)
            : base(validator, noteManager, lookupRepo, authorProvider)
        {
            ArgumentNullException.ThrowIfNull(autoManager);
            ArgumentNullException.ThrowIfNull(noteSerializer);
            ArgumentNullException.ThrowIfNull(dateProvider);
            _autoManager = autoManager;
            NoteSerializer = noteSerializer;
            _dateProvider = dateProvider;
        }

        public override INoteSerializer<GasLog> NoteSerializer { get; }

        // GetEntitiesAsync - uses base class implementation
        // GetEntityByIdAsync - uses base class implementation

        /// <summary>
        /// Gets gas logs for a specific automobile.
        /// </summary>
        public async Task<ProcessingResult<PageList<GasLog>>> GetGasLogsAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var subject = GasLog.GetNoteSubject(automobileId);
                var notesResult = await GetNotesAsync(new GasLog(), n => n.Subject == subject, resourceCollectionParameters);

                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<GasLog>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var logTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var logs = await Task.WhenAll(logTasks);
                var logList = logs.Where(log => log != null);

                var result = new PageList<GasLog>(logList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<GasLog>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<GasLog>>.FromException(ex);
            }
        }

        /// <summary>
        /// Logs a historical gas entry without updating the automobile's meter reading.
        /// </summary>
        public async Task<ProcessingResult<GasLog>> LogHistoryAsync(GasLog entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasLog>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<GasLog>.Invalid(validationResult.ErrorMessage);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(createdNoteResult.Value.Id);
        }

        public override async Task<ProcessingResult<GasLog>> CreateAsync(GasLog entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasLog>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<GasLog>.Invalid(validationResult.ErrorMessage);
            }

            var autoResult = await _autoManager.GetEntityByIdAsync(entity.Car.Id);
            if (!autoResult.Success || autoResult.Value == null)
            {
                return ProcessingResult<GasLog>.NotFound("Cannot find automobile information");
            }

            var auto = autoResult.Value;
            var (isMeterValid, validationError) = MeterReadingValid(entity, auto);
            if (!isMeterValid)
            {
                return ProcessingResult<GasLog>.Invalid(validationError);
            }

            // Update automobile meter reader
            auto.MeterReading = (long)entity.Odometer.TotalKilometre;
            var updatedAutoResult = await _autoManager.UpdateAsync(auto);
            if (!updatedAutoResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(updatedAutoResult.ErrorMessage, updatedAutoResult.ErrorType);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            var result = await GetEntityByIdAsync(createdNoteResult.Value.Id);

            // Add warning if there was a non-critical validation message
            if (!string.IsNullOrEmpty(validationError))
            {
                return result.WithWarning(validationError);
            }

            return result;
        }

        public override async Task<ProcessingResult<GasLog>> UpdateAsync(GasLog entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasLog>.Invalid("Entity cannot be null");
            }

            var curLogResult = await GetEntityByIdAsync(entity.Id);
            if (!curLogResult.Success)
            {
                return ProcessingResult<GasLog>.NotFound("Cannot find gas log in data source");
            }

            var curLog = curLogResult.Value;
            var curNote = await GetUpdateLogNoteAsync(curLog, entity);

            var updatedNoteResult = await NoteManager.UpdateAsync(curNote);
            if (!updatedNoteResult.Success)
            {
                return ProcessingResult<GasLog>.Fail(updatedNoteResult.ErrorMessage, updatedNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(curLog.Id);
        }

        private async Task<HmmNote> GetUpdateLogNoteAsync(GasLog orgLog, GasLog delta)
        {
            Debug.Assert(orgLog != null);
            Debug.Assert(delta != null);

            orgLog.Date = delta.Date;
            orgLog.Distance = delta.Distance;
            orgLog.Odometer = delta.Odometer;
            orgLog.Fuel = delta.Fuel;
            orgLog.TotalPrice = delta.TotalPrice;
            orgLog.UnitPrice = delta.UnitPrice;
            orgLog.FuelGrade = delta.FuelGrade;
            orgLog.IsFullTank = delta.IsFullTank;
            orgLog.IsFirstFillUp = delta.IsFirstFillUp;
            orgLog.Discounts = delta.Discounts;
            orgLog.Station = delta.Station;
            orgLog.Location = delta.Location;
            orgLog.CityDrivingPercentage = delta.CityDrivingPercentage;
            orgLog.HighwayDrivingPercentage = delta.HighwayDrivingPercentage;
            orgLog.ReceiptNumber = delta.ReceiptNumber;
            orgLog.CreateDate = delta.CreateDate;
            orgLog.Comment = delta.Comment;

            var noteResult = await NoteSerializer.GetNote(orgLog);
            if (!noteResult.Success)
            {
                throw new InvalidOperationException($"Failed to serialize gas log: {noteResult.ErrorMessage}");
            }

            var curNote = noteResult.Value;
            curNote.Author = DefaultAuthor;

            return curNote;
        }

        private static (bool isValid, string validationError) MeterReadingValid(GasLog log, AutomobileInfo auto)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (auto == null) throw new ArgumentNullException(nameof(auto));

            if (auto.MeterReading <= 0)
            {
                return (false, "The automobile's meter reading is invalid");
            }

            // automobile's meter reading should be less than new meter reading
            if (auto.MeterReading > log.Odometer.TotalKilometre)
            {
                return (false, "The automobile's meter reading is larger than gas log");
            }

            // the difference between current automobile meter reading and new meter reading should be greater than log distance
            if (log.Odometer - Dimension.FromKilometer(auto.MeterReading) < log.Distance)
            {
                return (false, "The logging distance is invalid");
            }

            return log.Odometer - Dimension.FromKilometer(auto.MeterReading) != log.Distance
                ? (true, "Current log distance plus automobile's current meter reading does not match log's current meter reading")
                : (true, string.Empty);
        }
    }
}
