namespace Melcher
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor;
    using UnityEditor.SceneManagement;

    /// <summary>
    ///     Changes the name of the main Unity editor window to include the
    ///     product name.
    ///     <br /><br />
    ///
    ///     This is helpful when the folder the project is saved in is named
    ///     "Unity", as is the case for our project template.
    ///     <br /><br />
    ///
    ///     "My Product - Unity - MenuScene - Unity 2022.1.1f1"
    /// </summary>
    public static partial class MelcherSetUnityEditorTitle
    {
        // ====================================================================
        // Static constructor
        // ====================================================================

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            // TODO:    Update when the product name changes. Is there a way to
            //          listen for that?

            ChangeEditorWindowTitle();
        }

        /// <summary>
        ///     Fixes the window's title after a scene is loaded in single load
        ///     mode. Unity resets the application's title to its default value
        ///     sometime between this function being called and the next scene
        ///     finishing its load.
        /// </summary>
        private static void OnSceneClosed(Scene scene)
        {
            int loadedSceneCount =
#if UNITY_2022_2_OR_NEWER
                SceneManager.loadedSceneCount;
#else
                EditorSceneManager.loadedSceneCount;
#endif  // if UNITY_2022_2_OR_NEWER else

            if (loadedSceneCount == 0)
            {
                // This isn't perfect timing. The title resets to its default
                // value sometime after this function is called and before the
                // next scene loads. The editor blocks until the next scene
                // loads. delayCall works, but we're left with a few moments of
                // the default title being shown.

                EditorApplication.delayCall += ChangeEditorWindowTitle;
            }
        }

        private static void ChangeEditorWindowTitle()
        {
            string projectFolderName =
                System.IO.Path.GetFileName(MelcherPredefinedFilePath.project);

            // The Vanilla title uses the format:
            // "ProjectFolderName - SceneName - BuildTargetName - UnityVersion"

            string buildTargetName = EditorUserBuildSettings.selectedBuildTargetGroup.ToString();

            if (EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone)
            {
                buildTargetName = "Windows, Mac, Linux";
            }

            string sceneName = SceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(sceneName))
            {
                sceneName = "Untitled";
            }

            string graphicsApiName = SystemInfo.graphicsDeviceType.ToString();

            switch (SystemInfo.graphicsDeviceType)
            {
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                    graphicsApiName = "DX11";
                    break;

                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    graphicsApiName = "DX12";
                    break;
            }

            string customTitle =
                string.Format(
                    format: "{0} - {1} - {2} - Unity {3} <{4}> - {5}",
                    projectFolderName,
                    sceneName,
                    buildTargetName,
                    Application.unityVersion,
                    graphicsApiName,
                    Application.productName
                );

            bool isImplemented = false;

            ChangeEditorWindowTitle(customTitle, ref isImplemented);

            if (!isImplemented)
            {
                MelcherLog.LogWarning("Not implemented for this platform: " + Application.platform);
            }
        }

        /// <summary>
        ///     Recreate the default window title except prepend it with the
        ///     project's product name.
        /// </summary>
        static partial void ChangeEditorWindowTitle(string newTitle, ref bool isImplemented);

    }   // class MelcherSetUnityEditorTitle

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-03-02
/// ===========================================================================
