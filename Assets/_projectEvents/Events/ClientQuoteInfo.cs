namespace KitchenDesign.Events
{
    public readonly struct ClientQuoteInfo
    {
        public ClientQuoteInfo(int clientId, int quoteId)
        {
            this.ClientId = clientId;
            this.QuoteId = quoteId;
        }

        public int ClientId { get; }
        public int QuoteId { get; }
    }
}
