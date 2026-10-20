namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// <see cref="MelcherVersion"/> is a <see cref="ScriptableObject"/> that
    /// provides an optional facade for the app's version number. The facade is
    /// useful for the Android platform where the Application version prevents
    /// downgrading.
    /// </summary>
    public partial class MelcherVersion : MelcherScriptableObject
    {
        public const string ResourcePath = nameof(MelcherVersion);

        // ====================================================================
        // Static properties
        // ====================================================================

        /// <summary>
        /// When <see langword="true"/>, the version returned by <see
        /// cref="version"/> is the facade version; a version separate from
        /// the true application version.
        /// </summary>
        public static bool useFacade
        {
            get => Get().m_UseFacade;

            #if UNITY_EDITOR
            set => SetUseFacade(value);
            #endif  // if UNITY_EDITOR
        }

        public static string version
        {
            get => useFacade ? facadeVersion : realVersion;

            #if UNITY_EDITOR
            set => SetVersion(value);
            #endif  // if UNITY_EDITOR
        }

        public static string realVersion
        {
            get => Application.version;

            #if UNITY_EDITOR
            set => SetRealVersion(value);
            #endif  // if UNITY_EDITOR
        }

        public static string facadeVersion
        {
            get => Get().m_FacadeVersion;

            #if UNITY_EDITOR
            set => SetFacadeVersion(value);
            #endif  // if UNITY_EDITOR
        }

        public static MelcherVersion Get()
        {
            CreateAssetIfDoesntExist();

            return Resources.Load<MelcherVersion>(ResourcePath);
        }

        // These partial functions are implemented in the editor portion of this
        // script. They only exist at editor time
        static partial void CreateAssetIfDoesntExist();

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [SerializeField]
        protected bool m_UseFacade = true;

        [SerializeField]
        private string m_FacadeVersion = null;

    }   // class MelcherVersion

}   // namespace Melcher
