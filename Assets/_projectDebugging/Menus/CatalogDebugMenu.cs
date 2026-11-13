using UnityEditor;
using UnityEngine;
using KitchenDesign.Shared.Enums;
using KitchenDesign.Server;

namespace KitchenDesign.Debugging
{
    public static class CatalogDebugMenu
    {
        [MenuItem("Tools/Kitchen Design/Dump sinks with zero depth")]
        private static void DumpSinksWithZeroDepth()
        {
            if (!DebugSystems.TryGet(out AppSystems systems))
            {
                Debug.Log("The product list exists only in play mode.");
                return;
            }

            foreach (ProductInfo productInfo in systems.CatalogData.SourceProducts)
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
