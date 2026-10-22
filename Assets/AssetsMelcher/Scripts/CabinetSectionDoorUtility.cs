namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using System.Collections.Generic;
    using KitchenDesign.Serializables;

    public static class CabinetSectionDoorUtility
    {
#region Methods
        // =====================================================================
        // Static Methods
        // =====================================================================
        public static ProductVisualSO FindDoorStyleVisual(
            IReadOnlyList<ProductVisualSO>    productVisuals,
            ProductVisualSO                   cabinetSectionVisual,
            BrandCatalogSO                    brandCatalog,
            string                          doorStyleName)
        {
            if(brandCatalog == null)
            {
                return null;
            }

            TryInitialize(brandCatalog);

            if (productVisuals == null)
            {
                throw new System.ArgumentNullException(nameof(productVisuals));
            }

            if (!cabinetSectionVisual)
            {
                throw new System.ArgumentNullException(nameof(cabinetSectionVisual));
            }

            // This has a '^' suffix.
            string cabinetSectionVisualName;

            string alternativeVisualName = FindMappedDoorId(cabinetSectionVisual.info.id);
            if(alternativeVisualName != string.Empty && brandCatalog.cabinetSectionDoorStyle != null)
            {
                cabinetSectionVisualName = $"{"ProductVisual"}_{alternativeVisualName}";
            }
            else
            {
                string usedName = cabinetSectionVisual.name;
                
                cabinetSectionVisualName =
                    usedName.Substring(0, length: usedName.Length - 1);
            }

            string expectedDoorStyleVisualName =
                cabinetSectionVisualName + "@" + doorStyleName + "^";

            string alternativeDoorStyleVisualName = 
                cabinetSectionVisualName + "@" + brandCatalog.defaultCabinetSectionDoorStyleName + "^";

            bool foundAlternative = false;

            ProductVisualSO doorStyleVisual = null;
            ProductVisualSO alternativeDoorStyleVisual = null;

            foreach (ProductVisualSO visual in productVisuals)
            {
                if (visual.info.runtimePrefabCategory != ProductCategorySpecific.CabinetDoor)
                {
                    continue;
                }

                if(foundAlternative == false)
                {
                    if(visual.name == alternativeDoorStyleVisualName)
                    {
                        alternativeDoorStyleVisual = visual;
                        foundAlternative = true;
                    }
                }

                if (visual.name != expectedDoorStyleVisualName)
                {
                    continue;
                }

                doorStyleVisual = visual;
                break;
            }

            bool foundCabinetDoorVisual = doorStyleVisual;

            if (foundCabinetDoorVisual != cabinetSectionVisual.info.expectingCabinetDoors)
            {
                // Warn that finding a door style was or wasn't expected here.
                // The type of expectation depends on the cabinet section.

                if (cabinetSectionVisual.info.expectingCabinetDoors)
                {
                    MelcherLog.LogError($"Could not find a visual with the name {MelcherFormat.ConsoleString(doorStyleName)}");
                }
                else
                {
                    MelcherLog.LogError($"Found a visual with the name {MelcherFormat.ConsoleString(doorStyleName)} but expected to find none.");
                }
            }

            if(doorStyleVisual != null)
            {
                return doorStyleVisual;
            }
            else if(alternativeDoorStyleVisual != null)
            {
                MelcherLog.LogWarning($"Could not find a visual with the name {MelcherFormat.ConsoleString(expectedDoorStyleVisualName)}, but found {MelcherFormat.ConsoleString(alternativeDoorStyleVisualName)}. Returning the latter.");
                return alternativeDoorStyleVisual;
            }
            else
            {
                MelcherLog.LogError($"Could not find a visual with the name {MelcherFormat.ConsoleString(expectedDoorStyleVisualName)} or {MelcherFormat.ConsoleString(alternativeDoorStyleVisualName)}.");
                return null;
            }
        }

        public static void TryInitialize(BrandCatalogSO catalog)
        {
            if(m_IsCollectionsInitialized == false)
            {
                m_InitializedCatalogsCollection = new();
                m_RejectedCatalogsCollection = new();
                m_DoorMapping = new();

                m_IsCollectionsInitialized = true;
            }

            if(m_RejectedCatalogsCollection.Contains(catalog.catalogId))
            {
                return;
            }

            if(m_InitializedCatalogsCollection.Contains(catalog.catalogId))
            {
                return;
            }

            if(catalog.cabinetSectionDoorStyle == default)
            {
                m_RejectedCatalogsCollection.Add(catalog.catalogId);
                return;
            }

            AddToDoorMapping(catalog.cabinetSectionDoorStyle.mapping);
        }

        private static void AddToDoorMapping(CabinetSectionDoorData[] catalogMapping)
        {
            for(int i = 0; i < catalogMapping.Length; i++)
            {
                CabinetSectionDoorData mappingLine = catalogMapping[i];
                m_DoorMapping[mappingLine.key] = mappingLine.value;
            }
        }

        public static string FindMappedDoorId(string cabinetId)
        {
            return m_DoorMapping.ContainsKey(cabinetId) ? 
                    m_DoorMapping[cabinetId] :
                    string.Empty;
        }
#endregion /Methods

#region Fields
        // =====================================================================
        // Static Fields
        // =====================================================================

        private static HashSet<int> m_InitializedCatalogsCollection;
        private static HashSet<int> m_RejectedCatalogsCollection;
        private static bool m_IsCollectionsInitialized = false;
        private static Dictionary<string, string> m_DoorMapping;
#endregion /Fields
    }   // class CabinetSectionDoorUtility

}   // namespace KitchenDesign

