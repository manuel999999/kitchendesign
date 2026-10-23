namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Serializables;
    using KitchenDesign.AddressableContent;
    using System.Collections.Generic;
    using UnityEngine;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Products;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(fileName = "Product Visual Database", menuName = "Melcher Studios/Pro-Fit/Product Visual Database")]
    public class ProductVisualDatabaseSO : ScriptableObject
    {
        public IReadOnlyList<ProductVisualSO> runtimeEntries => m_RuntimeEntries;

        public IReadOnlyList<ProductVisualSO> sourceEntries => m_SourceEntries;

        public ProductVisualSO FindRuntimeVisualByID(string visualId)
        {
            if(visualId == null)
            {
                return null;
            }

            m_VisualIdToRuntimeProductVisual.TryGetValue(visualId, out ProductVisualSO productVisual);

            return productVisual;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        protected static void InitializeOnLoad()
        {
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange newState)
            {
                if (newState == UnityEditor.PlayModeStateChange.EnteredPlayMode)
                {
                    UnityEditor.EditorApplication.delayCall +=
                        InitializeLoadedDatabases;
                }
            }

            static void InitializeLoadedDatabases()
            {
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabaseSO)}"))
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualDatabaseSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                }

                ProductVisualDatabaseSO[] databases =
                    Resources.FindObjectsOfTypeAll<ProductVisualDatabaseSO>();

                foreach (ProductVisualDatabaseSO database in databases)
                {
                    database.MakeRuntimeEntries();
                }
            }
        }
#endif

#if !UNITY_EDITOR
        protected void Awake()
        {
            if (MelcherUnityUtility.IsApplicationPlaying())
            {
                MakeRuntimeEntries();
            }
        }
#endif

        protected void OnDestroy()
        {
            DestroyRuntimeEntries();
        }

        protected void MakeRuntimeEntries()
        {
            DestroyRuntimeEntries();

            foreach (ProductVisualSO sourceEntry in m_SourceEntries)
            {
                ProductVisualSO runtimeEntry = sourceEntry.MakeRuntimeClone();

                m_RuntimeEntries.Add(runtimeEntry);

                m_VisualIdToRuntimeProductVisual[runtimeEntry.info.id] = runtimeEntry;
            }

            MelcherLog.Log(
                $"Created {m_RuntimeEntries.Count} runtime product visuals.",
                severity: MelcherLogSeverity.Verbose
            );
        }

        protected void DestroyRuntimeEntries()
        {
            m_VisualIdToRuntimeProductVisual.Clear();

            foreach (ProductVisualSO runtimeEntry in m_RuntimeEntries)
            {
                if (runtimeEntry)
                {
                    Destroy(runtimeEntry);
                }
            }

            m_RuntimeEntries.Clear();

            MelcherLog.Log(
                $"Destroyed runtime product visuals.",
                severity: MelcherLogSeverity.Verbose
            );
        }

        [SerializeField]
        private List<ProductVisualSO> m_SourceEntries;

        private List<ProductVisualSO> m_RuntimeEntries = new ();

        private Dictionary<string, ProductVisualSO> m_VisualIdToRuntimeProductVisual = new ();

