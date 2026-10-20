namespace Melcher
{
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(MelcherMonoBehaviour), editorForChildClasses: true)]
    public class MelcherMonoBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MelcherEasyButtonController.DoButtonCreation(target, targets);
        }
    }   // class MelcherEasyButtonAttributeEditor
}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2022-08-16
/// ===========================================================================
