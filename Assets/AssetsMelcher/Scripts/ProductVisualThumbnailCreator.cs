#if UNITY_EDITOR

namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using System.Linq;
    using System.Collections.Generic;
    using MEC;
    using KitchenDesign.Geometry;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Products;

    /// <summary>
    ///     These functions were originally defined in <see
    ///     cref="ProductVisualDatabaseManager"/> but this is defined in the
    ///     editor assembly and therefore inaccessible to runtime code.
    ///     <br /><br />
    ///
    ///     This class must be accessible to the runtime assembly so <see
    ///     cref="ProductVisualContent"/>'s can be right-clicked to render their
    ///     thumbnails from a context window.
    /// </summary>
    public static class ProductVisualThumbnailCreator
    {

        public const int ThumbnailResolution = 256;

        private static string m_UsedBrandCatalogName = "";

        /// <summary>
        ///     Prompt the user to load the screenshot scene so that the
        ///     rendered thumbnails have the intended reflections.
        /// </summary>
        public static bool PromptLoadScreenshotScene()
        {
            string activeScenePath = EditorSceneManager.GetActiveScene().path;
            string screenshotScenePath =
                AssetDatabase.GetAssetPath(
                    ProFitGlobals.appProfile.screenshotScene
                );

            if (activeScenePath == screenshotScenePath)
            {
                // The screenshot scene is already loaded and active
                return true;
            }

            string screenshotSceneName =
                System.IO.Path.GetFileNameWithoutExtension(
                    screenshotScenePath
                );

            int dialogResult =
                EditorUtility.DisplayDialogComplex(
                    title: "Load screenshot scene?",
                    message: "The screenshot scene assigned to " +
                        $"{nameof(AppProfileSO)} isn't loaded/active:\n" +
                        $"{screenshotScenePath}\n" +
                        "If thumbnails are rendered without this as the active " +
                        "scene, the images will have incorrect reflections.\n" +
                        "\n" +
                        "Do you wish to load the screenshot scene and then " +
                        "render the thumbnails?",
                        ok: "Load Scene and Render",
                        cancel: "Cancel",
                        alt: "Render As-Is"
                    );

            if (dialogResult == Const.DialogResult_Cancel)
            {
                // Abort
                return false;
            }

            if (dialogResult == Const.DialogResult_OK)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // Abort
                    return false;
                }

                EditorSceneManager.OpenScene(
                    screenshotScenePath,
                    OpenSceneMode.Single
                );
            }

            return true;
        }

        public static CoroutineHandle RenderScreenshot(
            ProductVisualInfoSO visualInfo,
            ProductVisualContent visualContent,
            bool forceRender)
        {
            return Timing.RunCoroutine(
                RenderScreenshotRoutine(visualInfo, visualContent, forceRender),
                segment: Segment.EditorUpdate
            );
        }

        private static IEnumerator<float> RenderScreenshotRoutine(
            ProductVisualInfoSO visualInfo,
            ProductVisualContent visualContent,
            bool forceRender)
        {
            ProductVisualContent originalTarget = visualContent;

            // In order to be able to screenshot (and instantiate temporary
            // cabinet legs, if necessary), the prefab has to be writable.
            // If we cannot open the prefab in the prefab stage, we cannot
            // instantiate temporary cabinet legs and we cannot set its
            // thumbnail camera to the prefab stage's scene.

            string prefabAssetPath = AssetDatabase.GetAssetPath(visualContent);
            if (!string.IsNullOrEmpty(prefabAssetPath))
            {
                PrefabStage prefabStage =
                    PrefabStageUtility.OpenPrefab(prefabAssetPath);

                visualContent = prefabStage.prefabContentsRoot
                    .GetComponentInChildren<ProductVisualContent>();
            }
            else if (!AssetDatabase.OpenAsset(visualContent))
            {
                MelcherLog.LogError("Cannot open visual in the prefab stage.");
                yield break;
            }

            if (!forceRender &&
                !visualContent.productModelPrefab &&
                !visualContent.sectionModelPrefab &&
                !visualContent.finishMaterial)
            {
                MelcherLog.Log($"Skipping thumbnail render for {visualContent}. It has nothing renderable.", visualContent);
                yield break;
            }

            Transform temporaryHandle = InstantiateDoorHandle(visualInfo, visualContent);

            // In order to add temporary cabinet legs to the visual (i.e.,
            // WeatherStrong cabinet sections), we must get a "prefab content"
            // version of the asset. Otherwise, we cannot instantiate objects
            // into the asset (persistent game object errors).

            List<Transform> temporaryCabinetLegs = new ();

            if (visualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetSection &&
                visualInfo.brandCatalogName == "WeatherStrong")
            {
                // Create temporary cabinet legs for WeatherStrong cabinets

                BrandCatalogSO weatherStrongBrandCatalog =
                    MelcherCommon.Find(
                        ProFitGlobals.appProfile.presetBrandCatalogs,
                        static x => x.catalogName == "WeatherStrong"
                    );

                Transform legModelPrefab =
                    weatherStrongBrandCatalog.cabinetSectionLegStyle.legModelPrefab;

                Vector2 legOffsetInMeters =
                    weatherStrongBrandCatalog.cabinetSectionLegStyle.horizontalInsetFromCabinetEdgesInMeters;

                Vector2 legOffsetInMetersOverride =
                    visualInfo.cabinetLegHorizontalInsetFromCabinetEdgesOverrideInMeters;

                if (float.IsNormal(legOffsetInMetersOverride.x))
                {
                    legOffsetInMeters.x = legOffsetInMetersOverride.x;
                }

                if (float.IsNormal(legOffsetInMetersOverride.y))
                {
                    legOffsetInMeters.y = legOffsetInMetersOverride.y;
                }

                Cuboid cabinetCuboid =
                    new Cuboid(
                        position: visualContent.modelCutOutExtentMin.position,
                        size: visualContent.modelCutOutExtentMax.position - visualContent.modelCutOutExtentMin.position
                    );

                if (visualInfo.sectionCabinetType != CabinetType.Corner)
                {
                    InstantiateLeg(
                        temporaryCabinetLegs,
                        legModelPrefab,
                        visualContent.scaleOrigin,
                        cabinetCuboid,
                        normalizedCabinetAnchor: new Vector3(0.0f, 0.0f, 0.0f),
                        offsetFromCabinetAnchorInMeters:
                            new Vector3(
                                legOffsetInMeters.x,
                                0.0f,
                                legOffsetInMeters.y
                            )
                    );
                }

                InstantiateLeg(
                    temporaryCabinetLegs,
                    legModelPrefab,
                    visualContent.scaleOrigin,
                    cabinetCuboid,
                    normalizedCabinetAnchor: new Vector3(1.0f, 0.0f, 0.0f),
                    offsetFromCabinetAnchorInMeters:
                        new Vector3(
                            -legOffsetInMeters.x,
                            0.0f,
                            legOffsetInMeters.y
                        )
                );

                InstantiateLeg(
                    temporaryCabinetLegs,
                    legModelPrefab,
                    visualContent.scaleOrigin,
                    cabinetCuboid,
                    normalizedCabinetAnchor: new Vector3(0.0f, 0.0f, 1.0f),
                    offsetFromCabinetAnchorInMeters:
                        new Vector3(
                            legOffsetInMeters.x,
                            0.0f,
                            -legOffsetInMeters.y
                        )
                );

                InstantiateLeg(
                    temporaryCabinetLegs,
                    legModelPrefab,
                    visualContent.scaleOrigin,
                    cabinetCuboid,
                    normalizedCabinetAnchor: new Vector3(1.0f, 0.0f, 1.0f),
                    offsetFromCabinetAnchorInMeters:
                        new Vector3(
                            -legOffsetInMeters.x,
                            0.0f,
                            -legOffsetInMeters.y
                        )
                );

                static void InstantiateLeg(
                    List<Transform> legs,
                    Transform legModelPrefab,
                    Transform parent,
                    Cuboid cabinetCuboid,
                    Vector3 normalizedCabinetAnchor,
                    Vector3 offsetFromCabinetAnchorInMeters)
                {
                    Vector3 anchor =
                        Cuboid.NormalizedToPoint(
                            cabinetCuboid,
                            normalizedCabinetAnchor
                        );

                    Transform leg =
                        (Transform)PrefabUtility.InstantiatePrefab(legModelPrefab, parent);

                    leg.name = legModelPrefab.name;

                    leg.localPosition = anchor + offsetFromCabinetAnchorInMeters;

                    legs.Add(leg);
                }
            }

            // We introduce a delay here so that textures have a chance to
            // fully load on the GPU. At least, that's my theory as to why the
            // first time a texture is used it renders in as a solid color.

            yield return Timing.WaitForOneFrame;

            MeshRenderer[] instanceMeshRenderers = null;
            MeshRenderer[] prefabMeshRenderers = null;

            if (visualContent.productModelPrefab && !visualInfo.useCabinetMaterial)
            {
                // Temporarily remap the meshes using the default kitchen
                // material with the intended stainless steel product material.
                // This is intended because `useCabinetMaterial` is disabled.

                instanceMeshRenderers =
                    visualContent.scaleOrigin.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                prefabMeshRenderers =
                    visualContent.productModelPrefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                if (instanceMeshRenderers.Length != prefabMeshRenderers.Length)
                {
                    MelcherLog.LogError("Mismatched material count between instance and prefab: " + visualContent.scaleOrigin);
                }
                else
                {
                    for (int i = 0; i < prefabMeshRenderers.Length; ++i)
                    {
                        Material[] meshMaterials = prefabMeshRenderers[i].sharedMaterials;

                        for (int j = 0; j < meshMaterials.Length; ++j)
                        {
                            if (meshMaterials[j] == ProFitGlobals.appProfile.editorOnlyDefaultKitchenMaterial)
                            {
                                // Found the "cabinet material" identifier. It
                                // is either substituted with the kitchen's
                                // current cabinet material (so the product can
                                // match the kitchen) or it is given the
                                // usual occupant material.

                                meshMaterials[j] =
                                    ProFitGlobals.appProfile.editorOnlyDefaultOccupantMaterial;
                            }
                        }

                        instanceMeshRenderers[i].sharedMaterials = meshMaterials;

                        PrefabUtility.RecordPrefabInstancePropertyModifications(instanceMeshRenderers[i]);
                    }
                }
            }

            using var _ =
                MelcherDeferred.Defer(
                    () =>
                    {
                        // Restore the model's original prefab materials.
                        // This undoes the temporary reassignment that happens
                        // to products that use the kitchen default material
                        // but have requested to use the normal product
                        // material.
                        if (instanceMeshRenderers != null)
                        {
                            foreach (Renderer renderer in instanceMeshRenderers)
                            {
                                if (renderer)
                                {
                                    PrefabUtility.RevertObjectOverride(renderer, InteractionMode.AutomatedAction);
                                }
                            }
                        }

                        if (temporaryCabinetLegs != null && temporaryCabinetLegs.Count > 0)
                        {
                            // Destroy the temporary cabinet legs.
                            foreach (Transform leg in temporaryCabinetLegs)
                            {
                                if (leg)
                                {
                                    Object.DestroyImmediate(leg.gameObject);
                                }
                            }

                            // Ensure the collider is not including the legs.
                            visualContent.FitColliderToModel(default);
                        }

                        if(temporaryHandle != default)
                        {
                            Object.DestroyImmediate(temporaryHandle.gameObject);
                        }
                    }
                );

            Texture2D thumbnail = null;

            using var releaseThumbnail =
                MelcherDeferred.Defer(
                    () =>
                    {
                        if (thumbnail != null)
                        {
                            Object.DestroyImmediate(thumbnail);
                            thumbnail = null;
                        }
                    }
                );

            // Using a non-compressed texture format to enable Texture.EncodeToPNG()
            const TextureFormat ThumbnailTextureFormat = TextureFormat.RGBA32;

            // If the product is a finish (i.e. a texture), re-render the
            // texture as a thumbnail
            if (visualContent.finishTexture != null)
            {
                Texture2D squareCrop = null;

                using var releaseSquareCrop =
                    MelcherDeferred.Defer(
                        () =>
                        {
                            // Clean up the temporary crop
                            if (squareCrop != null)
                            {
                                Object.DestroyImmediate(squareCrop);
                                squareCrop = null;
                            }
                        }
                    );

                if (visualContent.finishTexture.width != visualContent.finishTexture.height)
                {
                    // Make the texture readable to allow GetPixels to work
                    string finishTexturePath = AssetDatabase.GetAssetPath(visualContent.finishTexture);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(finishTexturePath);

                    bool wasReadable = importer.isReadable;

                    using var resetReadability =
                        MelcherDeferred.Defer(
                            () =>
                            {
                                // Restore the previous readability setting when
                                // the scope exits
                                importer.isReadable = wasReadable;
                                importer.SaveAndReimport();
                            }
                        );

                    importer.isReadable = true;
                    importer.SaveAndReimport();

                    // Ensure we sample a square portion of the texture
                    // because square looks better in the product toolbar
                    int squareSize = Mathf.Min(
                        visualContent.finishTexture.width,
                        visualContent.finishTexture.height
                    );
                    squareCrop = new Texture2D(
                        squareSize,
                        squareSize,
                        ThumbnailTextureFormat,
                        mipChain: false
                    );

                    Color[] squarePixels = visualContent.finishTexture.GetPixels(0, 0, squareSize, squareSize);
                    squareCrop.SetPixels(squarePixels);
                    squareCrop.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                }

                // Perform texture scaling by blitting to a render texture
                // (i.e. native texture size => thumbnail size)
                RenderTexture thumbnailRT = RenderTexture.GetTemporary(ThumbnailResolution, ThumbnailResolution);

                using var releaseRT =
                    MelcherDeferred.Defer(
                        toCapture: thumbnailRT,
                        onDispose: thumbnailRT => RenderTexture.ReleaseTemporary(thumbnailRT)
                    );

                RenderTexture previousRT = RenderTexture.active;

                using var restorePreviousRT =
                    MelcherDeferred.Defer(
                        toCapture: previousRT,
                        onDispose: previousRT => RenderTexture.active = previousRT
                    );

                RenderTexture.active = thumbnailRT;

                Graphics.Blit(squareCrop ?? visualContent.finishTexture, thumbnailRT);

                thumbnail = new Texture2D(
                    ThumbnailResolution,
                    ThumbnailResolution,
                    ThumbnailTextureFormat,
                    mipChain: false
                );

                // Keep the thumbnail texture readable so we can
                thumbnail.ReadPixels(new Rect(0, 0, ThumbnailResolution, ThumbnailResolution), 0, 0);
                thumbnail.Apply(updateMipmaps: false, makeNoLongerReadable: false);

                // Save out the thumbnail sprite
                SaveThumbnailTextureAsSpriteAndLinkToVisual(originalTarget, thumbnail);

                yield break;
            }
            else if (visualContent.finishMaterial)
            {
                // If the material doesn't have a texture, let's fall back to
                // rendering a thumbnail that is a solid colour.

                thumbnail =
                    new Texture2D(
                        ThumbnailResolution,
                        ThumbnailResolution,
                        ThumbnailTextureFormat,
                        mipChain: false
                    );

                const string ColorPropertyName = "_AlbedoTint";

                Color tintColor = Color.white;

                if (visualContent.finishMaterial.HasColor(ColorPropertyName))
                {
                    tintColor = visualContent.finishMaterial.GetColor(ColorPropertyName);
                }
                else
                {
                    MelcherLog.LogError("Material's thumbnail being generated without a texture and without a tint color.", visualContent);
                }

                Color[] colors =
                    Enumerable.Repeat(
                        tintColor,
                        ThumbnailResolution * ThumbnailResolution
                    ).ToArray();

                thumbnail.SetPixels(colors);
                thumbnail.Apply(updateMipmaps: false, makeNoLongerReadable: false);

                // Save out the thumbnail sprite
                SaveThumbnailTextureAsSpriteAndLinkToVisual(originalTarget, thumbnail);

                yield break;
            }

            // Load screenshot-quality render pipeline
            UniversalRenderPipelineAsset screenshotQuality =
                ProFitGlobals.appProfile.screenshotQuality;

            if (!Utility.RenderQuality.TrySetRenderQuality(screenshotQuality))
            {
                MelcherLog.LogError("Did not find requested render quality", screenshotQuality);
            }

            if (!AssetDatabase.OpenAsset(visualContent))
            {
                MelcherLog.LogError($"Could not open prefab stage for visual.");
                yield break;
            }

            // Get a temporary render texture for our thumbnail (enable MSAA)
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(ThumbnailResolution, ThumbnailResolution);
            descriptor.depthBufferBits = 16;
            descriptor.mipCount = 0;
            descriptor.useMipMap = false;
            descriptor.msaaSamples = screenshotQuality.msaaSampleCount;
            RenderTexture targetTextureForThumbnail = RenderTexture.GetTemporary(
                descriptor
            );

            // Render the thumbnail camera to a temporary render texture
            RenderTexture targetTextureToRestore = visualContent.thumbnailCamera.targetTexture;
            visualContent.thumbnailCamera.scene = PrefabStageUtility.GetCurrentPrefabStage().scene;   // we need to set the camera's scene manually
            visualContent.thumbnailCamera.targetTexture = targetTextureForThumbnail;
            visualContent.thumbnailCamera.Render();
            MelcherLog.Log("Took screenshot.");

            // Create a texture for the thumbnail and fill it with pixels
            // from the temporary render texture
            thumbnail = new Texture2D(
                ThumbnailResolution,
                ThumbnailResolution,
                ThumbnailTextureFormat,
                mipChain: false
            );

            RenderTexture activeTextureToRestore = RenderTexture.active;
            RenderTexture.active = targetTextureForThumbnail;
            {
                thumbnail.ReadPixels(new Rect(0, 0, ThumbnailResolution, ThumbnailResolution), 0, 0);
                thumbnail.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            }

            // Free up resources
            RenderTexture.active = activeTextureToRestore;
            visualContent.thumbnailCamera.targetTexture = targetTextureToRestore;
            RenderTexture.ReleaseTemporary(targetTextureForThumbnail);
            PrefabUtility.RevertObjectOverride(visualContent.thumbnailCamera, InteractionMode.AutomatedAction);

            //EditorSceneManager.CloseScene(prefabStage.scene, removeScene: true);

            SaveThumbnailTextureAsSpriteAndLinkToVisual(originalTarget, thumbnail);

            // Give the editor a chance to update the asset database so that
            // the product visual database line refreshes with the new thumbnail
            yield return Timing.WaitForOneFrame;
        }

        private static void SaveThumbnailTextureAsSpriteAndLinkToVisual(
            ProductVisualContent target,
            Texture2D thumbnailTexture)
        {
            // Create/update an asset for the thumbnail
            string visualAssetPath = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
            MelcherLog.Log($"visualAssetPath: {visualAssetPath}");
            string thumbnailAssetPath = System.IO.Path.ChangeExtension(visualAssetPath, ".png");
            //string thumbnailAssetPath = System.IO.Path.ChangeExtension(targetAssetPath, ".png");
            MelcherLog.Log($"thumbnailAssetPath: {thumbnailAssetPath}");

            string thumbnailFullPath =
                MelcherTransformFilePath.From(thumbnailAssetPath, relativeTo: MelcherPredefinedFilePath.project)
                    .ToAbsolute();

            MelcherLog.Log($"Writing thumbnail to path: {MelcherFormat.ConsoleString(thumbnailFullPath)}");

            byte[] thumbnailBytes = thumbnailTexture.EncodeToPNG();

            System.IO.File.WriteAllBytes(thumbnailFullPath, thumbnailBytes);
            AssetDatabase.ImportAsset(thumbnailAssetPath);

            // Setup thumbnail: transparent, sprite, no mips, etc.
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(thumbnailAssetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.maxTextureSize = ThumbnailResolution;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            // Link the thumbnail to the product visual
            {
                using SerializedObject serializedObject = new SerializedObject(target.editorOnlyVisualInfo);
                using SerializedProperty thumbnailProperty = serializedObject.FindProperty(ProductVisualInfoSO.PropertyName.Thumbnail);
                thumbnailProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(thumbnailAssetPath);
                serializedObject.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
        }

        private static Transform InstantiateDoorHandle(ProductVisualInfoSO visualInfo, ProductVisualContent visualContent)
        {
            if (visualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetDoor)
            {
                m_UsedBrandCatalogName = visualInfo.brandCatalogName;

                BrandCatalogSO usedBrandCatalog =
                    MelcherCommon.Find(
                        ProFitGlobals.appProfile.presetBrandCatalogs,
                        static x => x.catalogName == m_UsedBrandCatalogName
                    );

                if(usedBrandCatalog == null)
                {
                    return default;
                }

                return CabinetSectionDoorHandle.InstantiateHandleEditor(
                    usedBrandCatalog.cabinetSectionDoorHandleStyle,
                    visualInfo.id,
                    usedBrandCatalog.defaultCabinetSectionDoorHandleStyleName,
                    visualContent.scaleOrigin);
            }

            return default;
        }

    }   // class ProductVisualThumbnailCreator

}   // namespace KitchenDesign

#endif  // if UNITY_EDITOR

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-10
/// ===========================================================================
