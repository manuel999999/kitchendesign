namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(MelcherHeaderAttribute))]
    public class MelcherHeaderAttributeEditor : DecoratorDrawer
    {
        const string melcherHeaderLabelClassName = "melcher-header-drawer__label";

        public override void OnGUI(Rect position)
        {
            // MelcherLog.Log($"ON Gui for HeaderAttribute");

            position.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, (attribute as MelcherHeaderAttribute).header, (attribute as MelcherHeaderAttribute).style);
        }

        public override float GetHeight()
        {
            var indentedRect = new Rect(Screen.safeArea);
            GUIContent content = new GUIContent((attribute as MelcherHeaderAttribute).header);
            float fullTextHeight = 
                (attribute as MelcherHeaderAttribute).style.CalcHeight
                (content, indentedRect.width - (attribute as MelcherHeaderAttribute)
                .style.CalcSize(content).y);
            return EditorGUIUtility.singleLineHeight * 1.5f + fullTextHeight;
        }

    }   // class MelcherHeaderAttribute

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2022-08-16
/// ===========================================================================
