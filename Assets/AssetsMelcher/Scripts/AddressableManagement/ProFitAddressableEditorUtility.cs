#if UNITY_EDITOR
namespace Melcher
{
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;

    /// <summary>
    ///     Addressable editor utility functions that are specific to the
    ///     Pro-Fit project.
    /// </summary>
    public static class ProFitAddressableEditorUtility
    {
        /// <summary>
        ///     Mark an asset as addressable. It will be added to an Asset Group
        ///     named <paramref name="groupName"/> and the asset's address will
        ///     be its asset path.
        /// </summary>
        /// <returns>
        ///     The group's name.
        /// </returns>
        public static AddressableAssetEntry MakeAddressableWithGroupName(string guid, string groupName)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return AddressableEditorUtility.MakeAddressable(guid, groupName, assetPath);
        }

    }   // class ProFitAddressableEditorUtility

}   // namespace Melcher
#endif  // if UNITY_EDITOR
