namespace KitchenDesign
{
    using KitchenDesign.Scriptables;
    using UnityEngine;

    public static class AppProfileStore
    {
        private const string ProfileResourceName = "AppProfile";

        private static AppProfileSO _profile;

        public static AppProfileSO Profile
        {
            get
            {
                if (_profile != null)
                {
                    return _profile;
                }

                _profile = Resources.Load<AppProfileSO>(ProfileResourceName);

#if UNITY_EDITOR
                if (_profile == null)
                {
                    Debug.LogError("Failed to load app profile.");
                }
#endif

                return _profile;
            }
        }

        public static AppOptionsSO Options => Profile.appOptions;

        public static AppSkinSO Skin => Options.appSkin;

        public static void Bind(AppProfileSO profile)
        {
            _profile = profile;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBinding()
        {
            _profile = null;
        }
    }
}
