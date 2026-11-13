namespace KitchenDesign.Events
{
    public readonly struct ToastInfo
    {
        public ToastInfo(string toast, float holdDuration = 1.0f)
        {
            this.Toast = toast;
            this.HoldDuration = holdDuration;
        }

        public string Toast { get; }
        public float HoldDuration { get; }
    }
}
