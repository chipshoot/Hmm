namespace Hmm.Automobile.DomainEntity
{
    public class AutomobileInfo : AutomobileBase
    {
        public string Maker { get; set; }

        public string Brand { get; set; }

        public string Year { get; set; }

        public string Color { get; set; }

        public string Pin { get; set; }

        public string Plate { get; set; }

        public long MeterReading { get; set; }
    }
}