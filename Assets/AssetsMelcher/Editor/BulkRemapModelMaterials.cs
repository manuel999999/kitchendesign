namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     This tool is intended to help change the materials used by FBXs
    ///     in bulk.
    /// </summary>
    // TODO: Allow the filters to use regular expressions to make it useful
    //       it more situations.
    public class BulkRemapModelMaterials : EditorWindow
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Bulk remap model materials...")]
        public static void ShowWindow()
        {
            GetWindow<BulkRemapModelMaterials>();
        }

        public void OnGUI()
        {
            using SerializedObject serializedObject = new SerializedObject(this);

            using EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope();

            using var applyChange =
            MelcherDeferred.Defer(
                toCapture: ((serializedObject, changeCheckScope)),
                onDispose: static captured =>
                {
                    if (captured.changeCheckScope.changed)
                    {
                        captured.serializedObject.ApplyModifiedProperties();
                    }
                }
            );

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using SerializedProperty namesToMaterialsProperty =
                    serializedObject.FindProperty(nameof(m_RemapNamesToMaterials));

                EditorGUILayout.PropertyField(namesToMaterialsProperty);

                GUILayout.Space(8);

                if (GUILayout.Button("List material names used in selected assets"))
                {
                    Dictionary<string, int> nameToOccurenceCount =
                        new Dictionary<string, int>();

                    ForEachSelectedModelImporter(ListModelMaterialNames, nameToOccurenceCount);

                    string output =
                        string.Join("\n", nameToOccurenceCount.Select(kv => $"{kv.Key} = {kv.Value}"));

                    MelcherLog.Log(output);
                }

                if (GUILayout.Button("Remap materials in selected assets"))
                {
                    ForEachSelectedModelImporter(RemapModelMaterials, null);
                }

                if (GUILayout.Button("Find models with unmapped materials"))
                {
                    MelcherLog.Log("Start list of selected models with unmapped materials:");
                    ForEachSelectedModelImporter(CheckForUnmappedMaterials, null);
                    MelcherLog.Log("End list.");
                }
            }

            GUILayout.Space(32);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using SerializedProperty filterFindProperty =
                    serializedObject.FindProperty(nameof(m_FilterFindRule));

                EditorGUILayout.PropertyField(filterFindProperty);

                if (GUILayout.Button("Find models matching filter rules"))
                {
                    List<string> matchingFileNames = new List<string>();

                    MelcherLog.Log($"Start list of selected models that don't have {m_FilterFindRule.modelMustContainThisMaterialAsOverride}:");
                    ForEachSelectedModelImporter(FindModelsMatchingRules, matchingFileNames);
                    MelcherLog.Log("End list.");

                    MelcherLog.Log(string.Join("\n", matchingFileNames));
                }
            }
        }

        private bool ListModelMaterialNames(
            ModelImporter modelImporter,
            SerializedProperty modelImporterMaterials,
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> modelExternalAssetMap,
            object userData)
        {
            if (userData is not Dictionary<string, int> nameToOccurenceCount)
            {
                MelcherLog.LogError("Failed to cast userData to dictionary.");
                return false;
            }

            for (int i = 0; i < modelImporterMaterials.arraySize; ++i)
            {
                using SerializedProperty modelImporterMaterialNameProperty =
                    modelImporterMaterials.GetArrayElementAtIndex(i).FindPropertyRelative("name");

                string materialName = modelImporterMaterialNameProperty.stringValue;

                int occurenceCount = nameToOccurenceCount.GetValueOrDefault(materialName, 0);

                nameToOccurenceCount[materialName] = ++occurenceCount;
            }

            return false;
        }

        private bool RemapModelMaterials(
            ModelImporter modelImporter,
            SerializedProperty modelImporterMaterials,
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> modelExternalAssetMap,
            object userData)
        {
            bool needsReimport = false;

            for (int i = 0; i < modelImporterMaterials.arraySize; ++i)
            {
                using SerializedProperty modelImporterMaterialNameProperty =
                    modelImporterMaterials.GetArrayElementAtIndex(i).FindPropertyRelative("name");

                string materialInModelName = modelImporterMaterialNameProperty.stringValue;

                RemapNameToMaterial matching =
                    m_RemapNamesToMaterials.FirstOrDefault(nameToMaterial =>
                        materialInModelName.Contains(nameToMaterial.nameInModel)
                    );

                if (matching == null)
                {
                    continue;
                }

                AssetImporter.SourceAssetIdentifier key =
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), materialInModelName);

                if (modelExternalAssetMap.TryGetValue(key, out Object existingRemap) &&
                    existingRemap == matching.material)
                {
                    // Avoid flagging this model for reimport (which can
                    // potentially take a long time) if the requested material
                    // is already assigned.
                    continue;
                }

                modelImporter.AddRemap(key, matching.material);

                needsReimport = true;
            }

            return needsReimport;

        }

        private bool CheckForUnmappedMaterials(
            ModelImporter modelImporter,
            SerializedProperty modelImporterMaterials,
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> modelExternalAssetMap,
            object userData)
        {
            for (int i = 0; i < modelImporterMaterials.arraySize; ++i)
            {
                using SerializedProperty modelImporterMaterialNameProperty =
                    modelImporterMaterials.GetArrayElementAtIndex(i).FindPropertyRelative("name");

                string materialInModelName = modelImporterMaterialNameProperty.stringValue;

                AssetImporter.SourceAssetIdentifier key =
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), materialInModelName);

                if (modelExternalAssetMap.TryGetValue(key, out Object newMaterial) &&
                    newMaterial != null &&
                    newMaterial is Material)
                {
                    continue;
                }

                MelcherLog.Log($"  {modelImporter.assetPath}", modelImporter);
                break;
            }

            return false;
        }

        /// <summary>
        ///     Print the name of any model whose file name contains a
        ///     substring but whose material remaps does not include a specific
        ///     material.
        /// </summary>
        private bool FindModelsMatchingRules(
            ModelImporter modelImporter,
            SerializedProperty modelImporterMaterials,
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> modelExternalAssetMap,
            object userData)
        {
            string assetName =
                System.IO.Path.GetFileName(modelImporter.assetPath);

            if (!assetName.Contains(m_FilterFindRule.fileNameMustContainText))
            {
                return false;
            }

            if (userData is not List<string> matchingFileNames)
            {
                MelcherLog.LogError("Failed to cast userData to list.");
                return false;
            }

            bool containsMaterial = false;

            for (int i = 0; i < modelImporterMaterials.arraySize; ++i)
            {
                using SerializedProperty modelImporterMaterialNameProperty =
                    modelImporterMaterials.GetArrayElementAtIndex(i).FindPropertyRelative("name");

                string materialInModelName = modelImporterMaterialNameProperty.stringValue;

                AssetImporter.SourceAssetIdentifier key =
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), materialInModelName);

                if (modelExternalAssetMap.TryGetValue(key, out Object newMaterial) &&
                    newMaterial == m_FilterFindRule.modelMustContainThisMaterialAsOverride)
                {
                    containsMaterial = true;
                    break;
                }
            }

            if (!containsMaterial)
            {
                MelcherLog.Log($"  {modelImporter.assetPath}", modelImporter);
                matchingFileNames.Add(assetName);
            }

            return false;
        }

        private void ForEachSelectedModelImporter(ProcessModelImporter action, object userData)
        {
            if (action == null)
            {
                throw new System.ArgumentNullException(nameof(action));
            }

            string[] assetPaths =
                Selection.assetGUIDs
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .OrderBy(path => path)
                    .ToArray();

            foreach (string assetPath in assetPaths)
            {
                if (AssetImporter.GetAtPath(assetPath) is not ModelImporter modelImporter)
                {
                    continue;
                }

                using SerializedObject serializedModelImporter =
                    new SerializedObject(modelImporter);

                using SerializedProperty modelImporterMaterials =
                    serializedModelImporter.FindProperty("m_Materials");

                if (modelImporterMaterials == null)
                {
                    MelcherLog.LogError("Failed to find materials property.");
                    return;
                }

                if (!modelImporterMaterials.isArray)
                {
                    MelcherLog.LogError("Expected materials property to be an array.");
                    return;
                }

                Dictionary<AssetImporter.SourceAssetIdentifier, Object>
                    modelExternalAssetMap = modelImporter.GetExternalObjectMap();


                bool needsReimport =
                    action.Invoke(
                        modelImporter,
                        modelImporterMaterials,
                        modelExternalAssetMap,
                        userData
                    );

                if (needsReimport)
                {
                    modelImporter.SaveAndReimport();
                }
            }
        }

        // ====================================================================
        // Serialized-fields
        // ====================================================================

        [SerializeField]
        protected RemapNameToMaterial[] m_RemapNamesToMaterials = default;

        [SerializeField]
        protected FilterFindRule m_FilterFindRule = default;

        // ====================================================================
        // Nested types
        // ====================================================================

        public delegate bool ProcessModelImporter(
            ModelImporter importer,
            SerializedProperty modelImporterMaterials,
            Dictionary<AssetImporter.SourceAssetIdentifier, Object> externalmodelExternalAssetMap,
            object userData);

        [System.Serializable]
        public class RemapNameToMaterial
        {
            public string nameInModel => m_NameInModel;
            public Material material => m_MaterialToUse;

            [SerializeField]
            private string m_NameInModel;

            [SerializeField]
            private Material m_MaterialToUse;

        }   // class NameToMaterial

        [System.Serializable]
        public struct FilterFindRule
        {
            public string fileNameMustContainText;

            public Material modelMustContainThisMaterialAsOverride;
        }

    }   // class BulkRemapModelMaterials

}   // namespace Melcher
