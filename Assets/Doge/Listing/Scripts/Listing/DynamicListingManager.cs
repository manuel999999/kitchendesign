using System.Collections.Generic;
using UnityEngine;
using Doge.Common;

namespace Doge.Listing
{
    /// <summary>
    /// Create a list of items in UI and clear container every UpdateDisplay call.
    /// </summary>
    public abstract class DynamicListingManager<TPrefab, TData> : MonoBehaviour where TPrefab : ListingItem<TData>
    {
        protected abstract RectTransform ItemsContainer { get; }
        protected abstract TPrefab PrefabItem { get; }
        protected List<TPrefab> _listedItems;

        public List<TPrefab> ListedItems => _listedItems;

        public virtual void UpdateDisplay(List<TData> items) {
            ClearItemList();
            
            for (int i = 0; i < items.Count; i++) {
                CreateElement(items[i]);
            }
        }

        public virtual void CreateElement(TData item) {
			var newItem = Instantiate(PrefabItem, ItemsContainer);
			newItem.UpdateDisplay(item);
			_listedItems.Add(newItem);
		}

        public virtual void ClearItemList() {
			if(_listedItems == null || _listedItems.Count == 0) {
                _listedItems = new List<TPrefab>();
                return;
            }

			for(int i = 0; i < _listedItems.Count; i++) {
				Destroy(_listedItems[i].gameObject);
            }

            Helper.InitializeList(ref _listedItems);
        }
    }
}
