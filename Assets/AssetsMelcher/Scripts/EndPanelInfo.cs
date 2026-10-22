namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    [System.Serializable]
    public class EndPanelInfo
    {
        public string productName => m_ProductName;
        public string productId => m_ProductId;
        public float dealerPrice => m_PriceSheetPrice.GetPrice(ProFitGlobals.userProfile.currency);
        public float targetDepth => m_TargetDepth * Const.Inches2Meters;

        [SerializeField] protected string m_ProductName = default;
        [SerializeField] protected string m_ProductId = default;
        [SerializeField] private Price m_PriceSheetPrice = default;

        [Tooltip("Target depth in inches. Product name will be used for the cabinet with this depth.")]
        [SerializeField] protected float m_TargetDepth = default;
    }   // class EndPanelInfo

    [System.Serializable]
    public class EndPanelsInfo
    {
        public EndPanelInfo[] endPanels => m_EndPanels;
        public float specificPanelWidthInMeters => m_SpecificPanelWidthInInches * Const.Inches2Meters;

        [SerializeField] protected EndPanelInfo[] m_EndPanels = default;
        [SerializeField] protected float m_SpecificPanelWidthInInches = default;

        public EndPanelInfo GetEndPanelInfoByTargetDepthOrDefault(float targetDepth)
        {
            foreach (var endPanel in m_EndPanels)
            {
                if (Mathf.Approximately(endPanel.targetDepth, targetDepth))
                {
                    return endPanel;
                }
            }

            return m_EndPanels[0];
        }
    }

}   // namespace KitchenDesign

