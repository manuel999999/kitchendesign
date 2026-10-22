namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    public class VersionLabel : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        protected void Awake()
        {
            string phrase = $"v{Application.version}{ProFitGlobals.appOptions.versionSuffix}";

            if (ProFitWeb.IsTestServer)
            {
                phrase += " (test)";
            }

            m_VersionLabel.phrase = phrase;
        }

        // ====================================================================
        // OnValidate-serialized fields
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected MelcherLocalizedText m_VersionLabel = default;

    }   // class VersionLabel

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-10
/// ===========================================================================
