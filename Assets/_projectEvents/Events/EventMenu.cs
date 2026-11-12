using Doge.Events;

namespace KitchenDesign.Events
{
    public static class EventMenu
    {
        public static GenericEvent<SignUpInfo> OnSignUpRequested { get; } = new();
        public static GenericEvent<CredentialsInfo> OnSignInRequested { get; } = new();
        public static ActionEvent OnDeleteAccountRequested { get; } = new();
        public static GenericEvent<ResetPasswordInfo> OnResetPasswordRequested { get; } = new();
        public static GenericEvent<CredentialsInfo> OnPayNowRequested { get; } = new();
        public static GenericEvent<UnauthorizedDomainInfo> OnUnauthorizedDomainRequested { get; } = new();
        public static ActionEvent OnCreateClientRequested { get; } = new();
        public static ActionEvent OnClientListRequested { get; } = new();
        public static GenericEvent<ClientInfo> OnClientInfoRequested { get; } = new();
        public static GenericEvent<ClientInfo> OnDeleteClientRequested { get; } = new();
        public static ActionEvent OnClientQuoteListRequested { get; } = new();
        public static ActionEvent OnClientQuoteListRefreshRequested { get; } = new();
        public static GenericEvent<ClientQuoteInfo> OnQuoteInfoRequested { get; } = new();
        public static GenericEvent<ClientQuoteInfo> OnDeleteQuoteRequested { get; } = new();
        public static ActionEvent OnUserProfileRequested { get; } = new();
        public static ActionEvent OnManufacturerWhitelistRequested { get; } = new();
        public static ActionEvent OnLogOutPromptRequested { get; } = new();
        public static ActionEvent OnBackRequested { get; } = new();
        public static ActionEvent OnScreenOrientationToggleRequested { get; } = new();
        public static ActionEvent OnQuitPromptRequested { get; } = new();
    }
}
