using KitchenDesign.KitchenModel;
using UnityEngine;

namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        [SerializeField] private DesignerScreensControl screens;
        [SerializeField] private Kitchen kitchen;
        [SerializeField] private DesignerNavigationUI navigation;
        [SerializeField] private KitchenSelectionClickControl selectionClick;

        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;
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
