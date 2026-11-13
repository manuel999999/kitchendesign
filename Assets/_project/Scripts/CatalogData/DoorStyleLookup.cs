namespace KitchenDesign.CatalogData
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using System.Collections.Generic;
    using KitchenDesign.Serializables;

    public sealed class DoorStyleLookup
    {

        public ProductVisualSO FindDoorStyleVisual(
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

        public void TryInitialize(BrandCatalogSO catalog)
        {
            if(_isCollectionsInitialized == false)
            {
                _initializedCatalogsCollection = new();
                _rejectedCatalogsCollection = new();
                _doorMapping = new();

                _isCollectionsInitialized = true;
            }

            if(_rejectedCatalogsCollection.Contains(catalog.catalogId))
            {
                return;
            }

            if(_initializedCatalogsCollection.Contains(catalog.catalogId))
            {
                return;
            }

            if(catalog.cabinetSectionDoorStyle == default)
            {
                _rejectedCatalogsCollection.Add(catalog.catalogId);
                return;
            }

            AddToDoorMapping(catalog.cabinetSectionDoorStyle.mapping);
        }

        private void AddToDoorMapping(CabinetSectionDoorData[] catalogMapping)
        {
            for(int i = 0; i < catalogMapping.Length; i++)
            {
                CabinetSectionDoorData mappingLine = catalogMapping[i];
                _doorMapping[mappingLine.key] = mappingLine.value;
            }
        }

        public string FindMappedDoorId(string cabinetId)
        {
            return _doorMapping.ContainsKey(cabinetId) ?
                    _doorMapping[cabinetId] :
                    string.Empty;
        }

        private HashSet<int> _initializedCatalogsCollection;
        private HashSet<int> _rejectedCatalogsCollection;
        private bool _isCollectionsInitialized = false;
        private Dictionary<string, string> _doorMapping;
    }
}

