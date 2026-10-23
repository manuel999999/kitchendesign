using Cysharp.Threading.Tasks;
using KitchenDesign.Geometry;
using KitchenDesign.Products;
using KitchenDesign.Scriptables;
using KitchenDesign.Serializables;
using KitchenDesign.Server;
using KitchenDesign.Shared.Common;
using KitchenDesign.Shared.Enums;
using Melcher;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using KitchenDesign.Session;

namespace KitchenDesign.KitchenModel
{
    public sealed class OccupantDragControl
    {
        private readonly SessionStatus _session;
        private readonly Kitchen _kitchen;
        private readonly KitchenInteractionState _interaction;
        private readonly PhysicsRaycaster _cameraPhysicsRaycaster;
        private readonly LayerMask _productPointerInteractionMask;
        private KitchenWidgetsControl _widgets;

        internal OccupantDragControl(
            Kitchen kitchen,
            KitchenInteractionState interaction,
            PhysicsRaycaster cameraPhysicsRaycaster,
            LayerMask productPointerInteractionMask,
            SessionStatus session)
        {
            this._session = session;
            this._kitchen = kitchen;
            this._interaction = interaction;
            this._cameraPhysicsRaycaster = cameraPhysicsRaycaster;
            this._productPointerInteractionMask = productPointerInteractionMask;
        }

        internal void Connect(KitchenWidgetsControl widgets)
        {
            this._widgets = widgets;
        }

        internal void OnOccupantDragBegin(Occupant draggedOccupant, PointerEventData eventData)
        {
            if (this._interaction.volumeBeingResized != null || this._interaction.productBeingDragged)
            {
                eventData.pointerDrag = null;
                return;
            }

            Cabinet cabinet = (Cabinet)draggedOccupant.cabinet;

            bool didDetectDragPlane =
                ProFitUtility.CalculateCameraLocalDragPosition(
                    cabinet,
                    this._kitchen.GetCamera(),
                    eventData,
                    out Vector3 _,
                    out Plane _
                );

            if (!didDetectDragPlane)
            {
                MelcherLog.LogWarning("Aborting product drag. Raycast didn't hit the calculated drag plane.", cabinet);
                eventData.pointerDrag = null;
                return;
            }

            MelcherLog.LogEditorOnly("Beginning drag");

            this._interaction.productBeingDragged = draggedOccupant;

            this._cameraPhysicsRaycaster.eventMask &= ~this._productPointerInteractionMask;

            this._widgets.SetCabinetResizeWidgetsVisible(false);
            this._widgets.SetCabinetShelfResizeWidgetsVisible(target: null);
        }

