using Hmm.BigCalendar.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.BigCalendar.Contract
{
    public interface IAppointmentManager
    {
        bool Create(Appointment appointment);

        ProcessingResult ProcessResult { get; }
    }
}