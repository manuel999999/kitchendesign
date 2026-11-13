using Doge.Events;
using KitchenDesign.Shared.Enums;

namespace KitchenDesign.Events
{
    public static class EventDesigner
    {
        public static ActionEvent OnWhiteLabelClientInfoRequested { get; } = new();
        public static ActionEvent OnBuildingMethodsRequested { get; } = new();
        public static ActionEvent OnBrandCatalogsRequested { get; } = new();
        public static ActionEvent OnCabinetShapesRequested { get; } = new();
        public static ActionEvent OnKitchenTemplatesRequested { get; } = new();
        public static GenericEvent<CabinetSizesMode> OnCabinetSizesRequested { get; } = new();
        public static ActionEvent OnProductSearchRequested { get; } = new();
        public static GenericEvent<NotificationInfo> OnNotificationRequested { get; } = new();
        public static GenericEvent<ToastInfo> OnToastRequested { get; } = new();
        public static GenericEvent<DesignerLoadingMode> OnLoadingScreenRequested { get; } = new();
        public static ActionEvent OnLoadingImageRefreshRequested { get; } = new();
        public static GenericEvent<LoadingMessageInfo> OnLoadingMessageRequested { get; } = new();
    }
}
