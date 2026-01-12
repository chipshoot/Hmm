using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;

namespace Hmm.Automobile.DomainEntity
{
    public class Fuel
    {
        public Volume Amount { get; set; }

        public Money PricePerUnit { get; set; }

        public Money TotalCost { get; set; }

        public string FuelType { get; set; }
    }
}