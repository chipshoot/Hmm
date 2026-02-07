using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hmm.Automobile.DomainEntity
{
    public class GasLog : AutomobileBase
    {
        // ===== Core Information =====
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int AutomobileId { get; set; }

        // ===== Odometer & Distance =====
        [Required]
        public Dimension Odometer { get; set; }

        /// <summary>
        /// Distance traveled since last fill-up (calculated or entered)
        /// </summary>
        public Dimension Distance { get; set; }

        // ===== Fuel Information =====
        [Required]
        public Volume Fuel { get; set; }

        [Required]
        public FuelGrade FuelGrade { get; set; }

        /// <summary>
        /// Was the tank filled completely? Required for accurate MPG calculation.
        /// </summary>
        public bool IsFullTank { get; set; } = true;

        public bool IsPartialFillUp => !IsFullTank;

        public bool IsFirstFillUp { get; set; }

        // ===== Pricing =====
        [Required]
        public Money TotalPrice { get; set; } // Total cost of fill-up

        [Required]
        public Money UnitPrice { get; set; } // Price per gallon/liter

        public List<GasDiscountInfo> Discounts { get; set; } = new();

        // ===== Station & Location =====
        public GasStation Station { get; set; }

        [StringLength(100)]
        public string Location { get; set; }

        // ===== Driving Context (Optional but Useful) =====
        [Range(0, 100)]
        public int? CityDrivingPercentage { get; set; }

        [Range(0, 100)]
        public int? HighwayDrivingPercentage { get; set; }

        [StringLength(50)]
        public string ReceiptNumber { get; set; }

        // ===== Metadata =====
        public DateTime LogTime { get; set; } = DateTime.UtcNow;

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        // ===== Calculated Properties =====
        public decimal FuelEfficiency => CalculateFuelEfficiency();

        public Money TotalCostAfterDiscounts
        {
            get
            {
                var total = TotalPrice;
                if (Discounts == null && !Discounts.Any())
                {
                    return TotalPrice;
                }

                var totalDiscount = Discounts
                    .Aggregate(Money.Zero(TotalPrice.Currency), (current, discount) => current + discount.Amount);
                return TotalPrice - totalDiscount;
            }
        }

        // ===== Methods =====
        public decimal CalculateFuelEfficiency()
        {
            if (!IsFullTank || Fuel.Value <= 0 || Distance.Value <= 0)
            {
                return 0;
            }

            // MPG calculation
            if (Distance.Unit.ToString().Contains("Mile") && Fuel.Unit.ToString().Contains("Gallon"))
            {
                return (decimal)(Distance.Value / Fuel.Value);
            }

            // L/100km calculation
            if (Distance.Unit.ToString().Contains("Kilometer") && Fuel.Unit.ToString().Contains("Liter"))
            {
                return (decimal)((Fuel.Value / Distance.Value) * 100);
            }

            return 0;
        }

        public static string GetNoteSubject(int automobileId) =>
            NoteSubjectBuilder.BuildGasLogSubject(automobileId);
    }
}
