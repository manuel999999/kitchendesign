namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherHideInNormalInspectorAttribute))]
    class MelcherHideInNormalInspectorAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
        }
    }   // class MelcherHideInNormalInspectorAttributeDrawer

}   // namespace Melcher
