namespace Melcher
{
    using System.IO;
    using UnityEditor;

    /// <summary>
    /// This asset processor catches the creation of a CSharp script (more
    /// specifically, it's meta file) and uses the opportunity to handle
    /// custom find-and-replace macros.
    ///
    /// This is based on the implementation by "hpjohn":
    /// https://forum.unity.com/threads/c-script-template-how-to-make-custom-changes.273191/#post-1806467
    /// </summary>
    [InitializeOnLoad]
    public class MelcherCSharpAssetMacros : AssetModificationProcessor
    {
        private static void FindAndReplaceMacros(ref string fileContent)
        {
            string namesp = EditorSettings.projectGenerationRootNamespace;
            if (string.IsNullOrWhiteSpace(namesp))
            {
                namesp = "Melcher";
            }

            fileContent = fileContent.Replace("#NAMESPACE#", namesp);
            fileContent = fileContent.Replace("#DATECREATE#", System.DateTime.Now.ToString("yyyy-MM-dd"));
        }

        private static void OnWillCreateAsset(string path)
        {
            // Remove ".meta" from script meta files
            if (path.EndsWith(".cs.meta"))
            {
                path = path.Remove(path.Length - ".meta".Length);
                FileInfo fileInfo = new FileInfo(path);

                // Ignore large scripts (for performance, but they're also
                // probably already formatted)
                const int MaxFileSize = 1024;
                if (fileInfo.Exists && fileInfo.Length < MaxFileSize)
                {
                    string fileContent = File.ReadAllText(path);
                    int initialSize = fileContent.Length;

                    FindAndReplaceMacros(ref fileContent);

                    if (initialSize != fileContent.Length)
                    {
                        File.WriteAllText(path, fileContent);
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

    }   // class MelcherCSharpAssetMacros

}   // namespace Melcher
