using System;
using System.Collections.Generic;
using Hmm.BigCalendar.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.BigCalendar.Contract
{
    public interface IAppointmentManager
    {
        Appointment Create(Appointment appointment);

        Appointment Cancel(Appointment appointment);

        Appointment Update(Appointment appointment);

        Appointment GetAppointmentById(Guid id);

        IEnumerable<Appointment> GetAppointmentsByDateRange(DateTime startDate, DateTime endDate);

        IEnumerable<Appointment> GetAppointments();

        ProcessingResult ProcessResult { get; }
    }
}