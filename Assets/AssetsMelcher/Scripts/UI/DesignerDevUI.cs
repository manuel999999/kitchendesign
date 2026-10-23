namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Server;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Products;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using KitchenDesign.Cabinets;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Occupancy;

    public class DesignerDevUI : MelcherUI
    {
        protected override void Awake()
        {
            base.Awake();

            ProFitGlobals.ExecuteWhenReady(OnGlobalDataReady);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            foreach (MelcherButton saveButton in m_SaveRow.GetComponentsInChildren<MelcherButton>())
            {
                saveButton.onClick += SaveDesign;
            }

            foreach (MelcherButton loadButton in m_LoadRow.GetComponentsInChildren<MelcherButton>())
            {
                loadButton.onClick += LoadDesign;
            }

            m_PreviousProductBatchButton.onClick += SpawnPreviousProductBatch;
            m_NextProductBatchButton.onClick += SpawnNextProductBatch;
        }

        protected override void OnDisable()
        {
            foreach (MelcherButton saveButton in m_SaveRow.GetComponentsInChildren<MelcherButton>())
            {
                saveButton.onClick -= SaveDesign;
            }

            foreach (MelcherButton loadButton in m_LoadRow.GetComponentsInChildren<MelcherButton>())
            {
                loadButton.onClick -= LoadDesign;
            }

            m_PreviousProductBatchButton.onClick -= SpawnPreviousProductBatch;
            m_NextProductBatchButton.onClick -= SpawnNextProductBatch;

            base.OnDisable();
        }

        protected string GetAbsolutePathToLocalSaves()
        {
            string absolutePath =
                System.IO.Path.Combine(
                    Application.dataPath,
                    "..",
                    "UserSettings",
                    "EditorKitchenSaves",
                    ProFitGlobals.appOptions.fileSystemName
                );

            return System.IO.Path.GetFullPath(absolutePath);
        }

        protected void SaveDesign(MelcherButton saveButton)
        {
            int saveIndex = saveButton.GetRectTransform().GetSiblingIndex();

            string saveData =
                MiniJSON.Json.Serialize(
                    m_Kitchen.SaveToJson(),
                    prettyPrint: true
                );

            string outputFolderPath = GetAbsolutePathToLocalSaves();

            System.IO.Directory.CreateDirectory(outputFolderPath);

            string outputPath =
                System.IO.Path.Combine(
                    outputFolderPath,
                    $"editorSave{saveIndex}.json"
                );

            System.IO.File.WriteAllText(outputPath, saveData);
            MelcherLog.Log("Wrote: " + outputPath);
        }

        protected void LoadDesign(MelcherButton loadButton)
        {
            LoadDesignAsync().Forget();

            async UniTask LoadDesignAsync()
            {
                int saveIndex = loadButton.GetRectTransform().GetSiblingIndex();

                string inputFolderPath = GetAbsolutePathToLocalSaves();

                string inPath =
                    System.IO.Path.Combine(
                        inputFolderPath,
                        $"editorSave{saveIndex}.json"
                    );

                if (!System.IO.File.Exists(inPath))
                {
                    MelcherLog.Log("No save in that slot.");
                    return;
                }

                string saveData = System.IO.File.ReadAllText(inPath);
                IDictionary<string, object> saveJson =
                    (IDictionary<string, object>)MiniJSON.Json.Deserialize(saveData);

                await m_Kitchen.LoadFromJson(saveJson);

                MelcherLog.Log("Loaded: " + inPath);
            }
        }

        protected void SpawnPreviousProductBatch(MelcherButton caller)
        {
#if UNITY_EDITOR
            SpawnPreviousProductBatchAsync().Forget();

            async UniTask SpawnPreviousProductBatchAsync()
            {
                if (m_LastBatchProductIndexMin <= 0)
                {
                    // Already at the start (maybe we should clear the products)
                    return;
                }

                await ClearProducts();

                m_LastBatchProductIndexMax = m_LastBatchProductIndexMin - 1;

                MelcherSafeLoop safeLoop = MelcherSafeLoop.WithMaxIterations(maxIterations: 1000);

                while (safeLoop.While(m_LastBatchProductIndexMin - 1 >= 0))
                {
                    ProductInfo productAtIndex =
                        m_ProductsForBatchPreviewing[m_LastBatchProductIndexMin - 1];

                    if (!AllowProductInBatch(productAtIndex))
                    {
                        continue;
                    }

                    ProductVisualSO productVisual =
                        m_ProductVisualDatabase.FindRuntimeVisualByID(
                            productAtIndex.visualId
                        );

                    Occupant createdOccupant =
                        await m_Kitchen.TryInstantiateProductAnywhere(
                            productAtIndex,
                            productVisual,
                            productVisual.contentAddress.editorOnlyCachedAsset,
                            showMessageIfFailed: false
                        );

                    if (!createdOccupant)
                    {
                        break;
                    }

                    --m_LastBatchProductIndexMin;
                }

                MelcherLog.Log($"Batch: Products {m_LastBatchProductIndexMin} - {m_LastBatchProductIndexMax}");
            }
#endif  // if UNITY_EDITOR
        }

        protected void SpawnNextProductBatch(MelcherButton caller)
        {
            SpawnNextProductBatchAsync().Forget();

            async UniTaskVoid SpawnNextProductBatchAsync()
            {
                if (m_LastBatchProductIndexMax >= m_ProductsForBatchPreviewing.Length)
                {
                    // Already at the end (maybe we should clear the products and
                    // allow the batch spawning to wrap).
                    return;
                }

                ProductVisualContentStatus contentManager =
                    ProFitGlobals.productContent;

                await ClearProducts();

                m_LastBatchProductIndexMin = m_LastBatchProductIndexMax + 1;

                MelcherSafeLoop safeLoop = MelcherSafeLoop.WithMaxIterations(maxIterations: 1000);

                while (safeLoop.While(m_LastBatchProductIndexMax + 1 < m_ProductsForBatchPreviewing.Length))
                {
                    ProductInfo productAtIndex = m_ProductsForBatchPreviewing[m_LastBatchProductIndexMax + 1];

                    if (AllowProductInBatch(productAtIndex))
                    {
                        ProductVisualSO productVisual =
                            m_ProductVisualDatabase.FindRuntimeVisualByID(
                                productAtIndex.visualId
                            );

                        if (productVisual == null)
                        {
                            string productName = productAtIndex.name;

                            string manufacturerName = "Unknown Manufacturer";

                            if (ProFitGlobals.catalogData.FindManufacturerById(productAtIndex.manufacturerId, out ManufacturerInfo manufacturerInfo))
                            {
                                manufacturerName = manufacturerInfo.name;
                            }

                            MelcherLog.LogError($"No visual found for <b>{manufacturerName}</b> - <b>{productName}</b>, visual ID <b>{productAtIndex.visualId}</b>");
                        }
                        else
                        {
                            ProductVisualContent visualPrefab =
                                await contentManager.LoadContent(productVisual);

                            bool newOccupant =
                                await m_Kitchen.TryInstantiateProductAnywhere(
                                    productAtIndex,
                                    productVisual,
                                    visualPrefab,
                                    showMessageIfFailed: false
                                );

                            if (!newOccupant)
                            {
                                break;
                            }
                        }
                    }

                    ++m_LastBatchProductIndexMax;
                }

                MelcherLog.Log($"Batch: Products {m_LastBatchProductIndexMin} - {m_LastBatchProductIndexMax}");
            }
        }

        protected bool AllowProductInBatch(ProductInfo info)
        {
            if (info.category.IsFinish())
            {
                return false;
            }

            switch (info.category)
            {
                case ProductCategory.Unknown:
                case ProductCategory.BarTopHigh:
                case ProductCategory.Backsplash:
                    return false;
            }

            return true;
        }

        protected async UniTask ClearProducts()
        {
            foreach (ElementIndex<Cabinet> i in m_Kitchen.layout.EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                if (!cabinet.canHaveProducts)
                {
                    continue;
                }

                IReadOnlyList<Occupant> occupants = cabinet.occupants;

                for (int j = occupants.Count - 1; j >= 0; --j)
                {
                    Occupant occupant = occupants[j];

                    if (!AllowProductInBatch(occupant.productInfo))
                    {
                        // Skip products that wouldn't have been added by the
                        // batch system (backsplash, bar tops, etc.)

                        continue;
                    }

                    if (!await cabinet.TryRemoveOccupant(occupant))
                    {
                        MelcherLog.LogWarning($"Could not remove occupant <b>{occupant}</b> from cabinet <b>{cabinet}</b>");

                        continue;
                    }

                    Destroy(occupant.gameObject);
                }
            }
        }

        protected void OnGlobalDataReady()
        {
            m_ProductsForBatchPreviewing =
                ProFitGlobals.sourceProducts
                    .Where(x => AllowProductInBatch(x))
                    .ToArray();

            Array.Sort(
                m_ProductsForBatchPreviewing,
                comparison: (a, b) =>
                {
                    // We are most likely to want to preview new product
                    // models (those not added to the dashboard yet), so sort
                    // those to the front.
                    int compare = a.doesExistInServerDashboard.CompareTo(b.doesExistInServerDashboard);
                    if (compare != 0) return compare;

                    // Otherwise, group by category, product group, then
                    // manufacturer.
                    compare = a.category.CompareTo(b.category);
                    if (compare != 0) return compare;

                    compare = a.groupId.CompareTo(b.groupId);
                    if (compare != 0) return compare;

                    compare = a.manufacturerId.CompareTo(b.manufacturerId);
                    if (compare != 0) return compare;

                    string visualIdA = a.visualId ?? "";
                    string visualIdB = b.visualId ?? "";
                    compare = visualIdA.CompareTo(visualIdB);
                    if (compare != 0) return compare;

                    string skuA = a.sku ?? "";
                    string skuB = b.sku ?? "";
                    compare = skuA.CompareTo(skuB);
                    if (compare != 0) return compare;

                    return a.id.CompareTo(b.id);
                }
            );
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [MelcherHeader]

        [SerializeField]
        protected RectTransform m_SaveRow = default;

        [SerializeField]
        protected RectTransform m_LoadRow = default;

        [Space]

        [Tooltip("This button creates as many products as it can from " +
            "the product catalog until one doesn't fit (going backwards " +
            "from the last batch spawned).")]
        [SerializeField]
        protected MelcherButton m_PreviousProductBatchButton = default;

        [Tooltip("This button creates as many products as it can from " +
            "the product catalog until one doesn't fit (going forwards " +
            "from the last batch spawned).")]
        [SerializeField]
        protected MelcherButton m_NextProductBatchButton = default;

        [Space]

        [UnityEngine.Serialization.FormerlySerializedAs("m_ProductVisualCatalog")]
        [SerializeField, MelcherAssetField]
        protected ProductVisualDatabaseSO m_ProductVisualDatabase = null;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected Kitchen m_Kitchen = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     A list of placeable products (i.e., not finishes or backings)
        ///     used by the dev UI's batch product placer (used to quickly
        ///     preview multiple products on the kitchen at a time). This list
        ///     will be sorted such that the first products are those that
        ///     are most likely for the developer to want to preview. For
        ///     example, priority is given to those that have not been added
        ///     to the dashboard yet.
        /// </summary>
        protected ProductInfo[] m_ProductsForBatchPreviewing;

        protected int m_LastBatchProductIndexMin = 0;
        protected int m_LastBatchProductIndexMax = -1;

    }   // class DesignerDevUI

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-24
/// ===========================================================================
