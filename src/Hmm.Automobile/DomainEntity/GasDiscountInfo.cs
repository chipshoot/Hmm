using Hmm.Utility.Currency;

namespace Hmm.Automobile.DomainEntity
{
    public class GasDiscountInfo
    {
        public GasDiscount Program { get; set; }

        public Money Amount { get; set; }
    }
}