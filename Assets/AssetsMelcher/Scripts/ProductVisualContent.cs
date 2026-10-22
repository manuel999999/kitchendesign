namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using System.Collections.Generic;
    using UnityEngine;
    using TMPro;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;

    /// <summary>
    ///     A <see cref="ProductVisualContent"/> represents one of many
    ///     possible things:
    ///     <br /><br />
    ///
    ///     * An appliance. (E.g., BBQ, sink, etc.)<br />
    ///     * A finish (essentially, a material) meant to be applied to cabinet
    ///         walls, the counter top, or the floor.<br />
    ///     * A custom modeled cabinet section to be used during <see
    ///         cref="KitchenBuildingMethod.Catalog"/>.<br />
    ///     <br /><br />
    ///
    ///     <see cref="ProductVisualContent"/> controls the visuals parts of a
    ///     product (i.e. its thumbnail, model, cut-outs, etc.). It has no
    ///     concept of a <see cref="ProductInfo"/> and therefore relies on any
    ///     shared data to be fed in. Currently, this is just the initial
    ///     cut-out size of the product, and this value may change if the
    ///     product is resizable, such as a <see cref="CabinetShelf"/>.
    ///     <br /><br />
    ///
    ///     For the object that represents an actual placed product in the
    ///     kitchen, see <see cref="Occupant"/>.
    ///     <br /><br />
    ///
    ///     Visuals have an editor-only camera and lights that are used to
    ///     render their thumbnail.
    /// </summary>
    public partial class ProductVisualContent : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        /// Get the <see cref="Transform"/> of the model's scaling point. The
        /// model scales relative to this point when correcting for different
        /// cut-out sizes.
        /// </summary>
        public Transform scaleOrigin => m_ScaleOrigin;

        /// <summary>
        /// Get the <see cref="Transform"/> of the cut-out's minimum extents.
        /// This is where the cut-out starts and it can be offset from the
        /// scale origin and the model origin.
        /// </summary>
        public Transform modelCutOutExtentMin => m_ModelCutOutExtentMin;

        /// <summary>
        /// Get the <see cref="Transform"/> of the cut-out's maximum extents.
        /// This is where the cut-out ends.
        /// </summary>
        public Transform modelCutOutExtentMax => m_ModelCutOutExtentMax;

        public GameObject productModelPrefab
        {
            get => m_ProductModelPrefab;
#if UNITY_EDITOR
            set => m_ProductModelPrefab = value;
#endif  // if UNITY_EDITOR
        }

        public Material finishMaterial => m_FinishMaterial;
        public Texture2D finishTexture => m_FinishTexture;

        public CabinetCutOut cabinetCutOut => m_CabinetCutOut;
        public CabinetCutOut countertopCutOut => m_CountertopCutOut;

        public Collider clickCollider => m_Collider;

        public IReadOnlyList<Renderer> additionalCabinetRenderMaskMeshes
            => m_AdditionalCabinetRenderMaskMeshes;

        public bool isProductVisible => m_ScaleOrigin.gameObject.activeSelf;

        public Transform sectionOccupantOrientation => m_SectionOccupantOrientation;

        public GameObject sectionModelPrefab
        {
            get => m_SectionModelPrefab;
#if UNITY_EDITOR
            set => m_SectionModelPrefab = value;
#endif // if UNITY_EDITOR
        }

        public Material sectionMaterialOverride => m_SectionMaterialOverride;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     This is only intended to be called by product visual
        ///     instances.
        /// </summary>
        /// <param name="productInfo"></param>
        public void SetProductInfo(ProductInfo productInfo)
        {
            if (Utility.RuntimePrefabs.IsPrefab(gameObject))
            {
                // The ProductInfo field is only intended to be used by
                // Occupants. This is to avoid the wrong ProductInfo
                // accidentally being shared between instances.

                throw new System.InvalidOperationException(
                    $"Cannot call {nameof(SetProductInfo)} on a " +
                    $"{nameof(ProductVisualContent)} prefab. Instantiate the " +
                    $"prefab and then call this method."
                );

            }

            m_ProductInfo = productInfo;
        }

        /// <summary>
        /// The occupant's origin transform. A <see cref="ProductVisualContent" />
        /// never adjusts this transform itself.
        /// </summary>
        public Transform GetTransform()
        {
            return m_Transform;
        }

        /// <summary>
        /// Get the size of the product's collider. This will be used as a
        /// fallback for the space the product occupies within the cabinet if
        /// the product is missing a cut-out size in the backend.
        /// <br /><br />
        ///
        /// Each channel may be used separately. I.e., if a product defines
        /// a cut-out width and height but not a depth, the depth will come
        /// from the collider's depth.
        /// </summary>
        public Vector3 GetColliderSize()
        {
            return m_Collider.size;
        }

        /// <summary>
        /// Set whether to draw the product. The cut-outs are rendered when
        /// the product isn't but they can also be drawn with the product if
        /// <see cref="m_RenderProductWithCutOut"/> is enabled.
        /// </summary>
        /// <param name="visible"></param>
        public void SetProductVisibility(bool visible)
        {
            m_ScaleOrigin.gameObject.SetActive(visible);

            RefreshCutOutVisibility();
        }

        /// <summary>
        /// Update the visibility of the cut-outs depending on the visual's
        /// current state. E.g., if the product is shown, the cut-outs may
        /// be hidden.
        /// </summary>
        public void RefreshCutOutVisibility()
        {
            bool showCutOuts = !isProductVisible || m_RenderProductWithCutOut;

            m_CountertopCutOut.gameObject.SetActive(m_NeedsCountertopCutOut && showCutOuts);

            m_CabinetCutOut.gameObject.SetActive(showCutOuts);
        }

        /// <summary>
        /// Set whether the product's cut-out measurements are enabled and
        /// therefore displayed. This function does not update the position of
        /// the measurement lines. See <see cref="KitchenScreenshotManager.SetupOccupantCutOutMeasurements(Kitchen, ProductVisualContent)"/>
        /// for that.
        /// </summary>
        public void SetCutOutMeasurementsVisibility(bool visible, ScreenshotAngle screenshotAngle)
        {
            bool frontIsVisible = visible && screenshotAngle == ScreenshotAngle.Front;
            bool topIsVisible = visible && screenshotAngle == ScreenshotAngle.Top;

            // Change visibility first. This gives the measures a chance to
            // Awake and initialize before try to refresh them
            m_MeasureX.gameObject.SetActive(frontIsVisible);
            m_MeasureY.gameObject.SetActive(frontIsVisible);
            m_MeasureZ.gameObject.SetActive(topIsVisible);
            m_MeasureXTop.gameObject.SetActive(topIsVisible);
            m_CutOutInfoLabelFront.gameObject.SetActive(frontIsVisible);
            m_CutOutInfoLabelTop.gameObject.SetActive(topIsVisible);
        }

        /// <summary>
        /// Applies the positions of the externally calculated line segment
        /// measures.
        /// </summary>
        public void SetCutOutMeasurementsPositions(
            ProductVisualInfo productVisualInfo,
            Transform cabinetTransform,
            Transform cabinetLocalOccupantOrientation,
            Vector3 cutOutLeftBottomTopCornerInProductSpace,
            Vector3 defaultCutOutSizeInMeters,
            LineSegment widthForFrontOrBackInProductSpace,
            LineSegment widthForTopInProductSpace,
            LineSegment heightInProductSpace,
            LineSegment depthInProductSpace,
            Vector3 frontOrBackLabelPositionInProductSpace,
            Vector3 topLabelPositionInProductSpace,
            float kitchenCountertopThicknessInMeters)
        {
            // If the cabinet is a 45-degree corner, we need to adjust the
            // measurements to match.
            Vector3 pivot = cutOutLeftBottomTopCornerInProductSpace;
            Quaternion rotation = cabinetLocalOccupantOrientation.localRotation;
            widthForFrontOrBackInProductSpace.RotateAroundPivot(pivot, rotation);
            widthForTopInProductSpace.RotateAroundPivot(pivot, rotation);
            heightInProductSpace.RotateAroundPivot(pivot, rotation);
            depthInProductSpace.RotateAroundPivot(pivot, rotation);

            // Convert the lines from the product's local-space to world-space.
            // The Measure class needs points in world-space

            widthForFrontOrBackInProductSpace.Transform(cabinetTransform);
            widthForTopInProductSpace.Transform(cabinetTransform);
            heightInProductSpace.Transform(cabinetTransform);
            depthInProductSpace.Transform(cabinetTransform);

            Vector3 cutOutSizeInMeters = GetCurrentCutOutSizeInMeters(defaultCutOutSizeInMeters);

            // Calculate the normalized label position within the line.
            // The labels are shifted so they're not centered in the
            // measurement line. This is to avoid overlapping with the product
            // name label.
            const float LabelInsetInMeters = 2.0f * Units.Inches2Meters;

            float labelInsetPercentX = LabelInsetInMeters / widthForFrontOrBackInProductSpace.distance;
            m_MeasureX.labelNormalizedPositionWithinLine = labelInsetPercentX;

            float labelInsetPercentY = LabelInsetInMeters / heightInProductSpace.distance;
            m_MeasureY.labelNormalizedPositionWithinLine = 1.0f - labelInsetPercentY;

            float labelInsetPercentZ = LabelInsetInMeters / depthInProductSpace.distance;
            m_MeasureZ.labelNormalizedPositionWithinLine = 1.0f - labelInsetPercentZ;

            // Duplicate of m_MeasureX but it faces upwards for top-down screenshot
            m_MeasureXTop.labelNormalizedPositionWithinLine = labelInsetPercentX;

            // Update the line renderers and distance labels
            m_MeasureX.MeasureFromTo(widthForFrontOrBackInProductSpace.begin, widthForFrontOrBackInProductSpace.end);
            m_MeasureXTop.MeasureFromTo(widthForTopInProductSpace.begin, widthForTopInProductSpace.end);
            m_MeasureY.MeasureFromTo(heightInProductSpace.begin, heightInProductSpace.end);
            if (productVisualInfo.anchor.IsOnTop())
            {
                // HACK: This is a kludge to fix the issue of the countertop
                //       growing "downwards". Once the cabinet's height becomes
                //       independent of the countertop's thickness, this will
                //       be removed.
                m_MeasureY.SetExtraDistanceForLabelOnly(kitchenCountertopThicknessInMeters);
            }
            m_MeasureZ.MeasureFromTo(depthInProductSpace.begin, depthInProductSpace.end);

            m_CutOutInfoLabelFront.transform.position = cabinetTransform.TransformPoint(frontOrBackLabelPositionInProductSpace);
            // Orient the text the other way if the product is anchored to the back
            m_CutOutInfoLabelFront.transform.localEulerAngles = new Vector3(0.0f, productVisualInfo.anchor.IsOnOrAtFront() ? 0.0f : 180.0f, 0.0f);
            // Resize the label bounding box. The text is constrained to
            // the box and this ensures all labels remain readable
            m_CutOutInfoLabelFront.rectTransform.sizeDelta = new Vector2(cutOutSizeInMeters.x, cutOutSizeInMeters.y);

            m_CutOutInfoLabelTop.transform.position = cabinetTransform.TransformPoint(topLabelPositionInProductSpace);
            // TODO: Does the bounding box need to be adjusted depending on
            //       orientation? Top-down only
            m_CutOutInfoLabelFront.rectTransform.sizeDelta = new Vector2(cutOutSizeInMeters.x, cutOutSizeInMeters.z);
        }

        public void SetCutOutMeasurementsUnits(MeasurementUnit units)
        {
            m_MeasureX.units = units;
            m_MeasureXTop.units = units;
            m_MeasureY.units = units;
            m_MeasureZ.units = units;
        }

        /// <summary>
        ///     This function toggles the visibility of the cut-out label that
        ///     is visible in top-down screenshots. Hiding the top label is
        ///     intended to be used to avoid overlapping text in top-down
        ///     screenshots when products within a shelf render in the same
        ///     place.
        /// </summary>
        public void SetCutOutInfoTopLabelVisible(bool isVisible)
        {
            m_CutOutInfoLabelTop.gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// Set the text that appears in the cut-out screenshot.
        /// </summary>
        /// <param name="text"></param>
        public void SetCutOutInfoLabelText(string text)
        {
            m_CutOutInfoLabelFront.text = text;
            m_CutOutInfoLabelTop.text = text;
        }

        /// <summary>
        ///     Get the product's current cut-out size (in meters). Currently,
        ///     only shelves are resizable so all other products will return
        ///     <paramref name="defaultCutOutSizeInMeters"/>.
        /// </summary>
        public Vector3 GetCurrentCutOutSizeInMeters(Vector3 defaultCutOutSizeInMeters)
        {
            return m_CurrentCutOutSizeInMeters.GetValueOrDefault(defaultCutOutSizeInMeters);
        }

        /// <summary>
        /// Resize the product's model to match the requested cut-out
        /// dimensions. Also synchronize the cut-outs to match.
        /// <br /><br />
        ///
        /// All measurements are in the product's local space.
        /// </summary>
        public void SetCutOutSizeInMeters(ProductVisualInfo productVisualInfo, Vector3 cutOutSizeInMeters, bool notifyListeners = true)
        {
            m_CurrentCutOutSizeInMeters = cutOutSizeInMeters;

            // TODO: Currently limiting fake shadows to Stoll products (i.e.,
            //       products with `useCabinetMaterial == true`) because we
            //       know those are not flush with the top or bottom of the
            //       cabinet. This code needs to become smart enough to disable
            //       slices if the product is touching the top or bottom of the
            //       cabinet. -Ryan

            bool showFrontCutOutShadow =
                productVisualInfo.runtimePrefabCategory != ProductCategorySpecific.CabinetDoor &&
                productVisualInfo.anchor.IsOnFront() &&
                productVisualInfo.useCabinetMaterial;

            m_FrontCutOutShadow.gameObject.SetActive(showFrontCutOutShadow);

            // Calculate the scale factor that takes the model's design-time
            // cut-out size to the actual cut-out size entered in the backend
            Vector3 modelCutOutSize = m_ModelCutOutExtentMax.localPosition - m_ModelCutOutExtentMin.localPosition;
            Vector3 cutOutScaleCorrection = Vector3.one;
            for (int axisIndex = 0; axisIndex < 3; ++axisIndex)
            {
                // Ensure measurements are all positive
                modelCutOutSize[axisIndex] = Mathf.Abs(modelCutOutSize[axisIndex]);
                cutOutSizeInMeters[axisIndex] = Mathf.Abs(cutOutSizeInMeters[axisIndex]);

                // Get the scale ratio (only scale if the cut-out has some
                // magnitude on this axis and we won't be dividing by zero)
                if (cutOutSizeInMeters[axisIndex] > 0.01f && modelCutOutSize[axisIndex] > 0.01f)
                {
                    // TODO: If a scale channel is 0, should we disable the cut-out?
                    cutOutScaleCorrection[axisIndex] = cutOutSizeInMeters[axisIndex] / modelCutOutSize[axisIndex];
                }
            }

            // If the product is rendered without its cut-outs, clamp the scale
            // factor to avoid some ridiculously tall BBQ's. If we're showing
            // cut-outs, we skip clamping because the product needs to properly
            // fill its cut-out hole.

            Vector3 modelScaleCorrection = cutOutScaleCorrection;
            if (!m_RenderProductWithCutOut)
            {
                switch (productVisualInfo.anchor)
                {
                    case ProductAnchor.OnTopAtFront:
                    case ProductAnchor.OnTopAtBack:
                        if (modelScaleCorrection.y > 1.08f)
                        {
                            MelcherLog.LogWarningEditorOnly(name + " Y-scale is bigger than 8%", this);
                        }
                        modelScaleCorrection.y = Mathf.Clamp(modelScaleCorrection.y, 0.92f, 1.08f);
                        break;

                    default:
                        if (modelScaleCorrection.z > 1.08f)
                        {
                            MelcherLog.LogWarningEditorOnly(name + " Z-scale is bigger than 8%", this);
                        }
                        modelScaleCorrection.z = Mathf.Clamp(modelScaleCorrection.z, 0.92f, 1.08f);
                        break;
                }
            }

            m_ScaleOrigin.localScale = modelScaleCorrection;

            // Update the collider's bounds to match the new scale
            FitColliderToModel(cutOutSizeInMeters);

            TransformHelper cabinetCutOut = m_CabinetCutOut.transformHelper;
            cabinetCutOut.sizeInMeters = cutOutSizeInMeters;

            // Shift the cut-outs' offset with respect to the correction scale
            cabinetCutOut.offsetInMeters =
                Vector3.Scale(m_ModelCutOutExtentMin.localPosition, cutOutScaleCorrection) + m_ScaleOrigin.localPosition;


            // Make the countertop cut-out the same size as the cabinet cut-out
            // but have zero height
            TransformHelper countertopCutOut = m_CountertopCutOut.transformHelper;

            countertopCutOut.sizeInMeters =
                Vector3.Scale(cabinetCutOut.sizeInMeters, new Vector3(1.0f, 0.0f, 1.0f));

            // Position the countertop cut-out at the top
            countertopCutOut.offsetInMeters =
                cabinetCutOut.offsetInMeters + (Vector3.up * cutOutSizeInMeters.y);

            // Position the fake shadow/contrast around the front cut-out. This
            // helps the product stand out, especially when the product is
            // textured the same as the cabinet, as is possible with Stoll
            // products.

            if (m_FrontCutOutShadow.gameObject.activeSelf)
            {
                m_FrontCutOutShadow.UpdateSlices(
                    new Cuboid(
                        position: cabinetCutOut.offsetInMeters + (Vector3.back * 0.003f),
                        size: cabinetCutOut.sizeInMeters
                    )
                );
            }

            if (notifyListeners && TryGetComponent(out IOccupantCutOutSizeChangedHandler occupantCutOutChanged))
            {
                occupantCutOutChanged.OnOccupantCutOutSizeChanged(cutOutSizeInMeters);
            }
        }

        /// <summary>
        /// Update the cut-outs (size and position) to reflect a new countertop
        /// size.
        /// </summary>
        /// <param name="cabinetMetersToKeepBelowCountertop">
        ///     The number of meters that the cabinet cut-out's max-y will be
        ///     reduced by to preserve some of the cabinet. This is used by
        ///     Stoll's fixed smoker cabinets for extra support.
        /// </param>
        public void SetCountertopSize(float countertopThicknessInMeters, float countertopOverhangInMeters, float cabinetMetersToKeepBelowCountertop)
        {
            Vector3 cutOutSizeInMeters = GetCurrentCutOutSizeInMeters(m_ProductInfo.productSizeInMeters);

            {
                TransformHelper countertop = m_CountertopCutOut.transformHelper;

                Vector3 countertopSize = cutOutSizeInMeters;
                countertopSize.y = countertopThicknessInMeters;
                countertopSize.z = cutOutSizeInMeters.z + countertopOverhangInMeters;
                countertop.sizeInMeters = countertopSize;

                // TODO: Handle offsets for cut outs that do not share an origin
                //       with the scale origin. E.g. the Cut & Clean
                Vector3 offset = m_CabinetCutOut.transformHelper.offsetInMeters;
                offset.y += cutOutSizeInMeters.y - countertopThicknessInMeters;
                offset.z += -countertopOverhangInMeters;
                countertop.offsetInMeters = offset;

                m_NeedsCountertopCutOut = countertopThicknessInMeters > 0.0f;
            }

            // Reduce the cabinet cut out height by the countertop thickness
            {
                TransformHelper cabinet = m_CabinetCutOut.transformHelper;

                Vector3 size = cabinet.sizeInMeters;
                size.y = cutOutSizeInMeters.y - countertopThicknessInMeters - cabinetMetersToKeepBelowCountertop;
                cabinet.sizeInMeters = size;
            }

            RefreshCutOutVisibility();
        }

        /// <summary>
        ///     Set the material of the cut-out interiors. The materials are
        ///     as-is. I.e., passing <see langword="null"/> will result in the
        ///     cut-outs rendering in full magenta.
        /// </summary>
        /// <param name="productCabinetMaterialIdentifier">
        ///     The material that identifies a part of the product that should
        ///     be material-matched to the cabinet finish.
        /// </param>
        public async UniTask SetCabinetMaterials(Material cabinetMaterial, Material countertopMaterial, Material productCabinetMaterialIdentifier)
        {
            MelcherLog.Log(
                $"SetCabinetMaterials: {cabinetMaterial} | {countertopMaterial}",
                context: this,
                severity: MelcherLogSeverity.Verbose
            );

            // Restore the original prefab materials for each submesh. Find and
            // replace special materials in the product with the cabinet
            // material. This lets the product match the cabinet (used by
            // Stoll products).

            GameObject modelPrefab = m_ProductModelPrefab;

            if (m_SectionModelPrefab)
            {
                modelPrefab = m_SectionModelPrefab;
            }

            if (modelPrefab && productCabinetMaterialIdentifier)
            {
                MeshRenderer[] instanceMeshRenderers =
                    m_ScaleOrigin.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                MeshRenderer[] prefabMeshRenderers =
                    modelPrefab.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                if (instanceMeshRenderers.Length != prefabMeshRenderers.Length)
                {
                    MelcherLog.LogError("Mismatched material count between instance and prefab: " + m_ScaleOrigin);
                }
                else
                {
                    Material occupantDefaultMaterial =
                        await ProFitGlobals.appProfile.GetDefaultOccupantMaterial();

                    for (int i = 0; i < prefabMeshRenderers.Length; ++i)
                    {
                        Material[] meshMaterials = prefabMeshRenderers[i].sharedMaterials;

                        for (int j = 0; j < meshMaterials.Length; ++j)
                        {
                            if (meshMaterials[j] == productCabinetMaterialIdentifier)
                            {
                                // Found the "cabinet material" identifier. It
                                // is either substituted with the kitchen's
                                // current cabinet material (so the product can
                                // match the kitchen) or it is given the
                                // usual occupant material.

                                meshMaterials[j] =
                                    m_UseCabinetMaterial ?
                                        cabinetMaterial :
                                        occupantDefaultMaterial;
                            }
                        }

                        instanceMeshRenderers[i].sharedMaterials = meshMaterials;
                    }
                }
            }

            m_CabinetCutOut.SetInteriorMaterial(cabinetMaterial);
            m_CountertopCutOut.SetInteriorMaterial(countertopMaterial);
        }

        /// <summary>
        /// Highlight the product's model. The cut-out interiors are not
        /// highlighted.
        /// </summary>
        /// <param name="cabinetMaterial">
        ///     The material that is currently applied to the kitchen.
        /// </param>
        /// <param name="productCabinetMaterialIdentifier">
        ///     A material which, if found on the original model prefab, will
        ///     be replaced with <paramref name="cabinetMaterial"/>.
        /// </param>
        /// <param name="highlightMaterial">
        ///     The highlight material to assign to all of the product's
        ///     renderers. If this is <see langword="null"/>, the product's
        ///     highlight will be removed by restoring the materials found on
        ///     the corresponding renderers on the visual prefab.
        /// </param>
        public async UniTask SetModelHighlight(
            Material highlightMaterial,
            Material cabinetMaterial,
            Material countertopMaterial,
            Material productCabinetMaterialIdentifier)
        {
            if (highlightMaterial)
            {
                MeshRenderer[] meshRenderers =
                    m_ScaleOrigin.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

                foreach (MeshRenderer r in meshRenderers)
                {
                    Utility.RendererUtility.SetAllRendererMaterials(r, highlightMaterial);
                }
            }
            else
            {
                await SetCabinetMaterials(cabinetMaterial, countertopMaterial, productCabinetMaterialIdentifier);
            }
        }

        /// <summary>
        ///     Resize the collider (which is used for pointer clicks/drags) so
        ///     it matches the size of the current product scale.
        ///     <br /><br />
        ///
        ///     For most products, this function is called only a few times:
        ///     in the editor during <see cref="OnValidate"/> and when an
        ///     occupant is instantiated and sizes to match the server-specified
        ///     cut-out size.
        ///     <br /><br />
        ///
        ///     Occupants that can be resized (like <see cref="CabinetShelf"/>)
        ///     call this after they're resized to keep the hitbox synchronized
        ///     to the model.
        /// </summary>
        public void FitColliderToModel(Vector3 defaultCutOutSizeInMeters)
        {
            MeshRenderer[] meshRenderers =
                m_ScaleOrigin.GetComponentsInChildren<MeshRenderer>(includeInactive: false);

            if (Application.isEditor)
            {
                // Ensure that the visual and model prefab have the same number
                // of renderers. This is important for the highlight feature
                // because all of the model's meshes will use a highlight
                // material when selected, and the system will rely on the
                // renderers in the prefab to recover the correct materials
                // when the object is deselected/unhighlighted.

                MeshRenderer[] prefabMeshRenderers = m_ScaleOrigin.GetComponentsInChildren<MeshRenderer>();
                if (meshRenderers.Length != prefabMeshRenderers.Length)
                {
                    MelcherLog.LogError("Mismatched mesh count between visual prefab and model asset.", this);
                }
            }

            Bounds localBounds = new Bounds(m_Collider.center, m_Collider.size);

            if (meshRenderers.Length > 0)
            {
                // Calculate a bounds that encapsulates all submeshes

                // To do this reliably (regardless of non-axis-aligned
                // submeshes), we process the world space extents of each
                // submesh individually. That is, `Bounds` can only represent
                // AABB's and some submeshes use different rotations which
                // makes their bounds not directly compatible.

                static void CollectBoundsExtents(MeshRenderer submesh)
                {
                    Bounds localBounds = submesh.localBounds;
                    Transform t = submesh.transform;

                    Vector3 min = t.TransformPoint(localBounds.min);
                    Vector3 max = t.TransformPoint(localBounds.max);

                    s_WorldSpaceBoundsExtentsOfAllSubmeshes.Add(min);
                    s_WorldSpaceBoundsExtentsOfAllSubmeshes.Add(max);
                }

                static Vector3 WorldSpacePointToColliderLocal(Vector3 worldSpacePoint, Transform colliderTransform)
                {
                    return colliderTransform.InverseTransformPoint(worldSpacePoint);
                }

                s_WorldSpaceBoundsExtentsOfAllSubmeshes.Clear();
                for (int i = 0; i < meshRenderers.Length; ++i)
                {
                    CollectBoundsExtents(meshRenderers[i]);
                }

                Transform colliderTransform = m_Collider.transform;

                localBounds =
                    new Bounds(
                        center: WorldSpacePointToColliderLocal(s_WorldSpaceBoundsExtentsOfAllSubmeshes[0], colliderTransform),
                        size: Vector3.zero
                    );

                for (int i = 1; i < s_WorldSpaceBoundsExtentsOfAllSubmeshes.Count; ++i)
                {
                    localBounds.Encapsulate(
                        WorldSpacePointToColliderLocal(s_WorldSpaceBoundsExtentsOfAllSubmeshes[i], colliderTransform)
                    );
                }
            }
            else
            {
                // No submeshes so this must be a shelf (i.e., cut-outs only).
                // Just match the cut-out size exactly for the collider.

                Vector3 cutOutSizeInMeters =
                    GetCurrentCutOutSizeInMeters(defaultCutOutSizeInMeters);

                localBounds.size = cutOutSizeInMeters;
                localBounds.center =
                    m_ModelCutOutExtentMin.localPosition +
                    (cutOutSizeInMeters * 0.5f) +
                    (Const.OccupantMinimumHorizontalPaddingInMeters * 0.5f * Vector3.right);
            }

            // Size the collider to the calculated bounds
            m_Collider.center = localBounds.center;
            m_Collider.size = localBounds.size;
        }

        /// <summary>
        ///     Update the collider to a specific position. This is used by
        ///     <see cref="CabinetBacking"/>s to update their collider to match
        ///     the size of their cabinet when it resizes.
        /// </summary>
        /// <param name="position">
        ///     The min-extent position of the collider. I.e., this position
        ///     plus <paramref name="size"/> is the collider's max extent.
        /// </param>
        /// <param name="size">
        ///     The size of the collider in meters.
        /// </param>
        public void SetColliderBoundsInLocalSpace(Vector3 position, Vector3 size)
        {
            m_Collider.center = position + (size * 0.5f);
            m_Collider.size = size;
        }

        private void Awake()
        {
            // If manual selection is enabled, the collider must be disabled
            // to prevent Event System from automatically selecting it when
            // it is clicked. Unfortunately, the collider can't /really/ be
            // disabled because the collider would report a size of zero when
            // it is still needed.
            // TODO: Temporarily disabled until we have time to do this better.
            //       Doing it this way prevents it from being dragged.
            //m_Collider.gameObject.layer =
            //    LayerMask.NameToLayer(m_RequireManualSelection ? "Default" : "ProductPointerRaycast");
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [Header("Occupant details")]

        [Tooltip("A model asset that is used to display a component. This " +
            "may be null if the product is a finish/texture.")]
        [SerializeField, MelcherReadOnlyInInspector, MelcherAssetField]
        protected GameObject m_ProductModelPrefab = null;

        [Tooltip("If true, this product's material will match the current " +
            "cabinet material (or kitchen highlight if the kitchen is " +
            "selected).")]
        // TODO: Remove this. We now look for special materials assigned to the
        //       original prefab and replace those with the cabinet material.
        [SerializeField] protected bool m_UseCabinetMaterial = false;

        [Tooltip("If true, this object's cut-out will render while the " +
            "product's model is rendered. This is needed for some products " +
            "who don't fully seal against the cabinet. When this is on, no " +
            "clamping is performed on the product model's scale. This is " +
            "to ensure the visual model lines up with the cut-out.")]
        [SerializeField] protected bool m_RenderProductWithCutOut = false;

        [Tooltip("An optional list of other meshes that should be rendered " +
            "with the cabinet mesh the product is attached to. I.e., a " +
            "cabinet shelf uses this to render its own nested countertop " +
            "after the cut-outs are drawn.")]
        [SerializeField]
        protected Renderer[] m_AdditionalCabinetRenderMaskMeshes = default;

        // --------------------------------------------------------------------
        [Header("Occupant details - Cut out")]

        [SerializeField] protected CabinetCutOut m_CountertopCutOut = null;
        [SerializeField] protected CabinetCutOut m_CabinetCutOut = default;

        [Tooltip("A 9-slice mesh that is sized to match the front cut-out " +
            "and provide better contrast against the cabinet.")]
        [SerializeField] protected SlicedMesh m_FrontCutOutShadow = default;

        // --------------------------------------------------------------------
        [Header("Occupant details - Plan labels")]

        [SerializeField]
        [Tooltip("This label is exclusively for the measurement screenshot. " +
            "It displays the product's ID, name and manufacturer in the " +
            "front screenshot.")]
        protected TMP_Text m_CutOutInfoLabelFront = default;

        [SerializeField]
        [Tooltip("This label is exclusively for the measurement screenshot. " +
            "It displays the product's ID, name and manufacturer in the " +
            "top-down screenshot.")]
        protected TMP_Text m_CutOutInfoLabelTop = default;

        [SerializeField] protected Measure m_MeasureX = default;
        [SerializeField] protected Measure m_MeasureY = default;
        [SerializeField] protected Measure m_MeasureZ = default;
        [SerializeField] protected Measure m_MeasureXTop = default;

        // --------------------------------------------------------------------
        [Header("Occupant details - Size correction")]

        [Tooltip("This is point where the product's model will scale from. " +
            "The product's model should be a child of this. Ideally, this " +
            "will be (0, 0, 0).")]
        [SerializeField] protected Transform m_ScaleOrigin = default;

        [Tooltip("The starting corner of the model's cut-out. This should " +
            "be the smallest in every channel (XYZ) when compared to " +
            "Cut Out Extent Model. Ideally this will be at object X-position " +
            "zero.")]
        [SerializeField] protected Transform m_ModelCutOutExtentMin = default;

        [Tooltip("The most extreme corner of the cut out model's cut-out.")]
        [SerializeField] protected Transform m_ModelCutOutExtentMax = default;

        // --------------------------------------------------------------------
        [Header("Finish details")]

        [SerializeField] protected Material m_FinishMaterial = null;

        [Tooltip("If a material has more than one texture, this index can " +
            "be used to change which texture is used.")]
        [SerializeField] protected int m_FinishMaterialAlbedoTextureIndex = 0;

        // --------------------------------------------------------------------
        [Header("Cabinet section details")]

        [Tooltip("This transform is used exclusively by cabinet section " +
            "products. It indicates a custom rotation and offset to apply " +
            "to occupants in its cabinet. This is only used by 45-degree " +
            "cabinets as they need to align their products with a different " +
            "face than normal.")]
        [SerializeField]
        protected Transform m_SectionOccupantOrientation = default;

        [Tooltip("The model asset that is used to display a cabinet section. " +
            "This may be null if the visual represents an occupant (e.g., " +
            "BBQ) or a finish/texture.")]
        [SerializeField, MelcherAssetField]
        protected GameObject m_SectionModelPrefab = default;

        [Tooltip("The optional material that this cabinet is forced to use " +
            "rather than the user selected cabinet finish. This is intended " +
            "to allow refrigeration cabinets to use stainless steel.")]
        [SerializeField]
        protected Material m_SectionMaterialOverride = default;

        // --------------------------------------------------------------------
        [Header("Cabinet section door details")]

        [Tooltip("If this is a section door, this field is the name of the " +
            "style this door represents. This will match the part of the " +
            "asset name that follows the @ symbol. Otherwise, this is blank.")]
        [SerializeField]
        protected string m_SectionDoorStyleName = default;

        [SerializeField]
        protected string m_SectionDoorHandleStyleName = default;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        protected BoxCollider m_Collider = null;

        [SerializeField, MelcherAssetField, MelcherReadOnlyInInspector]
        protected Texture2D m_FinishTexture = null;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     The known, runtime cut-out size for this visual. It is
        ///     initialized in <see cref="SetCutOutSizeInMeters(Vector3)"/>.
        ///     This value is constant for most products, however, occupants
        ///     that are resizable (e.g., <see cref="CabinetShelf"/>) allow
        ///     their cut-out size to change according to the user. The active
        ///     and up-to-date value is reflected here.
        /// </summary>
        protected Vector3? m_CurrentCutOutSizeInMeters;

        protected bool m_NeedsCountertopCutOut;

        protected ProductInfo m_ProductInfo;

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        ///     For less GC, this list is reused by product visuals when they
        ///     are calculating their box collider shape.
        /// </summary>
        protected static List<Vector3> s_WorldSpaceBoundsExtentsOfAllSubmeshes = new List<Vector3>();

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum CabinetCutOutMode
        {
            /// <summary>
            ///     The default behaviour. The countertop and cabinet cut-outs
            ///     will render normally.
            /// </summary>
            Normal,

            /// <summary>
            ///     Special rule for Stoll fixed smoker cabinets. The front
            ///     countertop is preserved (forming a bridge in front of the
            ///     smoker) and the cabinet cut-out's height is reduced. In the
            ///     real world, this provides more stability to the cabinet.
            /// </summary>
            StollFixedSmokerCabinet,

            /// <summary>
            ///     This says that any occupant added to this cabinet will not
            ///     use cut-outs (countertop or cabinet). This is used with
            ///     Challenger's ceramic cabinets which have an empty shelf
            ///     below the countertop for a Smoker to be placed.
            /// </summary>
            NoCutOuts,

            /// <summary>
            ///     Like <see cref="NoCutOuts"/>, except it only prevents
            ///     cut-outs on smoker products.
            /// </summary>
            NoSmokerCutOuts,
        }

    }   // class ProductVisualContent

}   // namespace KitchenDesign

/// ============================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-13
/// ===========================================================================