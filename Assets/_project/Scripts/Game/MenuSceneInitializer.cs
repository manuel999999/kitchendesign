using KitchenDesign.Scriptables;
using KitchenDesign.Shared.Interfaces;
using KitchenDesign.Products;

namespace KitchenDesign
{
    public sealed class MenuSceneInitializer : SceneInitializer
    {
        public AppProfileSO AppProfile { get; private set; }
        public IAppRules AppRules { get; private set; }

        public override void Initialize(AppProfileSO appProfile, IAppRules appRules, ProductVisualContentStatus productContent)
        {
            this.AppProfile = appProfile;
            this.AppRules = appRules;
        }
    }
}
