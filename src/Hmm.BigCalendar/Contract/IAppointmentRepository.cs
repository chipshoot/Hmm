using Hmm.Utility.Misc;

namespace Hmm.BigCalendar.Contract
{
    public interface IAppointmentRepository
    {
        ProcessingResult<Unit> ProcessResult { get; }
    }
}