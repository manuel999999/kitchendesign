using UnityEditor;
using UnityEngine;

namespace KitchenDesign.Debugging
{
    public static class CatalogDebugMenu
    {
        [MenuItem("Tools/Kitchen Design/Dump sinks with zero depth")]
        private static void DumpSinksWithZeroDepth()
        {
            foreach (ProductInfo productInfo in ProFitGlobals.sourceProducts)
            {
                if (productInfo.category != ProductCategory.Sink)
                {
                    continue;
                }

                if (productInfo.cutOutSizeInMeters.z < 0.01f)
                {
                    Debug.Log($"Sink \"{productInfo.name}\" (\"{productInfo.sku}\") has a depth of {productInfo.cutOutSizeInMeters.z}m");
                }
            }
        }
    }
}
