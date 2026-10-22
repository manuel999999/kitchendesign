namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using JsonDict = System.Collections.Generic.IDictionary<string, object>;

    public enum ServerType
    {
        /// <summary>
        ///     The live/published app. Intended for real end users.
        /// </summary>
        Production,

        /// <summary>
        ///     A protected internal build intended for client testing.
        /// </summary>
        Test,

        // Ideally, we would have a dev server just for Melcher! -Ryan

    }   // enum ServerType

    public enum UserType
    {
        None,

        /// <summary>
        /// Indicates the user is using the 48-hour trial feature. Trials
        /// requires payment and approval in the backend. The trial can be
        /// renewed after it expires but this requires another payment.
        ///
        /// Trial users do not have the ability to whitelist manufacturers.
        /// </summary>
        Trial,

        /// <summary>
        /// Dealers are the first-class user type. They pay a monthy
        /// subscription cost and have no restrictions.
        /// </summary>
        Dealer,

        /// <summary>
        /// Logging in with guest/guest makes you an offline user. Offline
        /// users do not interact with any of the web systems but make mock
        /// calls/return mock data. They are used for testing.
        /// </summary>
        Offline,

    }   // enum UserType

    public struct User
    {
        public int id;

        public string apiToken;

        // =====================================================================
        // Non-server response data
        // =====================================================================

        /// <summary>
        /// The server doesn't actually return the user type during log in, but
        /// it can be learned from the user's profile. To make our API more
        /// convenient, we copy the profile's user type back into the user
        /// object after the profile is parsed.
        /// </summary>
        public UserType userType;

    }   // struct User

    /// <summary>
    ///     This contains the names of companies who will use the Pro-Fit app
    ///     and who need their data to be easily filtered in the dashboard.
    ///     For example, the backend has one table for dealers and one table
    ///     for quotes; to allow both Pro-Fit and Stoll to use the same
    ///     backend, the dashboard's results must be filtered by some value and
    ///     this is it. Server-side, this value is stored in a Dealer field
    ///     named "type".
    ///     <br /><br />
    ///
    ///     The main value for this type is stored in <see
    ///     cref="AppOptions.userFilter"/> and it is responsible
    ///     for associating new accounts to a particular client. This is then
    ///     used to filter which quotes are shown in the dashboard and which
    ///     products are displayed in the app.
    ///     <br /><br />
    ///
    ///     It is important that the names of these enums are compatible with
    ///     the names the server expects. The server uses "profit" and "stoll"
    ///     as permissible values, so these values can be converted to a string
    ///     and made lower case to become compatible.
    /// </summary>
    public enum UserFilter
    {
        None,

        ProFit,

        Stoll,

        Gemini,

    }   // enum UserFilter

    public struct UserProfile
    {
        // ====================================================================
        // Methods
        // ====================================================================

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
            // An active subscription overrides a trial
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

        // ====================================================================
        // Properties
        // ====================================================================

        public string contactInfo
        {
            get => string.IsNullOrEmpty(contactUrl) ? contactEmail : contactUrl;
            set
            {
                // Currently, there's no reason to have both a contact email
                // and a contact url.
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

        // ====================================================================
        // Public fields
        // ====================================================================

        public int userId;

        /// <summary>
        /// <see langword="true"/> if the account is waiting for approval;
        /// otherwise, <see langword="false"/>.
        /// <br /><br />
        ///
        /// New accounts are pending until an admin approves them in the
        /// backend. While an account is pending, it must be prevented from
        /// using the app. We will show a pop-up informing the user of their
        /// status if it is preventing log in.
        /// </summary>
        public bool isPending;

        /// <summary>
        /// <see langword="true"/> if the account has been approved in the
        /// backend; otherwise, <see langword="false"/>.
        /// <br /><br />
        ///
        /// Accounts that are pending (see <see cref="isPending"/>) can be
        /// approved or rejected in the backend. Rejected accounts can still
        /// be approved but will no longer be considered pending. Accounts that
        /// are approved, remain approved.
        /// </summary>
        public bool isApproved;

        /// <summary>
        /// <see langword="true"/> if the account has been marked as active in
        /// the backend; otherwise, <see langword="false"/>.
        /// <br /><br />
        ///
        /// This value is used to mark whether an account is in good standing
        /// (payment-wise). While an account is inactive, it must be prevented
        /// from using the app. We will show a pop-up informing the user of
        /// their status if it is preventing log in.
        /// </summary>
        public bool isActive;

        /// <summary>
        /// True if this user was created to start a trial.
        /// </summary>
        public bool isTrial;

        /// <summary>
        /// DateTime that the 48-hour trial should expire.
        /// </summary>
        public System.DateTime? trialExpireUtc;

        public UserFilter userFilter;

        public UserType userType;

        public string signInEmail;

        public string address;
        public string city;
        public string state;
        public string zipCode;

        // Pro-contractors and dealers
        public string businessName;
        public string contactFirstName;
        public string contactLastName;
        public string contactEmail;
        public string contactUrl;
        /// <summary>
        /// Unformatted contact phone number. E.g. "1234567890".
        /// </summary>
        public string contactPhoneNumber;

        // Dealer-only
        public string authorizedDomain;
        public string whiteLabelReturnUrl;              // The URL to return the user to after they save a design
        public List<int> dealerSelectedManufacturerIds;    // JSON field: manufacturers

        /// <summary>
        ///     The list of admin-specified manufacturer IDs that will appear
        ///     normally in the app. If this list has one or more items, all
        ///     non-matching manufacturer IDs will be greyed out. allow in the app
        /// </summary>
        public List<int> adminSelectedManufacturerIds;    // JSON field: whitelisted_manufacturers

        /// <summary>
        ///     The list of admin-specified brand catalog IDs that are allowed
        ///     to appear in the app for this user.
        /// </summary>
        public List<int> adminSelectedBrandCatalogIds;

        /// <summary>
        ///     This represents the amount this manufacturer's prices are marked
        ///     up by the current dealer. This value can be customized in the
        ///     user's profile but is ultimately a server-side value that is
        ///     not used anywhere in the Unity app.
        /// </summary>
        public Dictionary<int, decimal> manufacturerIdToPriceMarkupPercentage;

        /// <summary>
        /// Whether this user is allowed to use the white labeled mode. White
        /// labeled mode only affects WebGL builds and it skips the menu scene
        /// so players do not have to register (but they still have to enter
        /// all their details).
        /// </summary>
        public bool isWhiteLabeled;

        /// <summary>
        ///     When enabled, the dashboard has forced this dealer to only
        ///     use inches.
        /// </summary>
        public bool isInchesOnly;

        /// <summary>
        /// A URL to the image to use during white labeled mode.
        /// </summary>
        public string whiteLabelImageUrl;

        /// <summary>
        ///     The user's preferred display units. This affects quotes the
        ///     user generates as well as any on-screen measurement UI.
        ///     <br /><br />
        ///
        ///     <b>WARNING</b>: This value may be overridden by <see
        ///     cref="isInchesOnly"/>. To guarantee the correct units are
        ///     used, always use <see cref="activeUnits"/>.
        /// </summary>
        public MeasurementUnit preferredUnits;

        /// <summary>
        ///     The user's active currency. Currently, this can only be
        ///     selected in the dashboard.
        /// </summary>
        public Currency currency;

    }   // struct UserProfile

    /// <summary>
    ///     The available currencies supported by the app.
    /// </summary>
    public enum Currency
    {
        USD,
        CAD
    }   // enum Currency

    /// <summary>
    ///     The available display modes for kitchen measurements.
    /// </summary>
    public enum MeasurementUnit
    {
        None,

        FeetAndInches,

        Centimeters

    }   // enum UnitMeasurements

    public static class MeasurementUnitExtensions
    {
        public static string ToDisplayName(this MeasurementUnit unit)
        {
            return
                unit switch
                {
                    MeasurementUnit.FeetAndInches => "Feet and Inches",
                    _ => unit.ToString()
                };
        }
    }

    /// <summary>
    ///     A <see cref="UserClient"/> is a person that designed a kitchen.
    ///     Each client has a parent <see cref="User"/> that created them and
    ///     clients can have zero or more quotes (the designs they created).
    /// </summary>
    public struct UserClient
    {
        public int id;
        public string firstName;
        public string lastName;
        public string email;
        /// <summary>
        /// Unformatted contact phone number. E.g., "1234567890".
        /// </summary>
        public string phoneNumber;
        public string address;
        public string city;
        public string state;
        public string zipCode;

        public List<UserClientQuote> quotes;

    }   // struct UserClient

    public struct UserClientQuote
    {
        public int id;
        public string saveName;

        /// <summary>
        ///     Optional user-provided notes for this design.
        /// </summary>
        public string saveNotes;

        /// <remarks>
        /// Will look something like:
        /// "/api/pdf/178/distributor?signature=eyJtZXNzYWdlIjoiL2FwaS9wZGYvMTc4L2Rpc3RyaWJ1dG9yIiwiZXhwaXJ5RGF0ZSI6IjIwMjItMDYtMDZUMDA6NTM6MjguMjA3WiJ9.lmukIblk0-TenHfX9zuDgv8yDe7P-tqmIlIjAY0Jjgs"
        /// </remarks>
        public string pdfEndpoint;

        public string coverImageName;
        public string topDownImageName;
        public string[] frontImageNames;
        public JsonDict saveData;
    }   // struct UserClientDesign

    /// <summary>
    ///     Represents a tag
    /// </summary>
    public struct ProductTagInfo
    {
        public int id;
        public string name;
    }   // struct ProductTagInfo

    public class CabinetCompatibilityInfo
    {
        public int id;
        public string name;
        public HashSet<int> productTagIdDenylist = new ();
        public HashSet<int> productTagIdAllowlist = new ();
    }   // class CompatibilityInfo

    public struct ManufacturerInfo
    {
        public int id;
        public string name;

        /// <summary>
        ///     The sort order of the product. Lowest to highest.
        /// </summary>
        public int order;

        /// <summary>
        ///     The product category this manufacturer was grouped with in the
        ///     dashboard. This affects how the manufacturer is listed in
        ///     <see cref="ManufacturerWhitelistUI"/>.
        /// </summary>
        public ProductCategory? productCategory;

        /// <summary>
        ///     A list of door styles that were defined by this manufacturer.
        ///     These get used by <see cref="KitchenBuildingMethod.Catalog"/>
        ///     to swap out what the doors and drawers look like on cabinets.
        /// </summary>
        public List<ManufacturerDoorStyleInfo> doorStyles;

    }   // struct ManufacturerInfo

    /// <summary>
    ///     Defines a type of door style. Manufactures have the ability to
    ///     define door styles which are variations on how their cabinets look
    ///     in <see cref="KitchenBuildingMethod.Catalog"/>.
    /// </summary>
    public struct ManufacturerDoorStyleInfo
    {
        public int id;
        public string name;
    }   // struct ManufacturerDoorStyleInfo

    /// <summary>
    /// A product group is a bucket of products. The products appear together
    /// on the toolbar. They are defined using the server front-end. Products
    /// from different manufacturers can be added to the same group and they
    /// will be shown together.
    /// <br /><br />
    ///
    /// A <see cref="ProductInfo.groupId"/> references the group ID that the
    /// product is using.
    /// </summary>
    public struct ProductGroupInfo
    {
        public int id;
        public string name;
    }   // struct ProductGroupInfo

    public struct BrandCatalogInfo
    {
        public int id;
        public string name;
        public int cabinetManufacturerId;
        public List<int> availableManufacturerIds;
        public List<int> availableGroupIds;
        public List<int> startingCabinetSectionServerIds;
        /// <summary>
        ///     A list of manually selected cabinets that are /not/ allowed to
        ///     be used, even if their manufacturer is present in <see
        ///     cref="availableManufacturerIds"/>.
        /// </summary>
        public List<int> allowedCabinetSectionServerIds;

    }   // struct BrandCatalogInfo

    /// <summary>
    ///     A cabinet section category is a named group of cabinet sections.
    ///     These are used to organize and order the cabinet section toolbar.
    /// </summary>
    public struct CabinetSectionCategoryInfo
    {
        public int id;
        public string name;
        public int sortOrder;
    }   // struct CabinetSectionCategory


    public static class ResponseCode
    {
        public const int OK = 200;
        public const int NotFound = 404;
    }   // class ResponseCode

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-28
/// ===========================================================================
