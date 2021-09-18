using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using System;
using System.Collections.Generic;

namespace Hmm.Automobile.DomainEntity
{
    public class GasLog : AutomobileBase
    {
        public DateTime Date { get; set; }

        public AutomobileInfo Car { get; set; }

        public Dimension Distance { get; set; }

        public Dimension CurrentMeterReading { get; set; }

        public Volume Gas { get; set; }

        public Money Price { get; set; }

        public List<GasDiscountInfo> Discounts { get; set; }

        public string Station { get; set; }

        public DateTime CreateDate { get; set; }

        public string Comment { get; set; }
    }
}