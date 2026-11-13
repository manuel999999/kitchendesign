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
            _doorStyleDisplayName = doorStyleDisplayName == string.Empty ? doorStyleName : doorStyleDisplayName;
        }

        public string name => m_Name;
        public string doorStyleDisplayName => _doorStyleDisplayName;

        public ReadOnlyCollection<ProductVisualSO> visuals => _visuals.readOnly;

        public Sprite toolbarButtonSprite => _toolbarButtonSprite;

        public void AddVisual(ProductVisualSO productVisual)
        {
            if (!_visuals.readOnly.Contains(productVisual))
            {
                _visuals.mutable.Add(productVisual);
            }
        }

        protected string m_Name;
        protected string _doorStyleDisplayName;
        protected MelcherReadWriteList<ProductVisualSO> _visuals = new ();
        protected Sprite _toolbarButtonSprite;
    }
}
