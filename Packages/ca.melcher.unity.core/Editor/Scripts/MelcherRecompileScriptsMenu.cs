#if UNITY_2021_3_OR_NEWER
// This uses a feature that was added in Unity 2021
namespace Melcher
{
    using UnityEditor;
    using UnityEditor.Compilation;

    /// <summary>
    /// Provides a menu option to perform a clean build. This can be useful
    /// to view compile warnings that have been wiped from the console.
    /// </summary>
    internal static class MelcherRecompileScriptsMenu
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Recompile scripts...")]
        private static void Recompile()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                title: "Recompile Scripts",
                message: "Clear all cached build results, " +
                    "resulting in a full rebuild of all scripts?",
                ok: "Recompile Scripts",
                cancel: "Cancel"
            );

            if (confirmed)
            {
                MelcherLog.Log("Recompiling scripts...");
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            }
        }

    }   // class MelcherRecompileScriptsMenu

}   // namespace Melcher
#endif  // if UNITY_2021_3_OR_NEWER