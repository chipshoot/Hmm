using System;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutoScheduledServiceForUpdate
    {
        [StringLength(100)]
        public string Name { get; set; }

        public string Type { get; set; }

        [Range(1, int.MaxValue)]
        public int? IntervalDays { get; set; }

        [Range(1, int.MaxValue)]
        public int? IntervalMileage { get; set; }

        public DateTime? NextDueDate { get; set; }

        [Range(0, int.MaxValue)]
        public int? NextDueMileage { get; set; }

        public bool? IsActive { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }
    }
}
