namespace KitchenDesign.Photos
{
    using KitchenDesign.Utility;
    using KitchenDesign.Session;
    using KitchenDesign.Serializables;
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using KitchenDesign.Shared.Common;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using KitchenDesign.Geometry;
    using KitchenDesign.Cabinets;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Measures;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;

    public sealed class PlanMeasurePlacement
    {
        private readonly IReadOnlyList<Measure> _measurePool;
        private readonly Color _colorX;
        private readonly Color _colorY;
        private readonly Color _colorZ;
        private readonly SessionStatus _session;

        private int _nextMeasureIndex;

        public PlanMeasurePlacement(IReadOnlyList<Measure> measurePool, Color colorX, Color colorY, Color colorZ, SessionStatus session)
        {
            this._measurePool = measurePool;
            this._colorX = colorX;
            this._colorY = colorY;
            this._colorZ = colorZ;
            this._session = session;
        }

        /// <summary>
        ///     Calculate the line segments used to render the occupant's
        ///     cut-out measurements.
        /// </summary>
        public static void SetupOccupantCutOutMeasurements(Kitchen kitchen, Occupant occupant, MeasurementUnit units)
        {
            // Fetch the cabinet's cut-out mode in case there are any special
            // rules as they will affect the measurements here.
            CabinetCutOutMode cabinetCutOutMode =
                CabinetCutOutMode.Normal;

            if (occupant.cabinet is Cabinet cabinet &&
                cabinet.cabinetSectionVisualPrefab)
            {
                // The countertop cut-out does not go all the way through the
                // overhang and the cabinet's cut-out does not extend all the
                // way to the countertop.
                cabinetCutOutMode = cabinet.cabinetSectionVisual.info.sectionCutOutMode;
            }

            ProductVisualContent productVisual = occupant.productVisualContentInstance;

            Cuboid cutOutCuboidInVolumeSpace =
                occupant.GetCutOutCuboidInVolumeSpace();

            if (!ReferenceEquals(occupant.occupiedVolume, occupant.cabinet))
            {
                cutOutCuboidInVolumeSpace.position +=
                    occupant.occupiedVolume.occupiableCuboid.position;
            }

            occupant.TryGetComponent(out CabinetShelf productAsShelf);

            bool isProductOnOrAtFront = occupant.productVisual.info.anchor.IsOnOrAtFront();

            float normalizedCuboidZ = isProductOnOrAtFront ? 0.0f : 1.0f;

            // Define offsets used to push the measurement labels away from
            // cabinet so they render above everything else
            const float ProductMeasurementOffset = 10.0f * Units.Inches2Meters;
            Vector3 towardsFront = Vector3.back * ProductMeasurementOffset;
            Vector3 towardsBack = -towardsFront;
            Vector3 towardsCamera = isProductOnOrAtFront ? towardsFront : towardsBack;
            Vector3 upward = Vector3.up * ProductMeasurementOffset;

            // Calculate the points for the x-axis cut-out on the front
            LineSegment widthForFrontOrBack =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 0.0f, normalizedCuboidZ),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 0.0f, normalizedCuboidZ)
                );
            // Shift the line slightly into the cut-out box
            const float XLabelMetersFromBottom = 1 * Units.Inches2Meters;
            widthForFrontOrBack.begin.y += XLabelMetersFromBottom;
            widthForFrontOrBack.end.y += XLabelMetersFromBottom;
            // Shift the label closer to the camera
            widthForFrontOrBack.begin += towardsCamera;
            widthForFrontOrBack.end += towardsCamera;

            // Calculate the points for the x-axis cut-out on the top
            LineSegment widthForTop =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 0.0f),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 1.0f, 0.0f)
                );
            if (productAsShelf)
            {
                // Shift the line slightly out of the cut-out box. This avoids
                // the shelf's width measurement from overlapping any nested
                // occupants in the shelf.
                widthForTop.begin.z -= XLabelMetersFromBottom * 3;
                widthForTop.end.z -= XLabelMetersFromBottom * 3;
            }
            else
            {
                // Shift the line slightly into the cut-out box
                widthForTop.begin.z += XLabelMetersFromBottom;
                widthForTop.end.z += XLabelMetersFromBottom;
            }
            // Shift the label closer to the camera
            widthForTop.begin += upward;
            widthForTop.end += upward;

            // Calculate the points for the y-axis cut-out
            LineSegment height =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 0.0f, normalizedCuboidZ),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 1.0f, 1.0f, normalizedCuboidZ)
                );
            if (cabinetCutOutMode == CabinetCutOutMode.StollFixedSmokerCabinet &&
                !occupant.productVisual.info.anchor.IsAtBottom())
            {
                height.end.y -= StollSizes.FixedSmokerCabinetCutOutMetersToKeepBelowCountertop + kitchen.countertopThicknessInMeters;
            }
            // Shift the line slightly into the cut-out box
            const float YLabelMetersFromLeft = 1 * Units.Inches2Meters;
            height.begin.x -= YLabelMetersFromLeft;
            height.end.x -= YLabelMetersFromLeft;
            // Shift the label closer to the camera
            height.begin += towardsCamera;
            height.end += towardsCamera;

            // Calculate the points for the z-axis cut-out
            LineSegment depth =
                new LineSegment(
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 0.0f),
                    PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 1.0f, 1.0f)
                );
            // Shift the line slightly into the cut-out box
            const float ZLabelMetersFromLeft = 1 * Units.Inches2Meters;
            depth.begin.x += ZLabelMetersFromLeft;
            depth.end.x += ZLabelMetersFromLeft;
            // Shift the label closer to the camera
            depth.begin += upward;
            depth.end += upward;
            // If the product is at the top and directly at the front or back,
            // we must include the countertop overhang in the cut-out size
            if (!occupant.productVisual.info.anchor.IsAtBottom() && cabinetCutOutMode == CabinetCutOutMode.Normal)
            {
                if (isProductOnOrAtFront && occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Front))
                {
                    depth.begin.z -= kitchen.baseCountertopOverhangInMeters.towardMinZ;
                }
                else if (occupant.IsCutOutFlushWithVolume(CabinetLocalDirection.Back))
                {
                    depth.end.z += kitchen.baseCountertopOverhangInMeters.towardMaxZ;
                }
            }

            //Vector3 pointA = product.cabinet.GetTransform().TransformPoint(height.begin);
            //Vector3 pointB = product.cabinet.GetTransform().TransformPoint(height.end);
            //Debug.DrawLine(pointA, pointB, Color.black, 5.0f, false);

            // Calculate the label positions
            Vector3 frontOrBackLabelPositionInProductSpace =
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.5f, 0.5f, normalizedCuboidZ) + towardsCamera;

            Vector3 topLabelPositionInProductSpace =
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.5f, 1.0f, 0.5f) + upward;

            productVisual.SetCutOutMeasurementsUnits(units);

            Transform localOccupantOrientationTransform =
                occupant.cabinet.GetLocalOrientationForOccupants();

            Cabinet cab = (Cabinet)occupant.cabinet;
            if (cab.cabinetSectionVisualPrefab)
            {
                localOccupantOrientationTransform =
                    cab.cabinetSectionVisualPrefab.sectionOccupantOrientation;
            }

            productVisual.SetCutOutMeasurementsPositions(
                occupant.productVisual.info,
                occupant.cabinet.GetTransform(),
                localOccupantOrientationTransform,
                PointOnCuboid(cutOutCuboidInVolumeSpace, 0.0f, 0.0f, 0.0f),
                occupant.productInfo.cutOutSizeInMeters,
                widthForFrontOrBack,
                widthForTop,
                height,
                depth,
                frontOrBackLabelPositionInProductSpace,
                topLabelPositionInProductSpace,
                kitchen.countertopThicknessInMeters
            );

            // Show the top cut-out label only when we can be sure it won't
            // overlap with other text. The label has a chance to overlap other
            // text if the product is a shelf and the shelf has occupants.
            bool showTopLabel = !productAsShelf || productAsShelf.occupants.Count <= 0;
            productVisual.SetCutOutInfoTopLabelVisible(showTopLabel);

            // Calculate a point on a cuboid from a normalized
            // coordinate.
            static Vector3 PointOnCuboid(Cuboid cutOutCuboidInVolumeSpace, float x, float y, float z)
            {
                return Cuboid.NormalizedToPoint(cutOutCuboidInVolumeSpace, new Vector3(x, y, z));
            }
        }

        /// <summary>
        ///     Take a list of unique pairs of occupants (that we want to
        ///     place measurements between) and place the actual <see
        ///     cref="Measure"/> prefabs where they need to go. The returned
        ///     list is the pre-placed <see cref="Measure"/>'s with all the
        ///     data needed to filter them by <see cref="ScreenshotAngle"/> and
        ///     <see cref="CabinetSpan"/>.
        /// </summary>
        public List<MeasurementForScreenshot> PopulateMeasuresForScreenshots(
            Kitchen kitchen,
            IReadOnlyCollection<OccupantMeasurementPair> measurementPairs)
        {
            List<MeasurementForScreenshot> measurementsInScreenshots = new();

            // Reset the pooled measures objects
            for (int i = 0; i < this._nextMeasureIndex; ++i)
            {
                this._measurePool[i].gameObject.SetActive(false);
            }
            this._nextMeasureIndex = 0;

            // Define the offsets to shift the cabinet measurement lines to
            // help them render outside of the cabinet.
            const float CabinetMeasurementOffset = 4.0f * Units.Inches2Meters;
            Vector3 leftward    = CabinetMeasurementOffset * Vector3.left;
            Vector3 downward    = CabinetMeasurementOffset * Vector3.down;
            Vector3 towardsBack = CabinetMeasurementOffset * Vector3.forward;
            Vector3 towardsFront = CabinetMeasurementOffset * Vector3.back;
            Vector3 upward      = CabinetMeasurementOffset * Vector3.up;

            // Add measurements for the cabinets
            foreach (CabinetSpan span in kitchen.layout.spans)
            {
                if (span.IsSpace())
                {
                    // No need to measure a cabinet span that is made up of
                    // invisible spacer cabinets.

                    continue;
                }

                // Fetch this span's potential corner wedges for use to resize
                // the front cabinet measurements later.
                span.GetCornerWedges(
                    out Cabinet leftCornerWedge,
                    out Cabinet rightCornerWedge
                );

                CuboidPadding countertopOverhangInMeters =
                    kitchen.baseCountertopOverhangInMeters;

                if (span.backing && !span.backing.hasRiserAndRiserCountertop)
                {
                    // Some backings, such as Low Bar Tops, don't have a riser
                    // or countertop of their own. They modify the size of the
                    // cabinet's countertop directly. This size change is
                    // accounted for here to ensure the measurements properly
                    // line up with the countertop's real size.

                    CuboidPadding backingOverhangDeltaInMeters =
                        span.backing.countertopOverhangInMeters;

                    countertopOverhangInMeters.left         += backingOverhangDeltaInMeters.left;
                    countertopOverhangInMeters.right        += backingOverhangDeltaInMeters.right;
                    countertopOverhangInMeters.top          += backingOverhangDeltaInMeters.top;
                    countertopOverhangInMeters.bottom       += backingOverhangDeltaInMeters.bottom;
                    countertopOverhangInMeters.towardMinZ   += backingOverhangDeltaInMeters.towardMinZ;
                    countertopOverhangInMeters.towardMaxZ   += backingOverhangDeltaInMeters.towardMaxZ;
                }

                Cuboid outerCabinetSpanCuboid =
                    span.GetOutsideCuboidInSpanSpace(includeCountertop: false);

                Transform cabinetSpanTransform =
                    span.orientationAlignedCabinet.GetTransform();

                // Define common points we will be measuring between.
                // (these are in CabinetSpan-space)
                Vector3 outerLeftBottomFront    = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 0.0f, 0.0f));
                Vector3 outerLeftTopFront       = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 1.0f, 0.0f));
                Vector3 outerRightBottomFront   = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 0.0f, 0.0f));
                Vector3 outerLeftTopBack        = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (0.0f, 1.0f, 1.0f));
                Vector3 outerRightTopBack       = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 1.0f, 1.0f));
                Vector3 outerRightTopFront      = Cuboid.NormalizedToPoint(outerCabinetSpanCuboid, new (1.0f, 1.0f, 0.0f));

                if (kitchen.buildingRules.HasCabinetColumns &&
                    span.cabinets.Count > 0)
                {
                    // In Sections building method, we need to account for the
                    // depth of the columns that stick out from the adjoining
                    // perpendicular spans. We can determine whether there are
                    // columns to account for by checking if the first and last
                    // cabinets in the span are corners.

                    if (span.cabinets[0].element.cabinetType == CabinetType.Corner)
                    {
                        outerLeftBottomFront.x  -= countertopOverhangInMeters.towardMinZ;
                        outerLeftTopFront.x     -= countertopOverhangInMeters.towardMinZ;
                        outerLeftTopBack.x      -= countertopOverhangInMeters.towardMinZ;
                    }

                    if (span.cabinets[^1].element.cabinetType == CabinetType.Corner)
                    {
                        outerRightBottomFront.x += countertopOverhangInMeters.towardMinZ;
                        outerRightTopBack.x     += countertopOverhangInMeters.towardMinZ;
                        outerRightTopFront.x    += countertopOverhangInMeters.towardMinZ;
                    }
                }

                // --
                // Front screenshot measurements.
                {
                    // Measure the total width of cabinet span.
                    AddFrontMeasure(
                        span,
                        outerLeftBottomFront,
                        outerRightBottomFront,
                        downward * 2.0f,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        this._colorX,
                        MeasureDisplayFlag.All
                    );

                    // Measure the thickness of the countertop.
                    Vector3 countertopLeftTopFront = outerLeftTopFront;
                    countertopLeftTopFront.x -= countertopOverhangInMeters.left;

                    Vector3 countertopLeftBottomFront = countertopLeftTopFront;
                    countertopLeftBottomFront.y -= kitchen.countertopThicknessInMeters;

                    AddFrontMeasure(
                        span,
                        countertopLeftTopFront,
                        countertopLeftBottomFront,
                        leftward,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        this._colorY,
                        MeasureDisplayFlag.All
                    );

                    // Measure the cabinet's height minus the countertop.
                    Vector3 cabinetLeftTopFront = countertopLeftBottomFront;

                    Vector3 cabinetLeftBottomFront = outerLeftBottomFront;
                    cabinetLeftBottomFront.x -= countertopOverhangInMeters.left;

                    // The countertop currently grows "downward" (i.e. its top
                    // face is always at the same height) which is not correct.
                    // In order to display (some) correct vertical measurements,
                    // the thickness of the countertop is artificially re-added
                    float downwardGrowingCountertopFixDistanceInMeters =
                        kitchen.countertopThicknessInMeters;

                    AddFrontMeasure(
                        span,
                        cabinetLeftTopFront,
                        cabinetLeftBottomFront,
                        leftward,
                        extraDistanceInMetersForLabelOnly: downwardGrowingCountertopFixDistanceInMeters, // intentional, non-zero distance. Ideally will be 0 once countertops grow upwards.
                        this._colorY,
                        MeasureDisplayFlag.All
                    );

                    // Measure the total cabinet height (including countertop).
                    AddFrontMeasure(
                        span,
                        outerLeftTopFront,
                        outerLeftBottomFront,
                        leftward * 3.0f,
                        extraDistanceInMetersForLabelOnly: downwardGrowingCountertopFixDistanceInMeters, // intentional, non-zero distance. Ideally will be 0 once countertops grow upwards.
                        this._colorY,
                        MeasureDisplayFlag.All
                    );
                }

                // --
                // Top screen shots.
                {
                    bool isCabinetWedge = span.IsCornerWedge();

                    Vector3 countertopLeftTopBack = outerLeftTopBack;
                    Vector3 countertopLeftTopFront = outerLeftTopFront;

                    if (!isCabinetWedge)
                    {
                        countertopLeftTopBack.x -= countertopOverhangInMeters.left;
                        countertopLeftTopFront.x -= countertopOverhangInMeters.left;

                        countertopLeftTopBack.z += countertopOverhangInMeters.towardMaxZ;    // overhang back
                    }

                    // All spans (including corner wedges) overhang the front.
                    countertopLeftTopFront.z -= countertopOverhangInMeters.towardMinZ;   // overhang forward

                    Vector3 backPanel = new Vector3(0.0f, 0.0f, kitchen.backPanelDepthInMeters);
                    Vector3 backPanelMeasureOffset = 
                        (CabinetMeasurementOffset * new Vector3(1f, 0f, 0f)) +
                        (Vector3.forward * Units.Inches2Meters);

                    if (kitchen.layout.style == KitchenStyle.Straight ||
                        kitchen.layout.style == KitchenStyle.Galley)
                    {
                        // Cabinet depth.

                        // This measurement is redundant in L and U shape
                        // kitchens and is therefore omitted in those cases.
                        AddTopMeasure(
                            span,
                            countertopLeftTopBack,
                            countertopLeftTopFront,
                            leftward + upward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            this._colorZ,
                            MeasureDisplayFlag.All
                        );

                        if(kitchen.backPanelDepthInMeters > 0.0f)
                        {
                            AddTopMeasure(
                                span,
                                countertopLeftTopBack - backPanelMeasureOffset,
                                countertopLeftTopBack - backPanel - backPanelMeasureOffset,
                                leftward + upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                this._colorZ,
                                MeasureDisplayFlag.All
                            );
                        }
                    }

                    // Measure out the countertop's width
                    Vector3 countertopRightTopBack = outerRightTopBack;
                    if (!isCabinetWedge)
                    {
                        countertopRightTopBack.x += countertopOverhangInMeters.right;

                        countertopRightTopBack.z += countertopOverhangInMeters.towardMaxZ; // overhang back
                    }

                    // Measure the total width of the span.
                    Vector3 countertopWidthMeasureStart = countertopLeftTopBack;
                    Vector3 countertopWidthMeasureEnd = countertopRightTopBack;
                    Vector3 countertopWidthLabelOffset = (towardsBack * 2.0f) + upward;

                    if (span.IsCornerWedge())
                    {
                        // Make the measurement relative to the front of the
                        // cabinet for corner wedge spans.
                        countertopWidthMeasureStart = countertopLeftTopFront;
                        countertopWidthMeasureEnd.z = countertopWidthMeasureStart.z;

                        countertopWidthLabelOffset = (towardsFront * 2.0f) + upward;
                    }

                    AddTopMeasure(
                        span,
                        countertopWidthMeasureStart,
                        countertopWidthMeasureEnd,
                        countertopWidthLabelOffset,
                        extraDistanceInMetersForLabelOnly: 0.0f,
                        this._colorX,
                        MeasureDisplayFlag.All
                    );

                    // --
                    // Measure the width of the span's backing. This is only
                    // needed if there are backings on the neighbouring spans:
                    // I.e., if there is no backings on neighbouring spans, the
                    // current span and its backing will have the same width.
                    // This also does not apply to corner wedge cabinets.

                    if (!isCabinetWedge)
                    {
                        bool doesLeftSpanHaveBacking =
                            span.index > 0 &&
                            kitchen.layout.spans[span.index - 1].backing;

                        bool doesRightSpanHaveBacking =
                            span.index < kitchen.layout.spans.Count - 1 &&
                            kitchen.layout.spans[span.index + 1].backing;

                        if (span.backing && (doesLeftSpanHaveBacking || doesRightSpanHaveBacking))
                        {
                            // TODO: Calculate the left and right overhangs properly
                            //       with respect to low bar tops. -Ryan

                            // The countertop's cuboid represents the internal part
                            // of the countertop only. This means we must manually
                            // add the overhang.
                            Cuboid backingCountertopCuboidInSpanSpace =
                                span.backing.GetCountertopCuboidInSpanSpace(span.GetOutsideCuboidInSpanSpace());

                            backingCountertopCuboidInSpanSpace.xMin -= kitchen.baseCountertopOverhangInMeters.left;
                            backingCountertopCuboidInSpanSpace.xMax += kitchen.baseCountertopOverhangInMeters.right;
                            backingCountertopCuboidInSpanSpace.zMin -= kitchen.baseCountertopOverhangInMeters.towardMinZ;
                            backingCountertopCuboidInSpanSpace.zMax += kitchen.baseCountertopOverhangInMeters.towardMaxZ;

                            AddTopMeasure(
                                span,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new (0.0f, 1.0f, 1.0f)),
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new (1.0f, 1.0f, 1.0f)),
                                upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                this._colorX,
                                MeasureDisplayFlag.All
                            );
                        }

                        // Measure the depth of the backing.
                        if (span.backing && span.backing.hasRiserAndRiserCountertop)
                        {
                            Cuboid backingCountertopCuboidInSpanSpace =
                                span.backing.GetCountertopCuboidInSpanSpace(span.GetOutsideCuboidInSpanSpace());

                            AddTopMeasure(
                                span,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new(0.5f, 1.0f, 0.0f)) + kitchen.baseCountertopOverhangInMeters.towardMaxZ * Vector3.back,
                                Cuboid.NormalizedToPoint(backingCountertopCuboidInSpanSpace, new(0.5f, 1.0f, 1.0f)) + kitchen.baseCountertopOverhangInMeters.towardMinZ * Vector3.forward,
                                upward,
                                extraDistanceInMetersForLabelOnly: 0.0f,
                                this._colorZ,
                                MeasureDisplayFlag.All
                            );
                        }
                    }
                }

                int cabinetCountInSpan = span.cabinets.Count;
                for (int i = 0; i < span.cabinets.Count; i++)
                {
                    Cabinet cabinet = span.cabinets[i].element;

                    kitchen.layout.GetCabinetNeighbours(
                        cabinet,
                        out Cabinet leftCabinet,
                        out Cabinet rightCabinet
                    );

                    Cuboid cabinetCuboid = cabinet.resizerCuboid;

                    Vector3 cabinetLeftBottomFront  = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 0.0f));
                    Vector3 cabinetRightBottomFront = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 0.0f, 0.0f));

                    Vector3 cabinetLeftTopBack      = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 1.0f, 1.0f));
                    Vector3 cabinetRightTopBack     = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 1.0f));

                    if (cabinet.cabinetType == CabinetType.Corner &&
                        !cabinet.IsYawSimilar(span.orientationAlignedCabinet))
                    {
                        cabinetLeftBottomFront  = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 1.0f));
                        cabinetRightBottomFront = Cuboid.NormalizedToPoint(cabinetCuboid, new (0.0f, 0.0f, 0.0f));

                        cabinetLeftTopBack      = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 1.0f));
                        cabinetRightTopBack     = Cuboid.NormalizedToPoint(cabinetCuboid, new (1.0f, 1.0f, 0.0f));
                    }

                    if (kitchen.buildingRules.CabinetsAreProducts)
                    {
                        // Make the measurements relative to the span's
                        // main transform. This is necessary because
                        // `AddFrontMeasure` and `AddTopMeasure` assume the
                        // measurement positions are relative to the
                        // orientation aligned cabinet. If we don't transform
                        // the points to the span's main cabinet, the
                        // measurements will not properly offset from the
                        // cabinet's columns/sections.

                        cabinetLeftBottomFront =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetLeftBottomFront,
                                span.orientationAlignedCabinet
                            );

                        cabinetLeftTopBack =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetLeftTopBack,
                                span.orientationAlignedCabinet
                            );

                        cabinetRightBottomFront =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetRightBottomFront,
                                span.orientationAlignedCabinet
                            );

                        cabinetRightTopBack =
                            OccupiableVolume.TransformPositionBetweenVolumes(
                                cabinet,
                                cabinetRightTopBack,
                                span.orientationAlignedCabinet
                            );
                    }

                    if (kitchen.buildingRules.HasCabinetColumns)
                    {
                        // In Stoll, the cabinet dimensions should be measured
                        // from between the mullions.

                        float halfColumnWidthInMeters =
                            KitchenSizes.CabinetColumnWidthInMeters * 0.5f;

                        if (!leftCabinet || !leftCabinet.Is(CabinetType.HighLeftToLowRight))
                        {
                            cabinetLeftBottomFront.x    += halfColumnWidthInMeters;
                            cabinetLeftTopBack.x        += halfColumnWidthInMeters;
                        }

                        if (!rightCabinet || !rightCabinet.Is(CabinetType.LowLeftToHighRight))
                        {
                            cabinetRightBottomFront.x   -= halfColumnWidthInMeters;
                            cabinetRightTopBack.x       -= halfColumnWidthInMeters;
                        }
                    }

                    if (kitchen.buildingRules.UsesBrandCatalog &&
                        !cabinet.cabinetType.IsEnd() &&
                        i < cabinetCountInSpan - 1 &&
                        !span.cabinets[i + 1].element.cabinetType.IsEnd())
                    {
                        Vector3 cabinetRightTopFront = cabinetRightTopBack;
                        cabinetRightTopFront.z = cabinetRightBottomFront.z;

                        // Add separating lines between cabinets in Catalog mode.
                        AddFrontMeasure(
                            span,
                            cabinetRightBottomFront,
                            cabinetRightTopFront,
                            Vector3.zero,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            Color.black,
                            MeasureDisplayFlag.Line
                        );

                        // Add separating lines between cabinets in Catalog mode.
                        AddTopMeasure(
                            span,
                            cabinetRightTopBack,
                            cabinetRightTopFront,
                            Vector3.zero,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            Color.black,
                            MeasureDisplayFlag.Line
                        );
                    }

                    if (cabinet.cabinetType == CabinetType.Corner)
                    {
                        // Skip corners because they are not something the user
                        // can resize. If we ever need to draw their
                        // measurements, we need to transform the corner's
                        // coordinates correctly: the leading corner in a
                        // cabinet span uses a different rotation from the rest.
                        continue;
                    }

                    if (cabinet.cabinetType == CabinetType.CornerWedge)
                    {
                        // Skip corner wedges because they are a span made up
                        // of only one cabinet. Therefore, their measurements
                        // are already visible from the earlier span metrics.
                        continue;
                    }


                    if (kitchen.buildingRules.MeasuresOnlyProductCabinets)
                    {
                        if (cabinet.cabinetType.IsEnd(includeTransitions: true) ||
                            cabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                            cabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                        {
                            // In Sections building method, we want to skip
                            // measuring these cabinets. These cabinets normally
                            // don't exist or have no width, but that is not the
                            // case in Sections mode.
                            continue;
                        }
                    }

                    Transform cabinetTransform = cabinet.GetTransform();

                    if (cabinetCuboid.width > 0.0f)
                    {
                        Vector3 topDownMeasureDirection = towardsBack;

                        Vector3 topDownWidthMeasureStart = cabinetLeftTopBack;
                        Vector3 topDownWidthMeasureEnd = cabinetRightTopBack;

                        if (leftCabinet != null && leftCabinet.Is(CabinetType.Corner) && leftCornerWedge != null)
                        {
                            Cabinet wedgedCornerCabinet = span.cabinets[0].element;
                            MelcherLog.Assert(wedgedCornerCabinet.Is(CabinetType.Corner));

                            float wedgeOrthogonalLength =
                                CabinetCornerWedgeMath.CalculateOrthogonalLength(leftCornerWedge);

                            cabinetLeftBottomFront.x += wedgeOrthogonalLength;
                            topDownWidthMeasureStart.x += wedgeOrthogonalLength;
                            topDownWidthMeasureStart.z = cabinetLeftBottomFront.z;
                            topDownWidthMeasureEnd.z = topDownWidthMeasureStart.z;

                            topDownMeasureDirection = towardsFront;
                        }

                        if (rightCabinet != null && rightCabinet.Is(CabinetType.Corner) && rightCornerWedge != null)
                        {
                            Cabinet wedgedCornerCabinet = span.cabinets[^1].element;
                            MelcherLog.Assert(wedgedCornerCabinet.Is(CabinetType.Corner));

                            float wedgeOrthogonalLength =
                                CabinetCornerWedgeMath.CalculateOrthogonalLength(rightCornerWedge);

                            cabinetRightBottomFront.x -= wedgeOrthogonalLength;
                            topDownWidthMeasureEnd.x -= wedgeOrthogonalLength;
                            topDownWidthMeasureEnd.z = cabinetRightBottomFront.z;
                            topDownWidthMeasureStart.z = topDownWidthMeasureEnd.z;

                            topDownMeasureDirection = towardsFront;
                        }

                        // Measure the cabinet's width viewed from the front.
                        AddFrontMeasure(
                            span,
                            cabinetLeftBottomFront,
                            cabinetRightBottomFront,
                            downward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            this._colorX,
                            MeasureDisplayFlag.All
                        );

                        if (topDownMeasureDirection != towardsFront)
                        {
                            // Measure the cabinet's width viewed from the top.
                            topDownWidthMeasureStart.z += countertopOverhangInMeters.towardMaxZ;
                            topDownWidthMeasureEnd.z += countertopOverhangInMeters.towardMaxZ;
                        }

                        AddTopMeasure(
                            span,
                            topDownWidthMeasureStart,
                            topDownWidthMeasureEnd,
                            topDownMeasureDirection + upward,
                            extraDistanceInMetersForLabelOnly: 0.0f,
                            this._colorX,
                            MeasureDisplayFlag.All
                        );
                    }
                }

                foreach (OccupantMeasurementPair pair in measurementPairs)
                {
                    AddInterOccupantMeasures(pair, span);
                }

                /// <param name="point1">
                ///     A position that is relative to the span's orientation-
                ///     aligned cabinet.
                /// </param>
                /// <param name="point2">
                ///     A position that is relative to the span's orientation-
                ///     aligned cabinet.
                /// </param>
                void AddFrontMeasure(
                    CabinetSpan span,
                    Vector3     point1,
                    Vector3     point2,
                    Vector3     pointOffset,
                    float       extraDistanceInMetersForLabelOnly,
                    Color       color,
                    MeasureDisplayFlag displayFlags)
                {
                    AddMeasureBetweenCabinetPoints(
                        ScreenshotAngle.Front,
                        span.orientationAlignedCabinet.GetTransform(),
                        point1 + pointOffset,
                        point2 + pointOffset,
                        extraDistanceInMetersForLabelOnly,
                        span,
                        color,
                        displayFlags,
                        measurementsInScreenshots
                    );
                }

                void AddTopMeasure(
                    CabinetSpan span,
                    Vector3     point1,
                    Vector3     point2,
                    Vector3     pointOffset,
                    float       extraDistanceInMetersForLabelOnly,
                    Color       color,
                    MeasureDisplayFlag displayFlags)
                {
                    AddMeasureBetweenCabinetPoints(
                        ScreenshotAngle.Top,
                        span.orientationAlignedCabinet.GetTransform(),
                        point1 + pointOffset,
                        point2 + pointOffset,
                        extraDistanceInMetersForLabelOnly,
                        span,
                        color,
                        displayFlags,
                        measurementsInScreenshots
                    );
                }
            }

            return measurementsInScreenshots;

            /// <summary>
            ///     Given a measurement pair, generate the <see
            ///     cref="MeasurementForScreenshot"/>s for each camera angle the
            ///     measurement should be visible.
            /// </summary>
            void AddInterOccupantMeasures(
                OccupantMeasurementPair measurementPair,
                CabinetSpan             span)
            {
                if (kitchen.AreProductsForcedToCenterOfCabinets((Cabinet)measurementPair.occupantA.cabinet))
                {
                    // Do not show measurements between products when in
                    // Sections building method (aka. for Stoll).
                    return;
                }

                CabinetSpan cabinetSpan =
                    kitchen.layout.FindCabinetSpan((Cabinet)measurementPair.occupantA.cabinet);

                if (cabinetSpan != span)
                {
                    // This measurement is for a different span. It will be
                    // processed later when the other cabinet span is
                    // requested.
                    return;
                }

                Vector3 directionVector = measurementPair.directionAtoB.ToVector3();

                Cabinet cabinet = (Cabinet)measurementPair.occupantA.cabinet;

                Cuboid cabinetCuboid = new Cuboid(cabinet.sizeInMeters);

                // Get the first occupant's cut-out relative to the cabinet it
                // belongs to. The occupant may be nested in a shelf so this
                // transformation is necessary.
                Cuboid occupantCuboidA = measurementPair.occupantA.GetCutOutCuboidInVolumeSpace();
                if (!ReferenceEquals(measurementPair.occupantA.occupiedVolume, measurementPair.occupantA.cabinet))
                {
                    occupantCuboidA.position +=
                        measurementPair.occupantA.occupiedVolume.occupiableCuboid.position;
                }

                OrientedBounds a =
                    new(
                        new Bounds(occupantCuboidA.center, occupantCuboidA.size),
                        measurementPair.occupantA.cabinet.GetTransform().worldToLocalMatrix
                    );
                a.Draw(Color.red, 2.0f);

                MeasurementAxis axis = measurementPair.directionAtoB.ToAxis();

                if (measurementPair.occupantB)
                {
                    // This is a measurement between two occupants

                    Cuboid occupantCuboidB =
                        measurementPair.occupantB.GetCutOutCuboidInVolumeSpace();

                    if (!ReferenceEquals(measurementPair.occupantB.occupiedVolume, measurementPair.occupantB.cabinet))
                    {
                        occupantCuboidB.position +=
                            measurementPair.occupantB.occupiedVolume.occupiableCuboid.position;
                    }

                    OrientedBounds b =
                        new(
                            new Bounds(occupantCuboidB.center, occupantCuboidB.size),
                            measurementPair.occupantB.cabinet.GetTransform().worldToLocalMatrix
                        );
                    b.Draw(Color.green, 2.0f);

                    //Debug.DrawLine(a.min, b.min, Color.magenta, 2.0f);
                    //Debug.DrawLine(Vector3.zero + Vector3.one, a.min, Color.magenta, 2.0f);

                    AddMeasureBetweenOccupants(
                        measurementPair.screenshotAngle,
                        occupantCuboidA,
                        occupantCuboidB,
                        measurementPair.directionAtoB,
                        measurementPair.extraDistanceInMetersForLabelOnly,
                        span
                    );
                }
                else
                {
                    // This is a measurement between one occupant and the
                    // outside of the cabinet

                    Color lineColor = AxisToColor(axis);
                    Vector3 start = default;
                    Vector3 end = default;

                    switch (measurementPair.screenshotAngle)
                    {
                        default:
                            MelcherLog.LogError($"Unhandled {nameof(ScreenshotAngle)}: {measurementPair.screenshotAngle}");
                            return;

                        case ScreenshotAngle.Front:
                            end = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: false);
                            start = end + directionVector * measurementPair.distanceInMeters;
                            break;

                        case ScreenshotAngle.Back:
                            end = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: true);
                            start = end + directionVector * measurementPair.distanceInMeters;
                            break;

                        case ScreenshotAngle.Top:
                            start = GetPointOnCuboidFaceInDirection(occupantCuboidA, measurementPair.directionAtoB, anchorToBackFace: null);

                            // Align the points to the cabinet's top
                            start.y = cabinetCuboid.yMax;

                            if (axis == MeasurementAxis.X)
                            {
                                // Center the line within the occupant's depth
                                start.z = occupantCuboidA.center.z;
                            }

                            end = start + directionVector * measurementPair.distanceInMeters;

                            if (measurementPair.occupantA.occupiedVolume.parentVolume == null)
                            {
                                // This product directly occupies a cabinet
                                // (i.e., it is not in a shelf) and therefore
                                // we need to account for the overhang of the
                                // countertop.

                                if (axis == MeasurementAxis.X)
                                {
                                    // Adjust the width by the countertop's overhang
                                    kitchen.layout.GetCabinetNeighbours(cabinet, out Cabinet leftCabinet, out Cabinet rightCabinet);
                                    if (measurementPair.directionAtoB == CabinetLocalDirection.Left)
                                    {
                                        if (leftCabinet != null && leftCabinet.cabinetType != CabinetType.EndLeft)
                                        {
                                            // We have a left neighbour. Contract to accommodate
                                            // its overhang towards us
                                            end -= directionVector * kitchen.baseCountertopOverhangInMeters.right;
                                        }
                                        else
                                        {
                                            // No left neighbour. Extend leftwards to include the overhang
                                            end += directionVector * kitchen.baseCountertopOverhangInMeters.left;
                                        }
                                    }
                                    else if (measurementPair.directionAtoB == CabinetLocalDirection.Right)
                                    {
                                        if (rightCabinet != null && rightCabinet.cabinetType != CabinetType.EndRight)
                                        {
                                            // We have a right neighbour. Contract to accommodate
                                            // its overhang towards us
                                            end -= directionVector * kitchen.baseCountertopOverhangInMeters.left;
                                        }
                                        else
                                        {
                                            // No right neighbour. Extend rightwards to include the overhang
                                            end += directionVector * kitchen.baseCountertopOverhangInMeters.right;
                                        }
                                    }
                                }
                                else
                                {
                                    CuboidPadding countertopOverhang =
                                        cabinet.GetCountertopOverhangInMeters();

                                    // Z-axis. Extend the end point to the overhang
                                    if (measurementPair.directionAtoB == CabinetLocalDirection.Front)
                                    {
                                        end += directionVector * countertopOverhang.towardMinZ;
                                    }
                                    else
                                    {
                                        end += directionVector * countertopOverhang.towardMaxZ;
                                    }
                                }
                            }

                            break;
                    }

                    // Add a measurement that is visible from the front
                    AddMeasureBetweenCabinetPoints(
                        measurementPair.screenshotAngle,
                        cabinet.GetTransform(),
                        start,
                        end,
                        measurementPair.extraDistanceInMetersForLabelOnly,
                        span,
                        lineColor,
                        MeasureDisplayFlag.All,
                        measurementsInScreenshots
                    );
                }

                /// <summary>
                ///     Find the points to draw a measure line between for two
                ///     occupant's cut-outs, then generate an <see
                ///     cref="MeasurementForScreenshot"/> with those points and
                ///     add it to the enclosing function's inter-occupant
                ///     measure list.
                /// </summary>
                /// <param name="screenshotAngle">
                ///     The screenshot type where this measure will be shown.
                /// </param>
                /// <param name="mainCutOutCuboid">
                ///     The cut-out cuboid of the first occupant.
                /// </param>
                /// <param name="otherCutOutCuboid">
                ///     The cut-out cuboid of the second occupant.
                /// </param>
                /// <param name="directionToOtherCuboid">
                ///     The direction from <pararef name="mainCutOutCuboid"/> to
                ///     <paramref name="otherCutOutCuboid"/>.
                /// </param>
                /// <param name="cabinet">
                ///     Must not be <see langword="null"/>. It binds the measure
                ///     to a specific cabinet span so it can be selectively
                ///     rendered when needed.
                /// </param>
                void AddMeasureBetweenOccupants(
                    ScreenshotAngle         screenshotAngle,
                    Cuboid                  mainCutOutCuboid,
                    Cuboid                  otherCutOutCuboid,
                    CabinetLocalDirection   directionToOtherCuboid,
                    float                   extraDistanceInMetersForLabelOnly,
                    CabinetSpan             cabinetSpan)
                {
                    // Determine whether the measure's should be placed along
                    // the back of the product (the screenshot is from the back)
                    // or placed along the product's front (the default).
                    bool anchorToBackFace = screenshotAngle == ScreenshotAngle.Back;

                    Vector3 start = GetPointOnCuboidFaceInDirection(mainCutOutCuboid, directionToOtherCuboid, anchorToBackFace);
                    Vector3 end = GetPointOnCuboidFaceInDirection(otherCutOutCuboid, directionToOtherCuboid.Reverse(), anchorToBackFace);

                    switch (axis)
                    {
                        default:
                            throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(MeasurementAxis)}: {axis}");

                        case MeasurementAxis.X:
                            if (screenshotAngle == ScreenshotAngle.Top)
                            {
                                CenterWithinCuboidOverlap(MeasurementAxis.Z);
                                float cabinetHeight = cabinetSpan.orientationAlignedCabinet.sizeInMeters.y;
                                start.y = cabinetHeight;
                                end.y = cabinetHeight;
                            }
                            else
                            {
                                CenterWithinCuboidOverlap(MeasurementAxis.Y);
                            }
                            break;

                        case MeasurementAxis.Y:
                        case MeasurementAxis.Z:
                            CenterWithinCuboidOverlap(MeasurementAxis.X);
                            break;
                    }

                    Color lineColor = AxisToColor(axis);

                    AddMeasureBetweenCabinetPoints(
                        screenshotAngle,
                        cabinet.GetTransform(),
                        start,
                        end,
                        extraDistanceInMetersForLabelOnly,
                        cabinetSpan,
                        lineColor,
                        MeasureDisplayFlag.All,
                        measurementsInScreenshots
                    );

                    void CenterWithinCuboidOverlap(MeasurementAxis axisToAdjust)
                    {
                        int channel = (int)axisToAdjust;

                        float largestMin = Mathf.Max(mainCutOutCuboid.min[channel], otherCutOutCuboid.min[channel]);
                        float smallestMax = Mathf.Min(mainCutOutCuboid.max[channel], otherCutOutCuboid.max[channel]);

                        // Center the measure along the axis
                        float mid = Mathf.Lerp(largestMin, smallestMax, 0.5f);
                        start[channel] = mid;
                        end[channel] = mid;
                    }
                }

                /// <summary>
                ///     Given a cuboid and a cabinet direction, find a point on
                ///     the edge of that cuboid that is in the direction specified.
                /// </summary>
                /// <param name="cuboid"></param>
                /// <param name="direction"></param>
                /// <param name="anchorToBackFace">
                ///     Whether the measure should be placed on the side of the
                ///     product closest to the back.
                ///     This value is ignored if <paramref name="direction"/> is
                ///     along the <see cref="MeasurementAxis.Z"/> axis.
                /// </param>
                /// <returns></returns>
                Vector3 GetPointOnCuboidFaceInDirection(Cuboid cuboid, CabinetLocalDirection direction, bool? anchorToBackFace)
                {
                    Vector3 normalizedPoint = default;

                    float normalizedZ = anchorToBackFace.GetValueOrDefault() ? 1.0f : 0.0f;

                    switch (direction)
                    {
                        default:
                            throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(CabinetLocalDirection)}: {direction}");

                        case CabinetLocalDirection.Left:
                            // Left edge, vertically centered
                            normalizedPoint = new Vector3(0.0f, 0.5f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Right:
                            // Right edge, vertically centered
                            normalizedPoint = new Vector3(1.0f, 0.5f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Up:
                            // Top face, horizontally centered
                            normalizedPoint = new Vector3(0.5f, 1.0f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Down:
                            // Bottom face, horizontally centered
                            normalizedPoint = new Vector3(0.5f, 0.0f, normalizedZ);
                            break;

                        case CabinetLocalDirection.Front:
                            // Front face, horizontally centered, and at top.
                            // This intentionally does not use normalizedZ
                            normalizedPoint = new Vector3(0.5f, 1.0f, 0.0f);
                            break;

                        case CabinetLocalDirection.Back:
                            // Back face, horizontally centered, and at top.
                            // This intentionally does not use normalizedZ
                            normalizedPoint = new Vector3(0.5f, 1.0f, 1.0f);
                            break;
                    }

                    return Cuboid.NormalizedToPoint(cuboid, normalizedPoint);
                }
            }
        }

        /// <summary>
        ///     This is the base function to add any between-occupant
        ///     measurements. It works with points specified in the
        ///     cabinet's local space.
        /// </summary>
        /// <param name="screenshotAngle">
        ///     The screenshot type where this measure will be shown.
        /// </param>
        /// <param name="fromPointInInputSpace">
        ///     The position to measure from in cabinet-space.
        /// </param>
        /// <param name="toPointInInputSpace">
        ///     The position to measure to in cabinet-space.
        /// </param>
        /// <param name="cabinet">
        ///     Must not be <see langword="null"/>. It binds the measure to
        ///     a specific cabinet span so it can be selectively rendered when
        ///     needed.
        /// </param>
        private void AddMeasureBetweenCabinetPoints(
            ScreenshotAngle screenshotAngle,
            Transform       pointInputSpace,
            Vector3         fromPointInInputSpace,
            Vector3         toPointInInputSpace,
            float           extraDistanceInMetersForLabelOnly,
            CabinetSpan     cabinetSpan,
            Color           lineColor,
            MeasureDisplayFlag    displayFlags,
            List<MeasurementForScreenshot> measurementsForScreenshots)
        {
            if (this._nextMeasureIndex >= this._measurePool.Count)
            {
                // Ran out of pooled objects
                MelcherLog.LogWarning($"{nameof(PlanMeasurePlacement)} ran out of pooled {nameof(Measure)} for between-product measurements");
                return;
            }

            // Convert the screenshot angle to an offset that adjusts
            // the measure to be closer to the camera for the angle
            Vector3 cabinetLocalDirectionToCamera = default;
            Vector3 cameraWorldUp = Vector3.up;

            switch (screenshotAngle)
            {
                default:
                    throw new System.ArgumentOutOfRangeException($"Unhandled {nameof(ScreenshotAngle)}: {screenshotAngle}");

                case ScreenshotAngle.Top:
                    cabinetLocalDirectionToCamera = Vector3.up;
                    cameraWorldUp = Vector3.forward;
                    break;

                case ScreenshotAngle.Front:
                    // Intentional Vector3.back for ScreenshotAngle.Front
                    cabinetLocalDirectionToCamera = Vector3.back;
                    break;

                case ScreenshotAngle.Back:
                    // Intentional Vector3.forward for ScreenshotAngle.Back
                    cabinetLocalDirectionToCamera = Vector3.forward;
                    break;
            }

            // How far to shift the measures away from the cabinet (towards
            // the camera). This is meant to prevent the measure's label or
            // line clipping into the cabinet.
            const float MeasureOffsetFromCabinetInMeters = 2.0f * Units.Inches2Meters;

            Vector3 offset = cabinetLocalDirectionToCamera * MeasureOffsetFromCabinetInMeters;
            fromPointInInputSpace += offset;
            toPointInInputSpace += offset;

            Vector3 worldFrom   = pointInputSpace.TransformPoint(fromPointInInputSpace);
            Vector3 worldTo     = pointInputSpace.TransformPoint(toPointInInputSpace);
            Vector3 worldDirectionToCamera = pointInputSpace.TransformDirection(cabinetLocalDirectionToCamera);

            Measure measure = this._measurePool[this._nextMeasureIndex];
            measure.units = this._session.UserProfile.activeUnits;
            measure.transform.LookAt(measure.transform.position - worldDirectionToCamera, cameraWorldUp);
            measure.color = lineColor;
            measure.SetDisplayFlags(displayFlags);
            ++this._nextMeasureIndex;
            measure.SetExtraDistanceForLabelOnly(extraDistanceInMetersForLabelOnly);
            measure.MeasureFromTo(worldFrom, worldTo);

            measurementsForScreenshots.Add(
                new MeasurementForScreenshot()
                {
                    measure = measure,
                    screenshotAngle = screenshotAngle,
                    cabinetSpan = cabinetSpan
                }
            );
        }

        private Color AxisToColor(MeasurementAxis axis)
        {
            Color color = Color.black;

            switch (axis)
            {
                case MeasurementAxis.X:
                    color = this._colorX;
                    break;

                case MeasurementAxis.Y:
                    color = this._colorY;
                    break;

                case MeasurementAxis.Z:
                    color = this._colorZ;
                    break;
            }

            return color;
        }
    }
}
