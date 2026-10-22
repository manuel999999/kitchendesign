using System.Collections.ObjectModel;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{
    [System.Serializable]
    public struct ProductIdRemapEntry
    {
        public ProductIdRemapEntry(int productId, string productSku)
        {
            m_ProductId = productId;
            m_KnownProductSkus = new ();
            m_KnownProductSkus.mutable.Add(productSku);
        }

        public int productId => m_ProductId;

        public ReadOnlyCollection<string> skus => m_KnownProductSkus.readOnly;

        [SerializeField]
        private int m_ProductId;

        [SerializeField]
        [Tooltip("The list of known product SKUs that were once used by " +
            "this product ID.")]
        private MelcherReadWriteList<string> m_KnownProductSkus;
    }
}