        internal void OnOccupantDrag(Occupant draggedOccupant, PointerEventData eventData)
        {
            if (!this._lastOccupantDraggedTask.HasValue || this._lastOccupantDraggedTask.Value.Status != UniTaskStatus.Pending)
            {
                this._lastOccupantDraggedTask = OnOccupantDragAsync(draggedOccupant, eventData);
                this._lastOccupantDraggedTask.Value.Forget();
            }

            async UniTask OnOccupantDragAsync(Occupant draggedOccupant, PointerEventData eventData)
            {
                Camera mainCamera = this._kitchen.GetCamera();
                Ray pointerRay = mainCamera.ScreenPointToRay(eventData.position);

                bool isDraggingShelf = draggedOccupant.TryGetComponent(out CabinetShelf _);

                OccupiableVolume startingVolume = draggedOccupant.occupiedVolume;

                Cabinet nearestCabinet = null;
                float nearestCabinetDistance = float.MaxValue;
                foreach (ElementIndex<Cabinet> i in this._kitchen.layout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    if (draggedOccupant.isLockedToCabinet)
                    {
                        continue;
                    }

                    OrientedBounds cabinetOrientedBounds = cabinet.GetOrientedBounds();

                    if (cabinetOrientedBounds.IntersectRay(pointerRay, out float distance) &&
                        distance < nearestCabinetDistance)
                    {
                        nearestCabinet = cabinet;
                        nearestCabinetDistance = distance;
                    }
                }

                CabinetShelf nearestShelf = null;
                float nearestShelfDistance = float.MaxValue;
                if (nearestCabinet && !isDraggingShelf)
                {
                    foreach (Occupant occupant in nearestCabinet.occupants)
                    {
                        if (occupant.TryGetComponent(out CabinetShelf shelf))
                        {
                            Cuboid shelfCabinetCuboid = shelf.resizerCuboid;

                            OrientedBounds shelfBounds =
                                new OrientedBounds(
                                    new Bounds(shelfCabinetCuboid.center, shelfCabinetCuboid.size),
                                    shelf.GetTransform().worldToLocalMatrix
                                );

                            shelfBounds.Draw(Color.magenta, 2.0f);

                            if (shelfBounds.IntersectRay(pointerRay, out float distanceToShelf) &&
                                distanceToShelf < nearestShelfDistance)
                            {
                                nearestShelf = shelf;
                                nearestShelfDistance = distanceToShelf;
                            }
                        }
                    }
                }

                if (nearestShelf && !ReferenceEquals(nearestShelf, draggedOccupant.occupiedVolume))
                {
                    bool didDetectDragPlane =
                        ProFitUtility.CalculateCameraLocalDragPosition(
                            nearestShelf,
                            mainCamera,
                            eventData,
                            out Vector3 futureLastCameraLocalDragPosition,
                            out Plane _
                        );

                    if (didDetectDragPlane)
                    {
                        Vector3 worldDragPosition =
                            mainCamera.transform.TransformPoint(
                                futureLastCameraLocalDragPosition
                            );

                        Vector3 volumeLocalDragPosition =
                            nearestShelf.GetTransform().InverseTransformPoint(
                                worldDragPosition
                            );

                        float metersFromCabinetMinX =
                           DoDraggedOccupantSnapping(nearestShelf, draggedOccupant, volumeLocalDragPosition.x);

                        this._metersFromVolumeMinX.value = metersFromCabinetMinX;

                        this._metersFromVolumeMinY.value = -nearestShelf.resizerCuboid.height;

                        bool wasProductAdded = false;

                        Cuboid? backingOccupancyInWorldSpace = null;

                        CabinetSpan spanOfShelfCabinet =
                            this._kitchen.layout.FindCabinetSpan((Cabinet)nearestShelf.occupant.cabinet);

                        if (spanOfShelfCabinet.backing)
                        {
                            backingOccupancyInWorldSpace =
                                spanOfShelfCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                    spanOfShelfCabinet.backing.info,
                                    spanOfShelfCabinet.backing
                                );
                        }

                        if (this._kitchen.AreProductsForcedToCenterOfCabinets(cabinetForRuleset: null))
                        {
                            wasProductAdded =
                                await nearestShelf.TryAddOccupantCentered(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    this._metersFromVolumeMinX,
                                    this._metersFromVolumeMinY
                                );
                        }
                        else
                        {
                            wasProductAdded =
                                await nearestShelf.TryAddOccupantNear(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    this._metersFromVolumeMinX,
                                    this._metersFromVolumeMinY
                                );
                        }

                        if (wasProductAdded)
                        {
                            EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                                draggedOccupant,
                                eventData
                            );
                            return;
                        }
                    }
                }