#if UNITY_EDITOR

        public const string SourceEntriesMemberName = nameof(m_SourceEntries);

        public List<ProductVisualSO> mutableSourceEntries => m_SourceEntries;

        public void ErrorCheck(bool allowFixUp)
        {
            HashSet<ProductVisualSO> visitedVisuals = new HashSet<ProductVisualSO>();

            int entryCount = m_SourceEntries.Count;
            for (int i = entryCount - 1; i >= 0; --i)
            {
                ProductVisualSO entry = m_SourceEntries[i];

                if (entry == null)
                {
                    if (allowFixUp)
                    {
                        m_SourceEntries.RemoveAt(i);
                        MelcherLog.Log($"Fixed null entry at index {i}.", context: entry);
                    }
                    else
                    {
                        MelcherLog.LogError($"Null entry at index {i}", context: this);
                    }

                    continue;
                }

                if (visitedVisuals.Contains(entry))
                {
                    if (allowFixUp)
                    {
                        m_SourceEntries.RemoveAt(i);
                        MelcherLog.Log($"Fixed duplicate entry at index {i} ({entry.name})", context: entry);
                    }
                    else
                    {
                        int previousIndex = m_SourceEntries.LastIndexOf(entry);
                        MelcherLog.LogError($"Duplicate entry '{entry.name}' at indices {previousIndex} and {i}", entry);
                    }

                    continue;
                }

                visitedVisuals.Add(entry);

                if (entry.info == null)
                {
                    MelcherLog.LogError($"Null info for entry at index {i}: {entry.name}", entry);
                    continue;
                }

                if (entry.info.initialInsetFromEdgeInMeters < 0.0f)
                {
                    MelcherLog.LogError($"Negative inset. It may not be draggable. ({entry.name})", entry);
                }

                if (!entry.contentAddress.hasValue)
                {
                    MelcherLog.LogError($"Missing content address: {entry.name}", entry);
                    continue;
                }

                Object editorOnlyAsset =
                    ((IAssetAddress)entry.contentAddress).editorOnlyCachedAsset;

                ProductVisualContent visualPrefab =
                    editorOnlyAsset as ProductVisualContent;

                if (visualPrefab == null)
                {
                    MelcherLog.LogError($"Invalid content type. Expected " +
                        $"{MelcherFormat.ConsoleTypeName<ProductVisualContent>()} " +
                        $"but found {MelcherFormat.ConsoleTypeName(editorOnlyAsset)}: " +
                        $"{entry.name}", entry);

                    continue;
                }

                if (visualPrefab.modelCutOutExtentMin.localPosition.x < 0.0f)
                {
                    if (allowFixUp)
                    {
                        Transform t = visualPrefab.modelCutOutExtentMin;
                        Vector3 cutOutlocalPosition = t.localPosition;
                        cutOutlocalPosition.x = 0.0f;
                        t.localPosition = cutOutlocalPosition;
                        UnityEditor.EditorUtility.SetDirty(t);

                        MelcherLog.Log($"Fixed negative x-position in cut-out. ({entry.name})", context: entry);
                    }
                    else
                    {
                        MelcherLog.LogError($"Negative x-position in cut-out. It may not be placeable. ({entry.name})", context: entry);
                    }
                }

                bool isShelf = visualPrefab.TryGetComponent(out CabinetShelf shelf);

                if (entry.info.runtimePrefabCategory == ProductCategorySpecific.CabinetSection &&
                    visualPrefab.productModelPrefab &&
                    !isShelf)
                {
                    if (allowFixUp)
                    {
                        if (!visualPrefab.sectionModelPrefab && visualPrefab.productModelPrefab)
                        {
                            visualPrefab.sectionModelPrefab = visualPrefab.productModelPrefab;
                        }

                        visualPrefab.productModelPrefab = null;
                        MelcherLog.Log($"Moved product model to section model: {entry.name}", entry);
                        EditorUtility.SetDirty(entry);
                    }
                    else
                    {
                        MelcherLog.LogError($"Cabinet section has a product model: {entry.name}", entry);
                    }
                }

                if (((!visualPrefab.productModelPrefab && !visualPrefab.sectionModelPrefab) || visualPrefab.scaleOrigin.childCount <= 0) &&
                    entry.info.sectionModelType != CabinetModelType.Procedural)
                {
                    switch (entry.info.runtimePrefabCategory)
                    {
                        default:
                            if (isShelf)
                            {
                                break;
                            }

                            if (allowFixUp)
                            {
                                if (!visualPrefab.productModelPrefab)
                                {
                                    string expectedModelName =
                                        entry.name.Substring("ProductVisual_".Length);

                                    string[] guids =
                                        AssetDatabase.FindAssets("t:model " + expectedModelName);

                                    if (guids != null && guids.Length > 0)
                                    {
                                        foreach (string guid in guids)
                                        {
                                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                                            if (System.IO.Path.GetFileNameWithoutExtension(assetPath) == expectedModelName)
                                            {
                                                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                                                if (entry.info.runtimePrefabCategory == ProductCategorySpecific.CabinetSection)
                                                {
                                                    visualPrefab.sectionModelPrefab = modelPrefab;
                                                }
                                                else
                                                {
                                                    visualPrefab.productModelPrefab = modelPrefab;
                                                }
                                                MelcherLog.Log($"Located model for: {entry.name}", entry);
                                                EditorUtility.SetDirty(entry);
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (visualPrefab.scaleOrigin.childCount <= 0)
                                {
                                    try
                                    {
                                        Object instantiatedModel =
                                            visualPrefab.productModelPrefab ?
                                                PrefabUtility.InstantiatePrefab(visualPrefab.productModelPrefab, visualPrefab.scaleOrigin) :
                                                PrefabUtility.InstantiatePrefab(visualPrefab.sectionModelPrefab, visualPrefab.scaleOrigin);

                                        MelcherLog.Log($"Instantiated model for: {entry.name}", entry);
                                        EditorUtility.SetDirty(entry);
                                    }
                                    catch (System.Exception e)
                                    {
                                        MelcherLog.LogError($"Could not instantiate prefab into: {entry.name}\nException: {e}", entry);
                                    }
                                }
                            }

                            if (!visualPrefab.productModelPrefab && !visualPrefab.sectionModelPrefab)
                            {
                                MelcherLog.LogError($"Missing model prefab: ({entry.name})", context: entry);
                            }

                            if (visualPrefab.scaleOrigin.childCount <= 0)
                            {
                                MelcherLog.LogError($"Missing instantiated model: ({entry.name})", context: entry);
                            }

                            break;

                        case ProductCategorySpecific.Unknown:
                        case ProductCategorySpecific.Countertop:
                        case ProductCategorySpecific.Cabinet:
                        case ProductCategorySpecific.CabinetDoor:
                        case ProductCategorySpecific.Flooring:
                        case ProductCategorySpecific.Shelf:
                            break;
                    }
                }
            }

            if (allowFixUp)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }

        protected void OnValidate()
        {
            ErrorCheck(allowFixUp: false);
        }

#endif
    }
}
