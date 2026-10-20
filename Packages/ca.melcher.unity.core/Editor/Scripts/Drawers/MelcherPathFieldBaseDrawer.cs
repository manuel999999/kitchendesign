namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using System.Reflection;

    [CustomPropertyDrawer(typeof(MelcherPathFieldBase), useForChildren: true)]
    public class MelcherPathFieldBaseDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (var propertyScope = new EditorGUI.PropertyScope(position, label, property))
            {
                // Get the raw MelcherPathField object
                MelcherPathFieldBase pathField =
                    MelcherEditorUtility.GetActualObjectForSerializedProperty<MelcherPathFieldBase>(
                        fieldInfo,
                        property,
                        out int arrayIndex
                    );

                if (pathField == null)
                {
                    return;
                }

                System.Type assetType = pathField.assetType;

                string assetPath = AssetDatabase.GUIDToAssetPath(pathField.guid);

                Object currentAsset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);

                EditorGUI.BeginChangeCheck();
                {
                    // Show a label if it's not an array entry
                    if (arrayIndex < 0)
                    {
                        position = EditorGUI.PrefixLabel(
                            position,
                            propertyScope.content
                        );
                    }
                
                    currentAsset =
                        EditorGUI.ObjectField(
                            position,
                            currentAsset,
                            assetType,
                            allowSceneObjects: false
                        );
                }

                if (EditorGUI.EndChangeCheck())
                {
                    // TODO:    This only serializes the asset's path at the
                    //          moment it is selected. If the asset is renamed
                    //          or is moved to a different path, the serialized
                    //          path will be incorrect.

                    assetPath = AssetDatabase.GetAssetPath(currentAsset);

                    string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                    pathField.SetPathAndGuid(assetPath, assetGuid);

                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }
            }
        }
    }   // class MelcherPathFieldBaseDrawer

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
