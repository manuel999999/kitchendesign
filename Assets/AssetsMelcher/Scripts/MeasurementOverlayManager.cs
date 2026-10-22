namespace KitchenDesign
{
    using Melcher;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// This class updates the measurement overlays that appear when a product
    /// is selected in the designer scene that are used to show the distances
    /// from the edge of the product to its nearest product/cabinet end.
    ///
    /// It also handles displaying the measurement of a selected cabinet shelf.
    ///
    /// The overlays are in screen-space (using <see cref="MeasureOverlayUI"/>)
    /// to ensure the text size remains consistent regardless of camera zoom.
    /// </summary>
    public class MeasurementOverlayManager : MelcherMonoBehaviour
    {
        // =====================================================================
        // Constants
        // =====================================================================

        // The measurement line is shifted away from the cabinet by this
        // amount to prevent it from clipping with the kitchen geometry
        public const float MeasurementLineOffsetFromCabinetInMeters =
            1.0f * Const.Inches2Meters;

        // ====================================================================
        // Methods
        // ====================================================================

        public MeasureMode measureMode
        {
            get => m_MeasureMode;
            set
            {
                m_MeasureMode = value;

                UpdateMeasurements();
            }
        }

        protected void Awake()
        {
            OnKitchenRestart();
        }

        protected void OnEnable()
        {
            EventBridge.Designer.onHighlightChanged.OnInvoke += OnHighlightChanged;
            EventBridge.Designer.onCabinetResized.OnInvoke += OnCabinetResized;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke += OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetReordered.OnInvoke += OnCabinetReordered;
            EventBridge.Designer.onOccupantDragSuccess  += OnProductDragged;
            EventBridge.Designer.onOccupantResized.OnInvoke += OnOccupantResized;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke += OnOccupantAdded;
            EventBridge.Designer.onOccupantNudged       += OnOccupantNudged;
            EventBridge.Designer.restart                += OnKitchenRestart;
        }

        protected void OnDisable()
        {
            EventBridge.Designer.onHighlightChanged.OnInvoke -= OnHighlightChanged;
            EventBridge.Designer.onCabinetResized.OnInvoke -= OnCabinetResized;
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke -= OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetReordered.OnInvoke -= OnCabinetReordered;
            EventBridge.Designer.onOccupantDragSuccess  -= OnProductDragged;
            EventBridge.Designer.onOccupantResized.OnInvoke -= OnOccupantResized;
            EventBridge.Designer.onOccupantAddedToCabinet.OnInvoke -= OnOccupantAdded;
            EventBridge.Designer.onOccupantNudged       -= OnOccupantNudged;
            EventBridge.Designer.restart                -= OnKitchenRestart;
        }

        protected async UniTask OnHighlightChanged(GameObject highlightObject)
        {
            m_HighlightedObject = highlightObject;

            m_HighlightedCabinet = highlightObject?.GetComponent<Cabinet>();
            m_HighlightedProduct = highlightObject?.GetComponent<Occupant>();
            m_HighlightedBacking = highlightObject?.GetComponent<CabinetBacking>();

            // Left and right measurements are only displyed when a product is
            // selected. When a cabinet is selected, they are hidden.
            m_MeasureLeft.gameObject.SetActive(m_HighlightedProduct);
            m_MeasureRight.gameObject.SetActive(m_HighlightedProduct);

            m_CanMeasureSelectedObject =
                (m_HighlightedCabinet || m_HighlightedProduct) && !m_HighlightedBacking;

            m_MeasureSelected.gameObject.SetActive(m_CanMeasureSelectedObject);

            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        protected void UpdateMeasurements()
        {
            if (!m_HighlightedProduct)
            {
                // Remove the measurement overlays. These are used to keep the
                // labels at a consistent screen size but they can be hidden
                // when no product is selected
                m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureLeft);
                m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureRight);
            }

            if (!m_CanMeasureSelectedObject)
            {
                m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureSelected);
            }

            UpdateMeasurementsForProduct();
            UpdateMeasurementsForCabinet();
        }

        /// <summary>
        ///     Update the position of the 'selected measure' to match the
        ///     selected product (if it can be resized). Also update the
        ///     measures to the left and right products.
        ///     <br /><br />
        ///
        ///     If a product is not selected, this function does nothing.
        /// </summary>
        protected void UpdateMeasurementsForProduct()
        {
            if (!m_HighlightedProduct)
            {
                return;
            }

            OccupiableVolume occupiedVolume = m_HighlightedProduct.occupiedVolume;

            Cabinet cabinet = occupiedVolume as Cabinet;

            float metersToNearestLeft = 0.0f;
            float metersToNearestRight = 0.0f;

            Cuboid selectedProductCutOutCuboidInCabinetSpace =
                m_HighlightedProduct.GetCutOutCuboidInVolumeSpace();

            if (m_MeasureMode == MeasureMode.ToNearestOccupant)
            {
                if (!occupiedVolume.FindNearestOccupantInDirection(m_HighlightedProduct, CabinetLocalDirection.Left, out metersToNearestLeft) &&
                    cabinet != null)
                {
                    metersToNearestLeft =
                        cabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            metersToNearestLeft,
                            CabinetLocalDirection.Left
                        );
                }

                if (!occupiedVolume.FindNearestOccupantInDirection(m_HighlightedProduct, CabinetLocalDirection.Right, out metersToNearestRight) &&
                    cabinet != null)
                {
                    metersToNearestRight =
                        cabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            metersToNearestRight,
                            CabinetLocalDirection.Right
                        );
                }
            }
            else if (m_MeasureMode == MeasureMode.ToCabinetEdge)
            {
                metersToNearestLeft = selectedProductCutOutCuboidInCabinetSpace.xMin - occupiedVolume.occupiableCuboid.xMin;
                metersToNearestRight = occupiedVolume.occupiableCuboid.xMax - selectedProductCutOutCuboidInCabinetSpace.xMax;

                if (cabinet)
                {
                    metersToNearestLeft =
                        cabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            metersToNearestLeft,
                            CabinetLocalDirection.Left
                        );

                    metersToNearestRight =
                        cabinet.AdjustCabinetDistanceToEdgeOfVisualArea(
                            metersToNearestRight,
                            CabinetLocalDirection.Right
                        );
                }
            }
            else
            {
                throw new System.InvalidOperationException("Unhandled measure mode: " + measureMode);
            }

            // Calculate the points for the left measure

            Vector3 measurementLeftStartInVolumeSpace =
                selectedProductCutOutCuboidInCabinetSpace.min;

            if (m_HighlightedProduct.productVisual.info.anchor.IsOnTop())
            {
                // Snap the line renderer to the top of the cabinet
                measurementLeftStartInVolumeSpace.y =
                    Cuboid.NormalizedToPoint(occupiedVolume.resizerCuboid, new Vector3(0.0f, 1.0f, 0.0f)).y +
                    MeasurementLineOffsetFromCabinetInMeters;

                // Center the line renderer along the selected product's depth
                measurementLeftStartInVolumeSpace.z =
                    Mathf.Lerp(selectedProductCutOutCuboidInCabinetSpace.zMin, selectedProductCutOutCuboidInCabinetSpace.zMax, 0.5f);
            }
            else
            {
                // Center the line renderer vertically in the selected
                // product's height
                measurementLeftStartInVolumeSpace.y =
                    Mathf.Lerp(selectedProductCutOutCuboidInCabinetSpace.yMin, selectedProductCutOutCuboidInCabinetSpace.yMax, 0.5f);

                // Offset the measurement line from the cabinet to prevent
                // clipping
                if (m_HighlightedProduct.productVisual.info.anchor.IsOnFront())
                {
                    measurementLeftStartInVolumeSpace.z =
                        -MeasurementLineOffsetFromCabinetInMeters;
                }
                else
                {
                    bool showLineAtFront = m_HighlightedProduct.productVisual.info.anchor.IsOnOrAtFront();

                    float normalizedZ = showLineAtFront ? 0.0f : 1.0f;

                    measurementLeftStartInVolumeSpace.z =
                        Cuboid.NormalizedToPoint(occupiedVolume.resizerCuboid, new Vector3(0.0f, 0.0f, normalizedZ)).z;

                    if (showLineAtFront)
                    {
                        measurementLeftStartInVolumeSpace.z += MeasurementLineOffsetFromCabinetInMeters;
                    }
                    else
                    {
                        measurementLeftStartInVolumeSpace.z -= MeasurementLineOffsetFromCabinetInMeters;
                    }
                }
            }

            Vector3 measurementLeftEndInVolumeSpace = measurementLeftStartInVolumeSpace;
            measurementLeftEndInVolumeSpace.x -= metersToNearestLeft;

            DrawMeasurementLine(
                m_MeasureLeft,
                occupiedVolume,
                measurementLeftStartInVolumeSpace,
                measurementLeftEndInVolumeSpace,
                canvasLabelPivot: new Vector2(0.45f, 0.55f)  // shift the label leftwards and upwards
            );

            // Calculate the points for the right measure

            Vector3 measurementRightStartInVolumeSpace = measurementLeftStartInVolumeSpace;
            measurementRightStartInVolumeSpace.x += selectedProductCutOutCuboidInCabinetSpace.width;

            Vector3 measurementRightEndInVolumeSpace = measurementRightStartInVolumeSpace;
            measurementRightEndInVolumeSpace.x += metersToNearestRight;

            DrawMeasurementLine(
                m_MeasureRight,
                occupiedVolume,
                measurementRightStartInVolumeSpace,
                measurementRightEndInVolumeSpace,
                canvasLabelPivot: new Vector2(0.55f, 0.45f)  // shift the label rightwards and downwards
            );

            // Draw the length of the cabinet shelf (if one is selected)
            if (m_CanMeasureSelectedObject)
            {
                Cuboid shelfCutOut = m_HighlightedProduct.GetCutOutCuboidInVolumeSpace();

                Vector3 measurementSelectedStartInVolumeSpace = Cuboid.NormalizedToPoint(shelfCutOut, new Vector3(0.0f, 1.0f, 0.5f));
                Vector3 measurementSelectedEndInVolumeSpace = Cuboid.NormalizedToPoint(shelfCutOut, new Vector3(1.0f, 1.0f, 0.5f));

                measurementSelectedStartInVolumeSpace.y += MeasurementLineOffsetFromCabinetInMeters;
                measurementSelectedEndInVolumeSpace.y += MeasurementLineOffsetFromCabinetInMeters;

                DrawMeasurementLine(
                    m_MeasureSelected,
                    occupiedVolume,
                    measurementSelectedStartInVolumeSpace,
                    measurementSelectedEndInVolumeSpace,
                    canvasLabelPivot: new Vector2(0.5f, 0.5f)   // center the label
                );
            }
        }

        /// <summary>
        ///     Update the position of the 'selected measure' to match the
        ///     selected cabinet.
        ///     <br /><br />
        ///
        ///     If a cabinet isn't selected, this function does nothing.
        /// </summary>
        protected void UpdateMeasurementsForCabinet()
        {
            if (!m_HighlightedCabinet)
            {
                return;
            }

            // Draw the length of the cabinet
            Cuboid cabinetResizerCuboid = m_HighlightedCabinet.resizerCuboid;

            Vector3 measurementSelectedStartInVolumeSpace =
                Cuboid.NormalizedToPoint(cabinetResizerCuboid, new Vector3(0.0f, 1.0f, 0.5f));

            Vector3 measurementSelectedEndInVolumeSpace =
                Cuboid.NormalizedToPoint(cabinetResizerCuboid, new Vector3(1.0f, 1.0f, 0.5f));

            measurementSelectedStartInVolumeSpace.y += MeasurementLineOffsetFromCabinetInMeters;
            measurementSelectedEndInVolumeSpace.y += MeasurementLineOffsetFromCabinetInMeters;

            DrawMeasurementLine(
                m_MeasureSelected,
                m_HighlightedCabinet,
                measurementSelectedStartInVolumeSpace,
                measurementSelectedEndInVolumeSpace,
                canvasLabelPivot: new Vector2(0.5f, 0.5f)   // center the label
            );
        }

        /// <summary>
        /// Takes a cabinet and two positions in the cabinet's local space,
        /// converts the positions to world space and measures the distance
        /// between them - displaying a text widget with the length in feet and
        /// inches.
        /// </summary>
        /// <param name="measure">
        /// A measure object to use to draw the line and length label.
        /// </param>
        /// <param name="occupiableVolume">
        /// The cabinet (& transform) to use when transfroming the local-space
        /// positions into world-space.
        /// </param>
        /// <param name="fromPositionInVolumeResizerSpace"></param>
        /// <param name="toPositionInVolumeResizerSpace"></param>
        protected void DrawMeasurementLine(Measure measure, OccupiableVolume occupiableVolume, Vector3 fromPositionInVolumeResizerSpace, Vector3 toPositionInVolumeResizerSpace, Vector2 canvasLabelPivot)
        {
            Transform volumeTransform = occupiableVolume.GetTransform();

            // Convert the positions from volume-space to world-space
            Vector3 fromPositionInWorldSpace =
                volumeTransform.TransformPoint(fromPositionInVolumeResizerSpace);

            Vector3 toPositionInWorldSpace =
                volumeTransform.TransformPoint(toPositionInVolumeResizerSpace);

            measure.MeasureFromTo(fromPositionInWorldSpace, toPositionInWorldSpace);

            m_MeasurementOverlayUi.AddOrUpdateMeasurement(measure, canvasLabelPivot);
        }

        protected async UniTask OnCabinetResized(Cabinet cabinet)
        {
            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetAddedByUser(Cabinet cabinet)
        {
            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetReordered((Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans) args)
        {
            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Called when a product is dragged any non-zero distance by the user.
        /// This function updates the measurement lines that show how far the
        /// product is from its left and right neighbours.
        /// </summary>
        /// <param name="product">Not used.</param>
        /// <param name="eventData">Not used.</param>
        protected void OnProductDragged(Occupant product, PointerEventData eventData)
        {
            UpdateMeasurements();
        }

        /// <summary>
        ///     Called when an occupant (a product) is resized. This function
        ///     updates the measurement lines that show how far the product is
        ///     from its left and right neighbours.
        /// </summary>
        /// <param name="product">Not used.</param>
        protected async UniTask OnOccupantResized(Occupant product)
        {
            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Refresh the measurements whenever a product is added to the cabinet.
        /// </summary>
        /// <param name="productInfo">Not used.</param>
        protected async UniTask OnOccupantAdded(Occupant product)
        {
            UpdateMeasurements();

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Refresh the measurements whenever a product is nudged on the
        /// cabinet.
        /// </summary>
        protected void OnOccupantNudged(Occupant product, Vector3 cabinetLocalNudgeDistanceInMeters)
        {
            UpdateMeasurements();
        }

        /// <summary>
        /// Hide the measurements overlays.
        /// </summary>
        protected void OnKitchenRestart()
        {
            m_MeasureLeft.gameObject.SetActive(false);
            m_MeasureRight.gameObject.SetActive(false);
            m_MeasureSelected.gameObject.SetActive(false);

            m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureLeft);
            m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureRight);
            m_MeasurementOverlayUi.RemoveMeasurement(m_MeasureSelected);

            MeasurementUnit units = ProFitGlobals.userProfile.activeUnits;
            m_MeasureLeft.units = units;
            m_MeasureRight.units = units;
            m_MeasureSelected.units = units;

            m_MeasureMode = MeasureMode.ToNearestOccupant;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] protected Measure m_MeasureLeft = default;
        [SerializeField] protected Measure m_MeasureRight = default;
        [SerializeField] protected Measure m_MeasureSelected = default;

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [Space]
        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        protected MeasureOverlayUI m_MeasurementOverlayUi = default;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        protected List<Occupant> m_PossibleBlockers = new List<Occupant>();
        protected GameObject m_HighlightedObject;
        protected Occupant m_HighlightedProduct;
        protected CabinetBacking m_HighlightedBacking;

        /// <summary>
        ///     The <see cref="Cabinet"/> attached to the selected object. This
        ///     can only have a non-null value when Sections building method is
        ///     used which allows the player to select individual cabinets.
        /// </summary>
        protected Cabinet m_HighlightedCabinet;

        /// <summary>
        ///     Whether <see cref="m_HighlightedObject"/> should display a
        ///     measure for its own width. For example, most objects do not need
        ///     a measure displayed unless they are resizable like a <see
        ///     cref="CabinetShelf"/> or a <see cref="Cabinet"/> when <see
        ///     cref="KitchenBuildingMethod.Sections"/> is active.
        /// </summary>
        protected bool m_CanMeasureSelectedObject;

        /// <summary>
        ///     Defines which endpoints to consider for the measurement.
        /// </summary>
        protected MeasureMode m_MeasureMode = MeasureMode.ToNearestOccupant;

        // ====================================================================
        // Types
        // ====================================================================

        public enum MeasureMode
        {
            ToNearestOccupant,
            ToCabinetEdge,
        }

    }   // class MeasurementOverlayManager

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-09
/// ===========================================================================
