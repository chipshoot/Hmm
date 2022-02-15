using System;
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

        public bool Create(Appointment appointment)
        {
            if (appointment == null)
            {
                ProcessResult.AddErrorMessage("Null object found for appointment creating");
                return false;
            }

            if (!_validator.IsValidEntity(appointment, ProcessResult))
            {
                return false;
            }

            var savedApp = _appointRepo.Add(appointment);
            switch (savedApp)
            {
                case null:
                    ProcessResult.PropagandaResult(_appointRepo.ProcessMessage);
                    return false;
                default:
                    return true;
            }
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}