#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KitchenDesign
{
    public static class StartAppForEditor
    {
        public const string AppInitializerPrefabPath = "Assets/_projectAssets/Prefabs/AppInitializer.prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartAppWhenPlayingAnotherScene()
        {
            if (SceneManager.GetActiveScene().buildIndex <= AppInitializer.BootSceneBuildIndex)
            {
                return;
            }

            AppInitializer prefab = AssetDatabase.LoadAssetAtPath<AppInitializer>(AppInitializerPrefabPath);
            Object.Instantiate(prefab);
        }
    }
}
#endif
