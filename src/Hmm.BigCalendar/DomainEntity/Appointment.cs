using Hmm.Utility.Dal.DataEntity;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Hmm.BigCalendar.DomainEntity
{
    public class Appointment : GuidEntity
    {
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string ContactName { get; set; }

        public string ContactPhone { get; set; }

        public string ContactEmail { get; set; }

        public IEnumerable<EventNotification> Notifications { get; set; }
    }
}