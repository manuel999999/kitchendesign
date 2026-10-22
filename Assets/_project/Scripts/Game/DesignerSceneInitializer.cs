using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        public AppProfile AppProfile { get; private set; }
        public IAppRules AppRules { get; private set; }

        public override void Initialize(AppProfile appProfile, IAppRules appRules)
        {
            this.AppProfile = appProfile;
            this.AppRules = appRules;
        }
    }
}
