using KitchenDesign.Shared.Enums;
using Melcher;
using UnityEngine;

namespace KitchenDesign.Serializables
{


    [System.Serializable]
    public struct Price
    {
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

        [SerializeField]
        private float m_Usd;

        [SerializeField]
        private float m_Cad;
    }
}
