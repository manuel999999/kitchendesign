using Melcher;
using UnityEngine;

namespace KitchenDesign.Utility
{
    public static class RuntimePrefabs
    {
        public static PrefabType InstantiatePrefabAsRuntimePrefab<PrefabType>(PrefabType originalPrefab)
            where PrefabType : Component
        {
            PrefabType runtimePrefab = GameObject.Instantiate(originalPrefab);

            runtimePrefab.gameObject.name = originalPrefab.name;

            runtimePrefab.gameObject.SetActive(false);

            GameObject.DontDestroyOnLoad(runtimePrefab.gameObject);

            return runtimePrefab;
        }

        public static bool IsPrefab(GameObject gameObject)
        {
            if (!gameObject)
            {
                throw new System.ArgumentNullException(nameof(gameObject));
            }

            bool isPrefabAsset =
                !gameObject.scene.IsValid();

#if UNITY_EDITOR
            isPrefabAsset |=
                MelcherEditorUtility.IsPartOfCurrentPrefabStage(gameObject);
#endif

            bool isRuntimePrefab =
                gameObject.scene.path == "DontDestroyOnLoad";

            return isPrefabAsset || isRuntimePrefab;
        }
    }
}
