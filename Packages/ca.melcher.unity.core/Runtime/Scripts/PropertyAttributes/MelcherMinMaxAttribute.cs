namespace Melcher
{
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif  // if UNITY_EDITOR

    /// <summary>
    /// Clamp the values of Vector2 and Vector2Int between two values.
    /// </summary>
    public sealed class MelcherMinMaxAttribute : PropertyAttribute
    {
        public MelcherMinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public readonly float min;
        public readonly float max;
    }   // class MinMaxAttribute

#if UNITY_EDITOR
    /// <summary>
    /// Draws a MinMax slider in the inspector window.
    /// </summary>
    [CustomPropertyDrawer(typeof(MelcherMinMaxAttribute))]
    public class MelcherMinMaxDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2 &&
                property.propertyType != SerializedPropertyType.Vector2Int)
            {
                EditorGUI.LabelField(position, label.text, "You Must Use MelcherMinMaxAttribute with Vector2 or Vector2Int.");
            }
            else
            {
                label = EditorGUI.BeginProperty(position, label, property);
                {
                    Rect controlPosition = EditorGUI.PrefixLabel(position, label);
                    bool isGuiEnabled = GUI.enabled;

                    const int FieldPadding = 0;
                    const int SliderPadding = 8;
                    float numericWidth = Mathf.Min(40.0f, (controlPosition.width * 0.20f) - FieldPadding);

                    //For nested versions of this property
                    if (property.depth > 0)
                    {
                        controlPosition.x = controlPosition.x - position.x + 18;
                        controlPosition.width = controlPosition.width + position.x - 18;
                        numericWidth = Mathf.Min(70.0f, (controlPosition.width * 0.20f) - FieldPadding);
                    }

                    Rect limitMinPosition = controlPosition;
                    limitMinPosition.width = numericWidth;

                    //var valueMinPosition = limitMinPosition;
                    //valueMinPosition.x = limitMinPosition.xMax + FieldPadding;
                    Rect valueMinPosition = limitMinPosition;

                    Rect limitMaxPosition = valueMinPosition;
                    limitMaxPosition.x = controlPosition.xMax - limitMaxPosition.width;

                    //var valueMaxPosition = limitMaxPosition;
                    //valueMaxPosition.x = limitMaxPosition.xMin - valueMaxPosition.width - FieldPadding;
                    Rect valueMaxPosition = limitMaxPosition;

                    Rect minMaxSliderPosition = controlPosition;
                    minMaxSliderPosition.xMin = valueMinPosition.xMax + SliderPadding;
                    minMaxSliderPosition.xMax = valueMaxPosition.xMin - SliderPadding;

                    MelcherMinMaxAttribute minMax = attribute as MelcherMinMaxAttribute;

                    if (property.propertyType == SerializedPropertyType.Vector2)
                    {
                        // Draw min and max values
                        //GUI.enabled = false;
                        //EditorGUI.FloatField(limitMinPosition, minMax.min);
                        //EditorGUI.FloatField(limitMaxPosition, minMax.max);
                        //GUI.enabled = isGuiEnabled;

                        Vector2 value = property.vector2Value;

                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.MinMaxSlider(minMaxSliderPosition, ref value.x, ref value.y, minMax.min, minMax.max);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Limit slider to 2 decimal places
                            value.x = float.Parse(value.x.ToString("F2"));
                            value.y = float.Parse(value.y.ToString("F2"));
                            property.vector2Value = value;
                        }

                        EditorGUI.BeginChangeCheck();
                        {


                            value.x = EditorGUI.DelayedFloatField(valueMinPosition, value.x);
                            value.y = EditorGUI.DelayedFloatField(valueMaxPosition, value.y);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            value.x = Mathf.Clamp(value.x, minMax.min, value.y);
                            value.y = Mathf.Clamp(value.y, value.x, minMax.max);
                            property.vector2Value = value;
                        }
                    }
                    else
                    {
                        //GUI.enabled = false;
                        //EditorGUI.FloatField(limitMinPosition, minMax.min);
                        //EditorGUI.FloatField(limitMaxPosition, minMax.max);
                        //GUI.enabled = isGuiEnabled;

                        Vector2 value = property.vector2IntValue;
                        bool valueChanged = false;

                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUI.MinMaxSlider(minMaxSliderPosition, ref value.x, ref value.y, minMax.min, minMax.max);
                        }
                        valueChanged |= EditorGUI.EndChangeCheck();

                        EditorGUI.BeginChangeCheck();
                        {
                            value.x = EditorGUI.DelayedIntField(valueMinPosition, Mathf.FloorToInt(value.x));
                            value.y = EditorGUI.DelayedIntField(valueMaxPosition, Mathf.FloorToInt(value.y));
                        }
                        valueChanged |= EditorGUI.EndChangeCheck();

                        if (valueChanged)
                        {
                            value.x = Mathf.Clamp(value.x, minMax.min, value.y);
                            value.y = Mathf.Clamp(value.y, value.x, minMax.max);

                            property.vector2IntValue = new Vector2Int(
                                Mathf.FloorToInt(value.x),
                                Mathf.FloorToInt(value.y)
                            );
                        }
                    }
                }
                EditorGUI.EndProperty();
            }
        }
    }   // class MelcherMinMaxDrawer
#endif  // UNITY_EDITOR

}   // namespace Melcher
