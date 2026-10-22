using System.Collections.Generic;

using JsonDict = System.Collections.Generic.IDictionary<string, object>;
using JsonList = System.Collections.Generic.IList<object>;

namespace KitchenDesign.Pricing
{
    public sealed class PriceSheetInputs
    {
        public readonly JsonList manualPriceSheetEntries = new List<object>();
        public readonly Dictionary<int, float> productIdToPriceChangeFactor = new();

        public static JsonDict CreateManualPriceSheetEntry(string productSku, string productName, float price)
        {
            return
                new Dictionary<string, object>()
                {
                    { "product_id", productSku },
                    { "name", productName },
                    { "price", price }
                };
        }
    }
}
