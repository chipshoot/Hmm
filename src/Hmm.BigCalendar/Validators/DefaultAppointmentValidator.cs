using Hmm.BigCalendar.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;

namespace Hmm.BigCalendar.Validators;

public class DefaultAppointmentValidator : IHmmValidator<Appointment>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public DefaultAppointmentValidator(IDateTimeProvider dateTimeProvider)
    {
        Guard.Against<ArgumentNullException>(dateTimeProvider == null, nameof(dateTimeProvider));
        _dateTimeProvider = dateTimeProvider;
    }

    public bool IsValidEntity(Appointment entity, ProcessingResult processResult)
    {
        if (entity.HostId == Guid.Empty)
        {
            processResult.AddErrorMessage("Appointment Validation Error: host id is invalid");
            return false;
        }

        if (entity.StartTime < _dateTimeProvider.UtcNow)
        {
            processResult.AddErrorMessage("Appointment Validation Error: start time is past date");
            return false;
        }

        if (entity.StartTime >= entity.EndTime)
        {
            processResult.AddErrorMessage("Appointment Validation Error: start time later than end time");
            return false;
        }

        if (entity.Contact == null)
        {
            processResult.AddErrorMessage("Appointment Validation Error: contact is null");
            return false;
        }

        return true;
    }
}