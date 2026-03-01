using System.ComponentModel.DataAnnotations;

namespace Hmm.Automobile.DomainEntity
{
    public class GasStation : AutomobileBase
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(20)]
        public string ZipCode { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;
    }
}