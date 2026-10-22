namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets;
#endif  // if UNITY_EDITOR

    /// <summary>
    ///     An alternative to <see cref="AssetReferenceT{TObject}"/>. It stores
    ///     the address of an asset without any built-in load or unload
    ///     functionality.
    ///     <br /><br />
    ///
    ///     This is more fitting than <see cref="AssetReferenceT{TObject}"/>
    ///     when it comes to passing addressables between scripts. Unlike
    ///     Unity's <see cref="AssetReferenceT{TObject}"/>, this type expresses
    ///     no ownership on the underlying asset. This means the act of
    ///     referring to an asset is simple and without unnecessary complexity
    ///     of simultaneously managing its lifetime. The user is better able to
    ///     manage asset lifetime on their end.
    /// </summary>
    /// <intent>
    ///     The main issue with <see cref="AssetReferenceT{TObject}"/> is that
    ///     its load and unload helper functions are incredibly restrictive:
    ///     they only track a single load operation.
    ///     <br /><br />
    ///
    ///     This means that trying to pass a standard addressable reference
    ///     between scripts will lead to ambiguity determining who owns the
    ///     reference.
    ///     <br /><br />
    ///
    ///     It can be argued that <see cref="AssetReferenceT{TObject}"/>
    ///     incorrectly mixes the job of referencing an addressable with the
    ///     responsibility of managing its life cycle.
    ///     <br /><br />
    ///
    ///     One of the designers of Unity Addressables, unity_bill, has stated
    ///     that if he were to re-make <see cref="AssetReferenceT{TObject}"/>,
    ///     he would not include the loading and unloading helper functions:
    ///     https://discussions.unity.com/t/1-15-1-assetreference-not-allow-loadassetasync-twice/806407/40
    /// </intent>
    /// <typeparam name="TAsset">
    ///     The type of the Unity asset whose address is being held.
    /// </typeparam>
    // REVIEW:  Should we forbid TAsset from being a Component? (We technically
    //          can't declare that in C# with the current type constraints
    //          system, but Addressables can realistically only reference/fetch
    //          top-level object: GameObject, ScriptableObject, Asset, etc.
    //          E.g., it would be arduous to disambiguate between multiple
    //          components.
    //
    //          Yet, it's super convenient to be able to navigate through
    //          references of a specific type. We should try our best to
    //          preserve this functionality to reference anything.
    //
    //          -Ryan
    [System.Serializable]
    public struct AssetAddress<TAsset>
        : IKeyEvaluator
        , IAssetAddress
        , ISerializationCallbackReceiver
        where TAsset : Object
    {
        // ====================================================================
        // Constructors
        // ====================================================================

        /// <summary>
        ///     Create an <see cref="AssetAddress{AssetType}"/> from a raw
        ///     address. This does not validate the address or the asset type.
        /// </summary>
        public AssetAddress(string rawAddress)
        {
            m_Data.address = rawAddress;

#if UNITY_EDITOR
            // Try to establish a direct reference to the asset at editor time.
            // This means the runtime AssetAddresses can be used similarly to
            // inspector-serialized AssetAddresses.

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
#endif  // if UNITY_EDITOR
        }

        // ====================================================================
        // Properties
        // ====================================================================

        public bool hasValue => !string.IsNullOrEmpty(m_Data.address);


        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Return the raw address.
        /// </summary>
        public override string ToString()
        {
            return m_Data.address != null ? m_Data.address : "<NullAddress>";
        }

        // ====================================================================
        // Equality
        // ====================================================================

        public override bool Equals(object obj)
        {
            return obj is AssetAddress<TAsset> objAsThisType && this == objAsThisType;
        }

        public static bool operator ==(AssetAddress<TAsset> a, AssetAddress<TAsset> b)
        {
            // Use value-equality (`Equals()`) because reference-equality (`==`)
            // can return false-negatives ('unique' strings with identical content).
            return Equals(a.m_Data.address, b.m_Data.address);
        }

        public static bool operator !=(AssetAddress<TAsset> a, AssetAddress<TAsset> b)
        {
            return !(a == b);
        }

        // ====================================================================
        // GetHashCode
        // ====================================================================

        public override int GetHashCode()
        {
            return m_Data.address != null ? m_Data.address.GetHashCode() : 0;
        }

        // ====================================================================
        // IKeyEvaluator
        // ====================================================================

        object IKeyEvaluator.RuntimeKey => m_Data.address;

        bool IKeyEvaluator.RuntimeKeyIsValid()
        {
            return !string.IsNullOrWhiteSpace(m_Data.address);
        }

        // ====================================================================
        // IAssetAddress
        // ====================================================================

        System.Type IAssetAddress.assetType => typeof(TAsset);

        // ====================================================================
        // Fields
        // ====================================================================

        // REVIEW:  Rename this field to m_AssetAddressData. This makes it
        //          easier to write a tool to parse the YAML and fixup
        //          serialized AssetAddress when their asset's address is
        //          changed. -Ryan
        //          (This may not actually be necessary anymore. Made this
        //          type implement ISerializationCallbackReceiver so it should
        //          be able to auto-correct itself.)
        [SerializeField]
        private Internal.AssetAddressData m_Data;

        // ====================================================================
        // ISerializationCallbackReceiver
        // ====================================================================

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // Ensure that the serialized address stays synchronized if the
            // user ever changes it.
            if (m_Data.editorOnlyCachedAsset &&
                AddressableEditorUtility.IsAddressable(m_Data.editorOnlyCachedAsset, out AddressableAssetEntry entry) &&
                entry.address != m_Data.address)

            {
                MelcherLog.LogWarning($"Updating stale asset address for {m_Data.editorOnlyCachedAsset.name}: {m_Data.address} -> {entry.address}", m_Data.editorOnlyCachedAsset);
                m_Data.address = entry.address;
            }
#endif  // if UNITY_EDITOR
        }

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        /// <summary>
        ///     Type-safe access to the underlying asset. This is only
        ///     available in the editor.
        /// </summary>
        public TAsset editorOnlyCachedAsset => (TAsset)m_Data.editorOnlyCachedAsset;

        Object IAssetAddress.editorOnlyCachedAsset => m_Data.editorOnlyCachedAsset;

        // TODO: Make this accept a TAsset instead of an AddressableAssetEntry
        //       so as to support sub objects? Lookup the main asset's address
        //       and properly reference the subobject when necessary. But how
        //       to handle components on prefabs? -Ryan
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

                    // Try to support taking the address of a Component.
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

        /// <summary>
        ///     Try to create an <see cref="AssetAddress{AssetType}"/> directly
        ///     from an asset. This is limited to editor time.
        /// </summary>
        /// <param name="asset">
        ///     The addressable asset whose address to try to lookup.
        /// </param>
        /// <param name="result">
        ///     If successful, this is an <see cref="AssetAddress{AssetType}"/>
        ///     pointing at <paramref name="asset"/>. Otherwise, it is a
        ///     <see langword="default"/>/invalid address.
        /// </param>
        /// <returns>
        ///     Return <see langword="true"/> if the asset's address was
        ///     located and returned; otherwise, return <see langword="false"/>.
        /// </returns>
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

        /// <summary>
        ///     Utility function that loads an asset by GUID and then returns
        ///     <see cref="TryFromAsset(TAsset, out AssetAddress{TAsset})"/>.
        /// </summary>
        /// <param name="guid">
        ///     The GUID of the asset whose addressable address wants to be
        ///     known.
        /// </param>
        /// <inheritdoc cref="TryFromAsset(TAsset, out AssetAddress{TAsset})"/>
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

