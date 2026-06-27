using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Currency;
using System.Collections.Generic;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ServiceRecordTotalsTests
    {
        private static Money Cad(decimal a) => new(a, CurrencyCodeType.Cad);

        [Fact]
        public void Totals_split_by_type_and_add_tax()
        {
            var r = new ServiceRecord
            {
                Tax = Cad(28.90m),
                Parts = new List<PartItem>
                {
                    new() { Type = LineItemType.Labour, Name = "Service A", Quantity = 1, UnitCost = Cad(61.50m) },
                    new() { Type = LineItemType.Part, Name = "Oil", Quantity = 2, UnitCost = Cad(17.95m) },
                    new() { Type = LineItemType.Fee, Name = "Env fee", Quantity = 1, UnitCost = Cad(1.54m) },
                },
            };

            Assert.Equal(61.50m, r.LabourTotal);
            Assert.Equal(35.90m, r.PartsTotal);   // 2 × 17.95
            Assert.Equal(1.54m, r.FeesTotal);
            Assert.Equal(98.94m, r.Subtotal);     // 61.50 + 35.90 + 1.54
            Assert.Equal(127.84m, r.GrandTotal);  // 98.94 + 28.90
        }

        [Fact]
        public void New_PartItem_defaults_to_Part_type()
        {
            Assert.Equal(LineItemType.Part, new PartItem().Type);
        }
    }
}
