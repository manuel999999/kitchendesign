#if UNITY_EDITOR
namespace KitchenDesign.Products
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Scriptables;
    using KitchenDesign.AddressableContent;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using KitchenDesign.Geometry;

    using static UnityEditor.PrefabUtility;

    // The editor-only details.
    public partial class ProductVisualContent
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public ProductVisualInfoSO editorOnlyVisualInfo => m_EditorOnlyVisualInfo;

        /// <summary>
        ///     Only accessible in the editor. This camera is tagged as
        ///     EditorOnly
        /// </summary>
        public Camera thumbnailCamera => m_ThumbnailCamera;

        /// <summary>
        ///     Calculate the hash of the visual as it currently is. This can
        ///     be compared against <see cref="approvedHash"/> to see if the
        ///     visual prefab has changed since it was last approved.
        /// </summary>
        public Hash128 currentHash
        {
            get
            {
                Hash128 hash = default;

                Object asset = underlyingAsset;

                if (asset)
                {
                    string assetPath =
                        AssetDatabase.GetAssetPath(asset);

                    // TODO: Check if this considers scripts too. We only want
                    //       to hash model and material dependencies. -Ryan
                    hash = AssetDatabase.GetAssetDependencyHash(assetPath);
                }

                return hash;
            }
        }

        public Hash128 approvedHash => m_ApprovedHash;

        public Object underlyingAsset
        {
            get
            {
                Object asset = finishMaterial;

                if (!asset)
                {
                    asset = productModelPrefab;
                }

                if (!asset)
                {
                    asset = sectionModelPrefab;
                }

                return asset;
            }
        }

        public ProductVisualEditorSetupStatus editorSetupStatus
        {
            get
            {
                int referencedAssetCount = 0;
                int expectedAssetCount = 1;

                if (m_FinishMaterial)
                {
                    ++referencedAssetCount;
                }

                if (m_ProductModelPrefab)
                {
                    ++referencedAssetCount;
                }

                if (m_SectionModelPrefab)
                {
                    ++referencedAssetCount;
                }
                else if (m_EditorOnlyVisualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetSection)
                {
                    // If a cabinet doesn't have a model, it is treated as a
                    // procedural cabinet.
                    --expectedAssetCount;
                }

                if (!m_EditorOnlyVisualInfo ||
                    string.IsNullOrWhiteSpace(m_EditorOnlyVisualInfo.id) ||
                    referencedAssetCount != expectedAssetCount ||
                    !m_EditorOnlyVisualInfo.thumbnail ||
                    //m_ReferenceSizeInInches.sqrMagnitude <= 0.0f ||
                    (m_SectionModelPrefab && m_EditorOnlyVisualInfo.anchor == ProductAnchor.Unknown))
                {
                    return ProductVisualEditorSetupStatus.MissingFields;
                }

                if (m_ApprovedHash != currentHash)
                {
                    return ProductVisualEditorSetupStatus.HashHasChanged;
                }

                return ProductVisualEditorSetupStatus.Good;
            }
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public void EditorOnlySetEditorOnlyVisualInfo(ProductVisualInfoSO info)
        {
            m_EditorOnlyVisualInfo = info;
        }

        public void AssignProductGameObjectLayer()
        {
            // Force the model onto Product layer
            int productLayer = LayerMask.NameToLayer("Product");
            Debug.Assert(productLayer != 0);
            m_ScaleOrigin.SetLayerRecursively(productLayer, includeSelf: false);
        }

        public void UpdateAddressableGroup()
        {
            string currentAssetPath = AssetDatabase.GetAssetPath(this);

            string groupName = name;

            AddressablesForEditor.MakeAddressable(this, name, currentAssetPath);
        }

        public void ReportErrorsInConsole()
        {
            // Error if no visual assets have been set
            if (!m_ProductModelPrefab && !m_FinishMaterial)
            {
                MelcherLog.LogError(
                    tag: nameof(ProductVisualContent),
                    message: $"'{name}' does not have a model prefab set.",
                    context: this
                );
            }
            // Verify the model is used in the product
            else if (m_ProductModelPrefab && !FindGameObjectInPrefabInChildren(m_ProductModelPrefab, transform))
            {
                MelcherLog.LogError(
                    tag: nameof(ProductVisualContent),
                    message: $"'{name}' does not have an instance of its model prefab.",
                    context: this
                );
            }
            // Error if the model hash has changed
            else if (m_ApprovedHash != currentHash)
            {
                MelcherLog.LogError(
                    tag: nameof(ProductVisualContent),
                    message: $"'{name}''s model prefab has changed and needs to be approved in the Product Visual Catalog Manager.",
                    context: this
                );
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!m_EditorOnlyVisualInfo && PrefabUtility.IsPartOfPrefabAsset(this))
            {
                // In the editor, we keep a reference to the ProductVisualInfo.
                // This can only be used at editor time and just helps simplify
                // some one-off editor features like visualizing a product's
                // occupiable padding.

                string visualPrefabAssetPath = AssetDatabase.GetAssetPath(this);

                string extensionlessPath = System.IO.Path.ChangeExtension(visualPrefabAssetPath, extension: null);

                string visualInfoAssetPath =
                    extensionlessPath += "^.asset";

                m_EditorOnlyVisualInfo = AssetDatabase.LoadAssetAtPath<ProductVisualInfoSO>(visualInfoAssetPath);

                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }

            // We can only know `defaultCutOutSizeInMeters` at runtime, so for
            // now we will default to a 1ft cubed box.
            Bounds colliderBoundsBefore = m_Collider.bounds;

            FitColliderToModel(
                defaultCutOutSizeInMeters: Units.InchesPerFoot * Units.Inches2Meters * Vector3.one
            );

            if (colliderBoundsBefore != m_Collider.bounds &&
                !MelcherUnityUtility.IsApplicationPlaying())
            {
                // Ensure any new collider size is visible immediately. This
                // must be queued to happen outside of OnValidate, otherwise we
                // cause a warning to be printed in Unity:
                // "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate"
                EditorApplication.delayCall +=
                    () =>
                    {
                        using (SerializedObject serializedObject = new(this))
                        {
                            serializedObject.UpdateIfRequiredOrScript();
                        }
                    };
            }

            // Assign a permanent category to the product that is separate from
            // the category assigned by the user in the dashboard. This started
            // with the goal of finding Fridge products to modify their product
            // padding during Stoll builds, but has since extended to handle
            // CabinetSection's and their CabinetDoor's.
            if (m_EditorOnlyVisualInfo &&
                m_EditorOnlyVisualInfo.runtimePrefabCategory == ProductCategorySpecific.Unknown)
            {
                Match categoryPrefix = Regex.Match(name, @"^ProductVisual_([^_]+)_");
                if (categoryPrefix.Success)
                {
                    string specificCategoryName = categoryPrefix.Groups[1].Value;

                    if (!System.Enum.TryParse(specificCategoryName, out ProductCategorySpecific specificCategory))
                    {
                        specificCategory = ProductCategorySpecific.Unknown;
                    }
                    else if (specificCategory == ProductCategorySpecific.CabinetSection)
                    {
                        ProcessPotentialCabinetSection(categoryPrefix, ref specificCategory);
                    }

                    if (m_EditorOnlyVisualInfo.runtimePrefabCategory != specificCategory)
                    {
                        m_EditorOnlyVisualInfo.EditorOnlySetRuntimePrefabCategory(specificCategory);

                        EditorUtility.SetDirty(m_EditorOnlyVisualInfo);
                    }

                    if (m_SectionModelPrefab)
                    {
                        // Automatically try to position the thumbnail camera
                        // to frame the model.

                        AutomaticallyPositionThumbnailCamera();
                    }
                }
            }

            string currentMaterialGuid = "";
            if (m_FinishMaterial)
            {
                string currentMaterialPath = AssetDatabase.GetAssetPath(m_FinishMaterial);
                currentMaterialGuid = AssetDatabase.AssetPathToGUID(currentMaterialPath);
            }

            if (currentMaterialGuid != m_PreviousFinishMaterialGuid)
            {
                m_PreviousFinishMaterialGuid = currentMaterialGuid;

                // This product has a finish material. Lookup an albedo texture
                // it is using to generate its thumbnail.

                Texture2D finishTexture = null;

                if (m_FinishMaterial)
                {
                    Shader shader = m_FinishMaterial.shader;

                    int shaderPropertyCount = shader.GetPropertyCount();

                    // A counter that is used to skip m_FinishMaterialAlbedoTextureIndex
                    // count textures to find
                    int skippedTexturePropertyCount = 0;

                    for (int propertyIndex = 0; propertyIndex < shaderPropertyCount; ++propertyIndex)
                    {
                        if (shader.GetPropertyType(propertyIndex) != UnityEngine.Rendering.ShaderPropertyType.Texture)
                        {
                            // Ignore non-texture properties

                            continue;
                        }

                        int textureNameId = shader.GetPropertyNameId(propertyIndex);

                        Texture2D texture = m_FinishMaterial.GetTexture(textureNameId) as Texture2D;

                        if (texture == null)
                        {
                            continue;
                        }

                        string textureAssetPath = AssetDatabase.GetAssetPath(texture);

                        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(textureAssetPath);

                        if (textureImporter.textureType != TextureImporterType.Default)
                        {
                            // Ignore bump maps and other non-default textures

                            continue;
                        }

                        if (skippedTexturePropertyCount < m_FinishMaterialAlbedoTextureIndex)
                        {
                            ++skippedTexturePropertyCount;

                            continue;
                        }

                        finishTexture = texture;

                        break;
                    }

                    m_EditorOnlyVisualInfo?.EditorOnlySetId(m_FinishMaterial.name);
                }

                m_FinishTexture = finishTexture;

                PrefabUtility.RecordPrefabInstancePropertyModifications(this);

                // Update the prefabs name to match the material. This must
                // be performed during delayCall because we cannot change
                // assets' names within OnValidate()

                if (m_FinishMaterial)
                {
                    EditorApplication.delayCall += MatchAssetNameToFinishMaterialName;
                }

                void MatchAssetNameToFinishMaterialName()
                {
                    // Ensure the script still exists before we attempt to
                    // modify its game object. It will be invalid if it belongs
                    // to the open prefab stage

                    bool stillExists = (this);

                    if (stillExists && IsPartOfNonAssetPrefabInstance(gameObject))
                    {
                        gameObject.name = "ProductVisual_" + m_FinishMaterial.name;

                        EditorUtility.SetDirty(this);
                    }
                }
            }

            // Ensure required values are provided
            if (!m_ModelCutOutExtentMin)
            {
                MelcherLog.LogError($"{nameof(m_ModelCutOutExtentMin)} is not set in {name}", this);
            }

            if (!m_ModelCutOutExtentMax)
            {
                MelcherLog.LogError($"{nameof(m_ModelCutOutExtentMax)} is not set in {name}", this);
            }

            if (m_ModelCutOutExtentMin &&
                m_ModelCutOutExtentMax &&
                m_ModelCutOutExtentMin.parent != m_ModelCutOutExtentMax.parent &&
                m_ModelCutOutExtentMin.parent != m_Transform)
            {
                MelcherLog.LogError($"{nameof(m_ModelCutOutExtentMin)} and {nameof(m_ModelCutOutExtentMax)} must be immediate children of the script's transform", this);
            }

            if (!m_ScaleOrigin)
            {
                MelcherLog.LogError($"{nameof(m_ScaleOrigin)} is not set in {name}", this);
            }
            else
            {
                // Check that our reference points don't scale with the model
                if (m_ModelCutOutExtentMin.IsChildOf(m_ScaleOrigin))
                {
                    MelcherLog.LogError($"{nameof(m_ModelCutOutExtentMin)} must not be a child of {nameof(m_ScaleOrigin)}", this);
                }

                if (m_ModelCutOutExtentMax.IsChildOf(m_ScaleOrigin))
                {
                    MelcherLog.LogError($"{nameof(m_ModelCutOutExtentMax)} must not be a child of {nameof(m_ScaleOrigin)}", this);
                }
            }

            // TEMP: Checking which product visuals have an offset between
            //       their model origin and cut-out origin (ignores
            //       vertical offset). When this is the case, a product
            //       may have issues fitting in a cabinet. -Ryan
            //
            //       We're getting a lot of console spam at the moment and
            //       this message is mostly ignored so let's omit it for now.
            //if (m_ScaleOrigin && m_ModelCutOutExtentMin)
            //{
            //
            //    Vector3 originDifferences =
            //        m_ScaleOrigin.position - m_ModelCutOutExtentMin.position;
            //
            //    originDifferences.y = 0.0f;
            //
            //    if (!ProFitUtility.IsDistanceAlmostZero(originDifferences.magnitude))
            //    {
            //        MelcherLog.LogWarning("The scale origin and model origin are not overlapped: " + name, this);
            //    }
            //}

            if (m_EditorOnlyVisualInfo &&
                m_EditorOnlyVisualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetDoor)
            {
                // The thumbnail camera will be positioned in the same relative
                // position as the cabinet section's camera when a screenshot
                // is rendered from the Product Visual Catalog. This enables
                // the door thumbnails to overlay their cabinet thumbnail and
                // fit in the proper gaps in the cabinet.

                ValidateCabinetDoorValues();

                if (m_FrontCutOutShadow.gameObject.activeSelf)
                {
                    m_FrontCutOutShadow.gameObject.SetActive(false);
                    EditorUtility.SetDirty(this);
                }
            }

            if (m_EditorOnlyVisualInfo)
            {
                // Fix a shadow issue caused by Cabinet Doors. Cabinet Doors
                // are implemented as occupants and therefore have meshes for
                // rendering an occupant's cut-outs. These cut-outs are unused
                // by cabinet doors but they will cast a shadow on doors that
                // have inset faces. We hide these cut-outs to prevent the
                // undesired shadow.

                bool isCabinetDoor = m_EditorOnlyVisualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetDoor;
                m_CabinetCutOut.gameObject.SetActive(!isCabinetDoor);
                m_CountertopCutOut.gameObject.SetActive(!isCabinetDoor);
            }
        }

        protected void ProcessPotentialCabinetSection(
            Match categoryPrefix,
            ref ProductCategorySpecific currentSpecificCategory)
        {
            Match brandCatalogNameMatch =
                Regex.Match(name, "^ProductVisual_CabinetSection_[^_]*_([^_]*)_");

            if (brandCatalogNameMatch.Success)
            {
                string newBrandCatalogName = brandCatalogNameMatch.Groups[1].Value;

                if (newBrandCatalogName != m_EditorOnlyVisualInfo.brandCatalogName)
                {
                    m_EditorOnlyVisualInfo.EditorOnlySetBrandCatalogName(newBrandCatalogName);
                    EditorUtility.SetDirty(m_EditorOnlyVisualInfo);
                }
            }

            // CabinetDoor's follow the same naming as CabinetSection's, except
            // they have a trailing @ symbol and the name of the style.
            int doorStyleNameSeparatorIndex = name.IndexOf('@');

            if (doorStyleNameSeparatorIndex >= 0)
            {
                currentSpecificCategory = ProductCategorySpecific.CabinetDoor;
                string[] cabinetDoorNameSplit = name.Split('@');

                string doorStyleName = cabinetDoorNameSplit[1];

                if (doorStyleName != m_SectionDoorStyleName)
                {
                    // Since we're still using CabinetMaterial by default,
                    // I'll set this to true for now when creating a door prefab
                    m_UseCabinetMaterial = true; 

                    m_SectionDoorStyleName = doorStyleName;
                    
                    m_EditorOnlyVisualInfo.EditorOnlySetDoorStyleName(doorStyleName);

                    EditorUtility.SetDirty(this);
                    EditorUtility.SetDirty(m_EditorOnlyVisualInfo);
                }

                return;
            }

            m_EditorOnlyVisualInfo.EditorOnlySetExpectingDoors(true);
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(m_EditorOnlyVisualInfo);

            // Cabinet sections list their primary product category in their
            // model name. Fetch this and use it as the default product
            // whitelist category.
            Regex underscoreBoundedWord = new Regex(@"[^_]+");

            Match cabinetWhitelistMatch =
                underscoreBoundedWord.Match(
                    name,
                    startat: categoryPrefix.Index + categoryPrefix.Length
                );

            if (cabinetWhitelistMatch.Success)
            {
                string specificWhitelistName = cabinetWhitelistMatch.Value;

                bool didParse =
                    System.Enum.TryParse(
                        specificWhitelistName,
                        out ProductCategorySpecific whitelistCategory
                    );

                if (!didParse)
                {
                    if (specificWhitelistName == "Specialty")
                    {
                        whitelistCategory = ProductCategorySpecific.Cookware;
                    }
                }

                if (whitelistCategory == ProductCategorySpecific.Grill)
                {
                    // Grill cabinets are kind of the "default" cab and
                    // are allowed more than just grills.
                    m_EditorOnlyVisualInfo.productCategoriesAllowedInSection =
                        new[]
                        {
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Grill),
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Burner),
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Cookware)
                        };
                }
                else if (whitelistCategory == ProductCategorySpecific.Cookware)
                {
                    m_EditorOnlyVisualInfo.productCategoriesAllowedInSection =
                        new[]
                        {
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Cookware),
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Smoker)
                        };
                }
                else if (whitelistCategory == ProductCategorySpecific.Sink)
                {
                    m_EditorOnlyVisualInfo.productCategoriesAllowedInSection =
                        new[]
                        {
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.Sink),
                            new MelcherStableEnum<ProductCategorySpecific>(ProductCategorySpecific.IceBox)
                        };
                }
                else
                {
                    m_EditorOnlyVisualInfo.productCategoriesAllowedInSection =
                        new[]
                        {
                            new MelcherStableEnum<ProductCategorySpecific>(whitelistCategory)
                        };
                }
            }
        }

        /// <summary>
        ///     Matches the thumbnail camera to the position of the
        ///     corresponding Cabinet Section product visual. This ensures
        ///     the thumbnails rendered for sections and doors properly align.
        /// </summary>
        protected void ValidateCabinetDoorValues()
        {
            // TODO: Use an editor-only serialized reference to the ProductVisualInfo
            //       to access this? -Ryan
            if (m_EditorOnlyVisualInfo)
            {
                MelcherLog.Assert(m_EditorOnlyVisualInfo.runtimePrefabCategory == ProductCategorySpecific.CabinetDoor);
            }

            // Door Styles are expected to be name in the format:
            // "<CabinetSectionProductVisualName>@<CabinetDoorStyleName>"
            int doorStyleNameSeparatorPosition = name.IndexOf('@');

            if (doorStyleNameSeparatorPosition < 0)
            {
                MelcherLog.LogError(
                    $"Unexpected asset name format in {MelcherFormat.ConsoleString(name)}: " +
                    $"Expected to find an '@' in the name to separate the " +
                    $"door style name because the visual is a " +
                    $"{MelcherFormat.ConsoleSymbolName(m_EditorOnlyVisualInfo.runtimePrefabCategory)}."
                );

                return;
            }

            string correspondingCabinetSectionVisualName =
                name.Substring(startIndex: 0, length: doorStyleNameSeparatorPosition);

            string customCabinetDoorVisualName = FindCabinetBaseFromMapping(correspondingCabinetSectionVisualName);

            string usedCorrespondingCabinetSectionVisualName;

            if (string.IsNullOrEmpty(customCabinetDoorVisualName))
            {
                usedCorrespondingCabinetSectionVisualName = correspondingCabinetSectionVisualName;
            }
            else
            {
                usedCorrespondingCabinetSectionVisualName = customCabinetDoorVisualName;
            }

            string correspondingCabinetSectionVisualAssetPath =
                "Assets/AssetsMelcher/ProductVisuals/" + usedCorrespondingCabinetSectionVisualName + ".prefab";

            ProductVisualContent correspondingCabinetSectionVisual =
                AssetDatabase.LoadAssetAtPath<ProductVisualContent>(
                    correspondingCabinetSectionVisualAssetPath
                );

            if (!correspondingCabinetSectionVisual)
            {
                if(customCabinetDoorVisualName == CabinetSectionDoorEditorMappingSO.SuppressedDoorValidationErrorString)
                {
                    return;
                }

                MelcherLog.LogError(
                    $"Failed to load product visual from asset path: {MelcherFormat.ConsoleString(correspondingCabinetSectionVisualAssetPath)}",
                    context: this
                );

                return;
            }

            Vector3 intendedThumbnailCameraPosition =
                correspondingCabinetSectionVisual.m_ThumbnailCamera.transform.position;

            bool isThumbnailCameraAtIntendedPosition =
                MelcherCommon.IsAlmostEqual(
                    intendedThumbnailCameraPosition,
                    m_ThumbnailCamera.transform.position,
                    tolerance: 0.001f
                );

            if (!isThumbnailCameraAtIntendedPosition)
            {
                m_ThumbnailCamera.transform.position = intendedThumbnailCameraPosition;
                EditorUtility.SetDirty(this);
            }
        }

        protected string FindCabinetBaseFromMapping(string doorName)
        {
            CabinetSectionDoorEditorMappingSO mapping = AssetDatabase.LoadAssetAtPath<CabinetSectionDoorEditorMappingSO>("Assets/AssetsMelcher/Resources/CabinetSectionDoorEditorMapping.asset");
            if(mapping == null)
            {
                MelcherLog.LogError("Failed to load CabinetSectionDoorEditorMapping asset.");
                return string.Empty;
            }

            return mapping.FindCabinetBaseFromMapping(doorName);
        }

        [ContextMenu("Force Render Product Visual Thumbnail")]
        protected void RenderProductThumbnail()
        {
            if (ProductVisualThumbnailCreator.PromptLoadScreenshotScene())
            {
                PrefabStage prefabStage =
                    PrefabStageUtility.GetCurrentPrefabStage();

                ProductVisualThumbnailCreator.RenderScreenshot(
                    m_EditorOnlyVisualInfo,
                    this,
                    forceRender: true
                );
            }
        }

        [ContextMenu("Automatically position thumbnail camera")]
        protected void AutomaticallyPositionThumbnailCamera()
        {
            // Try to initialize the thumbnail camera to pre-fit the model's
            // bounds. The camera fitting code is based is based on the example
            // shared by `jvo3dc` here:
            // https://discussions.unity.com/t/fit-object-exactly-into-perspective-cameras-field-of-view-focus-the-object/677696/3

            const float DistanceFromModelCenterToPlaceCamera = 1.1f;

            Vector3 cutOutSize = m_Collider.size;

            Vector3 cutOutCenter =
                m_Transform.TransformPoint(m_Collider.center);

            float largestModelDimension =
                Mathf.Max(
                    cutOutSize.x,
                    cutOutSize.y,
                    cutOutSize.z
                );

            // Visible height 1 meter out from camera
            float visibleHeightOneMeterFromCamera =
                2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * m_ThumbnailCamera.fieldOfView);

            // Combined wanted distance from the object
            float distance =
                DistanceFromModelCenterToPlaceCamera * largestModelDimension / visibleHeightOneMeterFromCamera;

            // Estimated offset from the center to the outside of the object
            distance += 0.5f * largestModelDimension;

            Bounds cutOutLocalBounds =
                new Bounds(
                    center: cutOutCenter,
                    size: cutOutSize
                );

            // Bias slightly towards the front and bottom of the model to
            // better frame the product
            Vector3 pointWithinBoundsToFocus =
                Cuboid.NormalizedToPoint(
                    new Cuboid(cutOutLocalBounds.min, cutOutLocalBounds.size),
                    normalized: new Vector3(0.5f, 0.43f, 0.35f)
                );

            m_ThumbnailCamera.transform.position =
                pointWithinBoundsToFocus - (distance * m_ThumbnailCamera.transform.forward);

            EditorUtility.SetDirty(m_ThumbnailCamera);
        }

        /// <summary>
        ///     Try to find a GameObject within the hierarchy of <paramref
        ///     name="instanceRootToCheck"/> that is correlated with the
        ///     GameObject of a prefab asset
        ///     (<paramref name="objectFromAssetToFind"/>).
        /// </summary>
        /// <returns>
        ///     Return <see langword="true"/> if a corresponding object was
        ///     found; otherwise, return <see langword="false"/>.
        /// </returns>
        protected static bool FindGameObjectInPrefabInChildren(GameObject objectFromAssetToFind, Transform instanceRootToCheck)
        {
            if (objectFromAssetToFind == null)
            {
                throw new System.ArgumentNullException(nameof(instanceRootToCheck));
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(objectFromAssetToFind))
            {
                throw new System.ArgumentException("Must be part of a prefab asset.", nameof(objectFromAssetToFind));
            }

            if (instanceRootToCheck == null)
            {
                throw new System.ArgumentNullException(nameof(instanceRootToCheck));
            }

            s_BreadthFirstModelSearchCandidates.Clear();
            s_BreadthFirstModelSearchCandidates.Add(instanceRootToCheck);

            // Use breadth-first search because (in the original use-case) the
            // corresponding object was likely to be close to the top of the
            // instance root
            for (int i = 0; i < s_BreadthFirstModelSearchCandidates.Count; ++i)
            {
                Transform current = s_BreadthFirstModelSearchCandidates[i];

                // Cast the instance object to its corresponding object in the
                // prefab asset
                GameObject correspondingGameObjectInPrefab =
                    GetCorrespondingObjectFromSource(current.gameObject);

                if (correspondingGameObjectInPrefab == objectFromAssetToFind)
                {
                    return true;
                }

                foreach (Transform child in current)
                {
                    s_BreadthFirstModelSearchCandidates.Add(child);
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_EditorOnlyVisualInfo)
            {
                return;
            }

            Vector3 position = m_ModelCutOutExtentMin.position;

            Cuboid localCuboid =
                m_EditorOnlyVisualInfo.sectionOccupiableCuboidPaddingInMeters.Deflate(
                    new(position, m_ModelCutOutExtentMax.position - position)
                );

            OrientedBounds orientedBounds =
                new OrientedBounds(
                    new Bounds(localCuboid.center, localCuboid.size),
                    m_Transform.worldToLocalMatrix
                );

            orientedBounds.Draw(Color.blue, 0.0f);
        }

        [MenuItem("CONTEXT/" + nameof(ProductVisualContent) + "/Find matching info")]
        private static void FindMatchingInfo(MenuCommand command)
        {
            if (!(command.context is ProductVisualContent content))
            {
                return;
            }

            if (content.editorOnlyVisualInfo)
            {
                return;
            }

            string contentPath = AssetDatabase.GetAssetPath(content);

            string visualInfoPath =
                    System.IO.Path.GetDirectoryName(contentPath) + "/" + content.name + ".asset";

            ProductVisualInfoSO info =
                AssetDatabase.LoadAssetAtPath<ProductVisualInfoSO>(visualInfoPath);

            if (!info)
            {
                MelcherLog.LogWarning("Could not find a product visual info for: " + visualInfoPath, content);
                return;
            }

            content.EditorOnlySetEditorOnlyVisualInfo(info);

            EditorUtility.SetDirty(content);
        }

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector]
        private ProductVisualInfoSO m_EditorOnlyVisualInfo;

        [SerializeField, HideInInspector]
        protected string m_PreviousFinishMaterialGuid = null;

        [SerializeField, MelcherReadOnlyInInspector]
        [Tooltip("The hash of the visual when it was last approved. This " +
            "is used to indicate when a visual's dependencies have been " +
            "modified and should be reviewed.")]
        [UnityEngine.Serialization.FormerlySerializedAs("m_RecordedHash")]
        protected Hash128 m_ApprovedHash;

        [Tooltip("Editor-only camera used to render the thumbnail.")]
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected Camera m_ThumbnailCamera = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down, errorIfFewerThan: 1)]
        [NonReorderable]
        protected Light[] m_ThumbnailLights = null;

        // ====================================================================
        // Static fields
        // ====================================================================

        private static List<Transform> s_BreadthFirstModelSearchCandidates = new List<Transform>();

        // ====================================================================
        // Types
        // ====================================================================

        public static class PropertyName
        {
            public const string EditorOnlyVisualInfo = nameof(m_EditorOnlyVisualInfo);
            public const string ProductModelPrefab = nameof(m_ProductModelPrefab);
            public const string SectionModelPrefab = nameof(m_SectionModelPrefab);
            public const string FinishMaterial = nameof(m_FinishMaterial);
            public const string ApprovedHash = nameof(m_ApprovedHash);

        }   // class PropertyName



    }   // class ProductVisualContent

}   // namespace KitchenDesign
#endif  // if UNITY_EDITOR
