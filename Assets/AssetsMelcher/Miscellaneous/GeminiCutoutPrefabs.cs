namespace KitchenDesign
{
    using Melcher;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    public static class GeminiCutoutPrefabs
    {
        private const string POSITION_HELPER_PREFAB = "ComponentPositionHelper";
        private const string STORAGE_CUTOUT_HELPER_PREFAB = "StorageCutoutHelper";
        private const string COMPONENT_CUTOUT_HELPER_PREFAB = "ComponentCutoutHelper";

        [MenuItem("GameObject/Gemini/Get/Storage Helpers")]
        private static void GetStorageHelpers()
        {
            CreatePositionHelper(false);
            
            CreateStorageCutoutHelper();
        }

        [MenuItem("GameObject/Gemini/Get/Component Helpers")]
        private static void GetComponentHelpers()
        {
            CreatePositionHelper(false);
            
            CreateComponentCutoutHelper();
        }

        [MenuItem("GameObject/Gemini/Position Helper")]
        private static void CreatePositionHelper(bool select = true)
        {
            CreatePrefab(POSITION_HELPER_PREFAB, select);
        }

        [MenuItem("GameObject/Gemini/Cutout/Storage Cutout Helper")]
        private static void CreateStorageCutoutHelper()
        {
            CreatePrefab(STORAGE_CUTOUT_HELPER_PREFAB);
        }

        [MenuItem("GameObject/Gemini/Cutout/Component Cutout Helper")]
        private static void CreateComponentCutoutHelper()
        {
            CreatePrefab(COMPONENT_CUTOUT_HELPER_PREFAB);
        }

        private static void CreatePrefab(string prefabName, bool select = true)
        {
            string prefabPath = $"Assets/AssetsMelcher/Miscellaneous/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (!prefab)
            {
                Debug.LogWarning($"Prefab not found at path {prefabPath}");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, Selection.activeTransform);

            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");

            if(select)
            {
                Selection.activeObject = instance;
            }
        }
    } 
#endif
}

