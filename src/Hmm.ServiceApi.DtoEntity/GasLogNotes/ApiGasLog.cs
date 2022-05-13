using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiGasLog : ApiEntity, IValidatableObject
    {
        public int Id { get; set; }

        public int CarId { get; set; }

        public DateTime Date { get; set; }

        public DateTime CreateDate { get; set; }

        public float Distance { get; set; }

        public float CurrentMeterReading { get; set; }

        public float Gas { get; set; }

        public decimal Price { get; set; }

        public List<ApiDiscountInfo> DiscountInfos { get; set; }

        public string GasStation { get; set; }

        public string Comment { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            throw new NotImplementedException();
        }
    }
}