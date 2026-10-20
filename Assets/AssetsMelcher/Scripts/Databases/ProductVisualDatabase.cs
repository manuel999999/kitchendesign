namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    ///     A scriptable object that stores all of the product visuals. It has
    ///     a companion editor window, <see cref="ProductVisualDatabaseManager"/>,
    ///     that displays a list of the product visuals details. The list can
    ///     be filtered and allows thumbnails to be easily captured.
    /// </summary>
    // TODO: This system seems to be plagued by the issue described in this
    //       discussion post: https://discussions.unity.com/t/editing-prefab-causes-references-to-it-to-become-null-until-the-object-referencing-it-is-reloaded/717906/14
    //       In brief: Prefabs that are loaded in the editor can go stale when
    //       they are modified by editor scripts. This causes a bunch of grief
    //       as the previously loaded prefab handles can report that they're
    //       null. As a quick fix, we have kept Domain Reloading (in play mode
    //       optimizations) turned on.
    [CreateAssetMenu(fileName = "Product Visual Database", menuName = "Melcher Studios/Pro-Fit/Product Visual Database")]
    public class ProductVisualDatabase : ScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     Get the runtime entries. Runtime entries are copies of the
        ///     <see cref="sourceEntries"/> that are safe to modify without
        ///     altering the source prefabs. This list will be null or point at
        ///     destroyed objects during edit-mode. It is only intended to be
        ///     accessed at runtime.
        /// </summary>
        public IReadOnlyList<ProductVisual> runtimeEntries => m_RuntimeEntries;

        public IReadOnlyList<ProductVisual> sourceEntries => m_SourceEntries;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Lookup the runtime product visual entry using a dictionary.
        /// </summary>
        /// <returns>
        ///     The matching runtime product visual or <see langword="null"/> if
        ///     none is found.
        /// </returns>
        public ProductVisual FindRuntimeVisualByID(string visualId)
        {
            if(visualId == null)
            {
                return null;
            }

            m_VisualIdToRuntimeProductVisual.TryGetValue(visualId, out ProductVisual productVisual);

            return productVisual;
        }

#if UNITY_EDITOR
        /// <summary>
        ///     Create the runtime entries for when in the editor's play mode.
        /// </summary>
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
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabase)}"))
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualDatabase>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                }

                ProductVisualDatabase[] databases =
                    Resources.FindObjectsOfTypeAll<ProductVisualDatabase>();

                foreach (ProductVisualDatabase database in databases)
                {
                    database.MakeRuntimeEntries();
                }
            }
        }
#endif  // if UNITY_EDITOR

#if !UNITY_EDITOR
        /// <summary>
        ///     In release builds, the ScriptableObject can create its runtime
        ///     entries in Awake. This works because (unlike in edit mode), the
        ///     Awake function is guaranteed to be called when the game starts
        ///     and the database is loaded. In the editor, Awake is only called
        ///     once then the object is cached and this happens while the app
        ///     isn't running.
        /// </summary>
        protected void Awake()
        {
            if (MelcherUnityUtility.IsApplicationPlaying())
            {
                // Make the runtime entries, but only while the app is playing.
                MakeRuntimeEntries();
            }
        }
#endif  // if !UNITY_EDITOR

        protected void OnDestroy()
        {
            DestroyRuntimeEntries();
        }

        /// <summary>
        ///     Create copies of the source <see cref="ProductVisual"/>
        ///     entries at runtime. This allows <see cref="ProductVisual"/>s
        ///     to be modified at runtime without affecting the source data so
        ///     it can be referenced if we wish to reset a modified value back
        ///     to its default.
        /// </summary>
        protected void MakeRuntimeEntries()
        {
            DestroyRuntimeEntries();

            foreach (ProductVisual sourceEntry in m_SourceEntries)
            {
                ProductVisual runtimeEntry = sourceEntry.MakeRuntimeClone();

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

            foreach (ProductVisual runtimeEntry in m_RuntimeEntries)
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

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        private List<ProductVisual> m_SourceEntries;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     A list of cloned <see cref="ProductVisual"/>,
        ///     where each entry has a unique <see cref="ProductVisualInfo"/>.
        ///     This allows the app to override product details at runtime
        ///     while preserving the original data for the purpose of rolling
        ///     back changes. E.g., certain brand catalogs or building methods
        ///     may apply changes to the products and these need to be
        ///     reversible.
        /// </summary>
        private List<ProductVisual> m_RuntimeEntries = new ();

        /// <summary>
        ///     A dictionary for faster product visual lookup. Maps the visual's
        ///     ID to the runtime product visual.
        /// </summary>
        private Dictionary<string, ProductVisual> m_VisualIdToRuntimeProductVisual = new ();

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public const string SourceEntriesMemberName = nameof(m_SourceEntries);

        public List<ProductVisual> mutableSourceEntries => m_SourceEntries;

        public void ErrorCheck(bool allowFixUp)
        {
            HashSet<ProductVisual> visitedVisuals = new HashSet<ProductVisual>();

            // Ensure there are not duplicate or null entries
            int entryCount = m_SourceEntries.Count;
            for (int i = entryCount - 1; i >= 0; --i)
            {
                ProductVisual entry = m_SourceEntries[i];

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

                    // TODO: Investigate if this can be fixed automatically.
                    //       The issue this is reporting about is when a product
                    //       is shifted too far outwards (along the attached
                    //       cabinet face's normal). This is something that we
                    //       may do intentionally to avoid a product z-fighting
                    //       with the cabinet or countertop. The ideal fix is
                    //       to avoid negative insets (or in this case, better
                    //       called an outset) and fix the issue in the model.
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
                                    // Try to find the correct prefab.

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
                                    // Add the selected model to the prefab
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
                            // These don't need a model.
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

        /// <summary>
        /// Verify there are no duplicate references in the entry list.
        /// </summary>
        protected void OnValidate()
        {
            ErrorCheck(allowFixUp: false);
        }

#endif  // if UNITY_EDITOR

    }   // class ProductVisualDatabase

}

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-13
/// ===========================================================================