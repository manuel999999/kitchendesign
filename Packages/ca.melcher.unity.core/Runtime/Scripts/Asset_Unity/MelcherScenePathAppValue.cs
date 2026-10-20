namespace Melcher
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// A reference to a scene.
    /// </summary>
    [CreateAssetMenu(fileName = "Scene", menuName = "Melcher Studios/App Value/Unity - Scene Path")]
    public class MelcherScenePathAppValue : MelcherPathAppValue
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public bool isRuntimeScene => m_IsRuntimeScene;

        /// <summary>
        /// Scene name.
        /// </summary>
        public string sceneName => m_IsRuntimeScene ? m_RuntimeSceneName : System.IO.Path.GetFileNameWithoutExtension(m_Value);

        /// <summary>
        /// The path to the scene, suitable for loading from the SceneManager:
        /// includes the Assets/ prefix and strips the .unity extension.
        /// </summary>
        // TODO: Does this need Assets prepended? If yes, then we may only need
        //       to do that outside of the editor. In the editor, we don't need
        //       to add Assets/ to the front.
        // TODO: Write tests to verify if loading scenes, resources, and
        //       streaming assets (?) is done using data-relative paths
        public string scenePath => m_IsRuntimeScene ? null : System.IO.Path.ChangeExtension(m_Value, extension: null);

        public int? buildIndex => m_IsRuntimeScene ? null : (int?)m_BuildIndex;

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [Tooltip("When true, this represents a runtime instantiated scene. " +
            "In that case, scenePath will return null so sceneName must be " +
            "used.")]
        [SerializeField]
        private bool m_IsRuntimeScene = false;

        [Tooltip("The scene name to use when isRuntimeScene is true.")]
        [SerializeField]
        private string m_RuntimeSceneName = null;

        [SerializeField, MelcherReadOnlyInInspector]
        private int m_BuildIndex = default;

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        // Names of fields accessed through reflection (editor-time only)
        // from the custom property drawer.
        public const string IsRuntimeSceneMemberName    = nameof(m_IsRuntimeScene);
        public const string RuntimeSceneNameMemberName  = nameof(m_RuntimeSceneName);

        /// <summary>
        /// Given a scene's GUID, return its build index (-1 if not added or
        /// disabled) and its EditorBuildSettingsScene.
        /// </summary>
        public static int GetBuildIndexFromGUID(string guid, out EditorBuildSettingsScene buildSettingsScene)
        {
            int buildIndex = -1;
            buildSettingsScene = null;

            if (!string.IsNullOrWhiteSpace(guid))
            {
                var buildScenes = EditorBuildSettings.scenes;
                int buildSceneCount = buildScenes.Length;
                var castGuid = new GUID(guid);

                int runningBuildIndex = 0;
                for (int i = 0; i < buildSceneCount; ++i)
                {
                    bool isEnabled = buildScenes[i].enabled;

                    if (buildScenes[i].guid == castGuid)
                    {
                        if (isEnabled)
                        {
                            buildIndex = runningBuildIndex;
                        }
                        buildSettingsScene = buildScenes[i];
                        break;
                    }

                    if (isEnabled)
                    {
                        ++runningBuildIndex;
                    }
                }
            }

            return buildIndex;
        }

        private static string GetAssetBundleNameContainingScene(string scenePath)
        {
            string[] assetBundleNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (string bundleName in assetBundleNames)
            {
                string[] assetPathsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
                foreach (string assetPath in assetPathsInBundle)
                {
                    if (assetPath == scenePath)
                    {
                        return bundleName;
                    }
                }
            }

            return null;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            // Validate the scene GUID

            // Lookup the scene dataRelativePath and asset. This can fail if the scene was
            // deleted or its GUID was regenerated.
            string scenePath = AssetDatabase.GUIDToAssetPath(m_Guid);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            if (sceneAsset)
            {
                // Overwrite old scene dataRelativePath (the file may have moved).
                SetPathAndGuid(scenePath, m_Guid);

                m_BuildIndex = GetBuildIndexFromGUID(m_Guid, out EditorBuildSettingsScene _);
                //m_AssetBundleName = null;

                // if (m_BuildIndex < 0)
                // {
                //     m_AssetBundleName = GetAssetBundleNameContainingScene(m_AssetPath);

                //     if (m_AssetBundleName == null)
                //     {
                //         Debug.LogWarning("The referenced scene isn't included or is disabled in the build settings: " + m_AssetPath, this);
                //     }
                // }
            }
            else if (!string.IsNullOrWhiteSpace(m_Guid))
            {
                // The scene wasn't found. Don't overwrite the dataRelativePath because it
                // tell us which file the GUID originally referenced.
                MelcherLog.LogError(
                    $"The referenced scene doesn't exist in the project: {m_Value} (GUID: {m_Guid})",
                    context: this
                );
            }
        }
#endif  // if UNITY_EDITOR

    }   // class MelcherScenePathAsset

}   // namespace Melcher
