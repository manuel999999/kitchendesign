#if UNITY_EDITOR
namespace Melcher
{
    using UnityEngine;

    using static System.IO.Path;

    public partial class MelcherVersion : MelcherScriptableObject
    {
        public const string AssetFolder = "Assets/AssetsMelcher/Resources";

        private const string UseFacadeEditorPrefKey = "Melcher.MelcherVersion.useFacade";

        // ====================================================================
        // Static properties
        // ====================================================================

        private static void SetUseFacade(bool isUsingFacade)
        {
            MelcherVersion instance = Get();
            instance.m_UseFacade = isUsingFacade;
            UnityEditor.EditorUtility.SetDirty(instance);
            UnityEditor.EditorPrefs.SetBool(UseFacadeEditorPrefKey, instance.m_UseFacade);
        }

        private static void SetVersion(string version)
        {
            if (useFacade)
            {
                SetFacadeVersion(version);
            }
            else
            {
                SetRealVersion(version);
            }
        }

        private static void SetRealVersion(string version)
        {
            UnityEditor.PlayerSettings.bundleVersion = version;
        }

        private static void SetFacadeVersion(string version)
        {
            Get().m_FacadeVersion = version;
        }

        static partial void CreateAssetIfDoesntExist()
        {
            // Create all intermediate directories
            // TODO: This may be a candidate for an Editor Utility method
            void CreateDirectoryChain(string path)
            {
                if (!UnityEditor.AssetDatabase.IsValidFolder(path))
                {
                    string parentPath = GetDirectoryName(path);
                    string folderName = GetFileName(path);
                    if (!UnityEditor.AssetDatabase.IsValidFolder(parentPath))
                    {
                        CreateDirectoryChain(parentPath);
                    }
                    UnityEditor.AssetDatabase.CreateFolder(parentPath, folderName);
                }
            }

            if (Resources.Load<MelcherVersion>(ResourcePath) == null)
            {
                CreateDirectoryChain(AssetFolder);

                MelcherVersion asset = CreateInstance<MelcherVersion>();
                // Use the current real version as the starting point
                asset.m_FacadeVersion = Application.version;
                string assetPath = Combine(AssetFolder, $"{nameof(MelcherVersion)}.asset");
                UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);

                asset.m_UseFacade = UnityEditor.EditorPrefs.GetBool(UseFacadeEditorPrefKey, defaultValue: true);
            }

        }

    }   // class MelcherVersion

}   // namespace Melcher
#endif  // if UNITY_EDITOR
