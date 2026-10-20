namespace Melcher
{
    using System;
    using UnityEditor;
    using System.Reflection;

    /// <summary>
    /// Snagged from Minchuilla's answer at
    /// https://answers.unity.com/questions/1328682/inspector-debug-mode-from-code.html
    /// </summary>
    public static class MelcherToggleInspectorMode
    {
        [MenuItem("Tools/Melcher Studios/Toggle Inspector Mode &d")]//Change the shortcut here
        static void ToggleInspectorDebug()
        {
            // "EditorWindow.editorFocused" can be used instead
            EditorWindow targetInspector = EditorWindow.mouseOverWindow;

            if (targetInspector != null && targetInspector.GetType().Name == "InspectorWindow")
            {
                // Get the type of the inspector window to find out the variable/method from
                Type type = Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.InspectorWindow");
                // Get the field we want to read, for the type (not our instance)
                FieldInfo field = type.GetField("m_InspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

                InspectorMode mode = (InspectorMode)field.GetValue(targetInspector);
                mode = (mode == InspectorMode.Normal ? InspectorMode.Debug : InspectorMode.Normal);

                // Find the method to change the mode for the type
                MethodInfo method = type.GetMethod("SetMode", BindingFlags.NonPublic | BindingFlags.Instance);
                //Call the function on our targetInspector, with the new mode as an object[]
                method.Invoke(targetInspector, new object[] { mode });

                //Refresh inspector
                targetInspector.Repaint();
            }
        }
    }   // class ToggleInspectorMode

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender
/// Initial creation date: 2023-02-10
/// ===========================================================================
