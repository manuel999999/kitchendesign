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
    public sealed class KitchenWidgetsControl
    {
        private readonly SessionStatus _session;
        private readonly ClientListStatus _clientList;
        private readonly Kitchen _kitchen;
        private readonly KitchenInteractionState _interaction;
        private readonly KitchenSelections _selections;
        private readonly MelcherLocalizedText _topInfoLabel;
        private readonly Transform _tickerDimensionsRoot;
        private readonly CabinetTickerWidget _tickerDimensionsPrefab;
        private readonly ButtonWidget _inputCabinetSizeWidget;
        private readonly ResizerWidget _resizeCabinetWidgetPrefab;
        private readonly Transform _cabinetResizeWidgetRoot;
        private readonly Transform _cabinetShelfResizeWidgetRoot;

        internal KitchenWidgetsControl(
            Kitchen kitchen,
            KitchenInteractionState interaction,
            KitchenSelections selections,
            MelcherLocalizedText topInfoLabel,
            Transform tickerDimensionsRoot,
            CabinetTickerWidget tickerDimensionsPrefab,
            ButtonWidget inputCabinetSizeWidget,
            ResizerWidget resizeCabinetWidgetPrefab,
            Transform cabinetResizeWidgetRoot,
            Transform cabinetShelfResizeWidgetRoot,
            SessionStatus session,
            ClientListStatus clientList)
        {
            this._session = session;
            this._clientList = clientList;
            this._kitchen = kitchen;
            this._interaction = interaction;
            this._selections = selections;
            this._topInfoLabel = topInfoLabel;
            this._tickerDimensionsRoot = tickerDimensionsRoot;
            this._tickerDimensionsPrefab = tickerDimensionsPrefab;
            this._inputCabinetSizeWidget = inputCabinetSizeWidget;
            this._resizeCabinetWidgetPrefab = resizeCabinetWidgetPrefab;
            this._cabinetResizeWidgetRoot = cabinetResizeWidgetRoot;
            this._cabinetShelfResizeWidgetRoot = cabinetShelfResizeWidgetRoot;
        }

        internal void RefreshTickerLabels()
        {
            foreach (Transform child in this._tickerDimensionsRoot)
            {
                if (child.TryGetComponent(out CabinetTickerWidget ticker))
                {
                    ticker.RefreshLabel();
                }
            }
        }
        internal void RefreshTopTickerInfo()
        {
            string topInfoPhrase = "";

            if (this._selections.layoutPrefab)
            {
                string combinedClientName = this._clientList.TryGetClientCombinedName();

                string countertopThicknessDisplayFormat = "";

                MeasurementUnit units = this._session.UserProfile.activeUnits;

                if (units == MeasurementUnit.Centimeters)
                {
                    countertopThicknessDisplayFormat =
                        Utility.MeasurementFormat.FormatMetersToCentimeters(
                            this._selections.countertopThicknessInMeters
                        );
                }
                else
                {
                    countertopThicknessDisplayFormat =
                    Utility.MeasurementFormat.FormatMetersToImperial(this._selections.countertopThicknessInMeters);
                }

                topInfoPhrase = $"{combinedClientName}. {this._selections.layoutPrefab.displayName}. Counter-top Thickness: {countertopThicknessDisplayFormat}";
            }

            this._topInfoLabel.phrase = topInfoPhrase;
        }

        internal void RecreateResizeWidgets()
        {
            foreach (Transform child in this._cabinetResizeWidgetRoot)
            {
                if (child.TryGetComponent(out ResizerWidget oldResizerWidget))
                {
                    oldResizerWidget.onResizeBegin -= OnCabinetResizeBegin;
                    oldResizerWidget.onResizeEnd -= OnCabinetResizeEnd;
                    oldResizerWidget.onResized -= OnCabinetResizedByWidget;
                }

                Object.Destroy(child.gameObject);
            }

            if (this._kitchen.layout)
            {
                Camera mainCamera = Camera.main;
                foreach (ElementIndex<Cabinet> i in this._kitchen.layout.EnumerateCabinets())
                {
                    Cabinet cabinet = i.element;

                    if (cabinet.isResizable)
                    {
                        CreateResizeWidgetsForCabinet(cabinet, mainCamera);
                    }
                }

                SetCabinetResizeWidgetsVisible(false, forceAffectAll: true);
            }
        }
        internal (ResizerWidget left, ButtonWidget middle, ResizerWidget right) CreateResizeWidgetsForCabinet(Cabinet cabinet, Camera mainCamera)
        {
            float zAnchor =
                cabinet.cabinetType is CabinetType.Space or CabinetType.CornerWedge ?
                    0.0f :
                    1.0f;

            ResizerWidget leftWidget =
                CreateResizerWidget(
                    "ResizeLeft_" + cabinet.name,
                    new Vector3(0.0f, 1.0f, zAnchor),
                    Extent.Min
                );

            ResizerWidget rightWidget =
                CreateResizerWidget(
                    "ResizeRight_" + cabinet.name,
                    new Vector3(1.0f, 1.0f, zAnchor),
                    Extent.Max
                );

            ButtonWidget middleWidget =
                CreateKitchenWidget(
                    this._inputCabinetSizeWidget,
                    widgetParent: this._cabinetResizeWidgetRoot,
                    mainCamera,
                    cabinet
                );

            middleWidget.name = "InputCabinetSizeWidget";
            middleWidget.normalizedTargetAnchor = new Vector3(0.5f, 1.0f, 1.0f);
            middleWidget.onClicked += OnInputCabinetSizeClicked;

            return (leftWidget, middleWidget, rightWidget);

            ResizerWidget CreateResizerWidget(string name, Vector3 anchor, Extent affectExtent)
            {
                ResizerWidget resizer =
                    CreateKitchenWidget(
                        this._resizeCabinetWidgetPrefab,
                        this._cabinetResizeWidgetRoot,
                        mainCamera,
                        cabinet
                    );

                resizer.name = name;
                resizer.normalizedTargetAnchor = anchor;
                resizer.affectExtent = affectExtent;
                resizer.onResizeBegin += OnCabinetResizeBegin;
                resizer.onResizeEnd += OnCabinetResizeEnd;
                resizer.onResized += OnCabinetResizedByWidget;

                return resizer;
            }
        }
        internal ResizerWidget CreateCabinetResizer(Cabinet cabinet, Camera mainCamera)
        {
            ResizerWidget resizer =
                Object.Instantiate(
                    this._resizeCabinetWidgetPrefab,
                    parent: this._cabinetResizeWidgetRoot
                );

            resizer.target = cabinet;
            resizer.worldCamera = mainCamera;
            resizer.onResizeBegin += OnCabinetResizeBegin;
            resizer.onResizeEnd += OnCabinetResizeEnd;
            resizer.onResized += OnCabinetResizedByWidget;

            return resizer;
        }
        internal void RecreateTickerWidgets()
        {
            foreach (Transform child in this._tickerDimensionsRoot)
            {
                Object.Destroy(child.gameObject);
            }

            if (!this._kitchen.layout)
            {
                return;
            }

            foreach (CabinetSpan span in this._kitchen.layout.spans)
            {
                foreach (ElementIndex<Cabinet> i in span.cabinets)
                {
                    Cabinet cabinet = i.element;

                    if (!cabinet.isResizable && !cabinet.canHaveProducts)
                    {
                        continue;
                    }

                    CabinetTickerWidget widget =
                        Object.Instantiate(
                            this._tickerDimensionsPrefab,
                            this._tickerDimensionsRoot
                        );

                    widget.targetCabinetSpan = span.index;
                    widget.RefreshLabel();

                    break;
                }
            }
        }

        internal void SetCabinetResizeWidgetsVisible(bool visible, bool forceAffectAll = false)
        {
            var cabinetWidgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    root: this._cabinetResizeWidgetRoot
                );

            if (forceAffectAll)
            {
                while (cabinetWidgets.MoveNext())
                {
                    cabinetWidgets.Current.SetVisible(visible);
                }

                return;
            }

            Cabinet mustMatch = null;
            this._interaction.highlightObject?.TryGetComponent(out mustMatch);

            while (cabinetWidgets.MoveNext())
            {
                KitchenWidget widget = cabinetWidgets.Current;

                if (mustMatch && widget.target != mustMatch)
                {
                    continue;
                }

                Cabinet cabinetForWidget =
                    (Cabinet)cabinetWidgets.Current.target;

                if (cabinetForWidget.isResizable && widget.isVisible != visible)
                {
                    cabinetWidgets.Current.SetVisible(visible);
                }
            }
        }
        internal void SetCabinetShelfResizeWidgetsVisible(OccupiableVolume target)
        {
            if (target && target is CabinetShelf shelf && !shelf.isResizable)
            {
                target = null;
            }

            var shelfWidgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    root: this._cabinetShelfResizeWidgetRoot
                );

            while (shelfWidgets.MoveNext())
            {
                shelfWidgets.Current.target = target;
                shelfWidgets.Current.SetVisible(target != null);
            }
        }
        internal void OnCabinetResizeBegin(ResizerWidget caller)
        {
            if (this._interaction.volumeBeingResized != null || this._interaction.productBeingDragged)
            {
                return;
            }

            this._interaction.volumeBeingResized = caller.target;

            var widgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    this._cabinetResizeWidgetRoot
                );

            while (widgets.MoveNext())
            {
                widgets.Current.SetVisible(caller == widgets.Current);
            }
        }
        internal void OnCabinetResizeEnd(ResizerWidget caller)
        {
            if (this._interaction.volumeBeingResized == caller.target)
            {
                this._interaction.volumeBeingResized = null;

                SetCabinetResizeWidgetsVisible(true);
            }
        }
        internal void OnCabinetShelfResizeBegin(ResizerWidget caller)
        {
            if (this._interaction.volumeBeingResized != null || this._interaction.productBeingDragged)
            {
                return;
            }

            this._interaction.volumeBeingResized = caller.target;

            var widgets =
                new ComponentsInChildrenEnumerator<KitchenWidget>(
                    this._cabinetShelfResizeWidgetRoot
                );

            while (widgets.MoveNext())
            {
                widgets.Current.SetVisible(caller == widgets.Current);
            }
        }
        internal void OnCabinetShelfResizeEnd(ResizerWidget caller)
        {
            if (this._interaction.volumeBeingResized == caller.target)
            {
                this._interaction.volumeBeingResized = null;

                SetCabinetShelfResizeWidgetsVisible(caller.target);
            }
        }

        internal async UniTask OnCabinetShelfResized(ResizerWidget caller, float deltaLengthInMeters)
        {
            CabinetShelf resizedShelf = (CabinetShelf)caller.target;

            await EventBridge.Designer.onOccupantResized.InvokeIfNonEmpty(resizedShelf.occupant);
        }
        internal void OnInputCabinetSizeClicked(ButtonWidget caller)
        {
            EventBridge.Designer.showSetOccupiableVolumeWidthPrompt?.Invoke(caller.target);
        }
        internal async UniTask OnCabinetResizedByWidget(ResizerWidget cabinetResizerWidget, float lengthMetersDelta)
        {
            Cabinet cabinet = (Cabinet)cabinetResizerWidget.target;

            if (cabinetResizerWidget.affectExtent == Extent.Min && !cabinet.Is(CabinetType.CornerWedge))
            {
                Transform targetTransform =
                    cabinetResizerWidget.target.GetTransform();

                targetTransform.parent.position +=
                    targetTransform.TransformDirection(Vector3.right) * -lengthMetersDelta;
            }

            await EventBridge.Designer.onCabinetResized.InvokeIfNonEmpty(cabinet);
        }
        internal static WidgetType CreateKitchenWidget<WidgetType>(WidgetType widgetPrefab, Transform widgetParent, Camera eventCamera, OccupiableVolume target)
            where WidgetType : KitchenWidget
        {
            WidgetType widget = Object.Instantiate(widgetPrefab, widgetParent);

            widget.worldCamera = eventCamera;
            widget.target = target;

            return widget;
        }
    }
}
