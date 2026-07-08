using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hmm.ServiceApi.DtoEntity.Utility
{
    /// <summary>
    /// Wire contract returned by <c>POST /v1/receipts/extract</c> — structured
    /// data extracted from a receipt for the client's receipt-scan feature.
    ///
    /// The app's Newtonsoft formatter serializes PascalCase by default (no
    /// camelCase resolver is configured, and this endpoint has no result
    /// filter). The Flutter client parses camelCase, so the JsonProperty names
    /// below pin this contract to camelCase — without them the client reads
    /// zero line items and null scalars from a successful extraction.
    /// </summary>
    public class ApiReceiptDraft
    {
        [JsonProperty("shopName")]
        public string ShopName { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("odometer")]
        public int? Odometer { get; set; }

        [JsonProperty("tax")]
        public double? Tax { get; set; }

        [JsonProperty("total")]
        public double? Total { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("lineItems")]
        public IList<ApiReceiptLineItem> LineItems { get; set; } = new List<ApiReceiptLineItem>();
    }

    public class ApiReceiptLineItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("unitCost")]
        public double? UnitCost { get; set; }

        [JsonProperty("amount")]
        public double? Amount { get; set; }
    }
}
