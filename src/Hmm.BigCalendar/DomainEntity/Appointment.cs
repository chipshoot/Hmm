using Hmm.Utility.Dal.DataEntity;
using System;
using System.Collections.Generic;

namespace Hmm.BigCalendar.DomainEntity
{
    public class Appointment : GuidEntity
    {
        public Guid HostId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public ContactInfo Contact { get; set; }

        public bool Cancelled { get; set; }

        public IEnumerable<EventNotification> Notifications { get; set; }
    }
}