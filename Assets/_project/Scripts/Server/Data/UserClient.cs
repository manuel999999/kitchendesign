using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public struct UserClient
    {
        public int id;
        public string firstName;
        public string lastName;
        public string email;
        public string phoneNumber;
        public string address;
        public string city;
        public string state;
        public string zipCode;

        public List<UserClientQuote> quotes;
    }
}
