using Hmm.BigCalendar.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Hmm.BigCalendar.Contract
{
    public interface IAppointmentManager
    {
        Appointment Create(Appointment appointment);

        Appointment Cancel(Appointment appointment);

        Appointment Update(Appointment appointment);

        Appointment GetAppointmentById(Guid id);

        IEnumerable<Appointment> GetAppointmentsByDateRange(DateTime startDate, DateTime endDate);

        IEnumerable<Appointment> GetAppointments(Expression<Func<Appointment, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        ProcessingResult ProcessResult { get; }
    }
}