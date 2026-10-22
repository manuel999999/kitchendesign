namespace KitchenDesign
{
    using Melcher;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [CreateAssetMenu(fileName = "CabinetSectionDoorHandleStyle", menuName = "Melcher Studios/Pro-Fit/Cabinet Section Door Handle Style")]
    public class CabinetSectionDoorHandleStyle : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public BrandCatalog brandCatalog => m_BrandCatalog;
        public float baseHandlePrice => m_BaseHandlePrice;
        public TextAsset mapping => m_Mapping;
        public CabinetSectionDoorHandleData defaultPrefab => m_DefaultHandle;
        public CabinetSectionDoorHandleData[] prefabs => m_Handles;


        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] private BrandCatalog m_BrandCatalog;
        [SerializeField] private float m_BaseHandlePrice;
        [SerializeField] private TextAsset m_Mapping;
        [SerializeField] private CabinetSectionDoorHandleData m_DefaultHandle;
        [SerializeField] private CabinetSectionDoorHandleData[] m_Handles;
        
        // =====================================================================
        // Editor
        // =====================================================================
#if UNITY_EDITOR
        [Header("Editor")]
        [SerializeField] private Transform[] m_Models;

        [ContextMenu("Build Handle Data From Mapping")]
        private void BuildHandleDataFromMapping()
        {
            if (m_Mapping == null)
            {
                Debug.LogError("Mapping is not assigned.", this);
                return;
            }

            string[] lines =
                m_Mapping.text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            List<CabinetSectionDoorHandleData> prefabs = new List<CabinetSectionDoorHandleData>();
            int accepts = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    Debug.Log($"Skipping comment/empty line {i + 1}: {line}", this);
                    continue;
                }

                string[] parts = line.Split(',');
                if (parts.Length < 3)
                {
                    Debug.LogWarning($"Skipping malformed line {i + 1}: {line}", this);
                    continue;
                }

                string id = parts[0].Trim();
                string prefabName = parts[1].Trim();
                string handleCountText = parts[2].Trim();

                if (m_Models != null)
                {
                    bool hasModel = false;

                    for (int j = 0; j < m_Models.Length; j++)
                    {
                        Transform model = m_Models[j];
                        if (model != null && model.name.ToLower() == prefabName.ToLower())
                        {
                            if (!int.TryParse(handleCountText, out int handleCount))
                            {
                                Debug.LogWarning($"Invalid handle count '{handleCountText}' on line {i + 1}.", this);
                                handleCount = 0;
                            }

                            prefabs.Add(
                                new CabinetSectionDoorHandleData
                                {
                                    id = id,
                                    prefab = model,
                                    count = handleCount
                                }
                            );
                            hasModel = true;
                            accepts++;
                            break;
                        }
                    }

                    if(hasModel == false)
                    {
                        Debug.LogWarning($"No model found for '{prefabName}' on line {i + 1}.", this);
                    }
                }
            }

            Debug.Log($"Processed {lines.Length} lines, accepted {accepts} handle prefabs.", this);

            m_Handles = prefabs.ToArray();
            EditorUtility.SetDirty(this);
        }
#endif
    }   // class CabinetSectionDoorHandleStyle

}   // namespace KitchenDesign
