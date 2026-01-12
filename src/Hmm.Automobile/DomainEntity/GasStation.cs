using System;

namespace Hmm.Automobile.DomainEntity
{
    public class GasStation
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Address { get; set; }

        public DateTime? LastVisited { get; set; }

        public string Comment { get; set; }
    }
}