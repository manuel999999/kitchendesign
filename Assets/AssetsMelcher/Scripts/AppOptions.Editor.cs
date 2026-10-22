#if UNITY_EDITOR
namespace KitchenDesign
{
    using Melcher;
    using KitchenDesign.Shared.Enums;
    using UnityEditor;
    using UnityEngine;

    // Editor-only implementation details of ProFitAppOptions
    public partial class AppOptions : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public string appIdentifier => m_AppIdentifier;

        public Texture2D appIcon => m_AppIcon;

        public AndroidProjectSettings androidProjectSettings => m_AndroidProjectSettings;

        public bool simulateWhiteLabelMode
        {
            get => m_SimulateWhiteLabel;
            set => m_SimulateWhiteLabel = value;
        }

        public string simulatedWhiteLabelDomain
            => m_SimulatedWhiteLabelDomain;

        public string simulatedWhiteLabelDistributorUrlParameter
            => m_SimulatedWhiteLabelDistributorUrlParameter;

        public MeasurementUnit simulatedPreferredUnits
            => m_SimulatedPreferredUnits;

        // ====================================================================
        // Methods
        // ====================================================================

        public override void OnEditorPlayModeStateChanged(PlayModeStateChange mode)
        {
            base.OnEditorPlayModeStateChanged(mode);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            bool isMoreThanOneBitSet =
                (m_DefaultBuildingMethod.value & (m_DefaultBuildingMethod.value - 1)) != 0;

            if (isMoreThanOneBitSet)
            {
                MelcherLog.LogError("Only one default building method should be selected.", this);
            }
        }

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [Header("Editor-only")]

        [Tooltip("The application identifier used by Android/iOS. This is " +
            "in reverse domain order: ca.melcher.<client>.<project>")]
        [SerializeField]
        protected string m_AppIdentifier = default;

        [SerializeField]
        protected Texture2D m_AppIcon = default;

        [SerializeField]
        protected AndroidProjectSettings m_AndroidProjectSettings = default;

        [Space]


        [SerializeField] private bool m_SimulateWhiteLabel;

        [SerializeField] private string m_SimulatedWhiteLabelDomain;
        [SerializeField] private string m_SimulatedWhiteLabelDistributorUrlParameter;

        [Space]

        [SerializeField] protected MeasurementUnit m_SimulatedPreferredUnits = default;

        // ====================================================================
        // Nested types
        // ====================================================================

        [System.Serializable]
        public class AndroidProjectSettings
        {
            public Texture2D adaptiveIconBackground;

            public Texture2D adapativeIconForeground;

        }   // class AndroidProjectSettings

    }   // class ProFitAppOptions

}   // namespace KitchenDesign

#endif  // if UNITY_EDITOR
