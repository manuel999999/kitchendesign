namespace KitchenDesign
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
                int.TryParse(hashOfGuid, out s_GuidHashCodeOfLastOpenedPrefab);
            }
        }

        private static void OnProjectItemGUI(string guid, Rect rect)
        {
            if (s_GuidHashCodeOfLastOpenedPrefab != guid.GetHashCode())
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

            s_LinesForOutline[0] = a;
            s_LinesForOutline[1] = b;
            s_LinesForOutline[2] = b;
            s_LinesForOutline[3] = c;
            s_LinesForOutline[4] = c;
            s_LinesForOutline[5] = d;
            s_LinesForOutline[6] = d;
            s_LinesForOutline[7] = a;

            Handles.color = Color.green;
            Handles.DrawSolidRectangleWithOutline(rect, faceColor: new Color(0.0f, 0.8f, 0.1f, 0.04f), outlineColor: Color.green);
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            string prefabGuid =
                AssetDatabase.AssetPathToGUID(prefabStage.assetPath);

            s_GuidHashCodeOfLastOpenedPrefab = prefabGuid.GetHashCode();

            // Save the GUID so it can be recovered if the user recompiles
            // any scripts.
            MelcherEditorUserSettings.Set(EditorUserSetting.GuidHashCodeOfLastOpenedPrefab, s_GuidHashCodeOfLastOpenedPrefab.ToString());
        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static int s_GuidHashCodeOfLastOpenedPrefab;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static Vector3[] s_LinesForOutline = new Vector3[8];

        // ====================================================================
        // Nested types
        // ====================================================================

        public static class EditorUserSetting
        {
            public const string GuidHashCodeOfLastOpenedPrefab = nameof(GuidHashCodeOfLastOpenedPrefab);
        }

    }   // class PersistentProjectWindowSelectionHighlight

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-10-23
/// ===========================================================================
