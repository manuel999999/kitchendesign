using KitchenDesign.Shared.Common;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public class BackPanelsInfo
    {
        public FridgePanelInfo fallbackBackPanel => m_FallbackBackPanel;
        public BackPanelInfo defaultBackPanels => m_DefaultBackPanels;
        public BackPanelInfo[] backPanels => m_BackPanels;
        public float panelDepthInMeters => m_PanelDepthInInches * Units.Inches2Meters;

        [SerializeField] protected FridgePanelInfo m_FallbackBackPanel = default;
        [SerializeField] protected BackPanelInfo m_DefaultBackPanels = default;
        [SerializeField] protected BackPanelInfo[] m_BackPanels = default;
        [SerializeField] protected float m_PanelDepthInInches = default;
    }
}
