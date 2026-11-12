using UnityEngine;

namespace KitchenDesign
{
    public sealed class SplashSceneInitializer : SceneInitializer
    {
        [SerializeField] private SplashVideoControl splashVideo;

        public override void Initialize(AppSystems systems)
        {
            this.splashVideo.Initialize();
        }
    }
}
