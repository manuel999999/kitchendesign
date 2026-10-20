using UnityEngine;

namespace Doge.Listing
{
    public abstract class ListingItem<T> : MonoBehaviour
    {
        public abstract void UpdateDisplay(T item);
    }
}
