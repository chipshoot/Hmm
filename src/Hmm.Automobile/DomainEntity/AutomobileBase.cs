using System;

namespace Hmm.Automobile.DomainEntity
{
    public class AutomobileBase
    {
        public int Id { get; init; }

        public Guid AuthorId { get; internal set; }

        public bool IsDeleted { get; set; }
    }
}