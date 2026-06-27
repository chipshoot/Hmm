namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    public class ApiPartItem
    {
        public string Type { get; set; } = "Part";

        public string Name { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal? UnitCost { get; set; }

        public string Currency { get; set; }
    }
}
