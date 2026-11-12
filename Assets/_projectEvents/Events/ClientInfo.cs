namespace KitchenDesign.Events
{
    public readonly struct ClientInfo
    {
        public ClientInfo(int clientId)
        {
            this.ClientId = clientId;
        }

        public int ClientId { get; }
    }
}
