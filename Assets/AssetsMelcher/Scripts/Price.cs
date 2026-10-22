namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     Allows a price to be specified in various currencies. Currently,
    ///     this is only used by <see cref="BrandCatalog"/> (specifically, its
    ///     <see cref="BrandCatalog.FridgePanelInfo"/>) because most other
    ///     prices are handled server-side which the Unity app can be currency-
    ///     agnostic to.
    /// </summary>
    [System.Serializable]
    public struct Price
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public float GetPrice(Currency currency)
        {
            return
                currency switch
                {
                    Currency.USD => m_Usd,
                    Currency.CAD => m_Cad,
                    _ => throw new System.ArgumentException($"Unhandled currency: {currency}", nameof(currency))
                };
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        private float m_Usd;

        [SerializeField]
        private float m_Cad;
    }   // class Price

}   // namespace KitchenDesign

