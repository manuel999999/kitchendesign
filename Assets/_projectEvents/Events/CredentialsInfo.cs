namespace KitchenDesign.Events
{
    public readonly struct CredentialsInfo
    {
        public CredentialsInfo(string email, string password)
        {
            this.Email = email;
            this.Password = password;
        }

        public string Email { get; }
        public string Password { get; }
    }
}
