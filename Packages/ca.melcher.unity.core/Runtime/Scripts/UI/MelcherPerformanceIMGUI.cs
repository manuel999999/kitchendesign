namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Draw the framerate in the top right corner of the screen.
    /// </summary>
    public class MelcherPerformanceIMGUI : MelcherMonoBehaviour
    {
        // ====================================================================
        // Static methods
        // ====================================================================
        public static void CreatePersistentInstance()
        {
            GameObject go = new GameObject(nameof(MelcherPerformanceIMGUI));
            go.AddComponent<MelcherPerformanceIMGUI>();
            DontDestroyOnLoad(go);
        }
        public static GameObject CreatePersistentInstanceReturningGameObject()
        {
            GameObject go = new GameObject(nameof(MelcherPerformanceIMGUI));
            go.AddComponent<MelcherPerformanceIMGUI>();
            DontDestroyOnLoad(go);
            return go;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        private void Awake()
        {
            m_Content = new GUIContent();

            m_TextStyleForeground = new GUIStyle();
            m_TextStyleForeground.alignment = TextAnchor.UpperRight;
            m_TextStyleForeground.normal.textColor = Color.white;
            m_TextStyleForeground.fontStyle = FontStyle.Bold;
            m_TextStyleForeground.fontSize = 72;

            m_TextStyleBackground = new GUIStyle(m_TextStyleForeground);
            m_TextStyleBackground.normal.textColor = Color.black;
        }

        private void OnGUI()
        {
            float screenDiagonal = new Vector2(Screen.width, Screen.height).sqrMagnitude;
            float scaleAdjust = screenDiagonal / s_ReferenceScreenDiagonal;

            int scaledFontSize = Mathf.RoundToInt(s_ReferenceFontSize * scaleAdjust);
            m_TextStyleForeground.fontSize = scaledFontSize;
            m_TextStyleBackground.fontSize = scaledFontSize;

            if (Time.frameCount % FramesBetweenUpdate == 0)
            {
                m_Content.text = (1.0f / Time.smoothDeltaTime).ToString("0");
            }

            // Calculate text size
            Vector2 size = m_TextStyleBackground.CalcSize(m_Content);

            // Calculate padding from corner of screen
            const int Pad = 16;
            Rect safeArea = MelcherSafeAreaConformer.GetSafeAreaWithMinimums(Pad, Pad, Pad, Pad);
            float x = safeArea.xMax - size.x - s_CornerOffsetSize * scaleAdjust;
            float y = Screen.height - safeArea.yMax + s_CornerOffsetSize * scaleAdjust;

            Rect rect = new Rect(x, y, size.x, size.y);

            // Draw the FPS. Draw 3 copies: 2 in black and offset, 1 in white
            // on top for contrast
            GUI.Label(rect, m_Content, m_TextStyleBackground);

            rect.position += s_TextLayeringOffset * scaleAdjust * 2;
            GUI.Label(rect, m_Content, m_TextStyleBackground);

            rect.position -= s_TextLayeringOffset * scaleAdjust;
            GUI.Label(rect, m_Content, m_TextStyleForeground);
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        /// Number of frames to wait between updating the FPS.
        /// </summary>
        public const int FramesBetweenUpdate = 10;

        /// <summary>
        /// How far from the corner should the text render.
        /// </summary>
        private const float s_CornerOffsetSize = 24.0f;

        private const int s_ReferenceFontSize = 72;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly float s_ReferenceScreenDiagonal = new Vector2(1920.0f, 1080.0f).sqrMagnitude;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly Vector2 s_TextLayeringOffset = -Vector2.one * 4.0f;

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private GUIContent m_Content = null;
        private GUIStyle m_TextStyleForeground = null;
        private GUIStyle m_TextStyleBackground = null;

    }   // class PerformanceIMGUI

}   // namespace Melcher
