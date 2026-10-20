namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     <see cref="MelcherPathFieldBase"/> is a type that is used to
    ///     reference an asset by its path. It has a custom drawer that
    ///     restricts which asset types can be assigned in the inspector.
    ///     <br /><br />
    ///
    ///     This is similar to a <see cref="MelcherPathAppValue"/> but without
    ///     needing the intermediate scriptable object. This is sometimes
    ///     preferable when you already have an existing script or <see
    ///     cref="ScriptableObject"/> that can store the reference and where it
    ///     otherwise feels redundant to add a whole new asset.
    ///     <br /><br />
    ///
    ///     Internally, the asset is remembered by its GUID and therefore
    ///     doesn't break if the file is renamed.
    /// </summary>
    [System.Serializable]
    public abstract class MelcherPathFieldBase
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public string guid => m_Guid;

        /// <summary>
        /// Asset path; relative to <see cref="MelcherPredefinedFilePath.data"/>.
        /// </summary>
        public string dataRelativePath => m_DataRelativePath;

        public abstract System.Type assetType { get; }

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector]
        protected string m_Guid;

        [SerializeField, MelcherReadOnlyInInspector]
        protected string m_DataRelativePath;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public void SetPathAndGuid(string projectRelativePath, string guid)
        {
            if (!string.IsNullOrEmpty(projectRelativePath))
            {
                m_DataRelativePath =
                    MelcherTransformFilePath.From(projectRelativePath, relativeTo: MelcherPredefinedFilePath.project)
                        .To(MelcherPredefinedFilePath.data);
            }
            else
            {
                m_DataRelativePath = string.Empty;
            }

            m_Guid = guid;
        }

        public const string SetPathAndGuidName = nameof(SetPathAndGuid);

#endif // if UNITY_EDITOR

    }   // class MelcherPathFieldBase

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
