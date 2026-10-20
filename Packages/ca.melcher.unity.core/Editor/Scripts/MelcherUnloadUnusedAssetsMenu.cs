namespace Melcher
{
    using System.Collections;
    using UnityEditor;

    /// <summary>
    /// Provides a way to unload assets that are not directly used. This is
    /// helpful for testing addressables and loading times in the editor where
    /// normally assets are cached once and kept loaded until the editor quits.
    /// </summary>
    public static class MelcherUnloadUnusedAssetsMenu
    {

        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Unload unused assets...")]
        private static void PurgeLoadedAssets()
        {
            MelcherEditorCoroutine.StartEditorCoroutine(Loop());

            IEnumerator Loop()
            {
                string okButtonText = "Unload Assets";
                string okButtonTextAgain = okButtonText + " (again)";

                string cancelButtonText = "Cancel";
                string cancelButtonTextAgain = "Done";

                // Sometimes, to properly unload assets, it's necessary to make
                // multiple calls to UnloadUnusedAssets. The `while` loop causes
                // the prompt to redisplay until the user chooses to cancel.
                while (true)
                {
                    bool confirmed =
                        EditorUtility.DisplayDialog(
                            title: "Unload Unused Assets",
                            message: "Unload assets that are not used?",
                            okButtonText,
                            cancelButtonText
                        );

                    if (!confirmed)
                    {
                        break;
                    }

                    AssetDatabase.SaveAssets();
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    System.GC.WaitForPendingFinalizers();
                    System.GC.Collect();
                    yield return null;

                    okButtonText = okButtonTextAgain;
                    cancelButtonText = cancelButtonTextAgain;
                }
            }
        }

    }   // class MelcherPurgeLoadedAssetsMenu

}   // namespace Melcher
