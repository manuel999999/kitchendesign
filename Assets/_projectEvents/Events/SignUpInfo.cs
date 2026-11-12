namespace KitchenDesign.Events
{
    public readonly struct SignUpInfo
    {
        public SignUpInfo(KitchenDesign.Shared.Enums.UserType userType)
        {
            this.UserType = userType;
        }

        public KitchenDesign.Shared.Enums.UserType UserType { get; }
    }
}
