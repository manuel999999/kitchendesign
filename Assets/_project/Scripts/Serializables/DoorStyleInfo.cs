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
    public class DoorStyleInfo
    {
        public DoorStyleInfo(string doorStyleName)
        {
            m_Name = doorStyleName;
        }

        public DoorStyleInfo(string doorStyleName, string doorStyleDisplayName)
        {
            m_Name = doorStyleName;
            m_DoorStyleDisplayName = doorStyleDisplayName == string.Empty ? doorStyleName : doorStyleDisplayName;
        }

        public string name => m_Name;
        public string doorStyleDisplayName => m_DoorStyleDisplayName;

        public ReadOnlyCollection<ProductVisualSO> visuals => m_Visuals.readOnly;

        public Sprite toolbarButtonSprite => m_ToolbarButtonSprite;

        public void AddVisual(ProductVisualSO productVisual)
        {
            if (!m_Visuals.readOnly.Contains(productVisual))
            {
                m_Visuals.mutable.Add(productVisual);
            }
        }

        protected string m_Name;
        protected string m_DoorStyleDisplayName;
        protected MelcherReadWriteList<ProductVisualSO> m_Visuals = new ();
        protected Sprite m_ToolbarButtonSprite;
    }
}
