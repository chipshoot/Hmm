using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Automobile
{
    public class AutomobileManager : EntityManagerBase<AutomobileInfo>
    {
        public AutomobileManager(INoteSerializer<AutomobileInfo> noteSerializer, IHmmNoteManager noteManager, IEntityLookup lookupRepo, Author defaultAuthor)
            : base(noteManager, lookupRepo, defaultAuthor)
        {
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            NoteSerializer = noteSerializer;
        }

        public override IEnumerable<AutomobileInfo> GetEntities()
        {
            try
            {
                var notes = GetNotes(new AutomobileInfo());
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

        public override AutomobileInfo GetEntityById(int id)
        {
            var car = GetEntities()?.FirstOrDefault(c => c.Id == id);
            return car;
        }

        public override AutomobileInfo Create(AutomobileInfo entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            if (!AuthorValid())
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

        public override AutomobileInfo Update(AutomobileInfo entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curAuto = GetEntityById(entity.Id);
            if (curAuto == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile in data source");
                return null;
            }

            curAuto.Brand = entity.Brand;
            curAuto.Maker = entity.Maker;
            curAuto.MeterReading = entity.MeterReading;
            curAuto.Pin = entity.Pin;
            curAuto.Year = entity.Year;

            var curNote = curAuto.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Update(curNote);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    return GetEntityById(curAuto.Id);
            };
        }

        public override INoteSerializer<AutomobileInfo> NoteSerializer { get; }
    }
}