namespace KitchenDesign.Events
{
    public readonly struct MenuSceneRequestInfo
    {
        public MenuSceneRequestInfo(System.Action onLoaded)
        {
            this.OnLoaded = onLoaded;
        }

        public System.Action OnLoaded { get; }
    }
}
