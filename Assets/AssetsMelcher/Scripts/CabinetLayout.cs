namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>
    ///     This class manages the list of cabinets that belongs to a kitchen.
    ///     The order and types of cabinets determine its overall appearance.
    ///     E.g., the number of corners and ends dictates how many <see
    ///     cref="CabinetSpan"/>s the cabinet has.
    /// </summary>
    public class CabinetLayout : MelcherMonoBehaviour
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public KitchenStyle style => m_Style;

        public string displayName => m_DisplayName;

        public float designerCameraStartYaw => m_DesignerCameraStartYaw;

        public float saveCoverCameraYaw => m_SaveCoverCameraYaw;

        public float saveTopDownCameraRoll => m_SaveTopDownCameraRoll;

        public ReadOnlyCollection<CabinetSpan> spans
        {
            get
            {
                if (m_CabinetSpansNeedsRebuild)
                {
                    RebuildSpans(EnumerateCabinets(), m_CabinetSpans.mutable);

                    m_CabinetSpansNeedsRebuild = false;
                }

                return m_CabinetSpans.readOnly;
            }
        }

        public IKitchen kitchen => m_Kitchen;

        // =====================================================================
        // Methods
        // =====================================================================

        public Transform GetTransform()
        {
            return m_Transform;
        }

        public int CountCabinets()
        {
            int cabinetCount = 0;

            foreach (ElementIndex<Cabinet> cabinet in EnumerateCabinets())
            {
                ++cabinetCount;
            }

            return cabinetCount;
        }

        public IEnumerable<ElementIndex<Cabinet>> EnumerateCabinets()
        {
            ComponentsInChildrenEnumerator<Cabinet> enumerator =
                new (m_Transform);

            int currentCabinetIndex = -1;

            while (enumerator.MoveNext())
            {
                ++currentCabinetIndex;

                Cabinet cabinet = enumerator.Current;

                yield return ElementIndex.From(cabinet, currentCabinetIndex);
            }
        }

        public IEnumerable<ElementIndex<Cabinet>> EnumerateCabinetsInSpan(int spanIndex)
        {
            if (spanIndex < 0 || spanIndex >= m_CabinetSpans.readOnly.Count)
            {
                throw new System.ArgumentOutOfRangeException($"Given index {spanIndex}, but collection size is {m_CabinetSpans.readOnly.Count} elements", nameof(spanIndex));
            }

            CabinetSpan cabinetSpan = spans[spanIndex];

            foreach (ElementIndex<Cabinet> currentCabinet in EnumerateCabinets())
            {
                if (cabinetSpan.Contains(currentCabinet.index))
                {
                    yield return currentCabinet;
                }
            }
        }

        public void SetKitchen(IKitchen kitchen)
        {
            m_Kitchen = kitchen;

            foreach (ElementIndex<Cabinet> currentCabinet in EnumerateCabinets())
            {
                currentCabinet.element.SetKitchen(kitchen);
            }
        }

        public async UniTask SetCabinetMaterials(
            Material cabinetMaterial,
            Material countertopMaterial,
            Material productCabinetMaterialIdentifier,
            Material cutOutMaterialOverride = null)
        {
            MelcherLog.LogEditorOnly($"Changing cabinet materials: {cabinetMaterial} | {countertopMaterial}", this);

            foreach (ElementIndex<Cabinet> i in EnumerateCabinets())
            {
                Cabinet cabinet = i.element;

                await cabinet.SetCabinetMaterials(
                    cabinetMaterial,
                    countertopMaterial,
                    productCabinetMaterialIdentifier,
                    cutOutMaterialOverride
                );
            }
        }

        public Cabinet GetCabinetAtIndex(int requestedIndex)
        {
            Cabinet cabinetAtIndex = null;

            foreach (ElementIndex<Cabinet> cabinet in EnumerateCabinets())
            {
                if (cabinet.index == requestedIndex)
                {
                    cabinetAtIndex = cabinet.element;
                    break;
                }
            }

            return cabinetAtIndex;
        }

        public int FindCabinetIndex(Cabinet cabinet)
        {
            int cabinetIndex = -1;

            foreach (ElementIndex<Cabinet> i in EnumerateCabinets())
            {
                if (i.element == cabinet)
                {
                    cabinetIndex = i.index;
                    break;
                }
            }

            return cabinetIndex;
        }

        public int FindCabinetSpanIndex(Cabinet cabinet)
        {
            int cabinetIndex = FindCabinetIndex(cabinet);

            return cabinetIndex >= 0 ? FindCabinetSpanIndex(cabinetIndex) : -1;
        }

        public int FindCabinetSpanIndex(int cabinetIndex)
        {
            int currentSpanIndex = -1;

            foreach (CabinetSpan span in spans)
            {
                ++currentSpanIndex;

                if (span.Contains(cabinetIndex))
                {
                    return currentSpanIndex;
                }
            }

            return -1;
        }

        /// <summary>
        ///     A cabinet is possibly reordable if it is a straight section
        ///     (or blind corner) and it has at least one immediate neighbour
        ///     that is also possibly reorderable. This second condition is to
        ///     ensure that the kitchen never completely loses one side/branch.
        ///     <br /><br />
        ///
        ///     "Possibly reorderable" means the cabinet's type allows it to
        ///     be reordered, however not all conditions have been checked.
        ///     For example, there may not be a valid destination for the
        ///     cabinet if moved one direction versus the other.
        /// </summary>
        public bool CabinetHasMidSpanCabinetNeighbour(Cabinet cabinet)
        {
            if (cabinet.kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog)
            {
                if (!cabinet.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
                {
                    return false;
                }
            }
            else if (cabinet.cabinetType != CabinetType.Straight)
            {
                return false;
            }

            CabinetSpan span = FindCabinetSpan(cabinet);

            int midSpanCabinetCount = 0;

            foreach (ElementIndex<Cabinet> i in span.cabinets)
            {
                Cabinet cabinetInSpan = i.element;

                if (cabinetInSpan.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
                {
                    ++midSpanCabinetCount;
                }

                if (midSpanCabinetCount > 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Check whether an existing cabinet in the layout can be replaced
        ///     with a cabinet whose product info is <paramref
        ///     name="possibleReplacementCabinetInfo"/>. This method is
        ///     intended to be used when a new kitchen is created in
        ///     <see cref="KitchenBuildingMethod.Catalog"/> building method
        ///     where the default cabinet sections need to be replaced but the
        ///     admin-provided cabinet sections are not compatible with the
        ///     kitchen's existing layout. I.e., adding two blind corners to
        ///     the samer corner isn't allowed by Gemini.
        /// </summary>
        /// <param name="productVisualDatabase">
        ///     This is necessary to convert <paramref
        ///     name="possibleReplacementCabinetInfo"/> to a <see
        ///     cref="ProductVisualContent"/>. Must not be null.
        /// </param>
        /// <param name="cabinetToReplace">
        ///     The cabinet from the newly constructed template kitchen that
        ///     will be replaced with a cabinet matching <paramref
        ///     name="possibleReplacementCabinetInfo"/>.
        /// </param>
        /// <param name="possibleReplacementCabinetInfo">
        ///     The product info for a cabinet that may occupy <paramref
        ///     name="cabinetToReplace"/>'s current position.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CanReplaceCabinetWithCabinetByProductInfo(
            ProductVisualDatabase   productVisualDatabase,
            Cabinet                 cabinetToReplace,
            ProductInfo             possibleReplacementCabinetInfo)
        {
            if (cabinetToReplace == null)
            {
                throw new ArgumentNullException(nameof(cabinetToReplace));
            }

            if (possibleReplacementCabinetInfo == null)
            {
                throw new ArgumentNullException(nameof(possibleReplacementCabinetInfo));
            }

            if (possibleReplacementCabinetInfo.category != ProductCategory.CabinetSection)
            {
                MelcherLog.LogError($"Expected a product info of category {ProductCategory.CabinetSection} but got {possibleReplacementCabinetInfo.category}.");
                return false;
            }

            switch (cabinetToReplace.cabinetType)
            {
                case CabinetType.Straight:
                case CabinetType.StraightOnLeftOfCorner:
                case CabinetType.StraightOnRightOfCorner:
                    break;

                default:
                    // We currently don't support replacing any other type
                    // of cabinet as it does not have a proposed use-case.
                    return false;
            }

            ProductVisual replacementCabinetVisual =
                productVisualDatabase.FindRuntimeVisualByID(
                    possibleReplacementCabinetInfo.visualId
                );

            if (replacementCabinetVisual &&
                replacementCabinetVisual.info.sectionCabinetType != CabinetType.Straight)
            {
                // This is a special type of cabinet that has rules for whether
                // it can be placed here. For example, Blind Corner cabinets
                // can only appear next to a cabinet's corner if there isn't
                // already an opposite-facing Blind Corner cabinet attached to
                // this corner.

                GetCabinetNeighbours(
                    cabinetToReplace,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                if (replacementCabinetVisual.info.sectionCabinetType == CabinetType.StraightOnRightOfCorner)
                {
                    if (!leftCabinet ||
                        leftCabinet.cabinetType != CabinetType.Corner ||
                        IsNextNeighbourCabinetType(leftCabinet, Extent.Min, CabinetType.StraightOnLeftOfCorner))
                    {
                        // No corner cab or the corner cab already a blind
                        // corner cabinet occupying it (which prevents this
                        // one).
                        return false;
                    }
                }
                else if (replacementCabinetVisual.info.sectionCabinetType == CabinetType.StraightOnLeftOfCorner)
                {
                    if (!rightCabinet ||
                        rightCabinet.cabinetType != CabinetType.Corner ||
                        IsNextNeighbourCabinetType(rightCabinet, Extent.Max, CabinetType.StraightOnRightOfCorner))
                    {
                        // No corner cab or the corner cab already a blind
                        // corner cabinet occupying it (which prevents this
                        // one).
                        return false;
                    }
                }
            }

            return true;

            bool IsNextNeighbourCabinetType(
                Cabinet     currentNeighbour,
                Extent      neighbourDirection,
                CabinetType expectedCabinetType)
            {
                Cabinet neighboursNeighbourCabinet =
                    GetCabinetNeighbour(currentNeighbour, neighbourDirection);

                if (neighboursNeighbourCabinet &&
                    neighboursNeighbourCabinet.cabinetSectionProductInfo != null)
                {
                    ProductVisual neighbourVisual =
                        productVisualDatabase.FindRuntimeVisualByID(
                            neighboursNeighbourCabinet.cabinetSectionProductInfo.visualId
                        );

                    if (neighbourVisual &&
                        neighbourVisual.info.sectionCabinetType == expectedCabinetType)
                    {
                        return false;
                    }
                }

                return false;
            }
    }

        /// <summary>
        ///     Try to locate an index within the cabinet layout where a
        ///     cabinet with the properties of <paramref name="cabinetToInsert"/>
        ///     can exist.
        ///     <br /><br />
        ///
        ///     This is intended to be called when the user is attempting to
        ///     add a new cabinet to their design.
        /// </summary>
        /// <param name="cabinetToInsert">
        ///     The cabinet whose properties are used to determine the validity
        ///     of a new cabinet insert index. E.g., straight cabinets have
        ///     different placement rules than straight-before-corner cabinets.
        /// </param>
        /// <returns>
        ///     The index where the specified cabinet can be placed. If no
        ///     valid location is found, return <see langword="null"/>.
        /// </returns>
        public int? FindInsertIndexForCabinet(
            Cabinet cabinetToInsert,
            ProductInfo cabinetSectionProductInfo,
            int? startIndex = null,
            bool reverse = false,
            Cabinet prioritizedCabinetToReplace = null)
        {
            int? insertIndex = null;

            CabinetSpan lastCabinetSpan = null;
            bool canLastCabinetSpanReceiveCabinet = false;

            int cabinetToInsertOriginalSiblingIndex = int.MaxValue;
            if (cabinetToInsert.GetTransform().parent == m_Transform)
            {
                cabinetToInsertOriginalSiblingIndex =
                    cabinetToInsert.GetTransform().GetSiblingIndex();
            }

            int childCount = m_Transform.childCount;

            int increment = reverse ? -1 : 1;

            // Iterate over the cabinets, starting at the requested index and
            // moving until the last cabinet (or first cabinet if `reverse` is
            // true).

            for (int childIndex = startIndex.GetValueOrDefault(reverse ? childCount - 1 : 0);
                childIndex >= 0 && childIndex < childCount;
                childIndex += increment)
            {
                bool willInsertAfterCabinetAtIndex =
                    cabinetToInsertOriginalSiblingIndex < childIndex;

                Transform child = m_Transform.GetChild(childIndex);

                Cabinet cabinetAtIndex = child.GetComponent<Cabinet>();

                if (prioritizedCabinetToReplace &&
                    cabinetAtIndex != prioritizedCabinetToReplace)
                {
                    // Not the cabinet we want to replace.
                    continue;
                }

                if (cabinetAtIndex == cabinetToInsert &&
                    startIndex.HasValue &&
                    startIndex.Value == cabinetToInsert.GetTransform().GetSiblingIndex())
                {
                    // Skip this position. This skip allows the function to be
                    // called with an already-inserted cabinet and its current
                    // index as the start index without the function
                    // immediately returning its current position

                    continue;
                }

                CabinetSpan cabinetSpan = FindCabinetSpan(cabinetAtIndex);
                if (cabinetSpan != lastCabinetSpan)
                {
                    // We've entered a new cabinet span. We cache whether
                    // this span can receive a new cabinet so we don't repeat
                    // checking on each cabinet within the span.

                    lastCabinetSpan = cabinetSpan;

                    if (kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections &&
                        cabinetSpan.backing &&
                        cabinetSectionProductInfo != null &&
                        !MelcherCommon.IsAlmostEqual(cabinetSectionProductInfo.productSizeInMeters.y, 36.0f * Units.Inches2Meters, tolerance: 0.001f))
                    {
                        canLastCabinetSpanReceiveCabinet = false;

                        // Prevent cabinets of non-standard height from being
                        // added to a span that has a backing.
                        continue;
                    }

                    int midSectionCabinetsInSpan = 0;

                    foreach (ElementIndex<Cabinet> i in cabinetSpan.cabinets)
                    {
                        if (i.element != cabinetToInsert &&
                            i.element.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
                        {
                            ++midSectionCabinetsInSpan;
                        }

                    }

                    canLastCabinetSpanReceiveCabinet =
                        !cabinetSpan.IsSpace() &&
                        midSectionCabinetsInSpan < Const.MaxStraightCabinetsPerSpan;
                }

                if (!canLastCabinetSpanReceiveCabinet)
                {
                    continue;
                }

                GetCabinetNeighbours(
                    cabinetAtIndex,
                    out Cabinet leftCabinet,
                    out Cabinet rightCabinet
                );

                // Ensure the cabinet being moved isn't treated as a neighbour:
                // it's being moved and won't exist in the same location after
                // being moved.

                if (cabinetAtIndex != cabinetToInsert &&
                    cabinetAtIndex != prioritizedCabinetToReplace)
                {
                    if (willInsertAfterCabinetAtIndex)
                    {
                        leftCabinet = cabinetAtIndex;
                    }
                    else
                    {
                        rightCabinet = cabinetAtIndex;
                    }
                }

                if (!leftCabinet || !rightCabinet)
                {
                    // There should always be a left and right cabinet (e.g.,
                    // an end cabinet). Otherwise, it's not a valid location.
                    continue;
                }

                MelcherLog.Log($"Comparing left {leftCabinet}", leftCabinet);
                MelcherLog.Log($"  to right {rightCabinet}", rightCabinet);

                if (cabinetToInsert.cabinetType == CabinetType.Straight)
                {
                    if (leftCabinet.cabinetType == CabinetType.Space ||
                        rightCabinet.cabinetType == CabinetType.Space)
                    {
                        continue;
                    }

                    if (leftCabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                        rightCabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                    {
                        // Prevent a cabinet from being inserted between a
                        // corner and the blind cabinet.
                        continue;
                    }

                    if (!prioritizedCabinetToReplace)
                    {
                        if (increment > 0 && leftCabinet.cabinetType.IsHeightTransition() ||
                            increment < 0 && rightCabinet.cabinetType.IsHeightTransition())
                        {
                            // Skip over height transition sections.
                            continue;
                        }
                    }
                }
                else if (cabinetToInsert.cabinetType == CabinetType.StraightOnLeftOfCorner)
                {
                    if (rightCabinet.cabinetType != CabinetType.Corner ||
                        rightCabinet.cabinetModelType != CabinetModelType.Procedural)
                    {
                        // No corner or its not procedural.
                        continue;
                    }

                    if (leftCabinet.cabinetType == CabinetType.StraightOnLeftOfCorner)
                    {
                        // Already occupied.
                        continue;
                    }

                    Cabinet rightRightCabinet =
                        rightCabinet.kitchen.layout.GetCabinetNeighbour(rightCabinet, Extent.Max);

                    if (rightRightCabinet &&
                        rightRightCabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                    {
                        // Already occupied.
                        continue;
                    }
                }
                else if (cabinetToInsert.cabinetType == CabinetType.StraightOnRightOfCorner)
                {
                    if (leftCabinet.cabinetType != CabinetType.Corner ||
                        leftCabinet.cabinetModelType != CabinetModelType.Procedural)
                    {
                        // No corner or its not procedural.
                        continue;
                    }

                    if (rightCabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                    {
                        // Already occupied.
                        continue;
                    }

                    Cabinet leftLeftCabinet =
                        leftCabinet.kitchen.layout.GetCabinetNeighbour(leftCabinet, Extent.Min);

                    if (leftLeftCabinet &&
                        leftLeftCabinet.cabinetType == CabinetType.StraightOnLeftOfCorner)
                    {
                        // Already occupied.
                        continue;
                    }
                }
                else if (cabinetToInsert.cabinetType == CabinetType.Corner)
                {
                    // Must find a procedural corner to swap out. For Gemini
                    // (Catalog building method), the cabinet being swapped out
                    // must also not have any blind cabinet neighbours. I.e.,
                    // it cannot have a StraightOnLeftOfCorner or
                    // StraightOnRightOfCorner neighbour.

                    bool blindCornersCanExist =
                        kitchen.selectedBuildingMethod == KitchenBuildingMethod.Catalog;

                    if (prioritizedCabinetToReplace &&
                        prioritizedCabinetToReplace.cabinetType == CabinetType.Corner)
                    {
                        if (cabinetAtIndex != prioritizedCabinetToReplace)
                        {
                            // Not the corner cabinet we want to replace.
                            continue;
                        }

                        if (blindCornersCanExist)
                        {
                            if (leftCabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                                rightCabinet.cabinetType == CabinetType.StraightOnRightOfCorner)
                            {
                                // Corner already occupied by blind corner
                                // cabinets.
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (rightCabinet.cabinetType != CabinetType.Corner ||
                            rightCabinet.cabinetModelType != CabinetModelType.Procedural)
                        {
                            // Must replace a procedural corner.
                            continue;
                        }

                        if (blindCornersCanExist)
                        {
                            Cabinet rightRightCabinet =
                                rightCabinet.kitchen.layout.GetCabinetNeighbour(rightCabinet, Extent.Max);

                            if (leftCabinet.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                                (rightRightCabinet && rightRightCabinet.cabinetType == CabinetType.StraightOnRightOfCorner))
                            {
                                // Corner already occupied by blind corner
                                // cabinets.
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    MelcherLog.LogError("Unhandled cabinet type.");
                    return null;
                }

                insertIndex = childIndex;

                break;
            }

            MelcherLog.Log($"  insertIndex={insertIndex.GetValueOrDefault(-1)}");

            return insertIndex;
        }

        /// <summary>
        ///     Search for a cabinet in the layout.
        ///     <br /><br />
        ///
        ///     The search starts at <paramref name="startIndex"/>. If this
        ///     value is omitted, the search starts at the front (or, if
        ///     <paramref name="reverse"/> is <see langword="true"/>, the
        ///     search starts at the back).
        ///     <br /><br />
        ///
        ///     The predicate is optional.
        /// </summary>
        /// <param name="startIndex">
        ///     Where the search should begin from.
        /// </param>
        /// <param name="skipStart">
        ///     Whether to ignore the cabinet at <see cref="startIndex"/>. This
        ///     is useful when looking for a neighbour to a particular cabinet.
        /// </param>
        /// <param name="reverse">
        ///     When <see langword="true"/>, the search processes cabinets
        ///     in the reverse order they appear in the layout. The default
        ///     value is <see langword="false"/>.
        /// </param>
        /// <param name="predicate">
        ///     An optional predicate that allows the caller to filter which
        ///     cabinets are considered valid to return.
        /// </param>
        /// <returns>
        ///     The first <see cref="Cabinet"/> found that matches the
        ///     predicate.
        /// </returns>
        public Cabinet FindCabinet(
            int?                startIndex = null,
            bool                skipStart = false,
            bool                reverse = false,
            Predicate<Cabinet>  predicate = null)
        {
            if (!startIndex.HasValue)
            {
                startIndex = reverse ? m_Transform.childCount - 1 : 0;
            }

            int increment = reverse ? -1 : 1;

            if (skipStart)
            {
                startIndex += increment;
            }

            for (int childIndex = startIndex.Value;
                childIndex >= 0 && childIndex < m_Transform.childCount;
                childIndex += increment)
            {
                Transform child = m_Transform.GetChild(childIndex);

                Cabinet cabinetAtIndex = child.GetComponent<Cabinet>();

                if (predicate != null && !predicate.Invoke(cabinetAtIndex))
                {
                    continue;
                }

                return cabinetAtIndex;
            }

            return null;
        }

        /// <summary>
        ///     Given a <see cref="Cabinet"/>, find the <see
        ///     cref="CabinetSpan"/> it belongs to. A cabinet may belong to
        ///     more than one span, but this method only returns the first
        ///     one found. If no span is found, the function returns <see
        ///     langword="null"/>.
        /// </summary>
        public CabinetSpan FindCabinetSpan(Cabinet cabinet)
        {
            foreach (CabinetSpan span in spans)
            {
                if (span.Contains(cabinet))
                {
                    return span;
                }
            }

            return null;
        }

        public Cuboid GetCabinetOutsideCuboid(Cabinet cabinet)
        {
            return GetCabinetOutsideCuboid(cabinet, out Cabinet _, out Cabinet _);
        }

        /// <summary>
        ///     Expand the cuboid of one cabinet's size to encompass the
        ///     width of it's left and right neighbours. This is referred to as
        ///     the cabinet's "outside" measurement. This will only expand to
        ///     encompass a neighbour if the neighbour is a corner or an end.
        /// </summary>
        /// <param name="cabinet">
        ///     The cabinet in the middle of <paramref name="leftCabinet"/> and
        ///     <paramref name="rightCabinet"/>.
        /// </param>
        /// <param name="leftCabinet">
        ///     The cabinet on the left of the main cabinet.
        /// </param>
        /// <param name="rightCabinet">
        ///     The cabinet on the right of the main cabinet.
        /// </param>
        /// <returns></returns>
        public Cuboid GetCabinetOutsideCuboid(Cabinet cabinet, out Cabinet leftCabinet, out Cabinet rightCabinet)
        {
            Cuboid outsideEdgeCuboid = new Cuboid(cabinet.sizeInMeters);

            GetCabinetNeighbours(
                cabinet,
                out leftCabinet,
                out rightCabinet
            );

            if (leftCabinet != null && leftCabinet.isCornerOrEnd)
            {
                Vector3 leftSizeInMeters = Cabinet.TransformSizeBetweenVolumes(
                    leftCabinet,
                    leftCabinet.sizeInMeters,
                    cabinet
                );
                outsideEdgeCuboid.xMin -= leftSizeInMeters.x;
            }

            if (rightCabinet != null && rightCabinet.isCornerOrEnd)
            {
                Vector3 rightSizeInMeters = Cabinet.TransformSizeBetweenVolumes(
                    rightCabinet,
                    rightCabinet.sizeInMeters,
                    cabinet
                );
                outsideEdgeCuboid.xMax += rightSizeInMeters.x;
            }

            return outsideEdgeCuboid;
        }

        public Cabinet GetCabinetNeighbour(Cabinet cabinet, Extent extent)
        {
            GetCabinetNeighbours(
                cabinet,
                out Cabinet leftCabinet,
                out Cabinet rightCabinet
            );

            if (extent == Extent.Min)
            {
                return leftCabinet;
            }
            else if (extent == Extent.Max)
            {
                return rightCabinet;
            }
            else
            {
                MelcherLog.LogError($"Requested a cabinet's neighbour using an invalid extent enum: {extent}", cabinet);
                return null;
            }
        }

        /// <summary>
        ///     Find the left and right neighbours of a cabinet section.
        /// </summary>
        /// <param name="centerCabinet">
        ///     The cabinet whose cabinet neighbours will be found.
        /// </param>
        /// <param name="leftCabinet">
        ///     Output argument. If <paramref name="centerCabinet"/> has a
        ///     neighbour on its left, it will be returned here.
        /// </param>
        /// <param name="rightCabinet">
        ///     Output argument. If <paramref name="centerCabinet"/> has a
        ///     neighbour on its right, it will be returned here.
        /// </param>
        public void GetCabinetNeighbours(Cabinet centerCabinet, out Cabinet leftCabinet, out Cabinet rightCabinet)
        {
            leftCabinet = null;
            rightCabinet = null;

            int centerCabinetIndex = FindCabinetIndex(centerCabinet);

            if (centerCabinetIndex >= 0)
            {
                foreach (ElementIndex<Cabinet> i in EnumerateCabinets())
                {
                    if (i.index == centerCabinetIndex - 1)
                    {
                        leftCabinet = i.element;
                    }

                    if (i.index == centerCabinetIndex + 1)
                    {
                        rightCabinet = i.element;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Position all cabinets relative to their left-neighbour using
        ///     their <see cref="Cabinet.normalizedAnchorToPreviousSection"/>
        ///     value.
        /// </summary>
        public void UpdatePositions()
        {
            Quaternion currentRotation = Quaternion.identity;

            // Ensure straight cabinets are correctly rotated to match their
            // preceding section. This ensures that reordered cabinets are
            // properly rotated when moved over corners or gaps.
            foreach (ElementIndex<Cabinet> currentCabinet in EnumerateCabinets())
            {
                switch (currentCabinet.element.cabinetType)
                {
                    case CabinetType.Straight:
                    case CabinetType.StraightOnLeftOfCorner:
                    case CabinetType.StraightOnRightOfCorner:
                    case CabinetType.HighLeftToLowRight:
                    case CabinetType.LowLeftToHighRight:
                        currentCabinet.element.GetTransform().rotation = currentRotation;
                        break;

                    case CabinetType.Corner:
                        // Force a 90 degree rotation at corners. This ensures
                        // that L or U shapes work correctly. Corners use the
                        // same rotation as the piece before it but should alter
                        // the rotation of straight pieces that follow it.
                        currentCabinet.element.GetTransform().rotation = currentRotation;
                        currentRotation *= Quaternion.Euler(0.0f, 90.0f, 0.0f);
                        break;

                    default:
                        // Use the rotation of non-straight and non-corner
                        // cabinets as-is.
                        currentRotation =
                            currentCabinet.element.GetTransform().rotation;
                        break;
                }
            }

            Cabinet previousCabinet = null;

            int wedgeCornerCount = 0;

            foreach (ElementIndex<Cabinet> currentCabinet in EnumerateCabinets())
            {
                if (currentCabinet.element.Is(CabinetType.CornerWedge))
                {
                    // Corner wedges should use a 45-degree offset from
                    // their attached corner cabinet.

                    Cabinet wedge = currentCabinet.element;

                    Cabinet wedgedCornerCabinet = wedge.cornerCabinetBeingWedged;

                    Transform wedgeTransform = wedge.GetTransform();

                    Transform wedgedCornerCabinetTransform =
                        wedgedCornerCabinet.GetTransform();

                    wedgeTransform.rotation =
                        wedgedCornerCabinetTransform.rotation * Quaternion.Euler(0.0f, 45.0f, 0.0f);

                    // Use the Pythagoras theorem to calculate the offset of
                    // the corner cab from the 90-degree corner based on the
                    // length of the 45-degree corner face.
                    float wedgeOffsetFromWedgedCorner =
                        ProFitUtility.CalculateCabinetCornerWedgeOrthogonalLength(wedge);

                    wedgeTransform.position =
                        wedgedCornerCabinetTransform.position + wedgedCornerCabinetTransform.TransformDirection(Vector3.left * wedgeOffsetFromWedgedCorner);

                    GetCabinetNeighbours(
                        wedgedCornerCabinet,
                        out Cabinet leftCabinet,
                        out Cabinet rightCabinet
                    );

                    // TODO: Read this value from the cabinet's original prefab.
                    const float MinimumCabinetLength =
                        2.0f * Units.InchesPerFoot * Units.Inches2Meters;

                    // Ensure the neighbour cabinets occupiable area and size
                    // constraints account for the corner wedge.

                    float leftCabinetResizableWidthInMeters = float.MaxValue;

                    if (leftCabinet != null)
                    {
                        CuboidPadding occupiablePadding = leftCabinet.occupiablePaddingInMeters;
                        occupiablePadding.right = wedgeOffsetFromWedgedCorner;
                        leftCabinet.occupiablePaddingInMeters = occupiablePadding;

                        Vector3 minSizeInMeters = leftCabinet.minSizeInMeters;
                        minSizeInMeters.x = wedgeOffsetFromWedgedCorner + MinimumCabinetLength;
                        leftCabinet.minSizeInMeters = minSizeInMeters;

                        leftCabinetResizableWidthInMeters = leftCabinet.sizeInMeters.x;

                        float leftCabinetOccupantMaxX = 0.0f;

                        foreach (Occupant occupant in leftCabinet.occupants)
                        {
                            Cuboid occupiedCuboid =
                                occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(occupant);

                            leftCabinetOccupantMaxX =
                                Mathf.Max(leftCabinetOccupantMaxX, occupiedCuboid.xMax);
                        }

                        if (leftCabinetOccupantMaxX > MinimumCabinetLength)
                        {
                            leftCabinetResizableWidthInMeters -= leftCabinetOccupantMaxX;
                        }
                        else
                        {
                            leftCabinetResizableWidthInMeters -= MinimumCabinetLength;
                        }
                    }

                    float rightCabinetResizableWidthInMeters = float.MaxValue;

                    if (rightCabinet != null)
                    {
                        CuboidPadding occupiablePadding = rightCabinet.occupiablePaddingInMeters;
                        occupiablePadding.left = wedgeOffsetFromWedgedCorner;
                        rightCabinet.occupiablePaddingInMeters = occupiablePadding;

                        Vector3 minSizeInMeters = rightCabinet.minSizeInMeters;
                        minSizeInMeters.x = wedgeOffsetFromWedgedCorner + MinimumCabinetLength;
                        rightCabinet.minSizeInMeters = minSizeInMeters;

                        rightCabinetResizableWidthInMeters = rightCabinet.sizeInMeters.x;

                        float? rightCabinetOccupantMinX = null;

                        foreach (Occupant occupant in rightCabinet.occupants)
                        {
                            Cuboid occupiedCuboid =
                                occupant.occupiedVolume.CalculateOccupancyCuboidForOccupant(occupant);

                            rightCabinetOccupantMinX =
                                Mathf.Min(rightCabinetOccupantMinX.GetValueOrDefault(float.MaxValue), occupiedCuboid.xMin);
                        }

                        if (rightCabinetOccupantMinX.HasValue)
                        {
                            float metersFromEnd = rightCabinetResizableWidthInMeters - rightCabinetOccupantMinX.Value;

                            if (metersFromEnd > MinimumCabinetLength)
                            {
                                rightCabinetResizableWidthInMeters -= metersFromEnd;
                            }
                            else
                            {
                                rightCabinetResizableWidthInMeters -= MinimumCabinetLength;
                            }
                        }
                    }

                    // Constrain the wedge corner's size to its smallest
                    // neighbour.

                    float minNeighborWidthInMeters =
                        Mathf.Min(leftCabinetResizableWidthInMeters, rightCabinetResizableWidthInMeters);

                    float c =
                        Mathf.Sqrt(2.0f * (minNeighborWidthInMeters * minNeighborWidthInMeters));

                    Vector3 maxSizeInMeters = wedge.maxSizeInMeters;
                    maxSizeInMeters.x = c;
                    wedge.maxSizeInMeters = maxSizeInMeters;

                    ++wedgeCornerCount;
                }
                else if (previousCabinet)
                {
                    Cuboid previousCabinetCuboid =
                        new Cuboid(previousCabinet.sizeInMeters);

                    Vector3 localPosInPrevious =
                        Cuboid.NormalizedToPoint(
                            previousCabinetCuboid,
                            previousCabinet.normalizedAnchorForNextSection
                        );

                    Vector3 worldPos =
                        previousCabinet.GetTransform().TransformPoint(localPosInPrevious);

                    currentCabinet.element.GetTransform().position = worldPos;

                    if (kitchen.selectedBuildingMethod is KitchenBuildingMethod.Catalog or KitchenBuildingMethod.Sections)
                    {
                        float previousCabinetDepthInMeters = previousCabinetCuboid.depth;

                        if (previousCabinet.Is(CabinetType.Corner))
                        {
                            previousCabinetDepthInMeters = previousCabinetCuboid.width;
                        }

                        float currentCabinetDepthInMeters =
                            currentCabinet.element.sizeInMeters.z;

                        float bumpOutMeters =
                            previousCabinetDepthInMeters - currentCabinetDepthInMeters;

                        Vector3 localBumpOut =
                            Vector3.forward * bumpOutMeters;

                        Transform currentCabinetTransform =
                            currentCabinet.element.GetTransform();

                        currentCabinetTransform.position +=
                            currentCabinetTransform.TransformDirection(localBumpOut);
                    }
                }

                previousCabinet = currentCabinet.element;
            }
        }

        /// <summary>
        ///     Update the meshes for all of the cabinets so they reflect their
        ///     current cabinet size.
        /// </summary>
        public void UpdateMeshes()
        {
            foreach (Transform child in m_Transform)
            {
                if (child.TryGetComponent(out Cabinet cabinetSection))
                {
                    cabinetSection.ResizeMeshToCabinetSize();
                }
            }
        }

        /// <summary>
        ///     Create a cabinet at the requested sibling index between the
        ///     kitchen's existing cabinets. The cabinet is instantiated from
        ///     a prefab. The new cabinet's instance is sized to the requested
        ///     value but it will not exceed the building method's max size.
        ///     When the function exits, the kitchen will be left in a
        ///     visually-sound state.
        ///     <br /><br />
        ///
        ///     This is invoked during the chain of functions that starts when
        ///     the user clicks the "Add Section" toolbar button. This button
        ///     is only available in <see cref="KitchenBuildingMethod.Sections"/>.
        /// </summary>
        /// <param name="index">
        ///     The sibling index within the current layout's other cabinets
        ///     to place the new cabinet.
        /// </param>
        /// <param name="cabinetPrefab">
        ///     The prefab that is instantiated to make the new cabinet.
        /// </param>
        /// <param name="cabinetDoorVisualPrefab">
        ///     Only expected when the kitchen's building method is set to
        ///     <see cref="KitchenBuildingMethod.Catalog"/>. This prefab will
        ///     be instantiated as the cabinet door(s) for the cabinet.
        /// </param>
        /// <param name="kitchen">
        ///     The new cabinet's kitchen.
        /// </param>
        /// <param name="sizeInMeters">
        ///     The size of the new cabinet (in meters). This value will be
        ///     clamped to the limits imposed by the kitchen.
        /// </param>
        /// <returns>
        ///     The new cabinet.
        /// </returns>
        public async UniTask<Cabinet> UserInstantiateCabinetAtIndex(
            int             index,
            Cabinet         cabinetPrefab,
            ProductInfo     cabinetSectionProductInfo,
            IKitchen        kitchen,
            Vector3?        sizeInMeters,
            bool            applySpanBackings,
            System.Action<Cabinet> immediatelyAfterCabinetIsInstantiated)
        {
            Cabinet leftCabinet = null;
            if (index > 0)
            {
                leftCabinet = GetCabinetAtIndex(index - 1);
            }

            Cabinet rightCabinet = GetCabinetAtIndex(index);
            if (rightCabinet)
            {
                Vector3 rightCabinetAnchor =
                    rightCabinet.normalizedAnchorToPreviousSection;

                rightCabinetAnchor.x = 1.0f;

                rightCabinet.normalizedAnchorToPreviousSection = rightCabinetAnchor;
            }

            Cabinet newCabinet =
                await InstantiateCabinet(
                    cabinetPrefab,
                    cabinetSectionProductInfo,
                    kitchen
                );

            immediatelyAfterCabinetIsInstantiated?.Invoke(newCabinet);

            // Ensure the cabinet's order is set early to avoid a situation
            // where a corner cabinet is added, it is default added to the end,
            // and this gets treated as being an extra cabinet span.
            newCabinet.GetTransform().SetSiblingIndex(index);

            if (newCabinet.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
            {
                if (leftCabinet && leftCabinet.cabinetType != CabinetType.Corner)
                {
                    // Ensure new cabinets anchor after the end that precedes
                    // them. Otherwise, the end and straight piece will
                    // overlap and z-fight.
                    newCabinet.normalizedAnchorToPreviousSection = new Vector3(1.0f, 0.0f, 0.0f);
                }

                // Initialize the new cabinet's backing sprite to match the
                // matching neighbour cabinet type's sprite.

                Cabinet straightCabinetNeighbour = null;

                if (rightCabinet && rightCabinet.cabinetType.IsMidSpanCabinetType(kitchen.selectedBuildingMethod))
                {
                    straightCabinetNeighbour = rightCabinet;
                }
                else
                {
                    // We can conclude the left cabinet must be the straight
                    // cabinet. Straight cabinets can only be added next to
                    // other straight cabinets.
                    straightCabinetNeighbour = leftCabinet;
                }

                if (!straightCabinetNeighbour)
                {
                    throw new InvalidOperationException(
                        $"The user is attempting to add a straight cabinet at cabinet index {index} but there are no straight cabinet neighbours to copy the sprite backing from."
                    );
                }

                newCabinet.backingSprite = straightCabinetNeighbour.backingSprite;
            }

            Vector3 requestedSizeInMeters = newCabinet.sizeInMeters;

            if (sizeInMeters.HasValue)
            {
                requestedSizeInMeters = sizeInMeters.Value;
            }

            requestedSizeInMeters =
                new Vector3(
                    Mathf.Clamp(requestedSizeInMeters.x, newCabinet.minSizeInMeters.x, newCabinet.maxSizeInMeters.x),
                    Mathf.Clamp(requestedSizeInMeters.y, newCabinet.minSizeInMeters.y, newCabinet.maxSizeInMeters.y),
                    Mathf.Clamp(requestedSizeInMeters.z, newCabinet.minSizeInMeters.z, newCabinet.maxSizeInMeters.z)
                );

            if (kitchen.selectedExtraFeatures.Contains(KitchenExtraFeature.CabinetBackPanels))
            {
                float backPanelDepthInMeters = kitchen.selectedBrandCatalog.cabinetRuntimeBackPanelInfo.panelDepthInMeters;

                requestedSizeInMeters.z += backPanelDepthInMeters;

                if (newCabinet.Is(CabinetType.Corner))
                {
                    requestedSizeInMeters.x += backPanelDepthInMeters;
                }
            }

            newCabinet.sizeInMeters = requestedSizeInMeters;

            m_CabinetSpansNeedsRebuild = true;

            newCabinet.SetCountertopThickness(kitchen.countertopThicknessInMeters);

            newCabinet.SetCountertopOverhang(kitchen.baseCountertopOverhangInMeters);

            // Apply the cabinet's depth.
            if (newCabinet.Is(CabinetType.Corner))
            {
                // Corners technically have two depths, one for each span they
                // belong to. Find those spans and adjust the corner cabinet's
                // corresponding axis to match.
                foreach (CabinetSpan span in spans)
                {
                    if (span.Contains(newCabinet))
                    {
                        MelcherLog.Assert(span.orientationAlignedCabinet != null, "Span's orientation cabinet is null.", newCabinet);

                        Cabinet.ResizeAxis resizeAxis =
                            newCabinet.IsYawSimilar(span.orientationAlignedCabinet) ?
                                Cabinet.ResizeAxis.Z :
                                Cabinet.ResizeAxis.X;

                        newCabinet.SetBaseCabinetDepth(
                            span.baseCabinetDepthInMeters,
                            resizeAxis
                        );
                    }
                }
            }
            else
            {
                newCabinet.SetBaseCabinetDepth(
                    kitchen.baseCabinetDepthInMeters,
                    Cabinet.ResizeAxis.Z
                );
            }

            if (applySpanBackings)
            {
                await ApplyCabinetBackingsToCabinet(newCabinet);
            }

            // 2nd position update added to fix ends trying to resize to smaller cabs (gemini bumped out cabs)
            UpdatePositions();
            UpdateMeshes();
            UpdatePositions();
            UpdateMeshes();

            await newCabinet.InstantiateCabinetPlunge();

            return newCabinet;
        }

        /// <summary>
        ///     Intended to be called in three scenarios:
        ///     <br /><br />
        ///
        ///     1.  Immediately after the user adds a new non-corner cabinet to
        ///         their design (Sections and Catalog building method).
        ///         <br /><br />
        ///
        ///     2.  Immediately after a new corner cabinet is added and the
        ///         original cabinet being replaced is removed.
        ///         <br /><br />
        ///
        ///     3.  Immediately after a corner cabinet is removed and its
        ///         replacement has been created.
        ///         <br /><br />
        ///
        ///     This is necessary to workaround how cabinet spans are rebuilt:
        ///     Spans are bookended by corner sections and there is
        ///     temporarily one too many corners when a new corner is being
        ///     added. Therefore, in order to accurately check which span a
        ///     corner cabinet belongs to, we must post-pone accessing its
        ///     spans until the duplicate corner is removed.
        /// </summary>
        /// <param name="newCabinet"></param>
        public async UniTask ApplyCabinetBackingsToCabinet(Cabinet newCabinet)
        {
            foreach (CabinetSpan span in spans)
            {
                if (span.backing && span.Contains(newCabinet))
                {
                    await span.ApplyCabinetBackingToCabinet(newCabinet, span.backing, add: true);
                }
            }
        }

        /// <summary>
        ///     Constructs a cabinet from a prefab, adding it as the last
        ///     cabinet to the layout, and returns it. This method is
        ///     intended to be used when loading a saved kitchen design where
        ///     the layout is custom and must be rebuilt, or when the user
        ///     adds an extra cabinet to their kitchen using <see
        ///     cref="KitchenBuildingMethod.Sections"/>. The returned cabinet
        ///     is expected to be further processed by the load function.
        /// </summary>
        /// <param name="cabinetPrefab"></param>
        /// <param name="cabinetSectionProductInfo">
        ///     When the kitchen is in <see cref="KitchenBuildingMethod.Catalog"/>
        ///     building method, the product info for the cabinet must be
        ///     provided so the proper model and cabinet sizing can be used.
        ///     This can be <see langword="null"/> for all other building
        ///     methods.
        /// </param>
        /// <param name="kitchen"></param>
        /// <returns></returns>
        public async UniTask<Cabinet> InstantiateCabinet(
            Cabinet         cabinetPrefab,
            ProductInfo     cabinetSectionProductInfo,
            IKitchen        kitchen)
        {
            KitchenBuildingMethod buildingMethod =
                kitchen.selectedBuildingMethod;

            bool hasCabinetSectionProductInfo =
                cabinetSectionProductInfo != null;

            bool isCabinetSectionProductInfoUsable =
                cabinetPrefab.cabinetType == CabinetType.Straight ||
                cabinetPrefab.cabinetType == CabinetType.StraightOnLeftOfCorner ||
                cabinetPrefab.cabinetType == CabinetType.StraightOnRightOfCorner ||
                cabinetPrefab.cabinetType == CabinetType.Corner;

            bool isCabinetSectionProductInfoExpected =
                (buildingMethod == KitchenBuildingMethod.Catalog || buildingMethod == KitchenBuildingMethod.Sections) && isCabinetSectionProductInfoUsable;

            bool isCabinetSectionProductInfoOptional =
                cabinetPrefab.cabinetType == CabinetType.Corner || buildingMethod == KitchenBuildingMethod.Sections;

            if (hasCabinetSectionProductInfo != isCabinetSectionProductInfoExpected &&
                !isCabinetSectionProductInfoOptional)
            {
                throw new ArgumentException(
                    $"A cabinet section product info was provided when it " +
                    $"wasn't needed OR it wasn't provided when it was " +
                    $"needed. Building method is {kitchen.selectedBuildingMethod} " +
                    $"which means the product info being needed is " +
                    $"{isCabinetSectionProductInfoExpected}.",
                    nameof(cabinetSectionProductInfo)
                );
            }

            Cabinet newCabinet =
                Instantiate(cabinetPrefab, parent: m_Transform);

            // Remove the (Clone) suffix. This is purely for appearances sake.
            newCabinet.name = cabinetPrefab.name;

            newCabinet.SetKitchen(kitchen);

            newCabinet.SetCabinetSectionProductInfo(cabinetSectionProductInfo);

            if (cabinetSectionProductInfo != null)
            {
                // Apply the cabinet section min and max widths (if they exist).

                {
                    // Allow cabinets to specify a different cabinet height.
                    Vector3 minSizeInMeters = newCabinet.minSizeInMeters;
                    Vector3 maxSizeInMeters = newCabinet.maxSizeInMeters;

                    minSizeInMeters.y = cabinetSectionProductInfo.productSizeInMeters.y;
                    maxSizeInMeters.y = cabinetSectionProductInfo.productSizeInMeters.y;

                    newCabinet.minSizeInMeters = minSizeInMeters;
                    newCabinet.maxSizeInMeters = maxSizeInMeters;
                }

                if (cabinetSectionProductInfo.minWidthInMeters.HasValue)
                {
                    Vector3 minSizeInMeters = newCabinet.minSizeInMeters;
                    minSizeInMeters.x = cabinetSectionProductInfo.minWidthInMeters.Value;
                    newCabinet.minSizeInMeters = minSizeInMeters;
                }

                if (cabinetSectionProductInfo.maxWidthInMeters.HasValue)
                {
                    Vector3 maxSizeInMeters = newCabinet.maxSizeInMeters;
                    maxSizeInMeters.x = cabinetSectionProductInfo.maxWidthInMeters.Value;
                    newCabinet.maxSizeInMeters = maxSizeInMeters;
                }

                // Find the model prefab that this cabinet section will use.
                ProductVisual cabinetSectionVisual =
                    kitchen.productVisualDatabase.FindRuntimeVisualByID(
                        cabinetSectionProductInfo.visualId
                    );

                if (cabinetSectionVisual == null)
                {
                    MelcherLog.LogError(
                        $"No visual found for cabinet section with visual ID: {cabinetSectionProductInfo.visualId}"
                    );
                }
                else
                {
                    ProductVisualContentLoader contentLoader =
                        ProductVisualContentLoader.FirstFromActiveScene();

                    ProductVisualContent cabinetSectionVisualPrefab =
                        await contentLoader.ProductVisualToContent(cabinetSectionVisual);

                    newCabinet.SetCabinetSectionProductVisualPrefab(
                        cabinetSectionVisual,
                        cabinetSectionVisualPrefab
                    );

                    if (cabinetSectionVisualPrefab.sectionModelPrefab)
                    {
                        // Now find the door from the currently selected door style.
                        ProductVisual cabinetDoorVisual =
                            CabinetSectionDoorUtility.FindDoorStyleVisual(
                                kitchen.productVisualDatabase.runtimeEntries,
                                cabinetSectionVisual,
                                kitchen.selectedBrandCatalog,
                                kitchen.selectedDoorStyleName
                            );

                        ProductVisualContent cabinetDoorVisualPrefab = null;

                        if (cabinetDoorVisual)
                        {
                            cabinetDoorVisualPrefab =
                                await contentLoader.ProductVisualToContent(cabinetDoorVisual);
                        }

                        newCabinet.InstantiateCabinetModel(
                            kitchen.selectedBrandCatalog,
                            cabinetDoorVisual?.info,
                            cabinetDoorVisualPrefab,
                            cabinetSectionProductInfo.productSizeInMeters.x
                        );

                        // Add Door Handle if separated
                        string selectedDoorHandleStyleName = kitchen.selectedDoorHandleStyleName;

                        if(cabinetDoorVisual)
                        {
                            if(string.IsNullOrEmpty(selectedDoorHandleStyleName) == false)
                            {
                                CabinetSectionDoorHandle.InstantiateHandle(
                                    kitchen.selectedBrandCatalog.cabinetSectionDoorHandleStyle,
                                    cabinetSectionProductInfo.visualId,
                                    selectedDoorHandleStyleName,
                                    newCabinet.cabinetSectionInstance.transform,
                                    newCabinet.cabinet.transform
                                );
                            }
                            else if(string.IsNullOrEmpty(kitchen.selectedBrandCatalog.defaultCabinetSectionDoorHandleStyleName) == false)
                            {
                                CabinetSectionDoorHandle.InstantiateHandle(
                                    kitchen.selectedBrandCatalog.cabinetSectionDoorHandleStyle,
                                    cabinetSectionProductInfo.visualId,
                                    kitchen.selectedBrandCatalog.defaultCabinetSectionDoorHandleStyleName,
                                    newCabinet.cabinetSectionInstance.transform,
                                    newCabinet.cabinet.transform
                                );
                            }
                        }
                    }
                }
            }

            newCabinet.InstantiateCabinetLegs(kitchen.selectedBrandCatalog);

            (Material cabinetMaterial, Material countertopMaterial) =
                await kitchen.GetFinishMaterials(allowHighlightToReturn: true);

            await newCabinet.SetCabinetMaterials(
                cabinetMaterial,
                countertopMaterial,
                await ProFitGlobals.appProfile.GetDefaultKitchenMaterial()
            );

            // Ensure `spans` is rebuilt the next time it is accessed.
            m_CabinetSpansNeedsRebuild = true;

            ModifyCabinetForBuildingMethod(newCabinet);

            if (kitchen.selectedBrandCatalog &&
                kitchen.selectedBrandCatalog.cabinetVentPrefab)
            {
                // We need to attach vents to WeatherStrong's end cabinets
                // because their non-zero-width otherwise totally occludes its
                // neighbour's vent.
                bool isWeatherStrongEnd =
                    kitchen.selectedBrandCatalog.ruleset == BrandCatalogRuleset.WeatherStrong &&
                    newCabinet.cabinetType.IsEnd();

                bool isCustomCabinet =
                    newCabinet.cabinetSectionVisual &&
                    newCabinet.cabinetSectionVisual.info.sectionUsesCabinetVents &&
                    newCabinet.cabinetSectionVisual.info.sectionCabinetType != CabinetType.Corner;

                bool canHaveVent = isWeatherStrongEnd || isCustomCabinet;

                if (canHaveVent)
                {
                    newCabinet.InstantiateCabinetVents(
                        kitchen.selectedBrandCatalog.cabinetVentPrefab,
                        kitchen.selectedBrandCatalog.cabinetVentDistanceFromEdgeInMeters
                    );
                }
            }

            // Ensure new corner cabinets inherit any extra back panel depth.
            // This extra depth is otherwise handled when the user adds/removes
            // the back panel option (Gemini).
            if (newCabinet.Is(CabinetType.Corner, CabinetModelType.Authored))
            {
                CuboidPadding countertopOverhangInMeters = newCabinet.GetCountertopOverhangInMeters();

                float backPanelDepthInMeters = kitchen.backPanelDepthInMeters;

                countertopOverhangInMeters.towardMaxZ += backPanelDepthInMeters;
                countertopOverhangInMeters.right += backPanelDepthInMeters;

                newCabinet.SetCountertopOverhang(countertopOverhangInMeters);
            }

            return newCabinet;
        }

        /// <summary>
        ///     Apply specific changes to a cabinet that are required for
        ///     the current building method. This is intended to be called when
        ///     a new cabinet section is instantiated and/or for each cabinet
        ///     section in a newly instantiated <see cref="CabinetLayout"/>.
        /// </summary>
        // This could be a static function, but long-term we may need access
        // to other member fields.
        public void ModifyCabinetForBuildingMethod(Cabinet cabinet)
        {
            if (cabinet.cabinetType == CabinetType.Straight &&
                cabinet.kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections)
            {
                // Apply the appropriate size limits to the cabinet. This
                // ensures that Stoll's cabinet sections are restricted to a
                // different size than Pro-Fit's.

                Vector3 minSizeInMeters = cabinet.minSizeInMeters;
                minSizeInMeters.x = Const.SectionBuildingMethodMinCabinetLengthInMeters;
                cabinet.minSizeInMeters =  minSizeInMeters;

                Vector3 maxSizeInMeters = cabinet.maxSizeInMeters;
                maxSizeInMeters.x = Const.SectionBuildingMethodMaxCabinetLengthInMeters;
                cabinet.maxSizeInMeters = maxSizeInMeters;

                // --
                // Cabinets in sections building method have columns. These
                // columns must encroach on the cabinet's occupiable space.

                CuboidPadding occupiablePaddingInMeters =
                    cabinet.occupiablePaddingInMeters;

                float cabinetColumnWidthInMeters = ProFitGlobals.appRules.CabinetColumnWidthInMeters;

                occupiablePaddingInMeters.left = cabinetColumnWidthInMeters * 0.5f;
                occupiablePaddingInMeters.right = cabinetColumnWidthInMeters * 0.5f;

                

                cabinet.occupiablePaddingInMeters = occupiablePaddingInMeters;
            }

            cabinet.SetColumnsVisible(cabinet.kitchen.selectedBuildingMethod == KitchenBuildingMethod.Sections);
        }

        /// <summary>
        ///     Intended to be called when the user swaps out a cabinet with
        ///     a new one. The old cabinet is destroyed and the new cabinet is
        ///     moved into the old one's place using this function.
        /// </summary>
        public async UniTask ReorderCabinet(Cabinet cabinetToReorder, int destinationCabinetIndex)
        {
            List<CabinetSpan> previousCabinetSpans = new();
            foreach (CabinetSpan span in spans)
            {
                if (span.Contains(cabinetToReorder))
                {
                    previousCabinetSpans.Add(span);
                }
            }

            foreach (CabinetSpan previousSpan in previousCabinetSpans)
            {
                if (previousSpan.backing)
                {
                    await previousSpan.ApplyCabinetBackingToCabinet(cabinetToReorder, previousSpan.backing, add: false);
                }
            }

            int previousCabinetIndex = cabinetToReorder.GetTransform().GetSiblingIndex();

            cabinetToReorder.GetTransform().SetSiblingIndex(destinationCabinetIndex);

            // `RebuildSpans` compares cabinet rotations to determine when a
            // new cabinet span starts. Therefore, we must explicitly update
            // the cabinets' rotations before the spans are rebuilt to ensure
            // the reordered cabinet is correctly categorized in its new span.
            UpdateMeshes();
            UpdatePositions();

            m_CabinetSpansNeedsRebuild = true;

            foreach (CabinetSpan span in spans)
            {
                if (span.backing && span.Contains(cabinetToReorder))
                {
                    await span.ApplyCabinetBackingToCabinet(cabinetToReorder, span.backing, add: true);
                }
            }

            await
                EventBridge.Designer.onCabinetReordered.InvokeIfNonEmpty(
                    (cabinetToReorder, previousCabinetIndex, previousCabinetSpans)
                );
        }

        /// <summary>
        ///     Destroy a cabinet at a specific index (including all of its
        ///     occupants). This is intended to be used during
        ///     <see cref="KitchenBuildingMethod.Catalog"/>'s kitchen
        ///     initialization where the default "procedural" cabinets must be
        ///     replaced with custom models.
        /// </summary>
        /// <param name="cabinetIndex"></param>
        public void DestroyCabinetAtIndex(int cabinetIndex)
        {
            Cabinet cabinet = GetCabinetAtIndex(cabinetIndex);
            DestroyCabinet(cabinet);
        }

        public void DestroyCabinet(Cabinet cabinet)
        {
            Transform cabinetTransform = cabinet.GetTransform();

            cabinetTransform.SetParent(null);

            Destroy(cabinet.gameObject);

            MarkSpansForRebuild();
        }

        /// <summary>
        ///     Destroy all the cabinets in the layout, including any of their
        ///     occupants. This is intended to be used when loading a previously
        ///     saved kitchen. The cabinets are wiped so it can be reconstructed
        ///     from scratch but the layout's camera angle data is preserved.
        /// </summary>
        public void DestroyCabinets()
        {
            for (int i = m_Transform.childCount - 1; i >= 0; --i)
            {
                DestroyCabinetAtIndex(i);
            }
        }

        /// <summary>
        ///     Sets an internal flag that will result in <see cref="spans"/>
        ///     being updated the next time it is requested. This is intended
        ///     to be called whenever a cabinet (in Sections building method) is
        ///     moved to another cabinet span.
        /// </summary>
        public void MarkSpansForRebuild()
        {
            m_CabinetSpansNeedsRebuild = true;
        }

        protected void Awake()
        {
            m_CabinetSpansNeedsRebuild = true;
        }

        protected void OnEnable()
        {
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke += OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetResized.OnInvoke += OnCabinetResized;
            EventBridge.Designer.onCabinetReordered.OnInvoke += OnCabinetReordered;
            EventBridge.Designer.onPreCabinetDestroyedByUser += OnPreCabinetDestroyedByUser;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke += OnCabinetDestroyedByUser;
        }

        protected void OnDisable()
        {
            EventBridge.Designer.onCabinetAddedByUser.OnInvoke -= OnCabinetAddedByUser;
            EventBridge.Designer.onCabinetResized.OnInvoke -= OnCabinetResized;
            EventBridge.Designer.onCabinetReordered.OnInvoke -= OnCabinetReordered;
            EventBridge.Designer.onPreCabinetDestroyedByUser -= OnPreCabinetDestroyedByUser;
            EventBridge.Designer.onCabinetDestroyedByUser.OnInvoke -= OnCabinetDestroyedByUser;
        }

        protected async UniTask OnCabinetAddedByUser(Cabinet cabinet)
        {
            UpdateBackings();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetResized(Cabinet cabinet)
        {
            UpdateBackings();

            await UniTask.CompletedTask;
        }

        protected async UniTask OnCabinetReordered((Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans) args)
        {
            {
                // Update the visibility of the cabinet's countertop overhang,
                // adjusting whether the cabinet had a backing in its original
                // location and whether it has one now.

                foreach (CabinetSpan previousCabinetSpan in args.previousCabinetSpans)
                {
                    CabinetBacking previousCabinetBacking = previousCabinetSpan.backing;

                    if (previousCabinetBacking && previousCabinetBacking.cropCabinetCountertops)
                    {
                        args.reorderedCabinet.SetCountertopOverhangVisible(CabinetLocalDirection.Back, true);
                    }
                }

                foreach (CabinetSpan newCabinetSpan in spans)
                {
                    CabinetBacking newCabinetBacking = newCabinetSpan.backing;

                    if (newCabinetBacking && newCabinetBacking.cropCabinetCountertops)
                    {
                        args.reorderedCabinet.SetCountertopOverhangVisible(CabinetLocalDirection.Back, false);
                    }
                }
            }

            UpdateBackings();
            UpdateMeshes();
            UpdatePositions();

            await UniTask.CompletedTask;
        }

        /// <summary>
        ///     Called when a cabinet is being destroyed. This ensures the
        ///     backing (if it exists) is moved to a different cabinet so it is
        ///     not unintentionally destroyed alongside its parent.
        /// </summary>
        protected void OnPreCabinetDestroyedByUser(Cabinet cabinet)
        {
            CabinetSpan cabinetSpan = FindCabinetSpan(cabinet);

            cabinetSpan.RelocateBackingFromDestructingCabinet(cabinet);

            m_CabinetSpansNeedsRebuild = true;
        }

        protected async UniTask OnCabinetDestroyedByUser(int cabinetSpanIndex)
        {
            UpdateBackings();

            await UniTask.CompletedTask;
        }

        /// <summary>
        ///     Iterate over all cabinet spans and update their backings.
        /// </summary>
        protected void UpdateBackings()
        {
            // Notify the spans so they can resize their backings.
            foreach (CabinetSpan span in spans)
            {
                span.UpdateBackingSizeAndPosition();
            }
        }

        // =====================================================================
        // Internal static functions
        // =====================================================================

        /// <summary>
        ///     Iterate over the cabinets in the layout and ensure the cabinet
        ///     spans are properly synchronized.
        /// </summary>
        /// <remarks>
        ///     This method is static to guard against accidentally accessing
        ///     <see cref="spans"/> mid function which could possibly cause
        ///     <see cref="RebuildSpans"/> to be called recursively.
        /// </remarks>
        protected static void RebuildSpans(IEnumerable<ElementIndex<Cabinet>> cabinets, List<CabinetSpan> spans)
        {
            int nextSpanIndex = 0;

            CabinetSpan currentSpan = null;

            // Corners belong to both its connected cabinet spans. Therefore,
            // the previous corner is cached so it can be included in the
            // next span.
            ElementIndex<Cabinet>? previousCorner = null;

            // To actually determine when a new span starts, the yaw of each
            // pair of cabinets is compared.
            float previousCabinetYaw  = default;

            foreach (ElementIndex<Cabinet> i in cabinets)
            {
                Cabinet cabinet = i.element;

                // Ensure we successfully treat -180 rotation the same as +180.
                float cabinetYaw =
                    Mathf.Repeat(
                        cabinet.GetTransform().eulerAngles.y,
                        360.0f
                    );

                // Check if the cabinet is rotated differently from the previous
                // cabinet. If so, this marks the start of a new span. The yaw
                // rotation is checked rather than the cabinet type because
                // some layouts, (e.g., galley) use spacer cabinets to
                // change the layout's direction.
                bool isRotatedDifferently =
                    !MelcherCommon.IsAlmostEqual(cabinetYaw, previousCabinetYaw, 0.01f);

                if (isRotatedDifferently || i.element.Is(CabinetType.CornerWedge))
                {
                    // Wedge cabinets always represent their own span.
                    currentSpan = null;
                }

                if (currentSpan == null)
                {
                    // This is the first span or a new span after a corner.

                    if (nextSpanIndex < spans.Count)
                    {
                        // Clear and reuse a previously allocated span.
                        currentSpan = spans[nextSpanIndex];
                        currentSpan.Clear();
                    }
                    else
                    {
                        // Create a new cabinet span.
                        currentSpan = new CabinetSpan(cabinet.kitchen, nextSpanIndex);
                        spans.Add(currentSpan);
                    }

                    if (previousCorner.HasValue)
                    {
                        currentSpan.Add(previousCorner.Value.element, previousCorner.Value.index);
                    }

                    ++nextSpanIndex;
                }

                currentSpan.Add(cabinet, i.index);

                previousCorner =
                    (cabinet.cabinetType == CabinetType.Corner) ?
                        i :
                        null;

                previousCabinetYaw = cabinetYaw;
            }

            for (int i = spans.Count - 1; i >= nextSpanIndex; --i)
            {
                // Purge excess entries.

                // TODO: Destroy/remove the cabinet backing.
                //       Is this still necessary? -Ryan
                //spans[i].DestroyOccupants();
                spans.RemoveAt(i);
            }

            for (int i = 0; i < spans.Count; ++i)
            {
                MelcherLog.Assert(spans[i].orientationAlignedCabinet, $"Orientation cabinet missing after rebuild. Span {i}");
            }
        }

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField] protected KitchenStyle m_Style = KitchenStyle.Unknown;
        [SerializeField] protected string m_DisplayName = null;

        [Tooltip("The yaw to use when the camera layout is first selected.")]
        [SerializeField] protected float m_DesignerCameraStartYaw = 45.0f;

        [Tooltip("The yaw to use when taking a screenshot with the cover " +
            "camera. This rotates the camera to best view all sides of " +
            "the layout.")]
        [SerializeField] protected float m_SaveCoverCameraYaw = 90.0f;

        [Tooltip("The roll to use when taking a screenshot with the top down " +
            "camera. This orients the camera so the kitchen is more horizontal " +
            "than vertical.")]
        [SerializeField] protected float m_SaveTopDownCameraRoll = 0.0f;

        // =====================================================================
        // OnValidate-serialized fields
        // =====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = null;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected IKitchen m_Kitchen;

        protected MelcherReadWriteList<CabinetSpan> m_CabinetSpans = new ();

        /// <summary>
        ///     This field is <see cref="true"/> when <see
        ///     cref="m_CabinetSpans"/> should be rebuilt.
        /// </summary>
        protected bool m_CabinetSpansNeedsRebuild;

    }   // class CabinetLayout

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-08
/// ===========================================================================
