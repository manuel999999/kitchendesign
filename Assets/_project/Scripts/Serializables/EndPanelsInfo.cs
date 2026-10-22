using KitchenDesign.Shared.Common;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public class EndPanelsInfo
    {
        public EndPanelInfo[] endPanels => m_EndPanels;
        public float specificPanelWidthInMeters => m_SpecificPanelWidthInInches * Units.Inches2Meters;

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
}