#endif  // if UNITY_EDITOR
    }

    /// <summary>
    ///     A non-generic interface that is used by the custom drawer to
    ///     interact with the generic  <see cref="AssetAddress{AssetType}"/>.
    ///     This allows the generic asset type to be queried and visualized in
    ///     the inspector.
    /// </summary>
    public interface IAssetAddress
    {
        System.Type assetType { get; }

#if UNITY_EDITOR
        /// <summary>
        ///     Type-agnostic access to the underlying asset. This is only
        ///     accessible at editor-time and is called from <see
        ///     cref="PropertyDrawer"/> code.
        /// </summary>
        Object editorOnlyCachedAsset { get; }

        /// <summary>
        ///     Update the <see cref="AssetAddress{AssetType}"/> so it points
        ///     at <paramref name="targetEntry"/>. Intended to be called from
        ///     <see cref="PropertyDrawer"/> code. This uses <see
        ///     cref="SerializedProperty"/> to allow the original struct's
        ///     value to be assigned, rather than a copy.
        /// </summary>
        void SetAddress(SerializedProperty property, AddressableAssetEntry targetEntry);
#endif  // if UNITY_EDITOR
    }

    public static class AssetAddressUtility
    {
        /// <summary>
        ///     Convert an asset address from <typeparamref name="TDerived"/>
        ///     to <typeparamref name="TParent"/>. This is an upcast which
        ///     means the two types must follow a childclass-superclass
        ///     relationship.
        ///     <br /><br />
        ///
        ///     For example, casting a <see cref="Texture2D"/> address to a
        ///     <see cref="Texture"/> address.
        /// </summary>
        /// <typeparam name="TDerived">
        ///     The current asset type that will be converted to one if it's
        ///     superclass's type.
        /// </typeparam>
        /// <typeparam name="TParent">
        ///     A superclass of <typeparamref name="TParent"/>.
        /// </typeparam>
        /// <param name="address">
        ///     The address that will be casted to be of type <typeparamref
        ///     name="TParent"/>.
        /// </param>
        public static AssetAddress<TParent> Upcast<TDerived, TParent>(AssetAddress<TDerived> address)
            where TParent : Object
            where TDerived : TParent
        {
            return new (address.ToString());
        }
    }

    namespace Internal
    {
        /// <summary>
        ///     Stores an addressable asset's address and an editor-only
        ///     reference to the asset identified by the address.
        ///     <br /><br />
        ///
        ///     This is a separate type from <see cref="AssetAddress{TAsset}"/>
        ///     to address a quirk with Unity's serialization system: despite
        ///     Unity having support for editor-only serialized fields, that
        ///     support does not extend to generic types. Therefore, this type
        ///     is non-generic (and then nested within the generic, <see
        ///     cref="AssetAddress{TAsset})"/>) to allow editor-only serialized
        ///     fields to work in builds as intended.
        /// </summary>
        [System.Serializable]
        internal struct AssetAddressData
        {
            /// <summary>
            ///     The raw address. Identifies the asset (including sub-asset if
            ///     applicable).
            /// </summary>
            public string address;

#if UNITY_EDITOR
            public Object editorOnlyCachedAsset;
#endif  // if UNITY_EDITOR
        }
    }

}

