namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiAutomobileForCreate : ApiEntity
    {
        public int MeterReading { get; set; }

        public string Brand { get; set; }

        public string Maker { get; set; }

        public string Year { get; set; }

        public string Pin { get; set; }

        public string Color { get; set; }

        public string Plate { get; set; }
    }
}