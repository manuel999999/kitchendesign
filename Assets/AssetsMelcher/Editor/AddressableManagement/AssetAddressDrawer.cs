namespace Melcher
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.GUI;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(AssetAddress<>), useForChildren: true)]
    public class AssetAddressDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Convert the generic `AssetAddress` target into the non-generic
            // `IAssetAddress`. This allows us to query the generic's asset
            // type.

            Object targetObject = property.serializedObject.targetObject;

            string labelThatWeDontUse = label.text;
            object genericAssetAddress =
                property.GetActualObjectForSerializedProperty<object>(
                    fieldInfo,
                    ref labelThatWeDontUse
                );

            if (!(genericAssetAddress is IAssetAddress assetAddress))
            {
                position = EditorGUI.PrefixLabel(position, label);
                EditorGUI.LabelField(position, $"Could not cast field to {nameof(IAssetAddress)}.");
                return;
            }

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // "Detour" the object field's picker button to use our own custom
            // addressable picker. This is done by drawing an invisible button
            // in the same location where the object field's picker button will
            // appear.

            Rect buttonPosition = position;
            buttonPosition.xMin = buttonPosition.xMax - 18.0f;

            if (GUI.Button(buttonPosition, "", GUIStyle.none))
            {
                AddressableAssetSettings addressableSettings;

                if (!TryGetAddressableSettingsOrShowPopUp(out addressableSettings))
                {
                    return;
                }

                System.Type requestedAssetType = assetAddress.assetType;

                s_TemporaryAssetEntryList.Clear();

                // We must gather all assets first and apply the entry filter
                // afterwards because `GetAllAssets` will not pass sub objects
                // to the entry filter if it rejects a parent object.
                addressableSettings.GetAllAssets(
                    s_TemporaryAssetEntryList,
                    includeSubObjects: true,
                    groupFilter: null,
                    entryFilter: null
                );

                if (typeof(Component).IsAssignableFrom(requestedAssetType))
                {
                    //
                    s_TemporaryAssetEntryList.RemoveAll(
                        (entry) =>
                        {
                            return
                                entry.IsSubAsset ||
                                !(entry.TargetAsset is GameObject go) ||
                                !go.TryGetComponent(requestedAssetType, out _);
                        }
                    );
                }
                else
                {
                    s_TemporaryAssetEntryList.RemoveAll(
                        (entry) => !requestedAssetType.IsAssignableFrom(entry.MainAssetType)
                    );
                }

                FilteredObjectSelector.ShowPicker(
                    title: $"{MelcherCommon.PrettyTypeName(assetAddress.assetType)} addressables",
                    s_TemporaryAssetEntryList,
                    getRowName: index => s_TemporaryAssetEntryList[index].TargetAsset?.name,
                    getRowStatus: index => s_TemporaryAssetEntryList[index].address,
                    resultCallback: (selection) =>
                    {
                        if (selection.Count > 0)
                        {
                            assetAddress.SetAddress(property, s_TemporaryAssetEntryList[selection[0]]);
                        }
                    }
                );
            }

            position = EditorGUI.PrefixLabel(position, label);

            // Piggyback on Unity's built-in object field control. This handles
            // most features for us automatically (e.g., click to ping the
            // asset, double-click to open the asset, Delete key to clear the
            // asset, drag and drop). The only caveat is that the user can
            // press the Enter key when the field is focused and this will open
            // the unintended default picker window. However, we detect if the
            // user selects non-addressable assets and reject them.

            using (EditorGUI.ChangeCheckScope changeCheckScope = new ())
            using (EditorGUI.PropertyScope propertyScope = new (position, label, property))
            {
                // The indent needs to be set to 0 to render the ObjectField
                // with a consistent size. This only matters when the
                // AssetAddress is part of an array because otherwise its
                // object field renders slightly narrower than standard
                // controls in an array.

                Object newObject =
                    EditorGUI.ObjectField(
                        position,
                        assetAddress.editorOnlyCachedAsset,
                        assetAddress.assetType,
                        allowSceneObjects: false
                    );

                // Add a small icon to the object field's picker button to help
                // designers visually distinguish an addressable object from a
                // strong reference.
                Rect iconPosition = position;
                iconPosition.xMin = iconPosition.xMax - 18.0f;
                EditorGUI.LabelField(iconPosition, EditorGUIUtility.IconContent("PrefabOverlayModified Icon"));

                if (changeCheckScope.changed)
                {
                    if (!newObject)
                    {
                        assetAddress.SetAddress(property, null);
                    }
                    else if (TryGetAddressableSettingsOrShowPopUp(out AddressableAssetSettings addressableSettings))
                    {
                        string newObjectPath = AssetDatabase.GetAssetPath(newObject);
                        string newObjectGuid = AssetDatabase.AssetPathToGUID(newObjectPath);

                        AddressableAssetEntry newObjectEntry =
                            addressableSettings.FindAssetEntry(newObjectGuid);

                        if (newObjectEntry == null)
                        {
                            MelcherLog.LogError($"{MelcherFormat.ConsoleTypeName(genericAssetAddress)} cannot reference {MelcherFormat.ConsoleString(newObjectPath)} because it is not marked as addressable.");
                        }
                        else
                        {
                            assetAddress.SetAddress(property, newObjectEntry);
                        }
                    }
                }
            }

            EditorGUI.indentLevel = oldIndent;
        }

        /// <summary>
        ///     Fetch the <see cref="AddressableAssetSettings"/> asset which is
        ///     needed to discover which assets are addressable.
        ///     <br /><br />
        ///
        ///     This asset doesn't exist until the user creates it. This
        ///     function will display a pop-up box informing the user when the
        ///     file doesn't exist.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the settings exist; otherwise, <see
        ///     langword="false"/>.
        /// </returns>
        private static bool TryGetAddressableSettingsOrShowPopUp(out AddressableAssetSettings addressableSettings)
        {
            addressableSettings = AddressableAssetSettingsDefaultObject.Settings;

            if (!addressableSettings)
            {
                EditorUtility.DisplayDialog(
                    title: "No Addressable Settings",
                    message: "The Addressable settings asset is not created yet.",
                    ok: "OK"
                );
            }

            return addressableSettings;
        }

        /// <summary>
        ///     A reusable list for the main thread that holds the list of
        ///     all addressable entries that match the type of the <see
        ///     cref="AssetAddress{AssetType}"/> currently being rendered in
        ///     the inspector.
        ///     <br /><br />
        ///
        ///     The list's content is only valid/used within <see
        ///     cref="OnGUI(Rect, SerializedProperty, GUIContent)"/>.
        /// </summary>
        private static readonly List<AddressableAssetEntry> s_TemporaryAssetEntryList = new ();
    }   // class AssetAddressTDrawer

}   // namespace Melcher

