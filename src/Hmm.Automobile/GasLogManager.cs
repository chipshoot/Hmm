﻿using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Automobile
{
    public class GasLogManager : EntityManagerBase<GasLog>
    {
        public GasLogManager(INoteSerializer<GasLog> noteSerializer, IHmmNoteManager noteManager, IEntityLookup lookupRepo, Author defaultAuthor) 
            : base(noteManager, lookupRepo, defaultAuthor)
        {
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            NoteSerializer = noteSerializer;
        }

        public override IEnumerable<GasLog> GetEntities()
        {
            var notes = GetNotes(new GasLog());
            return notes?.Select(note => NoteSerializer.GetEntity(note)).ToList();
        }

        public override GasLog GetEntityById(int id)
        {
            return GetEntities().FirstOrDefault(l => l.Id == id);
        }

        public override GasLog Create(GasLog entity)
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

            curLog.Car = entity.Car;
            curLog.Gas = entity.Gas;
            curLog.Price = entity.Price;
            curLog.Station = entity.Station;
            curLog.Distance = entity.Distance;
            curLog.CurrentMeterReading = entity.CurrentMeterReading;
            curLog.Discounts = entity.Discounts;
            curLog.Comment = entity.Comment;

            var curNote = curLog.GetNote(NoteSerializer, DefaultAuthor);
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

        public override INoteSerializer<GasLog> NoteSerializer { get; }
    }
}