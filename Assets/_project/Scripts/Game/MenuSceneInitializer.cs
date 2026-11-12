using UnityEngine;

namespace KitchenDesign
{
    public sealed class MenuSceneInitializer : SceneInitializer
    {
        [SerializeField] private MenuNavigationControl navigation;

        public AppSystems Systems { get; private set; }

        public override void Initialize(AppSystems systems)
        {
            this.Systems = systems;
            this.navigation.Initialize(systems);
        }
    }
}
