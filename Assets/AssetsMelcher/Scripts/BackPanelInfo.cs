namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    [System.Serializable]
    public class BackPanelInfo
    {
        public int categoryId => m_CategoryId;
        public BrandCatalog.FridgePanelInfo[] backPanels => m_BackPanels;

#if UNITY_EDITOR
        [SerializeField] protected string m_Tag;
#endif

        [Tooltip("Cabinet Section Category ID. This is used to match the back panel to the correct cabinet section.")]
        [SerializeField] protected int m_CategoryId;
        [SerializeField] protected BrandCatalog.FridgePanelInfo[] m_BackPanels = default;
    }   // class BackPanelInfo

    [System.Serializable]
    public class BackPanelsInfo
    {
        public BrandCatalog.FridgePanelInfo fallbackBackPanel => m_FallbackBackPanel;
        public BackPanelInfo defaultBackPanels => m_DefaultBackPanels;
        public BackPanelInfo[] backPanels => m_BackPanels;
        public float panelDepthInMeters => m_PanelDepthInInches * Const.Inches2Meters;

        [SerializeField] protected BrandCatalog.FridgePanelInfo m_FallbackBackPanel = default;
        [SerializeField] protected BackPanelInfo m_DefaultBackPanels = default;
        [SerializeField] protected BackPanelInfo[] m_BackPanels = default;
        [SerializeField] protected float m_PanelDepthInInches = default;
    }   // class BackPanelsInfo

}   // namespace KitchenDesign

