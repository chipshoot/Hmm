using Hmm.Utility.Currency;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

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
        public List<ServiceType> Types { get; set; } = new();

        /// <summary>
        /// The primary (first) service category. Read-only projection kept for
        /// consumers that need a single scalar; write via <see cref="Types"/>.
        /// </summary>
        public ServiceType Type => Types is { Count: > 0 } ? Types[0] : ServiceType.Other;

        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public Money Cost { get; set; }

        [StringLength(100)]
        public string ShopName { get; set; }

        public List<PartItem> Parts { get; set; } = new();

        /// <summary>Manual tax amount (e.g. HST) entered from the receipt.</summary>
        public Money Tax { get; set; }

        private decimal TotalFor(LineItemType t) => Parts
            .Where(p => p.Type == t)
            .Sum(p => (p.UnitCost?.Amount ?? 0m) * p.Quantity);

        public decimal LabourTotal => TotalFor(LineItemType.Labour);
        public decimal PartsTotal => TotalFor(LineItemType.Part);
        public decimal FeesTotal => TotalFor(LineItemType.Fee);
        public decimal Subtotal => LabourTotal + PartsTotal + FeesTotal;
        public decimal GrandTotal => Subtotal + (Tax?.Amount ?? 0m);

        [StringLength(1000)]
        public string Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public static string GetNoteSubject(int automobileId) =>
            NoteSubjectBuilder.BuildServiceRecordSubject(automobileId);
    }
}
