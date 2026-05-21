using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Hmm.Core.Vault;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Data required to update an existing automobile.
    /// </summary>
    public class ApiAutomobileForUpdate
    {
        [StringLength(30)]
        public string Color { get; set; }

        [StringLength(20)]
        public string Plate { get; set; }

        [Range(0, long.MaxValue)]
        public long MeterReading { get; set; }

        public string OwnershipStatus { get; set; }

        public bool IsActive { get; set; }

        public DateTime? SoldDate { get; set; }

        public int? SoldMeterReading { get; set; }

        public decimal? SoldPrice { get; set; }

        public DateTime? RegistrationExpiryDate { get; set; }

        public DateTime? InsuranceExpiryDate { get; set; }

        [StringLength(100)]
        public string InsuranceProvider { get; set; }

        [StringLength(50)]
        public string InsurancePolicyNumber { get; set; }

        public DateTime? LastServiceDate { get; set; }

        public int? LastServiceMeterReading { get; set; }

        public DateTime? NextServiceDueDate { get; set; }

        public int? NextServiceDueMeterReading { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        // Attachments — see ApiAutomobile for the full design note.
        // Null PrimaryImage on update means "clear the photo"; an
        // empty Images list clears the gallery.
        public VaultRef PrimaryImage { get; set; }
        public IList<VaultRef> Images { get; set; } = new List<VaultRef>();
    }
}
