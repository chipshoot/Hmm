using Hmm.Utility.Currency;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    /// <summary>
    /// Auto insurance policy attached to a vehicle. Stored as JSON in HmmNote.Content
    /// and discriminated by the AutoInsurancePolicy NoteCatalog and the
    /// "AutoInsurancePolicy,AutomobileId:{id}" note Subject.
    /// </summary>
    public class AutoInsurancePolicy : AutomobileBase
    {
        [Required]
        public int AutomobileId { get; set; }

        [Required]
        [StringLength(100)]
        public string Provider { get; set; }

        [Required]
        [StringLength(50)]
        public string PolicyNumber { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public Money Premium { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Deductible { get; set; }

        public List<CoverageItem> Coverage { get; set; } = new();

        [StringLength(1000)]
        public string Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public static string GetNoteSubject(int automobileId) =>
            NoteSubjectBuilder.BuildAutoInsurancePolicySubject(automobileId);
    }
}
