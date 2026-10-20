namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// An abstract base class for the <see
    /// cref="MelcherVariable{ValueType, AssetType}"/> type. This base
    /// class allows generic variables to be referenced using a common type
    /// which, in this case, is mainly helpful for the inspector drawer.
    /// </summary>
    [System.Serializable]
    public abstract class MelcherVariableBase
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public abstract System.Type appValueType { get; }

        public abstract bool hasAsset { get; }

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public const string DrawAppValue = nameof(m_DrawAppValue);

        [Tooltip("This value is used in the inspector to switch which " +
            "property field will render in the inspector (i.e., the " +
            "inline value or the app value field).")]
        [SerializeField, MelcherReadOnlyInInspector]
        [UnityEngine.Serialization.FormerlySerializedAs("m_DrawSharedField")]
        protected bool m_DrawAppValue;
#endif  // if UNITY_EDITOR

    }   // class MelcherVariableBase

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-18
/// ===========================================================================
