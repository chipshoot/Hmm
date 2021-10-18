using System;
using Hmm.Automobile.DomainEntity;

namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiDiscount : ApiEntity
    {
        public int Id { get; set; }

        public string Program { get; set; }

        public GasDiscountType DiscountType { get; set; }

        public decimal Amount { get; set; }

        public Guid AuthorId { get; set; }

        public bool IsActive { get; set; }
    }
}