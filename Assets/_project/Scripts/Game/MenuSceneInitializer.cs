using KitchenDesign.Scriptables;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign
{
    public sealed class MenuSceneInitializer : SceneInitializer
    {
        public AppProfileSO AppProfile { get; private set; }
        public IAppRules AppRules { get; private set; }

        public override void Initialize(AppProfileSO appProfile, IAppRules appRules)
        {
            this.AppProfile = appProfile;
            this.AppRules = appRules;
        }
    }
}
