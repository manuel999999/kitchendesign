using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Doge.Listing
{
    public abstract class GridListingManager<T> : MonoBehaviour
    {
        [SerializeField] protected RectTransform body;
        [SerializeField] protected GridLayoutGroup gridContainer;
        
        public virtual void UpdateDisplay() {
            float size = InitialSize;

            var items = Items;

            for (int i = 0; i < items.Count; i++) {
                size += CellSize + CellSpacing;
                ListItem(items[i]);
            }

            size = size < MinimumSize ? MinimumSize : size;
            Finally(size);
        }

		/// <summary>
		/// List of items to be listed.
		/// </summary>
        protected abstract List<T> Items { get; }

		/// <summary>
		/// Initial witdh or height.
		/// </summary>
        protected abstract float InitialSize { get; }

		/// <summary>
		/// Minimum witdh or height.
		/// </summary>
        protected abstract float MinimumSize { get; }

		/// <summary>
		/// <para> Grid cell size. </para>
		/// <para> Use this.gridContainer.cellSize.x for width. </para>
		/// <para> Use this.gridContainer.cellSize.y for height. </para>
		/// </summary>
		protected abstract float CellSize { get; }

		/// <summary>
		/// <para> Grid cell spacing. </para>
		/// <para> Use this.gridContainer.spacing.x for horizontal. </para>
		/// <para> Use this.gridContainer.spacing.y for vertical. </para>
		/// </summary>
		protected abstract float CellSpacing { get; }

		/// <summary>
		/// Perform the displaying or listing of the item.
		/// </summary>
        protected abstract void ListItem(T item);

		/// <summary>
		/// <para> finalize the listing by resizing the container and resetting the scroll rect if any. </para>
		/// <para> example: </para>
		/// <para> this.body.sizeDelta = new Vector2(body.sizeDelta.x, size + 5); resize the container, this example shows resizing of height. </para>
		/// <para> this.scrollRect.verticalNormalizedPosition = 1; reset the position of scrollrect, this example shows vertical reset. </para>
		/// </summary>
		protected abstract void Finally(float size);
	}
}
