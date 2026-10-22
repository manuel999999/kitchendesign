using Doge.Events;

namespace KitchenDesign.Events
{
    public static class EventApp
    {
        public static ActionEvent OnSceneReady { get; } = new();
    }
}
