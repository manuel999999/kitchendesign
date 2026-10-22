using KitchenDesign.Shared.Interfaces;

namespace KitchenDesign.Server
{
    public sealed class ServerClient
    {
        public ServerClient(AppProfile appProfile, IAppRules appRules)
        {
            this.Connection = new ServerConnection(appProfile);
            this.Accounts = new AccountRequests(this.Connection);
            this.Clients = new ClientRequests(this.Connection);
            this.Quotes = new QuoteRequests(this.Connection);
            this.Catalog = new CatalogRequests(this.Connection, appRules);
            this.Analytics = new AnalyticsRequests(this.Connection);
        }

        public ServerConnection Connection { get; }
        public AccountRequests Accounts { get; }
        public ClientRequests Clients { get; }
        public QuoteRequests Quotes { get; }
        public CatalogRequests Catalog { get; }
        public AnalyticsRequests Analytics { get; }
    }
}
