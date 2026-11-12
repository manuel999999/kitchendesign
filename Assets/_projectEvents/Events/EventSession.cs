using Doge.Events;

namespace KitchenDesign.Events
{
    public static class EventSession
    {
        public static GenericEvent<SpinnerMessageInfo> OnSpinnerMessageRequested { get; } = new();
        public static GenericEvent<SpinnerInfo> OnFullScreenSpinnerRequested { get; } = new();
        public static GenericEvent<SpinnerInfo> OnBackgroundSpinnerRequested { get; } = new();
        public static ActionEvent OnHomeRequested { get; } = new();
        public static ActionEvent OnWelcomeRequested { get; } = new();
        public static ActionEvent OnLogInRequested { get; } = new();
        public static ActionEvent OnTrialCountdownRequested { get; } = new();
        public static ActionEvent OnLoggedOut { get; } = new();
        public static ActionEvent OnSubscriptionActivated { get; } = new();
        public static GenericEvent<UserKickedInfo> OnUserKickedPromptRequested { get; } = new();
        public static GenericEvent<MenuSceneRequestInfo> OnMenuSceneRequested { get; } = new();
    }
}
