namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    ///     Listens for Unity editor play-mode changes and informs all loaded
    ///     <see cref="MelcherScriptableObject"/>. This can be useful to allow
    ///     ScriptableObjects to reset their state between editor play sessions.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MelcherScriptableObject), editorForChildClasses: true)]
    public class MelcherScriptableObjectEditor : Editor
    {
        [InitializeOnLoadMethod]
        private static void AddHook()
        {
            EditorApplication.playModeStateChanged -= NotifyLoadedAssets;
            EditorApplication.playModeStateChanged += NotifyLoadedAssets;
        }

        private static void NotifyLoadedAssets(PlayModeStateChange mode)
        {
            // Resources.FindObjectsOfTypeAll<> returns only objects that are
            // loaded, which should be faster than scanning the whole asset
            // database in large projects.

            MelcherScriptableObject[] loadedObjects =
                Resources.FindObjectsOfTypeAll<MelcherScriptableObject>();

            foreach (MelcherScriptableObject loadedObject in loadedObjects)
            {
                loadedObject.OnEditorPlayModeStateChanged(mode);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MelcherEasyButtonController.DoButtonCreation(target, targets);
        }
    }   // class MelcherScriptableObjectEditor

}   // namespace Melcher
