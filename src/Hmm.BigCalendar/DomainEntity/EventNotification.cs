using System;

namespace Hmm.BigCalendar.DomainEntity
{
    public class EventNotification
    {
        public DateTime ActionTime { get; set; }

        public ActionType Action { get; set; }

        public string RecipientAddress { get; set; }
    }
}