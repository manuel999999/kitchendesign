using System.Collections.Generic;
using UnityEngine;

namespace Doge.Listing
{
    /// <summary>
    /// Create a list of items in UI.
    /// </summary>
    public abstract class ListingManager<TPrefab, TData> : MonoBehaviour where TPrefab : ListingItem<TData>
    {
        protected abstract RectTransform ItemsContainer { get; }
        protected abstract TPrefab PrefabItem { get; }

        public virtual void UpdateDisplay(List<TData> items) {
            for (int i = 0; i < items.Count; i++) {
                CreateElement(items[i]);
            }
        }

        public virtual void CreateElement(TData item) {
			var newItem = Instantiate(PrefabItem, ItemsContainer);
			newItem.UpdateDisplay(item);
		}
    }
}
