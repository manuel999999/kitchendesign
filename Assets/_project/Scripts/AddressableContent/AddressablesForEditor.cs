#if UNITY_EDITOR
namespace KitchenDesign.AddressableContent
{
    using Melcher;
    using System.Collections.Generic;
    using UnityEngine;

    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;

    public static class AddressablesForEditor
    {
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
                            schemasToCopy: s_DefaultSchemas
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

            entry.address = address;

            if (originalGroup != null &&
                originalGroup.entries.Count <= 0 &&
                !originalGroup.Default)
            {
                MelcherLog.Log($"Removing empty asset group {MelcherFormat.ConsoleString(originalGroup.name)}");

                addressableSettings.RemoveGroup(originalGroup);
            }

            return entry;
        }

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

        public static AddressableAssetEntry MakeAddressableWithGroupName(string guid, string groupName)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return MakeAddressable(guid, groupName, assetPath);
        }
    }
}
#endif
