using System.Collections.Generic;
using Hmm.ServiceApi.DtoEntity.Utility;
using Newtonsoft.Json;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests
{
    /// <summary>
    /// The receipt endpoint is serialized by the app's Newtonsoft formatter,
    /// which uses PascalCase by default (no camelCase resolver is configured).
    /// The Flutter client parses camelCase keys — matching every other endpoint
    /// (which reach camelCase via result filters). Pin the wire contract to
    /// camelCase so the client can actually read the extracted fields; a
    /// mismatch silently yields zero line items on the client.
    /// </summary>
    public class ReceiptDraftSerializationTests
    {
        [Fact]
        public void ApiReceiptDraft_serializes_with_camelCase_keys()
        {
            var dto = new ApiReceiptDraft
            {
                ShopName = "Bob Auto",
                Date = "2026-03-02",
                Odometer = 45000,
                Tax = 2.0,
                Total = 62.0,
                Currency = "CAD",
                LineItems = new List<ApiReceiptLineItem>
                {
                    new ApiReceiptLineItem
                    {
                        Type = "Part",
                        Name = "Filter",
                        Quantity = 1,
                        UnitCost = 10.0,
                        Amount = 10.0
                    }
                }
            };

            // Default settings == what MVC's Newtonsoft formatter uses here
            // (the camelCase contract resolver is commented out in Startup).
            var json = JsonConvert.SerializeObject(dto);

            Assert.Contains("\"shopName\"", json);
            Assert.Contains("\"lineItems\"", json);
            Assert.Contains("\"name\"", json);
            Assert.Contains("\"unitCost\"", json);
            Assert.Contains("\"amount\"", json);
            Assert.Contains("\"odometer\"", json);

            Assert.DoesNotContain("\"ShopName\"", json);
            Assert.DoesNotContain("\"LineItems\"", json);
            Assert.DoesNotContain("\"UnitCost\"", json);
        }
    }
}
