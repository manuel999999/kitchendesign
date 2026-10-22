using KitchenDesign.Shared.Common;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public class EndPanelInfo
    {
        public string productName => m_ProductName;
        public string productId => m_ProductId;
        public float dealerPrice => m_PriceSheetPrice.GetPrice(ProFitGlobals.userProfile.currency);
        public float targetDepth => m_TargetDepth * Units.Inches2Meters;

        [SerializeField] protected string m_ProductName = default;
        [SerializeField] protected string m_ProductId = default;
        [SerializeField] private Price m_PriceSheetPrice = default;

        [Tooltip("Target depth in inches. Product name will be used for the cabinet with this depth.")]
        [SerializeField] protected float m_TargetDepth = default;
    }
}
