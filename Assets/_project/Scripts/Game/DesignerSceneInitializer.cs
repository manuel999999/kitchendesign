using KitchenDesign.Products;
using KitchenDesign.Scriptables;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign
{
    public sealed class DesignerSceneInitializer : SceneInitializer
    {
        public AppProfileSO AppProfile { get; private set; }
        public IAppRules AppRules { get; private set; }

        private ProductVisualContentStatus _productContent;

        public override void Initialize(AppProfileSO appProfile, IAppRules appRules, ProductVisualContentStatus productContent)
        {
            this.AppProfile = appProfile;
            this.AppRules = appRules;
            this._productContent = productContent;
        }

        private void OnDestroy()
        {
            if (this._productContent == null)
            {
                return;
            }

            this._productContent.Purge();
        }
    }
}
