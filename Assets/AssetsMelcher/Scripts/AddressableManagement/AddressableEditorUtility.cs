#if UNITY_EDITOR
namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;

    /// <summary>
    ///     Utility functions for working with addressables. These take care of
    ///     finding the default addressable settings asset and throwing an
    ///     exception when it isn't found.
    /// </summary>
    public static class AddressableEditorUtility
    {
        /// <summary>
        ///     Return <see langword="true"/> if <paramref name="asset"/> is
        ///     marked as addressable, and return its entry in the <see
        ///     langword="out"/> argument, <paramref name="addressableEntry"/>.
        ///     Otherwise, return <see langword="false"/>.
        /// </summary>
        public static bool IsAddressable<TAsset>(TAsset asset, out AddressableAssetEntry addressableEntry)
            where TAsset : Object
        {
            addressableEntry = default;

            AddressableAssetSettings addressableSettings = GetAddressableSettingsOrThrow();

            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            addressableEntry = addressableSettings.FindAssetEntry(assetGuid);

            return addressableEntry != null;
        }

        /// <summary>
        ///     Mark <paramref name="asset"/> as addressable and place it in
        ///     a group named <paramref name="groupName"/>. If the group
        ///     doesn't exist, it will be created.
        /// </summary>
        public static AddressableAssetEntry MakeAddressable<TAsset>(TAsset asset, string groupName, string address)
            where TAsset : Object
        {
            if (!AssetDatabase.Contains(asset))
            {
                throw new System.InvalidOperationException("Must be given an asset and not an instance.");
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new System.InvalidOperationException("Asset does not have a path.");
            }

            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            return MakeAddressable(assetGuid, groupName, address);
        }

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        private static List<AddressableAssetGroupSchema> s_DefaultSchemas;

        /// <summary>
        ///     Mark the asset whose GUID is <paramref name="assetGuid"/> as
        ///     addressable and place it in a group named <paramref
        ///     name="groupName"/>. If the group doesn't exist, it will be
        ///     created.
        /// </summary>
        public static AddressableAssetEntry MakeAddressable(string assetGuid, string groupName, string address)
        {
            AddressableAssetSettings addressableSettings =
                GetAddressableSettingsOrThrow();

            AddressableAssetEntry entry =
                addressableSettings.FindAssetEntry(assetGuid);

            bool isAlreadyInDestinationGroup =
                entry != null && entry.parentGroup.Name == groupName;

            if (isAlreadyInDestinationGroup && entry.address == address)
            {
                // Happy path: Everything is already set how we want.
                return entry;
            }

            AddressableAssetGroup originalGroup = entry?.parentGroup;

            if (!isAlreadyInDestinationGroup)
            {
                AddressableAssetGroup destinationGroup =
                    addressableSettings.FindGroup(groupName);

                if (destinationGroup == null)
                {
                    MelcherLog.Log($"Creating asset group {MelcherFormat.ConsoleString(groupName)}");

                    destinationGroup =
                        addressableSettings.CreateGroup(
                            groupName,
                            setAsDefaultGroup: false,
                            readOnly: false,
                            postEvent: false,
                            //schemasToCopy: addressableSettings.DefaultGroup.Schemas // Copying directly from DefaultGroup like this causes exceptions.
                            schemasToCopy: s_DefaultSchemas // Copy from a cached version of the default group's schemas.
                        );

                    if (destinationGroup == null)
                    {
                        throw new System.InvalidOperationException("Failed to find or create the destination addressable group: " + groupName);
                    }

                    BundledAssetGroupSchema schema =
                        destinationGroup.GetSchema<BundledAssetGroupSchema>();

                    schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                }

                MelcherLog.Log($"Creating/moving asset {assetGuid} to group {MelcherFormat.ConsoleString(destinationGroup.name)} with address {MelcherFormat.ConsoleString(address)}");

                entry =
                    addressableSettings.CreateOrMoveEntry(
                        assetGuid,
                        destinationGroup,
                        postEvent: false
                    );
            }

            // Apply the requested address to the entry.
            entry.address = address;

            if (originalGroup != null &&
                originalGroup.entries.Count <= 0 &&
                !originalGroup.Default)
            {
                MelcherLog.Log($"Removing empty asset group {MelcherFormat.ConsoleString(originalGroup.name)}");

                // The original group is now empty and no longer needed.
                // This deletes the group asset.
                addressableSettings.RemoveGroup(originalGroup);
            }

            return entry;
        }

        /// <summary>
        ///     Get the default Addressable settings object. If it's not found,
        ///     an exception is thrown.
        /// </summary>
        /// <param name="showPopUp">
        ///     Whether to prompt the user with a pop-up. An exception will
        ///     still be thrown if the settings is not found.
        /// </param>
        public static AddressableAssetSettings GetAddressableSettingsOrThrow(bool showPopUp = true)
        {
            AddressableAssetSettings addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

            if (!addressableSettings)
            {
                string error = "The Addressable settings asset is not created yet.";

                if (showPopUp)
                {
                    EditorUtility.DisplayDialog(
                        title: "No Addressable Settings",
                        message: error,
                        ok: "OK"
                    );
                }

                throw new System.InvalidOperationException(error);
            }

            if (s_DefaultSchemas == null || s_DefaultSchemas.Count <= 0)
            {
                s_DefaultSchemas = new (addressableSettings.DefaultGroup.Schemas);
            }

            return addressableSettings;
        }

    }   // class AddressableEditorUtility

}   // namespace KitchenDesign
#endif  // if UNITY_EDITOR

