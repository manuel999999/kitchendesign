using KitchenDesign.CatalogData;
using KitchenDesign.Products;
using KitchenDesign.Scriptables;
using KitchenDesign.Server;
using KitchenDesign.Session;
using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign
{
    public sealed class AppSystems
    {
        public AppSystems(
            AppProfileSO profile,
            IAppRules rules,
            LaunchOptions launchOptions,
            ServerClient server,
            CatalogDataStatus catalogData,
            ClientListStatus clientList,
            SessionStatus session,
            ProductVisualContentStatus productContent)
        {
            this.Profile = profile;
            this.Rules = rules;
            this.LaunchOptions = launchOptions;
            this.Server = server;
            this.CatalogData = catalogData;
            this.ClientList = clientList;
            this.Session = session;
            this.ProductContent = productContent;
        }

        public AppProfileSO Profile { get; }
        public AppOptionsSO Options => this.Profile.appOptions;
        public IAppRules Rules { get; }
        public LaunchOptions LaunchOptions { get; }
        public ServerClient Server { get; }
        public CatalogDataStatus CatalogData { get; }
        public ClientListStatus ClientList { get; }
        public SessionStatus Session { get; }
        public ProductVisualContentStatus ProductContent { get; }
    }
}
