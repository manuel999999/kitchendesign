namespace KitchenDesign.Events
{
    public readonly struct SpinnerInfo
    {
        public SpinnerInfo(bool isShown)
        {
            this.IsShown = isShown;
        }

        public bool IsShown { get; }
    }
}
