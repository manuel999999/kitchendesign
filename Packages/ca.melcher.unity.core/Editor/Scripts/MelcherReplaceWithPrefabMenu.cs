namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    public class MelcherReplaceWithPrefabMenu : EditorWindow
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Replace with prefab...")]
        public static void ShowWindow()
        {
            GetWindow<MelcherReplaceWithPrefabMenu>().Show();
        }

        private void OnGUI()
        {
            if (m_SerializedObject == null)
            {
                m_SerializedObject = new SerializedObject(this);
            }

            if (m_PrefabProperty == null)
            {
                m_PrefabProperty = m_SerializedObject.FindProperty(nameof(m_ReplaceWith));
            }

            EditorGUILayout.LabelField("Replace with prefab");

            EditorGUILayout.ObjectField(m_PrefabProperty, new GUIContent("Prefab"));

            m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (GUILayout.Button("Replaced selected with prefab"))
            {
                GameObject[] selection = Selection.gameObjects;
                GameObject toReplace = null;
                GameObject sharedParent = null;

                if (!m_ReplaceWith)
                {
                    Debug.LogError("Choose a prefab to replace selection with");
                }
                else if (IsAnySelectedObjectChildOfSelectedObject(selection, ref toReplace, ref sharedParent))
                {
                    Debug.LogError("Cannot replace " + toReplace.name + " because it's parent " + sharedParent.name + " is also going to be replaced", toReplace);
                }
                else
                {
                    ReplaceSelected(selection);
                }
            }
        }

        private bool IsAnySelectedObjectChildOfSelectedObject(GameObject[] selection, ref GameObject replace, ref GameObject sharedParent)
        {
            int selectedCount = selection.Length;

            for (int i = 0; i < selectedCount; ++i)
            {
                GameObject current = selection[i];
                Transform t = current.transform;

                while (t)
                {
                    for (int j = 0; j < selectedCount; ++j)
                    {
                        if (j != i && selection[j] == t.gameObject)
                        {
                            replace = current;
                            sharedParent = selection[j];
                            return true;
                        }
                    }

                    t = t.parent;
                }
            }

            return false;
        }

        private void ReplaceSelected(GameObject[] selection)
        {
            GameObject prefab = m_ReplaceWith;

            // Make sure user has prefab selected
            if (PrefabUtility.IsPartOfPrefabInstance(prefab))
            {
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_ReplaceWith);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            if (!prefab)
            {
                Debug.LogError("Nothing selected or not a prefab!", m_ReplaceWith);
                return;
            }

            int selectedCount = selection.Length;

            int[] siblingIndices = new int[selectedCount];
            for (int i = 0; i < selectedCount; ++i)
            {
                siblingIndices[i] = selection[i].transform.GetSiblingIndex();
            }

            for (int i = 0; i < selectedCount; ++i)
            {
                Transform current = selection[i].transform;

                int currentSiblingIndex = siblingIndices[i];

                GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(replacement, "Replaced game object");

                replacement.name = current.gameObject.name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);

                // Set transform
                replacement.transform.SetParent(current.parent);
                replacement.transform.position = current.position;
                replacement.transform.rotation = current.rotation;
                replacement.transform.localScale = current.localScale;

                replacement.transform.SetSiblingIndex(currentSiblingIndex);

                EditorUtility.SetDirty(replacement);
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.transform);
            }

            for (int i = 0; i < selectedCount; ++i)
            {
                Undo.DestroyObjectImmediate(selection[i]);
            }
        }

        // ====================================================================
        // Editor-serialized fields
        // ====================================================================

        [SerializeField] protected GameObject m_ReplaceWith;

        // ====================================================================
        // Internal fields
        // ====================================================================

        protected SerializedObject m_SerializedObject;
        protected SerializedProperty m_PrefabProperty;

    }   // class MelcherReplaceWithPrefabMenu

}   // namespace Melcher
