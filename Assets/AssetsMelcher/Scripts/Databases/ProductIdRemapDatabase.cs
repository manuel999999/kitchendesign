namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>
    ///     This is a way to fix the design decision to not include the
    ///     permanent product ID in the save files. If the user changes a
    ///     product's ID, we must manually remap it to the new ID.
    /// </summary>
    [CreateAssetMenu(fileName = "", menuName = "Melcher Studios/Pro-Fit/Product ID Remap Database")]
    public class ProductIdRemapDatabase : MelcherScriptableObject
    {
        public int? Lookup(string sku)
        {
            foreach (Entry entry in m_Entries)
            {
                if (entry.skus.Contains(sku))
                {
                    return entry.productId;
                }
            }

            return m_NextDatabase ? m_NextDatabase.Lookup(sku) : null;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Tooltip("If the ID is not found in this database, it will try " +
            "searching this next database.")]
        [SerializeField]
        protected ProductIdRemapDatabase m_NextDatabase;

        [SerializeField]
        private List<Entry> m_Entries = new ();

        // ====================================================================
        // Nested types
        // ====================================================================

        [System.Serializable]
        public struct Entry
        {
            public Entry(int productId, string productSku)
            {
                m_ProductId = productId;
                m_KnownProductSkus = new ();
                m_KnownProductSkus.mutable.Add(productSku);
            }

            // ====================================================================
            // Properties
            // ====================================================================

            public int productId => m_ProductId;

            public ReadOnlyCollection<string> skus => m_KnownProductSkus.readOnly;

            // ====================================================================
            // Inspector-serialized fields
            // ====================================================================

            [SerializeField]
            private int m_ProductId;

            [SerializeField]
            [Tooltip("The list of known product SKUs that were once used by " +
                "this product ID.")]
            private MelcherReadWriteList<string> m_KnownProductSkus;
        }

#if UNITY_EDITOR
        /*
        [MelcherEasyButton]
        private void Migrate()
        {
            string fallbackManufacturerJsonPath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackManufacturersFileName
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
                ProFitWeb.TryParseManufacturers(
                    new MelcherJsonObjectParser(manufacturerJson),
                    out List<ManufacturerInfo> manufacturers
                );

            if (manufacturerException != null)
            {
                Debug.LogException(manufacturerException);
                return;
            }

            string fallbackProductListJsonPath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackProductListFileName
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
                ProFitWeb.TryParseProductList(
                    new MelcherJsonObjectParser(productListJson),
                    manufacturers,
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
                Entry entry = m_Entries[i];

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
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackManufacturersFileName
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
                ProFitWeb.TryParseManufacturers(
                    new MelcherJsonObjectParser(manufacturerJson),
                    out List<ManufacturerInfo> manufacturers
                );

            if (manufacturerException != null)
            {
                Debug.LogException(manufacturerException);
                return;
            }

            string fallbackProductListJsonPath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackProductListFileName
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
                ProFitWeb.TryParseProductList(
                    new MelcherJsonObjectParser(productListJson),
                    manufacturers,
                    out List<ProductInfo> productInfos
                );

            if (productListException != null)
            {
                Debug.LogException(productListException);
                return;
            }

            string fallbackCabinetListJsonPath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackCabinetListFileName
                );

            TextAsset fallbackCabinetListJson =
                Resources.Load<TextAsset>(
                   fallbackCabinetListJsonPath
                );

            string fallbackCabinetCategoriesJsonPath =
                ProFitUtility.GetFallbackJsonPath(
                    ProFitGlobals.appProfile,
                    ProFitWeb.FallbackCabinetSectionCategoriesFileName
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
                    ProFitWeb.TryParseCabinetSectionCategoryList(
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
                    ProFitWeb.TryParseCabinetList(
                        new MelcherJsonObjectParser(cabinetListJson),
                        manufacturers,
                        cabinetCategories,
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
                Entry entry = new Entry(productInfo.id, productInfo.sku);
                m_Entries.Add(entry);
            }

            if (cabinetProductInfos != null)
            {
                foreach (ProductInfo cabinetProductInfo in cabinetProductInfos)
                {
                    Entry entry = new Entry(cabinetProductInfo.id, cabinetProductInfo.sku);
                    m_Entries.Add(entry);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif  // if UNITY_EDITOR

    }   // class ProductIdRemapper

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2025-02-21
/// ===========================================================================
