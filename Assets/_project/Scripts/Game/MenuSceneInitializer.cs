namespace KitchenDesign
{
    public sealed class MenuSceneInitializer : SceneInitializer
    {
        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;
        }
    }
}
