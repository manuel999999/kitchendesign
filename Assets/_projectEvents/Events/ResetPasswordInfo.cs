namespace KitchenDesign.Events
{
    public readonly struct ResetPasswordInfo
    {
        public ResetPasswordInfo(string email)
        {
            this.Email = email;
        }

        public string Email { get; }
    }
}
