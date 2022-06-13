using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class GasLogManager : EntityManagerBase<GasLog>, IGasLogManager
    {
        private readonly IDateTimeProvider _dateProvider;
        private readonly IAutoEntityManager<AutomobileInfo> _autoManager;

        public GasLogManager(INoteSerializer<GasLog> noteSerializer, IHmmValidator<GasLog> validator, IHmmNoteManager noteManager, IAutoEntityManager<AutomobileInfo> autoManager, IEntityLookup lookupRepo, IDateTimeProvider dateProvider)
            : base(validator, noteManager, lookupRepo)
        {
            Guard.Against<ArgumentNullException>(autoManager == null, nameof(autoManager));
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            Guard.Against<ArgumentNullException>(dateProvider == null, nameof(dateProvider));
            _autoManager = autoManager;
            NoteSerializer = noteSerializer;
            _dateProvider = dateProvider;
        }

        public override IEnumerable<GasLog> GetEntities(ResourceCollectionParameters resourceCollectionParameters)
        {
            var notes = GetNotes(new GasLog(), resourceCollectionParameters);
            return notes?.Select(note => NoteSerializer.GetEntity(note)).ToList();
        }

        public override async Task<IEnumerable<GasLog>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameters)
        {
            try
            {
                var notes = await GetNotesAsync(new GasLog(), resourceCollectionParameters);
                return notes?.Select(note => NoteSerializer.GetEntity(note)).ToList();
            }
            catch (Exception e)
            {
                if (NoteSerializer.ProcessResult.Success)
                {
                    ProcessResult.WrapException(e);
                }
                else
                {
                    ProcessResult.PropagandaResult(NoteSerializer.ProcessResult);
                }

                return null;
            }
        }

        public override GasLog GetEntityById(int id)
        {
            var note = GetNote(id, new GasLog());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public override async Task<GasLog> GetEntityByIdAsync(int id)
        {
            var note = await GetNoteAsync(id, new GasLog());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public GasLog LogHistory(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            if (!Validator.IsValidEntity(entity, ProcessResult))
            {
                return null;
            }

            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Create(note);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    return GetEntityById(note.Id);
            }
        }

        public async Task<GasLog> LogHistoryAsync(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            if (!Validator.IsValidEntity(entity, ProcessResult))
            {
                return null;
            }

            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            await NoteManager.CreateAsync(note);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    var newLog = await GetEntityByIdAsync(note.Id);
                    return newLog;
            }
        }

        public override GasLog Create(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            // ToDo: validate current meter reader with auto meter reader
            if (!Validator.IsValidEntity(entity, ProcessResult))
            {
                return null;
            }

            var auto = _autoManager.GetEntityById(entity.Car.Id);
            if (auto == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile information");
                return null;
            }

            var (isMeterValid, validationError) = MeterReadingValid(entity, auto);
            if (!isMeterValid)
            {
                ProcessResult.AddErrorMessage(validationError);
                return null;
            }

            if (!string.IsNullOrEmpty(validationError))
            {
                ProcessResult.AddWaningMessage(validationError);
            }

            // Update automobile meter reader
            auto.MeterReading = (long)entity.CurrentMeterReading.TotalKilometre;
            var updatedAuto = _autoManager.Update(auto);
            if (updatedAuto == null)
            {
                ProcessResult.PropagandaResult(_autoManager.ProcessResult);
                return null;
            }

            // ReSharper disable once PossibleNullReferenceException
            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Create(note);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    return GetEntityById(note.Id);
            }
        }

        public override async Task<GasLog> CreateAsync(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            entity.CreateDate = _dateProvider.UtcNow;

            // ToDo: validate current meter reader with auto meter reader
            if (!Validator.IsValidEntity(entity, ProcessResult))
            {
                return null;
            }

            var auto = await _autoManager.GetEntityByIdAsync(entity.Car.Id);
            if (auto == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile information");
                return null;
            }

            var (isMeterValid, validationError) = MeterReadingValid(entity, auto);
            if (!isMeterValid)
            {
                ProcessResult.AddErrorMessage(validationError);
                return null;
            }

            if (!string.IsNullOrEmpty(validationError))
            {
                ProcessResult.AddWaningMessage(validationError);
            }

            // Update automobile meter reader
            auto.MeterReading = (long)entity.CurrentMeterReading.TotalKilometre;
            var updatedAuto = await _autoManager.UpdateAsync(auto);
            if (updatedAuto == null)
            {
                ProcessResult.PropagandaResult(_autoManager.ProcessResult);
                return null;
            }

            // ReSharper disable once PossibleNullReferenceException
            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            await NoteManager.CreateAsync(note);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:
                    var newLog = await GetEntityByIdAsync(note.Id);
                    return newLog;
            }
        }

        public override GasLog Update(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curLog = GetEntityById(entity.Id);
            if (curLog == null)
            {
                ProcessResult.AddErrorMessage("Cannot find gas log in data source");
                return null;
            }

            var curNote = GetUpdateLogNote(curLog, entity);
            NoteManager.Update(curNote);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    return GetEntityById(curLog.Id);
            }
        }

        public override async Task<GasLog> UpdateAsync(GasLog entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curLog = await GetEntityByIdAsync(entity.Id);
            if (curLog == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile in data source");
                return null;
            }

            var curNote = GetUpdateLogNote(curLog, entity);
            await NoteManager.UpdateAsync(curNote);

            if (!NoteManager.ProcessResult.Success)
            {
                return null;
            }

            var updatedLog = await GetEntityByIdAsync(curLog.Id);
            return updatedLog;
        }

        public override INoteSerializer<GasLog> NoteSerializer { get; }

        private HmmNote GetUpdateLogNote(GasLog orgLog, GasLog delta)
        {
            Debug.Assert(orgLog != null);
            Debug.Assert(delta != null);

            orgLog.Date = delta.Date;
            orgLog.Distance = delta.Distance;
            orgLog.CurrentMeterReading = delta.CurrentMeterReading;
            orgLog.Gas = delta.Gas;
            orgLog.Price = delta.Price;
            orgLog.Discounts = delta.Discounts;
            orgLog.Station = delta.Station;
            orgLog.CreateDate = delta.CreateDate;
            orgLog.Comment = delta.Comment;

            var curNote = orgLog.GetNote(NoteSerializer, DefaultAuthor);
            curNote.IsDeleted = delta.IsDeleted;

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

            // automobile's meter reading should less then new meter reading
            if (auto.MeterReading > log.CurrentMeterReading.TotalKilometre)
            {
                return (false, "The automobile's meter reading is larger then gas log");
            }

            // the difference between current automobile meter reading and new meter reading should greater then log distance
            if (log.CurrentMeterReading - Dimension.FromKilometer(auto.MeterReading) < log.Distance)
            {
                return (false, "The logging distance is invalid");
            }

            return log.CurrentMeterReading - Dimension.FromKilometer(auto.MeterReading) != log.Distance
                ? (true, "Current log distance plus automobile's current meter reading does not match log's current meter reading")
                : (true, string.Empty);
        }
    }
}