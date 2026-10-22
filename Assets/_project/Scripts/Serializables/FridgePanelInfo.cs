using KitchenDesign.Scriptables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public class FridgePanelInfo
    {
        public string productName => m_ProductName;
        public string productId => m_ProductId;

        public string panelHeight => m_PanelHeightInInches.ToString();

        public float dealerPrice => m_PriceSheetPrice.GetPrice(ProFitGlobals.userProfile.currency);

        public float specificPanelWidthInMeters => m_SpecificPanelWidthInInches * Units.Inches2Meters;

        [SerializeField]
        protected string m_ProductName = default;

        [SerializeField]
        protected string m_ProductId = default;

        [SerializeField]
        protected float m_PanelHeightInInches = default;

        [SerializeField]
        private Price m_PriceSheetPrice = default;

        [Tooltip("This field servers two purposes: For back panels, it " +
            "is the width of the cabinet from the fridge's left edge to " +
            "it's right edge. For side panels, it is the width the panel " +
            "adds to the fridge cabinet's padding on any side where the " +
            "fridge touches the end of the kitchen.")]
        [SerializeField]
        protected float m_SpecificPanelWidthInInches = default;
    }
}
