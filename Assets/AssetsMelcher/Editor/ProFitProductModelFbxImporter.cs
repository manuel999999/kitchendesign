namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     Remap the default material on product models to something of our
    ///     choosing. In this case, we want to use a metal material.
    ///
    ///     Our use-case is to allow product models (FBXs) to be used as
    ///     properly material'ed prefabs. If an FBX has a mesh with no
    ///     material, Unity assigns it a default material but does not expose
    ///     a field to remap it in the inspector. This asset post-processor
    ///     allows us to override the default material.
    ///
    ///     Based on: https://forum.unity.com/threads/batch-change-all-fbx-default-materials-help.626341/#post-6874607
    /// </summary>
    public class ProFitProductModelFbxImporter : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject modelAsset)
        {
            ProFitEditorOptions editorOptions = ProFitEditorOptions.Get();

            if (!editorOptions ||
                !editorOptions.defaultProductMaterial ||
                !editorOptions.productCabinetMaterialIdentifier ||
                !editorOptions.defaultKitchenMaterial)
            {
                DeferReimport(assetImporter.assetPath);
                return;
            }

            string projectRelativeProductModelsFolder =
                "Assets/" + editorOptions.dataRelativeProductModelsFolder;

            string projectRelativeCabinetSectionModelsFolder =
                "Assets/" + editorOptions.dataRelativeCabinetSectionModelsFolder;

            string projectRelativeCabinetSectionLegModelsFolder =
                "Assets/" + editorOptions.dataRelativeCabinetSectionLegModelsFolder;

            string assetPath = assetImporter.assetPath;

            if (assetPath.StartsWith(projectRelativeProductModelsFolder) ||
                assetPath.StartsWith(projectRelativeCabinetSectionLegModelsFolder))
            {
                RemapDefaultModelMaterials(modelAsset, _ => editorOptions.defaultProductMaterial);
            }
            else if (assetPath.StartsWith(projectRelativeCabinetSectionModelsFolder))
            {
                bool isDoorStyle = assetPath.Contains('@');

                RemapDefaultModelMaterials(
                    modelAsset,
                    renderer =>
                    {
                        if (isDoorStyle)
                        {
                            if (IsDoorHandle(renderer))
                            {
                                // Make the door handles metal.
                                return editorOptions.defaultProductMaterial;
                            }

                            return editorOptions.productCabinetMaterialIdentifier;

                            static bool IsDoorHandle(Renderer renderer)
                            {
                                return renderer &&
                                    (renderer.name.StartsWith("Handle") ||
                                    renderer.name.StartsWith("Cylinder"));
                            }
                        }

                        return editorOptions.defaultKitchenMaterial;
                    }
                );

            }
            else
            {
                MelcherLog.Log("Skipping material remap: Not inside a recognized folder.\n" + assetImporter.assetPath, modelAsset);
            }
        }

        protected void RemapDefaultModelMaterials(GameObject productModelAsset, System.Func<Renderer, Material> rendererToMaterial)
        {
            bool didModifyAsset = false;

            Renderer[] renderers =
                productModelAsset.GetComponentsInChildren<Renderer>(
                    includeInactive: true
                );

            foreach (Renderer renderer in renderers)
            {
                didModifyAsset |= RemapRendererMaterials(renderer, rendererToMaterial);
            }

            if (didModifyAsset)
            {
                string assetPath = assetImporter.assetPath;
                EditorApplication.delayCall += () => RemapModelSourceMaterials(assetPath, rendererToMaterial);
            }
        }

        public static bool RemapRendererMaterials(Renderer renderer, System.Func<Renderer, Material> rendererToMaterial)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool modified = false;

            for (int i = 0; i < sharedMaterials.Length; ++i)
            {
                if (sharedMaterials[i] != null &&
                    s_DefaultMaterialNames.Contains(sharedMaterials[i].name))
                {
                    sharedMaterials[i] = rendererToMaterial.Invoke(renderer);
                    modified = true;
                }
            }

            if (modified)
            {
                renderer.sharedMaterials = sharedMaterials;
            }

            return modified;
        }

        private static void RemapModelSourceMaterials(string assetPath, System.Func<Renderer, Material> rendererToMaterial)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (modelImporter == null)
            {
                return;
            }

            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
            modelImporter.materialName = ModelImporterMaterialName.BasedOnModelNameAndMaterialName;
            modelImporter.materialSearch = ModelImporterMaterialSearch.Local;

            // Use reflection to get the asset's list of source materials
            var sourceMaterials =
                typeof(ModelImporter)
                    .GetProperty("sourceMaterials", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(modelImporter) as AssetImporter.SourceAssetIdentifier[];

            if (sourceMaterials != null)
            {
                foreach (AssetImporter.SourceAssetIdentifier identifier in sourceMaterials)
                {
                    if (s_DefaultMaterialNames.Contains(identifier.name))
                    {
                        Material expectedMaterial = rendererToMaterial.Invoke(null);

                        modelImporter.AddRemap(identifier, expectedMaterial);
                    }
                }
            }

            modelImporter.SaveAndReimport();
        }

        private static void DeferReimport(string assetPath)
        {
            System.IO.File.AppendAllText(PendingReimportsPath, assetPath + "\n");

            if (!s_ReimportScheduled)
            {
                s_ReimportScheduled = true;
                EditorApplication.delayCall += ReimportPending;
            }
        }

        [InitializeOnLoadMethod]
        private static void ScheduleReimportOfPendingFromEarlierSession()
        {
            if (System.IO.File.Exists(PendingReimportsPath) && !s_ReimportScheduled)
            {
                s_ReimportScheduled = true;
                EditorApplication.delayCall += ReimportPending;
            }
        }

        private static void ReimportPending()
        {
            s_ReimportScheduled = false;

            if (!System.IO.File.Exists(PendingReimportsPath))
            {
                return;
            }

            if (!ProFitEditorOptions.Get())
            {
                MelcherLog.LogWarning($"Models in {PendingReimportsPath} were imported without their material remap because {nameof(ProFitEditorOptions)} could not be loaded. They are reimported once it can be.");
                return;
            }

            HashSet<string> paths = new HashSet<string>(System.IO.File.ReadAllLines(PendingReimportsPath));
            paths.Remove("");
            System.IO.File.Delete(PendingReimportsPath);

            MelcherLog.Log($"Reimporting {paths.Count} models that were imported before {nameof(ProFitEditorOptions)} could be loaded.");

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private const string PendingReimportsPath = "Library/ProFitPendingModelReimports.txt";

        private static bool s_ReimportScheduled;

        protected static readonly List<string> s_DefaultMaterialNames =
            new()
            {
                "Default-Material",
                "Lit",
            };

    }   // class ProFitProductModelFbxImporter

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-23
/// ===========================================================================
