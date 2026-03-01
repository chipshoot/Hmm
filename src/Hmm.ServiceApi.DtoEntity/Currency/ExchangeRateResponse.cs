using System;

namespace Hmm.ServiceApi.DtoEntity.Currency
{
    public class ExchangeRateResponse
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal Rate { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
