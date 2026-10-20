namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(MelcherSignal))]
    public class MelcherSignalEditor : Editor
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public void OnEnable()
        {
            m_QualifiedArgumentTypeNameProperty = serializedObject.FindProperty(MelcherSignal.QualifiedArgumentTypeNameMemberName);
            m_AllowNullProperty = serializedObject.FindProperty(MelcherSignal.AllowNullMemberName);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                // Display a text box to input the qualified type name
                EditorGUILayout.LabelField(m_QualifiedArgumentTypeNameProperty.displayName, s_NoLayoutOptions);
                m_QualifiedArgumentTypeNameProperty.stringValue =
                    EditorGUILayout.DelayedTextField(m_QualifiedArgumentTypeNameProperty.stringValue, s_NoLayoutOptions);

                // Checkbox, can the argument can be null
                EditorGUILayout.PropertyField(m_AllowNullProperty, s_NoLayoutOptions);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.Space(16.0f);

            // Display the signal's argument type name
            var signal = (MelcherSignal)serializedObject.targetObject;
            GUI.enabled = false;
            var expectedArgumentType = signal.GetExpectedArgumentType();
            var typeName = expectedArgumentType != null ? expectedArgumentType.FullName : "<unknown type>";
            EditorGUILayout.TextField(typeName, s_NoLayoutOptions);
            GUI.enabled = true;
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly GUILayoutOption[] s_NoLayoutOptions = System.Array.Empty<GUILayoutOption>();

        // =====================================================================
        // Internal fields
        // =====================================================================

        private SerializedProperty m_QualifiedArgumentTypeNameProperty;
        private SerializedProperty m_AllowNullProperty;

    }   // class MelcherSignalEditor

}   // namespace Melcher
