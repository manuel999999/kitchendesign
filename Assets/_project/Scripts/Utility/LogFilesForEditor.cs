#if UNITY_EDITOR
namespace KitchenDesign.Utility
{
    using UnityEngine;

    public static class LogFilesForEditor
    {
        public static string Overwrite(string logFileName, string content)
        {
            string logFolderPath =
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        Application.dataPath, "../Logs/ProFit"
                    )
                );

            System.IO.Directory.CreateDirectory(logFolderPath);

            string outputPath =
                System.IO.Path.Combine(logFolderPath, logFileName);

            System.IO.File.WriteAllText(outputPath, content);

            return outputPath;
        }
    }
}
#endif
