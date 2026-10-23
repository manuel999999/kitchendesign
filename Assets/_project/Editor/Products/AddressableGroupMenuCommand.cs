namespace KitchenDesign.ProductsEditor
{
    using Melcher;
    using KitchenDesign.Scriptables;
    using KitchenDesign.AddressableContent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;
    using KitchenDesign.Products;

    /// <summary>
    ///     Provides menu items to process the project's assets into their
    ///     intended Addressable asset groups.
    /// </summary>
    public static class AddressableGroupMenuCommand
    {
        [MenuItem("Tools/Melcher Studios/Pro-Fit/Fix missing addressable groups")]
        public static void FixMissingAddressableGroups()
        {
            AddressableAssetSettings addressableSettings =
                AddressablesForEditor.GetAddressableSettingsOrThrow(showPopUp: true);

            List<AddressableAssetGroup> addressableGroups = addressableSettings.groups;

            // Remove missing groups. It's likely that these groups still
            // technically exist but their GUIDs have changed. We can't work
            // with them as missing (null) values, so just remove them for now.
            List<int> addressableGroupIndiciesToRemove = new ();

            for (int i = 0; i < addressableGroups.Count; i++)
            {
                AddressableAssetGroup group = addressableSettings.groups[i];
                if (group == null)
                {
                    addressableGroupIndiciesToRemove.Add(i);
                }
            }

            for (int i = addressableGroupIndiciesToRemove.Count - 1; i >= 0; --i)
            {
                addressableGroups.RemoveAt(addressableGroupIndiciesToRemove[i]);
            }

            MelcherLog.Log($"Removed {addressableGroupIndiciesToRemove.Count} groups that were missing.");

            // Ensure all addressable groups that exist in the project are
            // reference. This will fix up any groups whose GUIDs were changed
            // and became "missing" entries.
            string[] addressableGroupGuids =
                AssetDatabase.FindAssets(
                    $"t:{nameof(AddressableAssetGroup)}",
                    new[] { addressableSettings.GroupFolder }
                );

            System.Text.StringBuilder message = new ();
            int groupsBefore = addressableGroups.Count;

            foreach (string assetGuid in addressableGroupGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                AddressableAssetGroup group =
                    AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(assetPath);

                if (!group)
                {
                    MelcherLog.LogWarning($"Could not load addressable asset group from path: {assetPath} (GUID: {assetGuid})");
                    continue;
                }

                if (addressableGroups.Contains(group))
                {
                    continue;
                }

                addressableGroups.Add(group);
                message.AppendLine(group.Name);
            }


            MelcherLog.Log($"Added {addressableGroupIndiciesToRemove.Count - groupsBefore} groups that were unlinked:\n" + message.ToString());

            // Fix up any addressable groups that do not have a schema. Not sure
            // how this happens, but all groups should have schema. We'll copy
            // the schemas from the default group (as we do elsewhere) and
            // ensure the pack mode is setup as "pack separately".

            List<AddressableAssetGroup> noSchemaAddressableGroups = new ();

            message.Clear();

            foreach (AddressableAssetGroup group in addressableGroups)
            {
                if (group.Schemas == null || group.Schemas.Count <= 0)
                {
                    noSchemaAddressableGroups.Add(group);
                    message.AppendLine(group.Name);
                }
            }

            List<AddressableAssetGroupSchema> defaultSchemas = addressableSettings.DefaultGroup.Schemas;

            foreach (AddressableAssetGroup group in noSchemaAddressableGroups)
            {
                foreach (AddressableAssetGroupSchema schemaToCopy in defaultSchemas)
                {
                    // Post-pone sending events until the end.
                    AddressableAssetGroupSchema addedSchema =
                        group.AddSchema(schemaToCopy, postEvent: false);

                    if (!group.IsDefaultGroup() && addedSchema is BundledAssetGroupSchema bundledAssetGroupSchema)
                    {
                        // Ensure the bundle is marked as "pack separately"
                        bundledAssetGroupSchema.BundleMode =
                            BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                    }
                }
            }

            MelcherLog.Log($"Added schema to {noSchemaAddressableGroups.Count} groups:\n" + message.ToString());

            // Send an event to notify the addressable settings that it has
            // been changed.

            addressableSettings.SetDirty(
                AddressableAssetSettings.ModificationEvent.BatchModification,
                eventData: null,
                postEvent: true,
                settingsModified: false
            );

            MelcherEditorUtility.Validate(addressableSettings);
        }

        [MenuItem("Tools/Melcher Studios/Pro-Fit/Remove empty addressable groups")]
        public static void RemoveEmptyAddressableGroups()
        {
            AddressableAssetSettings addressableSettings =
                AddressablesForEditor.GetAddressableSettingsOrThrow(showPopUp: true);

            List<AddressableAssetGroup> emptyAddressableGroups = new ();

            foreach (AddressableAssetGroup group in addressableSettings.groups)
            {
                if (group.entries.Count <= 0 && !group.IsDefaultGroup())
                {
                    emptyAddressableGroups.Add(group);
                }
            }

            MelcherLog.Log("Purging empty groups...");
            foreach (AddressableAssetGroup group in emptyAddressableGroups)
            {
                MelcherLog.Log("Purging " + group.Name + "...");
                addressableSettings.RemoveGroup(group);
            }
        }

        [MenuItem("Tools/Melcher Studios/Pro-Fit/Update addressable groups...")]
        public static void ProcessAssets()
        {
            bool wantsToContinue =
                EditorUtility.DisplayDialog(
                    title: "Update addressable groups",
                    message: "This may take a long time (on the order of 1 " +
                    "or more hours). This auto-organizes assets into asset " +
                    "groups that are efficient for loading content on a " +
                    "as-needed basis.",
                    ok: "Update addressable groups",
                    cancel: "Cancel"
                );

            if (!wantsToContinue)
            {
                return;
            }

            string logOutputPath =
                Path.Combine(
                    MelcherPredefinedFilePath.project,
                    nameof(AddressableGroupMenuCommand) + ".txt"
                );

            using StreamWriter logWriter = new StreamWriter(logOutputPath);

            System.DateTime before = System.DateTime.Now;

            // Cabinet section models and product visual prefabs are only made
            // addressable when the product visual database uses them. This
            // keeps assets that sit in those folders without being in the
            // database (e.g., a brand that isn't released yet) out of the
            // build.
            HashSet<string> databaseAssetGuids = null;

            MeasureTime(
                logWriter,
                "Collecting assets used by the product visual database",
                () => databaseAssetGuids = CollectProductVisualDatabaseAssetGuids()
            );

            MeasureTime(
                logWriter,
                "Updating product models",
                () => UpdateModelGroup(ProFitGlobals.appProfile.productModelFolderPath, "ProductModels", "ProductMaterials")
            );

            MeasureTime(
                logWriter,
                "Updating cabinet section models",
                () => UpdateModelGroup(ProFitGlobals.appProfile.cabinetSectionModelFolderPath, "CabinetModels", "ProductMaterials", databaseAssetGuids)
            );

            MeasureTime(
                logWriter,
                "Updating product visual prefabs",
                () => UpdateAssetGroups("t:prefab", ProFitGlobals.appProfile.productVisualFolderPath, "ProductVisuals", databaseAssetGuids)
            );

            MeasureTime(
                logWriter,
                "Updating finishes",
                () => UpdateFinishGroups()
            );

            MeasureTime(
                logWriter,
                "Updating product materials",
                () => UpdateAssetGroups("t:material", "Assets/AssetsMelcher/ProductMaterials", "ProductMaterials")
            );

            System.DateTime after = System.DateTime.Now;
            StartLogLine(logWriter, after);
            logWriter.WriteLine($"Completed everything in {FormatTimeSpan(after - before)}");

            MelcherLog.Log("Done");

            static void MeasureTime(StreamWriter log, string label, System.Action measuredAction)
            {
                System.DateTime before = System.DateTime.Now;

                StartLogLine(log, before);

                log.Write($"{label}...");

                measuredAction?.Invoke();

                System.DateTime after = System.DateTime.Now;
                log.WriteLine($"   Done: {FormatTimeSpan(after - before)}");
            }

            static void StartLogLine(StreamWriter log, System.DateTime time)
            {
                log.Write($"[{time.ToString("yyyy-MM-dd HH:mm:ss.fff")}] :: ");
            }

            static string FormatTimeSpan(System.TimeSpan timeSpan)
            {
                return timeSpan.ToString(@"hh\h\:mm\m\:ss\.fff\s");
            }
        }

        /// <summary>
        ///     Collect the GUIDs of every product visual prefab that is in the
        ///     product visual database, along with everything those prefabs
        ///     depend on (e.g., their cabinet section models).
        /// </summary>
        private static HashSet<string> CollectProductVisualDatabaseAssetGuids()
        {
            string[] databaseGuids =
                AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabaseSO)}");

            List<string> prefabPaths = new ();

            foreach (string databaseGuid in databaseGuids)
            {
                string databasePath = AssetDatabase.GUIDToAssetPath(databaseGuid);

                ProductVisualDatabaseSO database =
                    AssetDatabase.LoadAssetAtPath<ProductVisualDatabaseSO>(databasePath);

                foreach (ProductVisualSO visual in database.sourceEntries)
                {
                    if (!visual)
                    {
                        continue;
                    }

                    ProductVisualContent content =
                        visual.contentAddress.editorOnlyCachedAsset;

                    if (!content)
                    {
                        MelcherLog.LogError($"Could not find the content prefab of {visual.name}. It will be excluded from the addressable groups.", visual);
                        continue;
                    }

                    prefabPaths.Add(AssetDatabase.GetAssetPath(content));
                }
            }

            if (prefabPaths.Count <= 0)
            {
                // Continuing would exclude every cabinet section model and
                // product visual prefab from the build.
                throw new System.InvalidOperationException("Could not find any product visual prefabs in the product visual database.");
            }

            string[] dependencyPaths =
                AssetDatabase.GetDependencies(prefabPaths.ToArray(), recursive: true);

            HashSet<string> assetGuids = new ();

            foreach (string prefabPath in prefabPaths)
            {
                assetGuids.Add(AssetDatabase.AssetPathToGUID(prefabPath));
            }

            foreach (string dependencyPath in dependencyPaths)
            {
                assetGuids.Add(AssetDatabase.AssetPathToGUID(dependencyPath));
            }

            return assetGuids;
        }

        /// <summary>
        ///     Return the assets from <paramref name="assetGuids"/> that are
        ///     in <paramref name="onlyAssetGuids"/>. The other assets are
        ///     excluded from the build by removing them from their addressable
        ///     group, if they are in one.
        ///     <br /><br />
        ///
        ///     Everything is returned if <paramref name="onlyAssetGuids"/> is
        ///     <see langword="null"/>.
        /// </summary>
        private static string[] ExcludeAssetsNotIn(HashSet<string> onlyAssetGuids, string[] assetGuids, string searchInFolder)
        {
            if (onlyAssetGuids == null)
            {
                return assetGuids;
            }

            AddressableAssetSettings addressableSettings =
                AddressablesForEditor.GetAddressableSettingsOrThrow();

            List<string> includedGuids = new (assetGuids.Length);

            int removedEntryCount = 0;

            foreach (string guid in assetGuids)
            {
                if (onlyAssetGuids.Contains(guid))
                {
                    includedGuids.Add(guid);
                    continue;
                }

                if (addressableSettings.RemoveAssetEntry(guid, postEvent: false))
                {
                    ++removedEntryCount;
                }
            }

            int excludedCount = assetGuids.Length - includedGuids.Count;

            MelcherLog.Log($"Excluded {excludedCount} assets in {MelcherFormat.ConsoleString(searchInFolder)} that are not used by the product visual database. {removedEntryCount} of them were removed from their addressable group.");

            return includedGuids.ToArray();
        }

        /// <summary>
        ///     For each asset that passes the filter, it is moved to the group
        ///     matching its asset name (prefixed with <paramref
        ///     name="groupPrefix"/>). The group is created if needed. The
        ///     asset is assigned its asset path as its address.
        /// </summary>
        /// <param name="onlyAssetGuids">
        ///     When provided, assets that are not in this set are excluded.
        ///     See <see cref="ExcludeAssetsNotIn"/>.
        /// </param>
        private static void UpdateAssetGroups(string searchFilter, string searchInFolder, string addressableGroupName, HashSet<string> onlyAssetGuids = null)
        {
            string[] filteredGuids =
                AssetDatabase.FindAssets(searchFilter, new[] { searchInFolder });

            filteredGuids = ExcludeAssetsNotIn(onlyAssetGuids, filteredGuids, searchInFolder);

            foreach (string guid in filteredGuids)
            {
                AddressablesForEditor.MakeAddressableWithGroupName(guid, addressableGroupName);
            }
        }

        /// <summary>
        ///     For each model in the specified folder, it is assigned its own
        ///     addressable group.
        ///     <br /><br />
        ///
        ///     Each material the model references is added to its own
        ///     addressable group, but as a collection of all of its variants.
        ///     I.e., material variants are grouped with their root-most
        ///     parent.
        ///     <br /><br />
        ///
        ///     Each texture referenced one of the earlier materials is added
        ///     to its own group.
        ///     <br /><br />
        ///
        ///     This means that loading a material can load a bunch of textures
        ///     that the specific material being loaded doesn't necessarily
        ///     need because the textures are used by one of its variants in
        ///     the group.
        /// </summary>
        /// <param name="onlyAssetGuids">
        ///     When provided, models that are not in this set are excluded.
        ///     See <see cref="ExcludeAssetsNotIn"/>.
        /// </param>
        private static void UpdateModelGroup(string searchInFolder, string modelAddressableGroupName, string materialAddressableGroupName, HashSet<string> onlyAssetGuids = null)
        {
            string[] modelGuids =
                AssetDatabase.FindAssets(
                    filter: "t:model",
                    new[] { searchInFolder }
                );

            modelGuids = ExcludeAssetsNotIn(onlyAssetGuids, modelGuids, searchInFolder);

            // Models are added to the same group.
            foreach (string modelGuid in modelGuids)
            {
                AddressablesForEditor.MakeAddressableWithGroupName(modelGuid, modelAddressableGroupName);

                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuid);

                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);

                Dictionary<AssetImporter.SourceAssetIdentifier, Object> remappedObjects =
                    importer.GetExternalObjectMap();

                // Materials are collectively placed into a separate addressable group.
                foreach (var kvp in remappedObjects)
                {
                    if (kvp.Value is Material material)
                    {
                        string materialPath = AssetDatabase.GetAssetPath(material);

                        UpdateMaterial(material, materialPath, materialAddressableGroupName);
                    }
                }
            }
        }

        /// <summary>
        ///     Finishes are split across two Addressable Groups:
        ///     <br /><br />
        ///
        ///     1. FinishTextures<br />
        ///     2. FinishMaterials
        /// </summary>
        private static void UpdateFinishGroups()
        {
            string[] textureGuids =
                AssetDatabase.FindAssets(
                    filter: "t:texture",
                    new[] { ProFitGlobals.appProfile.finishesFolderPath }
                );

            foreach (string textureGuid in textureGuids)
            {
                AddressablesForEditor.MakeAddressableWithGroupName(textureGuid, "Textures");
            }

            string[] materialGuids =
                AssetDatabase.FindAssets(
                    filter: "t:material",
                    new[] { ProFitGlobals.appProfile.finishesFolderPath }
                );

            foreach (string materialGuid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);

                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (!material)
                {
                    MelcherLog.LogError($"Could not load material: {materialPath} (guid: {materialGuid})");
                    continue;
                }

                UpdateMaterial(material, materialGuid, "FinishMaterials");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="material"></param>
        /// <param name="materialAssetPath"></param>
        private static void UpdateMaterial(Material material, string materialGuid, string materialGroupName)
        {
            // Add all textures that are referenced by this material to their
            // own, per-texture asset group. These textures may be referenced
            // by other materials and therefore its simplest to keep them
            // isolated.

            string[] textureSlotNames =
                material.GetPropertyNames(MaterialPropertyType.Texture);

            // Add all textures to the textures group.
            foreach (string textureSlotName in textureSlotNames)
            {
                Texture texture =
                    material.GetTexture(textureSlotName);

                if (!texture)
                {
                    continue;
                }

                // Textures might be reused between different types of objects
                // (finishes, products, etc.). Therefore, we will group all
                // textures into one addressable group to avoid wastefully
                // moving the same texture between multiple groups each time
                // we refresh the Addressable Groups.
                string textureAssetPath = AssetDatabase.GetAssetPath(texture);
                string textureGuid = AssetDatabase.AssetPathToGUID(textureAssetPath);
                AddressablesForEditor.MakeAddressableWithGroupName(textureGuid, "Textures");
            }

            // Add the material to the materials group.
            AddressablesForEditor.MakeAddressableWithGroupName(materialGuid, materialGroupName);
        }

    }   // class ProFitAddressableGroupMenuCommand

}   // namespace KitchenDesign

