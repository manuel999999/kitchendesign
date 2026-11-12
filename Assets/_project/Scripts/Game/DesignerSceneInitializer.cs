namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;
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
