namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// The inspector drawer for <see cref="AssetPathAsset"/>'s. Displays the
    /// GUID of the referenced asset, its data path and draws an object field
    /// for selecting a new asset.
    /// </summary>
    /// <typeparam name="AssetFilterType">
    /// The asset type to restrict the property field to and to filter with in
    /// the asset picker.
    /// </typeparam>
    public abstract class MelcherPathAppValueEditor<AssetFilterType> : Editor
        where AssetFilterType : Object
    {
        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        /// Label to show in front of the asset's property field.
        /// </summary>
        public abstract string inspectorLabel { get; }

        // =====================================================================
        // Methods
        // =====================================================================

        public void OnEnable()
        {
            // Convert the target's GUID to an asset
            m_PathAsset = (MelcherPathAppValue)target;

            string assetPath = AssetDatabase.GUIDToAssetPath(m_PathAsset.guid);
            m_ObjectAtPath = AssetDatabase.LoadAssetAtPath<AssetFilterType>(assetPath);
        }

        public override void OnInspectorGUI()
        {
            // Draw the serialized fields
            using SerializedProperty iter = serializedObject.GetIterator();

            if (iter.NextVisible(enterChildren: true))
            {
                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    do
                    {
                        EditorGUILayout.PropertyField(iter);
                    }
                    while (iter.NextVisible(enterChildren: false));
                }
            }

            DrawAssetField();
        }

        protected virtual void DrawAssetField()
        {
            // Draw an object field that filters possible values to only those
            // matching AssetFilterType
            EditorGUI.BeginChangeCheck();
            {
                m_ObjectAtPath =
                    (AssetFilterType)EditorGUILayout.ObjectField(
                        label: inspectorLabel,
                        obj: m_ObjectAtPath,
                        objType: typeof(AssetFilterType),
                        allowSceneObjects: false,
                        options: System.Array.Empty<GUILayoutOption>()
                    );
            }
            if (EditorGUI.EndChangeCheck())
            {
                string projectRelAssetPath = AssetDatabase.GetAssetPath(m_ObjectAtPath);
                string assetGuid = AssetDatabase.AssetPathToGUID(projectRelAssetPath);

                m_PathAsset.SetPathAndGuid(projectRelAssetPath, assetGuid);

                EditorUtility.SetDirty(m_PathAsset);
                serializedObject.Update();
            }
        }

        // =====================================================================
        // Internal fields
        // =====================================================================

        protected MelcherPathAppValue m_PathAsset;
        protected AssetFilterType m_ObjectAtPath;

    }   // class MelcherPathAppValueEditor

    /// <summary>
    /// Default drawer for paths to an Object. This is used for by all <see
    /// cref="MelcherPathAsset"/>s, unless they provide a custom editor.
    /// </summary>
    [CustomEditor(typeof(MelcherPathAppValue), isFallback = true)]
    public class MelcherPathAppValueDefaultEditor : MelcherPathAppValueEditor<Object>
    {
        public override string inspectorLabel => "Asset";

    }   // class MelcherPathAppValueDefaultEditor

}   // namespace Melcher
