using System;
using Hmm.Automobile.DomainEntity;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiDiscountForUpdate : ApiEntity
    {
        public string Program { get; set; }

        public decimal Amount { get; set; }

        public GasDiscountType DiscountType { get; set; }

        public bool IsActive { get; set; }

        public string Comment { get; set; }
    }
}