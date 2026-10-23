namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Server;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.Shared.Interfaces;

    /// <summary>
    /// This interface is used by cabinets to request information from the
    /// kitchen they belong to.
    /// </summary>
    public interface IKitchen
    {
        public KitchenBuildingMethod selectedBuildingMethod { get; }

        public IBuildingMethodRules buildingRules { get; }

        public BrandCatalogSO selectedBrandCatalog { get; }

        public KitchenTemplateSO selectedKitchenTemplate { get; }

        public CountertopType selectedCountertopType { get; }

        public CabinetToeKickType selectedToeKickType { get; }

        public string selectedDoorStyleName { get; }
        public string selectedDoorHandleStyleName { get; }

        public CabinetLayout layout { get; }

        public float cabinetHeightInMeters { get; }
        public float countertopThicknessInMeters { get; }
        public float baseCabinetDepthInMeters { get; }
        public CuboidPadding baseCountertopOverhangInMeters { get; }
        public float backPanelDepthInMeters { get; }

        public ProductVisualDatabaseSO productVisualDatabase { get;  }

        /// <summary>
        ///     Return <see langword="true"/> if cabinets can display columns
        ///     at their corners. This is intended to be true when Sections
        ///     building method is enabled, i.e., in the Stoll app version.
        /// </summary>
        public bool doCabinetsHaveColumns { get; }

        public List<KitchenExtraFeature> selectedExtraFeatures { get; }

        public bool AreProductsForcedToCenterOfCabinets(Cabinet cabinetForRuleset);

        public UniTask<(Material cabinetMaterial, Material countertopMaterial)> GetFinishMaterials(bool allowHighlightToReturn);
        public UniTask<Occupant> InstantiateOccupant(ProductInfo productInfo, ProductVisualSO productVisual, ProductVisualContent productVisualPrefab);

    }   // interface IKitchen

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-01
/// ===========================================================================
