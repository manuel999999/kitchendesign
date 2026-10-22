namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Serializables;
    using KitchenDesign.Server;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    [CreateAssetMenu(fileName = "", menuName = "Melcher Studios/Pro-Fit/Product ID Remap Database")]
    public class ProductIdRemapDatabaseSO : MelcherScriptableObject
    {
        public int? Lookup(string sku)
        {
            foreach (ProductIdRemapEntry entry in m_Entries)
            {
                if (entry.skus.Contains(sku))
                {
                    return entry.productId;
                }
            }

            return m_NextDatabase ? m_NextDatabase.Lookup(sku) : null;
        }

        [Tooltip("If the ID is not found in this database, it will try " +
            "searching this next database.")]
        [SerializeField]
        protected ProductIdRemapDatabaseSO m_NextDatabase;

        [SerializeField]
        private List<ProductIdRemapEntry> m_Entries = new ();

#if UNITY_EDITOR
        /*
        [MelcherEasyButton]
        private void Migrate()
        {
            string fallbackManufacturerJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.Manufacturers
                );

            TextAsset fallbackManufacturerJson =
                Resources.Load<TextAsset>(
                   fallbackManufacturerJsonPath
                );

            if (!fallbackManufacturerJson)
            {
                MelcherLog.LogError("Failed to load manufacturer JSON.");
                return;
            }

            Dictionary<string, object> manufacturerJson =
                (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                    fallbackManufacturerJson.text
                );

            if (manufacturerJson == null)
            {
                MelcherLog.LogError("Failed to parse manufacturer JSON.");
                return;
            }

            System.Exception manufacturerException =
                Server.CatalogJson.TryParseManufacturers(
                    new MelcherJsonObjectParser(manufacturerJson),
                    out List<ManufacturerInfo> manufacturers
                );

            if (manufacturerException != null)
            {
                Debug.LogException(manufacturerException);
                return;
            }

            string fallbackProductListJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.Products
                );

            TextAsset fallbackProductListJson =
                Resources.Load<TextAsset>(
                    fallbackProductListJsonPath
                );

            if (!fallbackProductListJson)
            {
                MelcherLog.LogError("Failed to load product JSON.");
                return;
            }

            Dictionary<string, object> productListJson =
                (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                    fallbackProductListJson.text
                );

            if (productListJson == null)
            {
                MelcherLog.LogError("Failed to parse product JSON.");
                return;
            }

            MelcherJsonObjectParser parser =
                new (productListJson);

            System.Exception productListException =
                Server.CatalogJson.TryParseProductList(
                    new MelcherJsonObjectParser(productListJson),
                    manufacturers,
                    ProFitGlobals.productGroups,
                    ProFitGlobals.appRules,
                    out List<ProductInfo> productInfos
                );

            if (productListException != null)
            {
                Debug.LogException(productListException);
                return;
            }

            MelcherLog.Log($"Parsed {MelcherFormat.ConsoleString(fallbackProductListJsonPath)}");

            for (int i = 0; i < m_Entries.Count; i++)
            {
                ProductIdRemapEntry entry = m_Entries[i];

                if (entry.m_ProductId != 0)
                {
                    continue;
                }

                ProductInfo productInfo =
                    productInfos.Find(productInfo => productInfo.sku == entry.newProductSku);

                if (productInfo == null)
                {
                    MelcherLog.LogError($"Did not find product for SKU {MelcherFormat.ConsoleString(entry.newProductSku)}");
                    continue;
                }

                entry.m_ProductId = productInfo.id;

                if (entry.m_KnownProductSkus == null)
                {
                    entry.m_KnownProductSkus = new();
                }

                entry.m_KnownProductSkus.mutable.AddRange(entry.oldProductSkus);
                entry.m_KnownProductSkus.mutable.Add(entry.newProductSku);

                m_Entries[i] = entry;
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
        */

        [MelcherEasyButton]
        private void CaptureCurrentIds()
        {
            string fallbackManufacturerJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.Manufacturers
                );

            TextAsset fallbackManufacturerJson =
                Resources.Load<TextAsset>(
                   fallbackManufacturerJsonPath
                );

            if (!fallbackManufacturerJson)
            {
                MelcherLog.LogError("Failed to load manufacturer JSON.");
                return;
            }

            Dictionary<string, object> manufacturerJson =
                (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                    fallbackManufacturerJson.text
                );

            if (manufacturerJson == null)
            {
                MelcherLog.LogError("Failed to parse manufacturer JSON.");
                return;
            }

            System.Exception manufacturerException =
                Server.CatalogJson.TryParseManufacturers(
                    new MelcherJsonObjectParser(manufacturerJson),
                    out List<ManufacturerInfo> manufacturers
                );

            if (manufacturerException != null)
            {
                Debug.LogException(manufacturerException);
                return;
            }

            string fallbackProductListJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.Products
                );

            TextAsset fallbackProductListJson =
                Resources.Load<TextAsset>(
                    fallbackProductListJsonPath
                );

            if (!fallbackProductListJson)
            {
                MelcherLog.LogError("Failed to load product JSON.");
                return;
            }

            Dictionary<string, object> productListJson =
                (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                    fallbackProductListJson.text
                );

            if (productListJson == null)
            {
                MelcherLog.LogError("Failed to parse product JSON.");
                return;
            }

            MelcherJsonObjectParser parser =
                new (productListJson);

            System.Exception productListException =
                Server.CatalogJson.TryParseProductList(
                    new MelcherJsonObjectParser(productListJson),
                    manufacturers,
                    ProFitGlobals.productGroups,
                    ProFitGlobals.appRules,
                    out List<ProductInfo> productInfos
                );

            if (productListException != null)
            {
                Debug.LogException(productListException);
                return;
            }

            string fallbackCabinetListJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.Cabinets
                );

            TextAsset fallbackCabinetListJson =
                Resources.Load<TextAsset>(
                   fallbackCabinetListJsonPath
                );

            string fallbackCabinetCategoriesJsonPath =
                ProFitGlobals.appProfile.GetFallbackJsonPath(
                        Server.FallbackJsonFiles.CabinetSectionCategories
                );

            TextAsset fallbackCabinetCategoriesJson =
                Resources.Load<TextAsset>(
                   fallbackCabinetCategoriesJsonPath
                );

            List<ProductInfo> cabinetProductInfos = null;

            if (fallbackCabinetListJson && fallbackCabinetCategoriesJson)
            {
                Dictionary<string, object> cabinetCategoriesJson =
                    (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                        fallbackCabinetCategoriesJson.text
                    );

                if (cabinetCategoriesJson == null)
                {
                    MelcherLog.LogError("Failed to parse cabinet categories JSON.");
                    return;
                }

                Exception cabinetCategoriesException =
                    Server.CatalogJson.TryParseCabinetSectionCategoryList(
                        new MelcherJsonObjectParser(cabinetCategoriesJson),
                        out List<CabinetSectionCategoryInfo> cabinetCategories
                    );

                if (cabinetCategoriesException != null)
                {
                    Debug.LogException(cabinetCategoriesException);
                    return;
                }

                Dictionary<string, object> cabinetListJson =
                    (Dictionary<string, object>)MiniJSON.Json.Deserialize(
                        fallbackCabinetListJson.text
                    );

                if (cabinetListJson == null)
                {
                    MelcherLog.LogError("Failed to parse cabinet list JSON.");
                    return;
                }

                Exception cabinetListException =
                    Server.CatalogJson.TryParseCabinetList(
                        new MelcherJsonObjectParser(cabinetListJson),
                        manufacturers,
                        cabinetCategories,
                        ProFitGlobals.productGroups,
                        ProFitGlobals.sourceProducts,
                        ProFitGlobals.appRules,
                        out cabinetProductInfos
                    );

                if (cabinetListException != null)
                {
                    Debug.LogException(cabinetListException);
                    return;
                }
            }

            m_Entries.Clear();

            foreach (ProductInfo productInfo in productInfos)
            {
                ProductIdRemapEntry entry = new ProductIdRemapEntry(productInfo.id, productInfo.sku);
                m_Entries.Add(entry);
            }

            if (cabinetProductInfos != null)
            {
                foreach (ProductInfo cabinetProductInfo in cabinetProductInfos)
                {
                    ProductIdRemapEntry entry = new ProductIdRemapEntry(cabinetProductInfo.id, cabinetProductInfo.sku);
                    m_Entries.Add(entry);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

