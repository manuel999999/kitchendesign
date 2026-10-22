using KitchenDesign.Shared.Enums;
using System.Collections.Generic;

namespace KitchenDesign.Server
{
    public struct UserProfile
    {
        public bool IsSubscriptionOrTrialActive()
        {
            return IsSubscriptionActive() || IsTrialActive();
        }

        public bool IsSubscriptionActive()
        {
            return isApproved && isActive && !isPending;
        }

        public bool IsTrialActive()
        {
            return !IsSubscriptionActive() && isTrial && trialExpireUtc.GetValueOrDefault() > System.DateTime.UtcNow;
        }

        public decimal GetMarkupPercentageForManufacturerId(int manufacturerId)
        {
            manufacturerIdToPriceMarkupPercentage.TryGetValue(manufacturerId, out decimal markupPercentage);
            return markupPercentage;
        }

        public void SetMarkupPercentageForManufacturerId(int manufacturerId, decimal markupPercentage)
        {
            manufacturerIdToPriceMarkupPercentage[manufacturerId] = markupPercentage;
        }

        public string contactInfo
        {
            get => string.IsNullOrEmpty(contactUrl) ? contactEmail : contactUrl;
            set
            {
                contactEmail = "";
                contactUrl = null;

                if (value.Contains('@'))
                {
                    contactEmail = value;
                }
                else
                {
                    contactUrl = value;
                }
            }
        }

        public MeasurementUnit activeUnits
        {
            get
            {
                if (isInchesOnly)
                {
                    return MeasurementUnit.FeetAndInches;
                }

                return preferredUnits;
            }
        }

        public int userId;

        public bool isPending;

        public bool isApproved;

        public bool isActive;

        public bool isTrial;

        public System.DateTime? trialExpireUtc;

        public UserFilter userFilter;

        public UserType userType;

        public string signInEmail;

        public string address;
        public string city;
        public string state;
        public string zipCode;

        public string businessName;
        public string contactFirstName;
        public string contactLastName;
        public string contactEmail;
        public string contactUrl;
        public string contactPhoneNumber;

        public string authorizedDomain;
        public string whiteLabelReturnUrl;
        public List<int> dealerSelectedManufacturerIds;

        public List<int> adminSelectedManufacturerIds;

        public List<int> adminSelectedBrandCatalogIds;

        public Dictionary<int, decimal> manufacturerIdToPriceMarkupPercentage;

        public bool isWhiteLabeled;

        public bool isInchesOnly;

        public string whiteLabelImageUrl;

        public MeasurementUnit preferredUnits;

        public Currency currency;
    }
}
