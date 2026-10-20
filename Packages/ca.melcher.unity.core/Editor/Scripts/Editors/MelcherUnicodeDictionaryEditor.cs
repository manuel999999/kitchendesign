namespace Melcher
{
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// This editor adds a "Rebuild" button to the asset's inspector which
    /// allows the asset to recompute its character map for TextMesh Pro.
    /// </summary>
    [CustomEditor(typeof(MelcherUnicodeDictionary))]
    public class MelcherUnicodeDictionaryEditor : Editor
    {
        public const int IconSize = 18;

        public override void OnInspectorGUI()
        {
            using var resetCurrentFont =
                MelcherDeferred.Defer(
                    toCapture: currentGlyphStyle,
                    currentGlyphStyle => currentGlyphStyle.font = null
                );

            string projectRelativeFontPath =
                (target as MelcherUnicodeDictionary).GetSourceFontPath();

            if (projectRelativeFontPath != null)
            {
                currentGlyphStyle.font = AssetDatabase.LoadAssetAtPath<Font>(projectRelativeFontPath);
            }
            else
            {
                currentGlyphStyle.font = GUI.skin.button.font;
            }

            base.OnInspectorGUI();

            if (GUILayout.Button("Rebuild"))
            {
                (target as MelcherUnicodeDictionary).Rebuild();
            }
        }

        public static GUIStyle currentGlyphStyle
        {
            get
            {
                if (s_CurrentGlyphStyle == null)
                {
                    s_CurrentGlyphStyle = new GUIStyle(GUI.skin.button);
                    s_CurrentGlyphStyle.fontSize = IconSize;
                    s_CurrentGlyphStyle.alignment = TextAnchor.MiddleCenter;
                }
                return s_CurrentGlyphStyle;
            }
        }

        public static GUIContent entryKeyValueSeparator
        {
            get
            {
                const string RightArrow = "\u2192";
                s_EntryKeyValueSeparator ??= new GUIContent(RightArrow);
                return s_EntryKeyValueSeparator;
            }
        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIStyle s_CurrentGlyphStyle;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static GUIContent s_EntryKeyValueSeparator;

    }   // class MelcherUnicodeDictionaryEditor

}   // namespace Melcher
