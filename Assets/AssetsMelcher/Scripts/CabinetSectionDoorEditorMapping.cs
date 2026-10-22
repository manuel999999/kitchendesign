#if UNITY_EDITOR
namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEditor;
    using UnityEngine;

    [CreateAssetMenu(fileName = "CabinetSectionDoorEditorMapping", menuName = "Melcher Studios/Pro-Fit/Cabinet Section Door Editor Mapping")]
    public class CabinetSectionDoorEditorMapping : MelcherScriptableObject
    {
        // =====================================================================
        // Properties
        // =====================================================================
        public const string SuppressedDoorValidationErrorString = "--supressed-door-validation-error-+-";

        public CabinetSectionDoorData[] mapping => m_Doors;

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] private CabinetSectionDoorData[] m_Doors;
        [SerializeField] private string[] m_SuppressedValidationErrorNameContains;

        // ====================================================================
        // Methods
        // ====================================================================
        [ContextMenu("Load All Used Cabinet Door Mapping")]
        private void LoadFullCabinetDoorMapping()
        {
            List<CabinetSectionDoorData> map = new();
            HashSet<string> registeredDoors = new();

            AppProfile appProfile = AssetDatabase.LoadAssetAtPath<AppProfile>("Assets/AssetsMelcher/Resources/AppProfile.asset");
            
            ReadOnlyCollection<BrandCatalog> brandCatalogs = appProfile.presetBrandCatalogs;

            for(int i = 0; i < brandCatalogs.Count; i++)
            {
                TryAddToFullCabinetDoorMapping(ref map, ref registeredDoors, brandCatalogs[i]);
            }

            m_Doors = map.ToArray();
            EditorUtility.SetDirty(this);
        }

        private void TryAddToFullCabinetDoorMapping(ref List<CabinetSectionDoorData> map, ref HashSet<string> registeredDoors, BrandCatalog catalog)
        {
            CabinetSectionDoorStyle doorStyles = catalog.cabinetSectionDoorStyle;
            if(doorStyles == default)
            {
                Debug.LogWarning($"Brand Catalog {catalog.name} does not have a Cabinet Section Door Style assigned. Skipping.");
                return;
            }

            if(doorStyles.mapping.Length == 0)
            {
                Debug.LogWarning($"Brand Catalog {catalog.name} does not have any Cabinet Section Door mappings assigned. Skipping.");
                return;
            }

            for(int i = 0; i < doorStyles.mapping.Length; i++)
            {
                CabinetSectionDoorData doorData = doorStyles.mapping[i];
                if(registeredDoors.Contains(doorData.value))
                {
                    continue;
                }

                registeredDoors.Add(doorData.value);
                map.Add(doorData);
            }
        }

        private string GetNotFoundReturnString(string doorName)
        {
            if (m_SuppressedValidationErrorNameContains == null || m_SuppressedValidationErrorNameContains.Length == 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < m_SuppressedValidationErrorNameContains.Length; i++)
            {
                if (doorName.Contains(m_SuppressedValidationErrorNameContains[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    return SuppressedDoorValidationErrorString;
                }
            }

            return string.Empty;
        }

        // ====================================================================
        // Public Methods
        // ====================================================================
        public string FindCabinetBaseFromMapping(string doorName)
        {
            string productVisualPrefix = "ProductVisual_";
            string usedDoorName = doorName;
            if(usedDoorName.StartsWith(productVisualPrefix, System.StringComparison.Ordinal))
            {
                usedDoorName = usedDoorName.Substring(productVisualPrefix.Length);
            }

            for(int i = 0; i < mapping.Length; i++)
            {
                CabinetSectionDoorData item = mapping[i];
                if(usedDoorName == item.value)
                {
                    return $"{productVisualPrefix}{item.key}";
                }
            }

            return GetNotFoundReturnString(doorName);
        }
    }   // class CabinetSectionDoorEditorMapping

}   // namespace KitchenDesign
#endif