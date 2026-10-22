namespace KitchenDesign.AddressableContent
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;
#endif

    public interface IAssetAddress
    {
        System.Type assetType { get; }

#if UNITY_EDITOR
        Object editorOnlyCachedAsset { get; }

        void SetAddress(SerializedProperty property, AddressableAssetEntry targetEntry);
#endif
    }
}