                if (!nearestShelf && nearestCabinet && !ReferenceEquals(nearestCabinet, draggedOccupant.occupiedVolume))
                {
                    bool didDetectDragPlane =
                        ProFitUtility.CalculateCameraLocalDragPosition(
                            nearestCabinet,
                            mainCamera,
                            eventData,
                            out Vector3 futureLastCameraLocalDragPosition,
                            out Plane _
                        );

                    if (didDetectDragPlane)
                    {
                        Vector3 worldDragPosition =
                            mainCamera.transform.TransformPoint(
                                futureLastCameraLocalDragPosition
                            );

                        Vector3 volumeLocalDragPosition =
                            nearestCabinet.GetTransform().InverseTransformPoint(
                                worldDragPosition
                            );

                        this._metersFromVolumeMinX.value =
                           DoDraggedOccupantSnapping(nearestCabinet, draggedOccupant, volumeLocalDragPosition.x);

                        this._metersFromVolumeMinY.value =
                            draggedOccupant.GetLocalPositionInVolume().y;

                        if (draggedOccupant.occupiedVolume.parentVolume != null ||
                            (!draggedOccupant.productVisual.info.anchor.IsAtBottom() && !MelcherCommon.IsAlmostEqual(draggedOccupant.cabinet.sizeInMeters.y, nearestCabinet.sizeInMeters.y, tolerance: 0.001f)))
                        {
                            float currentDistanceFromTop =
                                draggedOccupant.occupiedVolume.occupiableCuboid.yMax - draggedOccupant.GetLocalPositionInVolume().y;

                            this._metersFromVolumeMinY.value = nearestCabinet.sizeInMeters.y - currentDistanceFromTop;
                        }

                        Cuboid? backingOccupancyInWorldSpace = null;

                        CabinetSpan spanOfNearestCabinet =
                            this._kitchen.layout.FindCabinetSpan(nearestCabinet);

                        if (spanOfNearestCabinet.backing)
                        {
                            backingOccupancyInWorldSpace =
                                spanOfNearestCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                    spanOfNearestCabinet.backing.info,
                                    spanOfNearestCabinet.backing
                                );
                        }

                        bool wasProductAdded;

                        if (this._kitchen.AreProductsForcedToCenterOfCabinets(cabinetForRuleset: nearestCabinet))
                        {
                            wasProductAdded =
                                await nearestCabinet.TryAddOccupantCentered(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    this._metersFromVolumeMinX,
                                    this._metersFromVolumeMinY
                                );
                        }
                        else
                        {
                            wasProductAdded =
                                await nearestCabinet.TryAddOccupantNear(
                                    draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    this._metersFromVolumeMinX,
                                    this._metersFromVolumeMinY
                                );
                        }

                        if (wasProductAdded)
                        {
                            EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                                draggedOccupant,
                                eventData
                            );
                            return;
                        }
                    }
                }

                if (this._kitchen.AreProductsForcedToCenterOfCabinets(cabinetForRuleset: (Cabinet)draggedOccupant.cabinet))
                {
                    return;
                }

                OccupiableVolume currentVolume = draggedOccupant.occupiedVolume;

                bool drag =
                    ProFitUtility.CalculateCameraLocalDragPosition(
                        currentVolume,
                        mainCamera,
                        eventData,
                        out Vector3 cameraLocalDragPosition,
                        out Plane cameraLocalDragPlane
                    );

                if (drag)
                {
                    Vector3 worldDragPosition = mainCamera.transform.TransformPoint(cameraLocalDragPosition);
                    Transform cabinetTransform = currentVolume.GetTransform();
                    Vector3 volumeLocalDragPosition = cabinetTransform.InverseTransformPoint(worldDragPosition);

                    this._metersFromVolumeMinX.value =
                        DoDraggedOccupantSnapping(currentVolume, draggedOccupant, volumeLocalDragPosition.x);

                    this._metersFromVolumeMinY.value = draggedOccupant.GetLocalPositionInVolume().y;

                    Cuboid? backingOccupancyInWorldSpace = null;

                    CabinetSpan spanOfCurrentCabinet =
                        this._kitchen.layout.FindCabinetSpan((Cabinet)draggedOccupant.cabinet);

                    if (spanOfCurrentCabinet.backing)
                    {
                        backingOccupancyInWorldSpace =
                            spanOfCurrentCabinet.GetBackingOccupancyCuboidInWorldSpace(
                                spanOfCurrentCabinet.backing.info,
                                spanOfCurrentCabinet.backing
                            );
                    }

                    bool didMoveProduct =
                        currentVolume.TryMoveOccupantNear(
                            draggedOccupant,
                            backingOccupancyInWorldSpace,
                            ref this._metersFromVolumeMinX.value,
                            ref this._metersFromVolumeMinY.value
                        );

                    if (didMoveProduct)
                    {
                        EventBridge.Designer.onOccupantDragSuccess?.Invoke(
                            draggedOccupant,
                            eventData
                        );
                    }
                }
            }
        }
        internal async UniTask OnOccupantDragEnd((Occupant draggedOccupant, PointerEventData eventData) args)
        {
            if (args.draggedOccupant == this._interaction.productBeingDragged)
            {
                this._interaction.productBeingDragged = null;

                this._cameraPhysicsRaycaster.eventMask |= this._productPointerInteractionMask;

                this._widgets.SetCabinetShelfResizeWidgetsVisible(this._interaction.highlightObject.GetComponent<CabinetShelf>());

                if (!await this._kitchen.TryToFitCabinetToOccupant(args.draggedOccupant))
                {
                    foreach (CabinetSpan cabinetSpan in this._kitchen.layout.spans)
                    {
                        Cuboid? backingOccupancyInWorldSpace = null;

                        if (cabinetSpan.backing)
                        {
                            backingOccupancyInWorldSpace =
                                cabinetSpan.GetBackingOccupancyCuboidInWorldSpace(
                                    cabinetSpan.backing.info,
                                    cabinetSpan.backing
                                );
                        }

                        foreach (ElementIndex<Cabinet> i in cabinetSpan.cabinets)
                        {
                            this._metersFromVolumeMinX.value = 0.0f;
                            this._metersFromVolumeMinY.value = 0.0f;

                            bool didAdd =
                                await i.element.TryAddOccupantCentered(
                                    args.draggedOccupant,
                                    backingOccupancyInWorldSpace,
                                    this._metersFromVolumeMinX,
                                    this._metersFromVolumeMinY
                                );

                            if (didAdd)
                            {
                                return;
                            }
                        }
                    }

                    MelcherLog.LogError("Failed to resize destination cabinet and could not locate a suitable alternative location.", args.draggedOccupant);
                }

                this._kitchen.RotateCameraToFrontOfCabinet(args.draggedOccupant.cabinet);
            }
        }

        internal float DoDraggedOccupantSnapping(OccupiableVolume occupiableVolume, Occupant draggedOccupant, float volumeLocalDragPositionX)
        {
            float productWidthInMeters =
                occupiableVolume.CalculateOccupancyCuboidForOccupant(
                    draggedOccupant,
                    overridePositionInVolumeX: 0.0f
                ).width;

            float occupantMetersFromVolumeMinX =
                volumeLocalDragPositionX - (productWidthInMeters * 0.5f);

            occupantMetersFromVolumeMinX =
                SnapOccupantToOtherOccupantCenters(
                    occupiableVolume,
                    draggedOccupant,
                    occupantMetersFromVolumeMinX,
                    productWidthInMeters,
                    maxSnapDistanceInMeters: 4.0f * Units.Inches2Meters,
                    out Occupant snappedTo
                );

            if (!snappedTo)
            {
                occupantMetersFromVolumeMinX =
                    SnapOccupantToVolumeIncrement(occupantMetersFromVolumeMinX);
            }

            Cuboid occupiableCuboid = occupiableVolume.occupiableCuboid;

            OrientedBounds bounds =
                new OrientedBounds(
                    new Bounds(occupiableCuboid.center, occupiableCuboid.size),
                    occupiableVolume.GetTransform().worldToLocalMatrix
                );

            bounds.Draw(Color.red, duration: 3.0f, drawX: true);

            return Mathf.Clamp(occupantMetersFromVolumeMinX, occupiableCuboid.xMin, occupiableCuboid.xMax - productWidthInMeters);
        }

        internal float SnapOccupantToVolumeIncrement(float metersFromVolumeMinX)
        {
            if (this._session.UserProfile.activeUnits == MeasurementUnit.Centimeters)
            {
                return Mathf.Round(metersFromVolumeMinX * Units.Meters2Centimeters) * Units.Centimeters2Meters;
            }

            return
                (float)ImperialUnit.FromTotalMeters(metersFromVolumeMinX)
                    .RoundToNearestDivision(8).totalMeters;
        }

        internal float SnapOccupantToOtherOccupantCenters(
            OccupiableVolume    occupiableVolume,
            Occupant            draggedOccupant,
            float               metersFromVolumeMinX,
            float               productWidthInMeters,
            float               maxSnapDistanceInMeters,
            out Occupant        snappedTo)
        {
            snappedTo = null;

            float centerPositionMetersFromVolumeMinX =
                metersFromVolumeMinX + productWidthInMeters * 0.5f;

            float closestCenterDistance = float.MaxValue;

            foreach (Occupant otherOccupant in occupiableVolume.occupants)
            {
                if (draggedOccupant == otherOccupant)
                {
                    continue;
                }

                Cuboid otherOccupantSize =
                    occupiableVolume.CalculateOccupancyCuboidForOccupant(otherOccupant);

                float otherCenterPositionMetersFromCabinetMinX =
                    otherOccupant.metersFromVolumeMinX + (otherOccupantSize.width * 0.5f);

                float horizontalDistanceInMeters =
                    Mathf.Abs(otherCenterPositionMetersFromCabinetMinX - centerPositionMetersFromVolumeMinX);

                if (horizontalDistanceInMeters < Mathf.Min(maxSnapDistanceInMeters, closestCenterDistance))
                {
                    snappedTo = otherOccupant;

                    closestCenterDistance = horizontalDistanceInMeters;

                    metersFromVolumeMinX =
                        otherCenterPositionMetersFromCabinetMinX - (productWidthInMeters * 0.5f);
                }
            }

            return metersFromVolumeMinX;
        }

        private UniTask? _lastOccupantDraggedTask = default;
        private Ref<float> _metersFromVolumeMinX = new (0.0f);
        private Ref<float> _metersFromVolumeMinY = new (0.0f);
    }
}
