using System.Collections.Generic;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Structured data extracted from a vehicle service receipt. Every scalar is
    /// optional — only fields present on the receipt are populated.
    /// </summary>
    public class ReceiptExtractionResult
    {
        public string ShopName { get; set; }

        /// <summary>ISO date (yyyy-MM-dd) as read from the receipt.</summary>
        public string Date { get; set; }

        /// <summary>Odometer / mileage reading; maps to ServiceRecord.mileage.</summary>
        public int? Odometer { get; set; }

        public double? Tax { get; set; }

        /// <summary>Grand total — used only to cross-check the itemized total.</summary>
        public double? Total { get; set; }

        public string Currency { get; set; }

        public IList<ReceiptExtractionLineItem> LineItems { get; set; }
            = new List<ReceiptExtractionLineItem>();
    }

    /// <summary>One extracted line item. <see cref="Type"/> is one of
    /// <c>labour</c>, <c>part</c>, <c>fee</c>.</summary>
    public class ReceiptExtractionLineItem
    {
        public string Type { get; set; } = "Part";

        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public double? UnitCost { get; set; }

        /// <summary>Printed line total for this item (unit price x quantity);
        /// the client uses it to reconcile quantity / unit price.</summary>
        public double? Amount { get; set; }
    }
}
