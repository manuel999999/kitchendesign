namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// An <see cref="MelcherPathAsset"/> is an <see cref="MelcherAsset"/> that
    /// stores a path to an asset relative to the project's data directory.
    /// Having the path relative to the data directory is convenient because it
    /// makes the relative paths consistent between the editor and builds.
    ///
    /// At editor time, this class uses a GUID in place of the asset's path.
    /// This allows the connection to survive renames and path changes.
    ///
    /// <see cref="AssetPathAsset"/> is useful for assets that can be loaded
    /// using their path and which you don't want to automatically load with
    /// the scene/ScriptableObject that references it. (Scenes, resources,
    /// streaming assets, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "NewAssetPathAsset", menuName = "Melcher Studios/App Value/Unity - Asset Path")]
    public class MelcherPathAppValue : MelcherAppValue<string>
    {
        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Relative path to the asset; relative to <see
        /// cref="MelcherPredefinedFilePath.data"/> to ensure it is compatible in
        /// the editor and builds.
        /// </summary>
        public string dataRelativePath => value;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Tooltip("The GUID used at editor time to reliably reference the " +
            "asset. This helps the reference survive renames and path changes.")]
        [SerializeField, MelcherReadOnlyInInspector]
        protected string m_Guid = null;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        /// <inheritdoc cref="AssetPathAsset.m_Guid"/>
        public string guid => m_Guid;

        /// <summary>
        /// Editor only function. Used to assign the asset's path and GUID
        /// after it is selected in the inspector.
        /// </summary>
        public void SetPathAndGuid(string projectRelPath, string guid)
        {
            m_Guid = guid;

            m_Value =
                MelcherTransformFilePath.From(projectRelPath, relativeTo: MelcherPredefinedFilePath.project)
                    .To(MelcherPredefinedFilePath.data);

            // Force the use of forward slashes because they are supported on
            // Windows and other OS's.
            m_Value = m_Value.Replace(@"\", "/");
        }

        public override void OnEditorPlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
        {
            // Refresh the path when entering play mode
            if (mode == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                string assetPathRelativeToProject = UnityEditor.AssetDatabase.GUIDToAssetPath(m_Guid);

                // Don't overwrite the path if the asset isn't found. The path
                // is preserved because it has more human readability than a
                // GUID
                if (!string.IsNullOrWhiteSpace(assetPathRelativeToProject))
                {
                    SetPathAndGuid(assetPathRelativeToProject, m_Guid);
                }
            }
        }
#endif  // if UNITY_EDITOR

    }   // class MelcherPathAppValue

}   // namespace Melcher
