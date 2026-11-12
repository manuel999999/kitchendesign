using UnityEngine;

namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        [SerializeField] private DesignerScreensControl screens;

        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;
            this.screens.Initialize(systems);
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
