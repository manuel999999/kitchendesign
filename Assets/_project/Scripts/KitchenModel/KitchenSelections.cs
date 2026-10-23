using KitchenDesign.Geometry;
using KitchenDesign.Scriptables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Enums;
using System.Collections.Generic;
using KitchenDesign.Cabinets;

namespace KitchenDesign.KitchenModel
{
    public sealed class KitchenSelections
    {
        public System.DateTime sessionStartTime;
        public KitchenBuildingMethod buildingMethod;
        public BrandCatalogSO brandCatalog;
        public KitchenTemplateSO kitchenTemplate;
        public string doorStyleName;
        public string doorHandleStyleName;
        public float endPanelWidthInMetersLeft;
        public float endPanelWidthInMetersRight;
        public float backPanelDepthInMeters;
        public CountertopType countertopType;
        public CabinetLayout layoutPrefab;
        public ProductInfo countertopFinish;
        public ProductInfo cabinetFinish;
        public ProductInfo flooringFinish;
        public float baseCabinetDepthInMeters;
        public float countertopThicknessInMeters;
        public CuboidPadding countertopOverhangInMeters;
        public CabinetToeKickType toeKickType;
        public List<KitchenExtraFeature> extraFeatures = new List<KitchenExtraFeature>();
        public string analyticsKey;
    }
}
