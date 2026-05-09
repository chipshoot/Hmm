using Hmm.Utility.Currency;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    /// <summary>
    /// Append-only record of vehicle service work. One note per service event.
    /// Stored as JSON in HmmNote.Content and discriminated by ServiceRecord catalog +
    /// "ServiceRecord,AutomobileId:{id}" subject.
    /// </summary>
    public class ServiceRecord : AutomobileBase
    {
        [Required]
        public int AutomobileId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Range(0, int.MaxValue)]
        public int Mileage { get; set; }

        [Required]
        public ServiceType Type { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public Money Cost { get; set; }

        [StringLength(100)]
        public string ShopName { get; set; }

        public List<PartItem> Parts { get; set; } = new();

        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public static string GetNoteSubject(int automobileId) =>
            NoteSubjectBuilder.BuildServiceRecordSubject(automobileId);
    }
}
