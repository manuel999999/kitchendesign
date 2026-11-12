namespace KitchenDesign.Events
{
    public readonly struct UserKickedInfo
    {
        public UserKickedInfo(string reason)
        {
            this.Reason = reason;
        }

        public string Reason { get; }
    }
}
