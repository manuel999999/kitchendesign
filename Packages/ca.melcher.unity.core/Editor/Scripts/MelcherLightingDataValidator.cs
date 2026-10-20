namespace Melcher
{
    using System.IO;
    using UnityEngine.SceneManagement;
    using UnityEditor;
    using UnityEditor.SceneManagement;

    /// <summary>
    ///     The purpose of this script is to provide early warning about scenes
    ///     that reference the same lighting settings as another scene, as is
    ///     the case when a scene is duplicated in the project.
    ///     <br /><br />
    ///
    ///     It's easy to forget or not know that the settings are shared and
    ///     then when tuning the lighting settings for one scene, also
    ///     unintentionally affect another.
    ///     <br /><br />
    ///
    ///     This script only validates scenes when they become the active scene
    ///     in the editor.
    /// </summary>
    [InitializeOnLoad]
    public static class MelcherLightingDataValidator
    {
        static MelcherLightingDataValidator()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= ValidateLightingData;
            EditorSceneManager.activeSceneChangedInEditMode += ValidateLightingData;
        }

        /// <summary>
        ///     Warn if the current lighting data doesn't match the active
        ///     scene's name or folder name and more accurately matches matches
        ///     another scene's name. Helps avoid issues when scenes are
        ///     duplicated.
        /// </summary>
        private static void ValidateLightingData(Scene currentScene, Scene nextScene)
        {
            if (!nextScene.IsValid() || nextScene.buildIndex < 0)
            {
                return;
            }

            LightingDataAsset lightingData = Lightmapping.lightingDataAsset;
            if (!lightingData)
            {
                return;
            }

            string lightingDataPath = AssetDatabase.GetAssetPath(lightingData);
            const string DefaultLightingDataPath = "Resources/unity_builtin_extra";

            if (string.IsNullOrWhiteSpace(lightingDataPath) ||
                lightingDataPath == DefaultLightingDataPath)
            {
                // No lighting data or using the built-in asset (readonly).
                return;
            }

            string scenePath = nextScene.path;
            string sceneName = nextScene.name;

            string lightingDataName = Path.GetFileNameWithoutExtension(lightingDataPath);
            string lightingDataFolderName = Path.GetDirectoryName(lightingDataPath);

            bool isDataNamedAfterNextScene =
                StringEqualsInsensitive(lightingDataName, sceneName);

            bool isDataFolderNamedAfterNextScene =
                StringEqualsInsensitive(lightingDataFolderName, sceneName);

            if (isDataNamedAfterNextScene || isDataFolderNamedAfterNextScene)
            {
                // The active lighting data is labeled pretty clearly after the
                // intended scene.
                return;
            }

            if (DoesNameMatchAnyScene(lightingDataName, out string otherSceneName) ||
                DoesNameMatchAnyScene(lightingDataFolderName, out otherSceneName))
            {
                // The active lighting data more closely matches another
                // scene's name.

                MelcherLog.LogWarning(
                    $"The name of the lighting data used by scene {MelcherFormat.ConsoleString(sceneName)} " +
                    $"more closely matches the name of a different scene {MelcherFormat.ConsoleString(otherSceneName)}. " +
                    $"This might be because the scene was duplicated. It is recommended to use a unique " +
                    $"lighting data instance for each scene to avoid developers " +
                    $"affecting more than one scene's lighting unknowingly.",
                    context: lightingData
                );
            }

            static bool StringEqualsInsensitive(string a, string b)
            {
                return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
            }

            static bool DoesNameMatchAnyScene(string name, out string matchedSceneName)
            {
                matchedSceneName = null;

                string[] sceneGuids = AssetDatabase.FindAssets($"t:Scene {name}");

                foreach (string sceneGuid in sceneGuids)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                    if (StringEqualsInsensitive(sceneName, name))
                    {
                        matchedSceneName = sceneName;

                        return true;
                    }
                }

                return false;
            }

        }
    }   // class MelcherLightingDataValidator

}   // namespace Melcher
