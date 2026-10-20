namespace Melcher
{
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    ///
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherStableEnumBase), useForChildren: true)]
    public class MelcherStableEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

            using var propertyScope =
                new EditorGUI.PropertyScope(position, label, property);

            label = propertyScope.content;

            using var changeCheck = new EditorGUI.ChangeCheckScope();

            // Determine whether to show a bitmask field or an exclusive value
            // pop-up.

            object fieldValue =
                fieldInfo.GetValue(
                    property.serializedObject.targetObject
                );

            // This might be an array, in which case we need to know what index
            // it is.

            int? arrayElementIndex = null;

            if (fieldValue.GetType().IsArray && fieldValue is object[] fieldValueArray)
            {
                Match indexMatch = Regex.Match(property.propertyPath, @"\[(\d+)\]$");

                arrayElementIndex = int.Parse(indexMatch.Groups[1].Value);

                fieldValue = null;

                if (fieldValueArray.Length > 0)
                {
                    fieldValue = fieldValueArray[arrayElementIndex.Value];
                }

                if (fieldValue == null)
                {
                    // Not expected to happen.
                    MelcherLog.Assert(fieldValue != null, "The MelcherStableEnum value within the array is null.");
                    return;
                }
            }

            // The first object (in case of a multi-object selection)
            MelcherStableEnumBase firstStableEnum =
                (MelcherStableEnumBase)fieldValue;

            System.Type stableEnumType = firstStableEnum.GetType();

            System.Type stableEnumUnderlyingType =
                stableEnumType.GenericTypeArguments[0];

            bool isFlagsEnum =
                stableEnumUnderlyingType.IsDefined(typeof(System.FlagsAttribute));

            System.Enum newValue;

            if (isFlagsEnum)
            {
                newValue =
                    EditorGUI.EnumFlagsField(position, label, firstStableEnum.underlyingValue);
            }
            else
            {
                newValue =
                    EditorGUI.EnumPopup(position, label, firstStableEnum.underlyingValue);
            }

            if (changeCheck.changed)
            {
                firstStableEnum.ClearErrorSource();

                foreach (Object targetObject in property.serializedObject.targetObjects)
                {
                    fieldValue =
                        fieldInfo.GetValue(
                            property.serializedObject.targetObject
                        );

                    if (fieldValue.GetType().IsArray && fieldValue is object[] fieldValueArray2)
                    {
                        fieldValue = null;

                        if (fieldValueArray2.Length > 0)
                        {
                            fieldValue = fieldValueArray2[arrayElementIndex.Value];
                        }
                    }

                    if (fieldValue != null)
                    {
                        MelcherStableEnumBase stableEnum =
                            (MelcherStableEnumBase)fieldValue;

                        stableEnum.underlyingValue = newValue;

                        EditorUtility.SetDirty(targetObject);

                        // TODO: Fix this so Validate is called implictly. We
                        //       probably need to assign the value through a
                        //       SerializedProperty. -Ryan
                        MelcherEditorUtility.Validate(targetObject);
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GUIStyle objectFieldStyle = EditorStyles.objectField;

            return objectFieldStyle.lineHeight + objectFieldStyle.padding.bottom + objectFieldStyle.padding.top;
        }

    }   // class MelcherStableEnumDrawer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2023-11-02
/// ===========================================================================
