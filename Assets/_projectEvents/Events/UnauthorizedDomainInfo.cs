namespace KitchenDesign.Events
{
    public readonly struct UnauthorizedDomainInfo
    {
        public UnauthorizedDomainInfo(string authorizedDomain)
        {
            this.AuthorizedDomain = authorizedDomain;
        }

        public string AuthorizedDomain { get; }
    }
}
