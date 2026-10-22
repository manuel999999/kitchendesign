namespace KitchenDesign
{
    using Melcher;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public static class ProductVisualDatabaseEntryFile
    {
        public const string FILE_EXTENSION = ".txt";

        public struct Entry
        {
            public string guid;
            public string name;
        }

        public static string GetFolder(string folderName)
        {
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), folderName);
        }

        public static string[] GetFilePaths(string folderName)
        {
            string folder = GetFolder(folderName);

            if (!Directory.Exists(folder))
            {
                return new string[0];
            }

            string[] filePaths = Directory.GetFiles(folder, "*" + FILE_EXTENSION);
            System.Array.Sort(filePaths, static (a, b) => EditorUtility.NaturalCompare(a, b));

            return filePaths;
        }

        public static bool IsValidFileName(string fileName)
        {
            return
                !string.IsNullOrWhiteSpace(fileName) &&
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }

        public static List<Entry> Read(string filePath)
        {
            List<Entry> entries = new ();

            foreach (string line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split('\t', 2);

                entries.Add(
                    new Entry
                    {
                        guid = parts[0].Trim(),
                        name = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim()
                    }
                );
            }

            return entries;
        }

        public static void Write(string filePath, IEnumerable<Entry> entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using StreamWriter writer = new StreamWriter(filePath, append: false, new UTF8Encoding(false));
            writer.NewLine = "\n";

            foreach (Entry entry in entries)
            {
                writer.WriteLine(entry.guid + "\t" + entry.name);
            }
        }

        public static bool TryGetEntry(ProductVisual visual, out Entry entry)
        {
            if (visual &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(visual, out string guid, out long _))
            {
                entry = new Entry { guid = guid, name = visual.name };
                return true;
            }

            entry = default;
            return false;
        }

        public static bool IsInProject(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return
                !string.IsNullOrEmpty(assetPath) &&
                typeof(ProductVisual).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath));
        }

        public static ProductVisual LoadVisual(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return string.IsNullOrEmpty(assetPath) ?
                null :
                AssetDatabase.LoadAssetAtPath<ProductVisual>(assetPath);
        }

        public static ProductVisualDatabase FindDefaultDatabase()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ProductVisualDatabase)}");

            if (guids.Length <= 0)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<ProductVisualDatabase>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );
        }

        public static void RefreshOpenDatabaseManagers(ProductVisualDatabase database)
        {
            foreach (ProductVisualDatabaseManager manager in Resources.FindObjectsOfTypeAll<ProductVisualDatabaseManager>())
            {
                if (manager.rootVisualElement.Q<ListView>("Table") != null)
                {
                    manager.SetDatabase(database);
                }
            }
        }
    }
}
