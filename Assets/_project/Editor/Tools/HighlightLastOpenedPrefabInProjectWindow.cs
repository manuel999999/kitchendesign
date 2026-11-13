namespace KitchenDesign.ToolsEditor
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;

    /// <summary>
    ///     Draw a green highlight/border/outline in the project window around
    ///     the last prefab that was opened. This is intended to help
    ///     developers keep track of which prefab they last updated, which is
    ///     useful when updating a long list of prefabs. Unity's default
    ///     behaviour is to remove the selection highlight and prevent an
    ///     otherwise helpful visual indicator of where the developer left off.
    /// </summary>
    public static class HighlightLastOpenedPrefabInProjectWindow
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.projectWindowItemOnGUI -= OnProjectItemGUI;
            EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;

            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;

            if (MelcherEditorUserSettings.TryGet(EditorUserSetting.GuidHashCodeOfLastOpenedPrefab, out string hashOfGuid))
            {
                int.TryParse(hashOfGuid, out _guidHashCodeOfLastOpenedPrefab);
            }
        }

        private static void OnProjectItemGUI(string guid, Rect rect)
        {
            if (_guidHashCodeOfLastOpenedPrefab != guid.GetHashCode())
            {
                return;
            }

            // Ensure the outline on the right edge of the rectangle remains
            // visible.
            rect.xMax -= 0.5f;

            Vector3 a = rect.min;
            Vector3 b = new(rect.xMax, rect.yMin);
            Vector3 c = rect.max;
            Vector3 d = new(rect.xMin, rect.yMax);

            _linesForOutline[0] = a;
            _linesForOutline[1] = b;
            _linesForOutline[2] = b;
            _linesForOutline[3] = c;
            _linesForOutline[4] = c;
            _linesForOutline[5] = d;
            _linesForOutline[6] = d;
            _linesForOutline[7] = a;

            Handles.color = Color.green;
            Handles.DrawSolidRectangleWithOutline(rect, faceColor: new Color(0.0f, 0.8f, 0.1f, 0.04f), outlineColor: Color.green);
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            string prefabGuid =
                AssetDatabase.AssetPathToGUID(prefabStage.assetPath);

            _guidHashCodeOfLastOpenedPrefab = prefabGuid.GetHashCode();

            // Save the GUID so it can be recovered if the user recompiles
            // any scripts.
            MelcherEditorUserSettings.Set(EditorUserSetting.GuidHashCodeOfLastOpenedPrefab, _guidHashCodeOfLastOpenedPrefab.ToString());
        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static int _guidHashCodeOfLastOpenedPrefab;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static Vector3[] _linesForOutline = new Vector3[8];

        // ====================================================================
        // Nested types
        // ====================================================================

        public static class EditorUserSetting
        {
            public const string GuidHashCodeOfLastOpenedPrefab = nameof(GuidHashCodeOfLastOpenedPrefab);
        }

    }   // class PersistentProjectWindowSelectionHighlight

}   // namespace KitchenDesign.ToolsEditor

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-23
/// ===========================================================================
