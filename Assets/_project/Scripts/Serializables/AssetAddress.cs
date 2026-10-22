namespace KitchenDesign.Serializables
{
    using Melcher;
    using KitchenDesign.AddressableContent;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets;
#endif

    [System.Serializable]
    public struct AssetAddress<TAsset>
        : IKeyEvaluator
        , IAssetAddress
        , ISerializationCallbackReceiver
        where TAsset : Object
    {
        public AssetAddress(string rawAddress)
        {
            m_Data.address = rawAddress;

#if UNITY_EDITOR

            TAsset asset = null;

            AddressableAssetEntry entry = FindEntryByAddress(rawAddress);

            if (entry != null)
            {
                asset = (TAsset)entry.TargetAsset;
            }

            m_Data.editorOnlyCachedAsset = asset;

            static AddressableAssetEntry FindEntryByAddress(string address)
            {
                AddressableAssetSettings addressableSettings =
                    AddressableAssetSettingsDefaultObject.Settings;

                if (!addressableSettings)
                {
                    return null;
                }

                foreach (AddressableAssetGroup group in addressableSettings.groups)
                {
                    foreach (AddressableAssetEntry entry in group.entries)
                    {
                        if (entry.address == address)
                        {
                            return entry;
                        }
                    }
                }

                return null;
            }
#endif
        }

        public bool hasValue => !string.IsNullOrEmpty(m_Data.address);

        public override string ToString()
        {
            return m_Data.address != null ? m_Data.address : "<NullAddress>";
        }

        public override bool Equals(object obj)
        {
            return obj is AssetAddress<TAsset> objAsThisType && this == objAsThisType;
        }

        public static bool operator ==(AssetAddress<TAsset> a, AssetAddress<TAsset> b)
        {
            return Equals(a.m_Data.address, b.m_Data.address);
        }

        public static bool operator !=(AssetAddress<TAsset> a, AssetAddress<TAsset> b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return m_Data.address != null ? m_Data.address.GetHashCode() : 0;
        }

        object IKeyEvaluator.RuntimeKey => m_Data.address;

        bool IKeyEvaluator.RuntimeKeyIsValid()
        {
            return !string.IsNullOrWhiteSpace(m_Data.address);
        }

        System.Type IAssetAddress.assetType => typeof(TAsset);

        [SerializeField]
        private AssetAddressData m_Data;

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (m_Data.editorOnlyCachedAsset &&
                AddressablesForEditor.IsAddressable(m_Data.editorOnlyCachedAsset, out AddressableAssetEntry entry) &&
                entry.address != m_Data.address)

            {
                MelcherLog.LogWarning($"Updating stale asset address for {m_Data.editorOnlyCachedAsset.name}: {m_Data.address} -> {entry.address}", m_Data.editorOnlyCachedAsset);
                m_Data.address = entry.address;
            }
#endif
        }

#if UNITY_EDITOR

        public TAsset editorOnlyCachedAsset => (TAsset)m_Data.editorOnlyCachedAsset;

        Object IAssetAddress.editorOnlyCachedAsset => m_Data.editorOnlyCachedAsset;

        public void SetAddress(SerializedProperty property, AddressableAssetEntry targetEntry)
        {
            using (SerializedProperty addressProperty = FindDataProperty(property, nameof(m_Data.address)))
            {
                addressProperty.stringValue = targetEntry?.address;
            }

            using (SerializedProperty cachedAssetProperty = FindDataProperty(property, nameof(m_Data.editorOnlyCachedAsset)))
            {
                Object asset = null;

                if (targetEntry != null)
                {
                    asset = targetEntry.TargetAsset;

                    if (asset is GameObject targetGameObject &&
                        typeof(Component).IsAssignableFrom(typeof(TAsset)))
                    {
                        asset = targetGameObject.GetComponent<TAsset>();
                    }
                }

                cachedAssetProperty.objectReferenceValue = asset;
            }

            property.serializedObject.ApplyModifiedProperties();

            static SerializedProperty FindDataProperty(SerializedProperty property, string subPropertyName)
            {
                return property.FindPropertyRelative(nameof(m_Data) + "." + subPropertyName);
            }
        }

        public static bool TryFromAsset(TAsset asset, out AssetAddress<TAsset> result)
        {
            result = default;

            if (!asset)
            {
                return false;
            }

            AddressableAssetSettings addressableSettings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (!addressableSettings)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            AddressableAssetEntry assetEntry =
                addressableSettings.FindAssetEntry(assetGuid);

            if (assetEntry == null)
            {
                return false;
            }

            result.m_Data.address = assetEntry.address;
            result.m_Data.editorOnlyCachedAsset = asset;

            return true;
        }

        public static bool TryFromGuid(string guid, out AssetAddress<TAsset> result)
        {
            result = default;

            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);

            return TryFromAsset(asset, out result);
        }

#endif
    }
}
