using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.Utility
{
    /// <summary>
    /// Wire contract returned by <c>POST /v1/receipts/extract</c> — structured
    /// data extracted from a receipt for the client's receipt-scan feature.
    /// </summary>
    public class ApiReceiptDraft
    {
        public string ShopName { get; set; }
        public string Date { get; set; }
        public int? Odometer { get; set; }
        public double? Tax { get; set; }
        public double? Total { get; set; }
        public string Currency { get; set; }
        public IList<ApiReceiptLineItem> LineItems { get; set; } = new List<ApiReceiptLineItem>();
    }

    public class ApiReceiptLineItem
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double? UnitCost { get; set; }
    }
}
