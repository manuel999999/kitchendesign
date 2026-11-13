namespace KitchenDesign.Events
{
    public readonly struct NotificationInfo
    {
        public NotificationInfo(string title, string message, System.Action onDismiss)
        {
            this.Title = title;
            this.Message = message;
            this.OnDismiss = onDismiss;
        }

        public string Title { get; }
        public string Message { get; }
        public System.Action OnDismiss { get; }
    }
}
