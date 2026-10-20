namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(InchSizeHelper))]
    public class InchSizeHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InchSizeHelper helper = (InchSizeHelper)target;

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!helper.start || !helper.end))
            {
                if (GUILayout.Button("Apply Size"))
                {
                    RecordUndo(helper, "Apply Size");
                    helper.ApplySize();
                }

                if (GUILayout.Button("Fill Between Start And End"))
                {
                    RecordUndo(helper, "Fill Between Start And End");
                    helper.FillBetweenStartAndEnd();
                }
            }

            using (new EditorGUI.DisabledScope(!helper.hook))
            {
                if (GUILayout.Button("Move Hook To Position"))
                {
                    Undo.RecordObject(helper.transform, "Move Hook To Position");
                    helper.MoveHookToPosition();
                }
            }
        }

        private static void RecordUndo(InchSizeHelper helper, string name)
        {
            Undo.RecordObjects(
                new Object[] { helper, helper.transform, helper.start, helper.end },
                name
            );
        }
    }
}
