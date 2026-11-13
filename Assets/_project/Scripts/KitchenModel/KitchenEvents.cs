namespace KitchenDesign.KitchenModel
{
    using Cysharp.Threading.Tasks;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Scriptables;
    using KitchenDesign.Shared.Enums;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public sealed class KitchenEvents
    {
        public delegate void LayoutChangedHandler(CabinetLayout layoutPrefab, CabinetLayout layoutInstance);
        public delegate void OccupantDragHandler(Occupant draggedOccupant, PointerEventData eventData);
        public delegate void OccupantNudgedHandler(Occupant nudgedOccupant, Vector3 cabinetLocalNudgeDistanceInMeters);

        public Action restart;
        public Action<KitchenBuildingMethod> onBuildingMethodChanged;
        public SequentiallyAwaitedDelegate<BrandCatalogSO> onBrandCatalogChanged = new ();
        public SequentiallyAwaitedDelegate onKitchenReady = new ();
        public LayoutChangedHandler onKitchenLayoutChanged;
        public SequentiallyAwaitedDelegate<GameObject> onHighlightChanged = new ();

        public SequentiallyAwaitedDelegate<Cabinet> onCabinetAddedByUser = new ();
        public SequentiallyAwaitedDelegate<Cabinet> onCabinetResized = new ();
        public SequentiallyAwaitedDelegate<(Cabinet reorderedCabinet, int previousCabinetIndex, List<CabinetSpan> previousCabinetSpans)> onCabinetReordered = new ();
        public Action<Cabinet> onPreCabinetDestroyedByUser;
        public SequentiallyAwaitedDelegate<int> onCabinetDestroyedByUser = new ();
        public Action<Cabinet> onCabinetSwapStart;
        public Action onCabinetSwapCanceled;

        public SequentiallyAwaitedDelegate<Occupant> onOccupantAddedToCabinet = new ();
        public SequentiallyAwaitedDelegate<Occupant> onOccupantResized = new ();
        public OccupantDragHandler onOccupantDragSuccess;
        public OccupantNudgedHandler onOccupantNudged;

        public Action<string> onDoorHandleStyleNameUpdate;
        public Action<OccupiableVolume> onSetVolumeWidthRequested;
    }
}
