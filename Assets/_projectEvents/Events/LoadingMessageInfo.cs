namespace KitchenDesign.Events
{
    public readonly struct LoadingMessageInfo
    {
        public LoadingMessageInfo(string message)
        {
            this.Message = message;
        }

        public string Message { get; }
    }
}
