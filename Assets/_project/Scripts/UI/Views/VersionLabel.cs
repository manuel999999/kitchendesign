namespace KitchenDesign.UI.Views
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using UnityEngine;

    public class VersionLabel : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        protected void Awake()
        {
            string phrase = $"v{Application.version}{AppProfileStore.Options.versionSuffix}";

            if (AppProfileStore.Profile.targetServerType == ServerType.Test)
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

}   // namespace KitchenDesign.UI.Views

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-08-10
/// ===========================================================================
