using System;
using System.Collections.Generic;
using System.Linq;
using Hmm.BigCalendar.Contract;
using Hmm.BigCalendar.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;

namespace Hmm.BigCalendar.Managers
{
    public class DefaultAppointmentManager : IAppointmentManager
    {
        private readonly IGuidRepository<Appointment> _appointRepo;
        private readonly IHmmValidator<Appointment> _validator;

        public DefaultAppointmentManager(IGuidRepository<Appointment> appointRepo, IHmmValidator<Appointment> validator)
        {
            _appointRepo = appointRepo ?? throw new ArgumentNullException(nameof(appointRepo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public Appointment Create(Appointment appointment)
        {
            if (appointment == null)
            {
                ProcessResult.AddErrorMessage("Null object found for appointment creating");
                return null;
            }

            if (!_validator.IsValidEntity(appointment, ProcessResult))
            {
                return null;
            }

            appointment.Id = Guid.NewGuid();
            var savedApp = _appointRepo.Add(appointment);
            switch (savedApp)
            {
                case null:
                    ProcessResult.PropagandaResult(_appointRepo.ProcessMessage);
                    return null;
                default:
                    return savedApp;
            }
        }

        public Appointment Cancel(Appointment appointment)
        {
            appointment.Cancelled = true;
            return appointment;
        }

        public Appointment Update(Appointment appointment)
        {
            return appointment;
        }

        public Appointment GetAppointmentById(Guid id)
        {
            var appointment = _appointRepo.GetEntity(id);
            return appointment;
        }

        public IEnumerable<Appointment> GetAppointmentsByDateRange(DateTime startDate, DateTime endDate)
        {
            var appointments = _appointRepo.GetEntities(a => a.StartTime >= startDate && a.StartTime <= endDate)
                .ToList();
            return appointments;
        }

        public IEnumerable<Appointment> GetAppointments()
        {
            return _appointRepo.GetEntities().ToList();
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}