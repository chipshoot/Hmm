using System;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    /// <summary>
    /// Recurring / upcoming maintenance schedule for a vehicle. Drives the
    /// AutomobileInfo NextServiceDueDate / NextServiceDueMeterReading snapshot.
    /// Stored as JSON in HmmNote.Content with the AutoScheduledService catalog and
    /// "AutoScheduledService,AutomobileId:{id}" subject.
    /// </summary>
    public class AutoScheduledService : AutomobileBase
    {
        [Required]
        public int AutomobileId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public ServiceType Type { get; set; }

        [Range(1, int.MaxValue)]
        public int? IntervalDays { get; set; }

        [Range(1, int.MaxValue)]
        public int? IntervalMileage { get; set; }

        public DateTime? NextDueDate { get; set; }

        public int? NextDueMileage { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public static string GetNoteSubject(int automobileId) =>
            NoteSubjectBuilder.BuildAutoScheduledServiceSubject(automobileId);
    }
}
