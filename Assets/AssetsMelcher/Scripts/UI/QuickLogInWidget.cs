namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    public class QuickLogInWidget : MelcherMonoBehaviour
    {
        public delegate void QuickLogInHandler(string email, string password);
        public static event QuickLogInHandler onQuickLogIn;

        public string email => m_Email;
        public string password => m_Password;

        // ====================================================================
        // Methods
        // ====================================================================

        private void Start()
        {
            gameObject.SetActive(
                ProFitGlobals.appProfile.appOptions.userFilter == m_UserFilter
            );
        }

        private void OnEnable()
        {
            m_Button.onClick += OnQuickLogIn;
        }

        private void OnDisable()
        {
            m_Button.onClick -= OnQuickLogIn;
        }

        private void OnQuickLogIn(MelcherButton caller)
        {
            onQuickLogIn?.Invoke(m_Email, m_Password);
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField] protected string m_Email = null;
        [SerializeField] protected string m_Password = null;
        [SerializeField] private MelcherStableEnum<UserFilter> m_UserFilter = new (default);

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherButton m_Button = default;

    }   // class QuickLogInWidget

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-04
/// ===========================================================================
