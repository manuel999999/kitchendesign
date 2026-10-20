namespace Melcher
{
    using UnityEditor;

    [CustomEditor(typeof(MelcherScenePathAppValue))]
    public class MelcherScenePathAppValueEditor : MelcherPathAppValueEditor<SceneAsset>
    {
        public override string inspectorLabel => "Scene";

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            bool enterChildren = true;
            using SerializedProperty iter = serializedObject.GetIterator();
            while (iter.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iter.propertyType == SerializedPropertyType.ObjectReference &&
                    iter.objectReferenceValue is MonoScript)
                {
                    using (new EditorGUI.DisabledScope(disabled: true))
                    {
                        // Draw the current property (the MonoScript field)
                        EditorGUILayout.PropertyField(iter);
                    }

                    using (new EditorGUI.DisabledScope(disabled: false))
                    {
                        // Draw "Is Runtime Scene" checkbox
                        SerializedProperty isRuntimeSceneProperty = serializedObject.FindProperty(
                            MelcherScenePathAppValue.IsRuntimeSceneMemberName
                        );
                        EditorGUI.BeginChangeCheck();
                        {
                            EditorGUILayout.PropertyField(isRuntimeSceneProperty);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        }

                        // If it's a runtime scene, show a textbox to enter its name
                        if ((m_PathAsset as MelcherScenePathAppValue).isRuntimeScene)
                        {
                            var sceneNameProperty = serializedObject.FindProperty(
                                MelcherScenePathAppValue.RuntimeSceneNameMemberName
                            );
                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUILayout.DelayedTextField(sceneNameProperty);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                                serializedObject.Update();
                            }
                        }
                        else
                        {
                            DrawAssetField();
                        }
                    }

                    return;
                }
            }
        }

    }   // class MelcherScenePathAppValueEditor

    /// <summary>
    /// Validate SceneReferenceAssets whenever the editor's scene build list
    /// changes.
    /// </summary>
    [InitializeOnLoad]
    public static class MelcherScenePathAppValueRefresher
    {
        static MelcherScenePathAppValueRefresher()
        {
            EditorBuildSettings.sceneListChanged -= RefreshSceneReferenceAssets;
            EditorBuildSettings.sceneListChanged += RefreshSceneReferenceAssets;
        }

        /// <summary>
        /// Refresh the path of all <see cref="MelcherScenePathAsset"/> so they
        /// match the settings in the project's build scene list.
        /// </summary>
        private static void RefreshSceneReferenceAssets()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(MelcherScenePathAppValue)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MelcherScenePathAppValue asset =
                    AssetDatabase.LoadAssetAtPath<MelcherScenePathAppValue>(path);
                MelcherEditorUtility.Validate(asset);
            }
        }

    }   // class MelcherScenePathAppValueRefresher

}   // namespace Melcher
