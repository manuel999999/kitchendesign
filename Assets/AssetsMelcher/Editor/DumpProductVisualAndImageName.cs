namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;

    /// <summary>
    /// Creates a menu item that allows for generating a JSON file containing
    /// product IDs and thumbnail names of every product, grouped by product
    /// category.
    /// </summary>
    public static class DumpProductVisualAndImageName
    {
        [MenuItem("Tools/Melcher Studios/Export Product Visual IDs and Thumbnails")]
        public static void Dump()
        {
            // Get a path to the project's export folder (create the folder
            // if it doesn't exist because we will be writing to it)
            string exportDirPath =
                MelcherTransformFilePath.From("Export", relativeTo: MelcherPredefinedFilePath.project)
                    .ToAbsolute();

            if (!Directory.Exists(exportDirPath))
            {
                Directory.CreateDirectory(exportDirPath);
            }

            // Find the Product Visual Database because we will be exporting its
            // item list
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabase)}");
            string databasePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            ProductVisualDatabase database = AssetDatabase.LoadAssetAtPath<ProductVisualDatabase>(databasePath);

            Dictionary<string, object> jsonRoot = new Dictionary<string, object>();

            List<object> categoriesListJson = new List<object>();
            jsonRoot.Add("product_categories", categoriesListJson);

            HashSet<string> thumbnailSourcePaths = new HashSet<string>();

            Dictionary<string, List<object>> categoryNameToEntryList = new Dictionary<string, List<object>>();

            foreach (ProductVisual entry in database.sourceEntries)
            {
                if (string.IsNullOrEmpty(entry.info.id))
                {
                    MelcherLog.LogWarning("Skipping entry with no ID: " + entry.name, entry);
                    continue;
                }

                if (!entry.info.thumbnail)
                {
                    MelcherLog.LogWarning("Skipping entry with no thumbnail: " + entry.name, entry);
                    continue;
                }

                string visualName;

                // Skip materials
                ProductVisualContent productVisualContent =
                    entry.contentAddress.editorOnlyCachedAsset;

                if (productVisualContent.finishMaterial)
                {
                    visualName = productVisualContent.finishTexture.name;
                }
                else if (productVisualContent.productModelPrefab)
                {
                    visualName = productVisualContent.productModelPrefab.name;
                }
                else
                {
                    MelcherLog.LogWarning("Skipping entry with no finish or model: " + entry.name, entry);
                    continue;
                }

                // Parse the category from the visual's name
                int underscore = visualName.IndexOf('_');
                string category = visualName.Substring(0, underscore);

                if (category.Length <= 0)
                {
                    MelcherLog.LogWarning("Skipping entry with no finish or model: " + entry.name, entry);
                    continue;
                }

                // Get the JSON for the category or create a new JSON
                if (!categoryNameToEntryList.TryGetValue(category, out List<object> entryList))
                {
                    entryList = new List<object>();

                    Dictionary<string, object> categoryDict = new Dictionary<string, object>()
                    {
                        { "category", AppCategoryToServerCategory[category] },
                        { "entries", entryList }
                    };

                    categoriesListJson.Add(categoryDict);

                    categoryNameToEntryList.Add(category, entryList);
                }

                string thumbnailFilePath = AssetDatabase.GetAssetPath(entry.info.thumbnail);
                string thumbnailFileName = Path.GetFileName(thumbnailFilePath);
                File.Copy(
                    thumbnailFilePath,
                    Path.Combine(exportDirPath, thumbnailFileName),
                    overwrite: true
                );

                Dictionary<string, object> entryJson = new Dictionary<string, object>()
                {
                    { "model_id", entry.info.id },
                    { "image_name", thumbnailFileName }
                };

                entryList.Add(entryJson);

                string thumbnailFullFilePath =
                    MelcherTransformFilePath.From(thumbnailFilePath, MelcherPredefinedFilePath.project)
                        .ToAbsolute();

                thumbnailSourcePaths.Add(thumbnailFullFilePath);
            }

            string output = MiniJSON.Json.Serialize(jsonRoot, prettyPrint: true);

            string outputPath = Path.Combine(exportDirPath, "ID-to-image.json");
            File.WriteAllText(outputPath, output);
            MelcherLog.Log("Wrote " + outputPath);

            MelcherLog.Log("Copying thumbnails to export folder");
            foreach (string thumbnailSourcePath in thumbnailSourcePaths)
            {
                string thumbnailFileName = Path.GetFileName(thumbnailSourcePath);
                string thumbnailOutputPath = Path.Combine(exportDirPath, thumbnailFileName);
                File.Copy(
                    thumbnailSourcePath,
                    thumbnailOutputPath,
                    overwrite: true
                );
            }
            MelcherLog.Log("Finished copying thumbnails");
        }

        public static Dictionary<string, string> AppCategoryToServerCategory = new Dictionary<string, string>()
        {
            { "Accessory", "Accessories" },
            { "Grill", "Grills" },
            { "Burner", "Burners" },
            { "Storage", "Storage" },
            { "Cookware", "Cookware" },
            { "Sink", "Fridge and Sinks" },
            { "Fridge", "Fridge and Sinks" },
            { "Cabinet", "Finishes" },
            { "Countertop", "Finishes" },
            { "Flooring", "Finishes" },
        };

    }   // class DumpProductVisualAndImageName

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-05
/// ===========================================================================
