namespace KitchenDesign.Events
{
    public readonly struct SpinnerMessageInfo
    {
        public SpinnerMessageInfo(string message)
        {
            this.Message = message;
        }

        public string Message { get; }
    }
}
