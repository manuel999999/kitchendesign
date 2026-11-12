using UnityEngine;

namespace KitchenDesign
{
    public sealed class PlatformSetupSceneInitializer : SceneInitializer
    {
        [SerializeField] private PlatformSetupControl platformSetup;

        public override void Initialize(AppSystems systems)
        {
            this.platformSetup.Initialize(systems);
        }
    }
}
