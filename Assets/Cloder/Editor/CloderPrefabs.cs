namespace Melcher
{
    using UnityEditor;
    using UnityEngine;

    public static class CloderPrefabs
    {
        private const string CUBOID_RULER_PREFAB = "Cuboid Ruler";

        [MenuItem("GameObject/Gemini/Cloder/Cuboid Ruler")]
        private static void CreateCuboidRuler()
        {
            CreatePrefab(CUBOID_RULER_PREFAB);
        }

        private static void CreatePrefab(string prefabName, bool select = true)
        {
            string prefabPath = $"Assets/Cloder/Prefabs/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (!prefab)
            {
                Debug.LogWarning($"Prefab not found at path {prefabPath}");
                return;
            }

            var instance =
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, Selection.activeTransform);

            // Start at the parent's origin (or the world origin) rather than
            // wherever the prefab happened to be saved.
            instance.transform.localPosition = Vector3.zero;

            Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");

            if (select)
            {
                Selection.activeObject = instance;
            }
        }
    }
}
