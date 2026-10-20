namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;
    /// <summary>
    /// <br>
    ///     PropertyDrawer for attribute <see cref="MelcherShowOnValueAttribute"/>.
    /// </br>
    /// <para>
    ///     We get the intended field type from the <see cref="MelcherShowOnValueAttribute"/>
    ///     as well as the value (in object form) that we need to compare it to.
    /// </para>
    /// <para>
    ///     If the value of incoming field matches the value of the property,
    ///     we display the property in the inspector, at it's usual height. Otherwise,
    ///     we simply don't draw it, and we set the property height to 0.
    /// </para>
    /// </summary>

    [CustomPropertyDrawer(typeof(MelcherShowOnValueAttribute))]
    public class MelcherShowOnValueAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            MelcherShowOnValueAttribute showOnValueAttribute = attribute as MelcherShowOnValueAttribute;

            UnityEngine.Object scriptInstance = property.serializedObject.targetObject;

            string fieldName = showOnValueAttribute.variable;
            object showOnValue = showOnValueAttribute.showOnValue;

            FieldInfo fieldInfo = (FieldInfo)showOnValueAttribute.fieldInfoObject;
            if (fieldInfo == null)
            {
                showOnValueAttribute.DisplayError(fieldName);
                return;
            }

            object fieldValue = fieldInfo.GetValue(scriptInstance);

            if (fieldValue.GetType() != showOnValue.GetType())
            {
                showOnValueAttribute.DisplayError(showOnValue.ToString());
                return;
            }

            EditorGUI.BeginChangeCheck();
            {
                if (object.Equals(fieldValue, showOnValue))
                {
                    m_IsShown = true;
                    position.xMin += 20;
                    EditorGUI.PropertyField(position, property, new GUIContent(ObjectNames.NicifyVariableName(property.name)), true);
                }
                else
                {
                    m_IsShown = false;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_IsShown ? base.GetPropertyHeight(property, label) : 0f;
        }
        bool m_IsShown;

    }   // class MelcherShowOnValueAttributeEditor

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2023-23-02
/// ===========================================================================
