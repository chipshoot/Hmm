namespace Hmm.ServiceApi.DtoEntity.GasLogNotes
{
    /// <summary>
    /// Single coverage line on an auto insurance policy DTO.
    /// </summary>
    public class ApiCoverageItem
    {
        public string Type { get; set; }

        public decimal Limit { get; set; }

        public decimal Deductible { get; set; }

        public string Currency { get; set; }
    }
}
