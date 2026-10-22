namespace KitchenDesign.Serializables
{
    using UnityEngine;

    [System.Serializable]
    internal struct AssetAddressData
    {
        public string address;

#if UNITY_EDITOR
        public Object editorOnlyCachedAsset;
#endif
    }
}
