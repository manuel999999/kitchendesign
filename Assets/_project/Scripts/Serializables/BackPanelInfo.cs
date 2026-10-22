using KitchenDesign.Shared.Common;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public class BackPanelInfo
    {
        public int categoryId => m_CategoryId;
        public FridgePanelInfo[] backPanels => m_BackPanels;

#if UNITY_EDITOR
        [SerializeField] protected string m_Tag;
#endif

        [Tooltip("Cabinet Section Category ID. This is used to match the back panel to the correct cabinet section.")]
        [SerializeField] protected int m_CategoryId;
        [SerializeField] protected FridgePanelInfo[] m_BackPanels = default;
    }
}
