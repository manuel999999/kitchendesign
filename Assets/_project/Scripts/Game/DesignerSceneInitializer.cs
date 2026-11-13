using KitchenDesign.Cameras;
using KitchenDesign.UI.DesignerScreens;
using KitchenDesign.KitchenModel;
using KitchenDesign.Measures;
using UnityEngine;

namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        [SerializeField] private DesignerScreensControl screens;
        [SerializeField] private Kitchen kitchen;
        [SerializeField] private DesignerNavigationUI navigation;
        [SerializeField] private KitchenSelectionClickControl selectionClick;
        [SerializeField] private DesignerToolbarUI toolbar;
        [SerializeField] private CabinetOtherSizesUI cabinetSizes;
        [SerializeField] private SetVolumeSizeUI setVolumeSizePrompt;
        [SerializeField] private MeasurementOverlayManager measurementOverlay;
        [SerializeField] private OrbitCameraControl orbitCamera;

        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;

            this.kitchen.Initialize(systems);
            this.measurementOverlay.Initialize(systems.Session);

            KitchenEvents events = this.kitchen.events;
            this.navigation.ListenTo(events);
            this.toolbar.ListenTo(events);
            this.cabinetSizes.ListenTo(events);
            this.measurementOverlay.ListenTo(events);
            this.kitchen.ListenTo(events);
            this.orbitCamera.ListenTo(events);
            this.screens.ListenTo(events);
            this.setVolumeSizePrompt.ListenTo(events);

            this.screens.Initialize(systems);
            this.selectionClick.Initialize(this.kitchen, this.navigation);
        }

        private void OnDestroy()
        {
            if (this.Systems == null)
            {
                return;
            }

            this.Systems.ProductContent.Purge();
        }
    }
}
