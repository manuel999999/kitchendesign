namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(ButtonStyle))]
    public class ButtonStyleEditor : Editor
    {
        public void OnEnable()
        {
            m_ForcePreviewButtonStyleRefresh = true;
        }

        protected void RefreshPreviewButtonStyle()
        {
            if (!s_TransparencyBackground)
            {
                string pathContainingThisScript =
                    MelcherCommon.GetCallerSourceCodePath();

                pathContainingThisScript = System.IO.Path.GetDirectoryName(pathContainingThisScript);

                string transparencyBackgroundImagePath =
                    MelcherTransformFilePath.From("TransparencyBackground.png", relativeTo: pathContainingThisScript)
                        .To(MelcherPredefinedFilePath.project);

                s_TransparencyBackground =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        transparencyBackgroundImagePath
                    );
            }
            ButtonStyle buttonStyleToReplicate = (ButtonStyle)target;

            m_PreviewButtonStyle = new GUIStyle(GUI.skin.button);
            m_PreviewButtonStyle.fontStyle |= FontStyle.Bold;
            m_PreviewButtonStyle.fontSize = 20;
            m_PreviewButtonStyle.padding = new RectOffset(5, 5, 5, 5);

            Color foreground;
            Color background;

            // Normal:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Normal);

            m_PreviewButtonStyle.normal.textColor = foreground;
            m_PreviewButtonStyle.normal.background =
                CreateBackgroundTexture(background);

            // Hovered:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Hovered);

            m_PreviewButtonStyle.hover.textColor = foreground;
            m_PreviewButtonStyle.hover.background =
                CreateBackgroundTexture(background);

            // Pressed/active:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Pressed);

            m_PreviewButtonStyle.active.textColor = foreground;
            m_PreviewButtonStyle.active.background =
                CreateBackgroundTexture(background);

            // Noninteractable
            m_NonInteractablePreviewButtonStyle = new GUIStyle(m_PreviewButtonStyle);

            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.NonInteractable);

            m_NonInteractablePreviewButtonStyle.normal.textColor = foreground;
            m_NonInteractablePreviewButtonStyle.normal.background =
                CreateBackgroundTexture(background);

            m_NonInteractablePreviewButtonStyle.hover = m_NonInteractablePreviewButtonStyle.normal;
            m_NonInteractablePreviewButtonStyle.active = m_NonInteractablePreviewButtonStyle.normal;

            static Texture2D CreateBackgroundTexture(Color color)
            {
                Texture2D background = new Texture2D(1, 1);

                background.SetPixel(0, 0, color);

                background.Apply(
                    updateMipmaps: false,
                    makeNoLongerReadable: true
                );

                return background;
            }
        }

        public override void OnInspectorGUI()
        {
            using (var defaultInspectorControls = new EditorGUI.ChangeCheckScope())
            {
                base.OnInspectorGUI();

                EditorGUILayout.Space(20);

                if (defaultInspectorControls.changed || m_ForcePreviewButtonStyleRefresh)
                {
                    RefreshPreviewButtonStyle();

                    m_ForcePreviewButtonStyleRefresh = false;
                }

                using (new EditorGUI.DisabledGroupScope(disabled: true))
                {
                    // Draw a disabled button. Its size will be used to
                    // determine the size of the transparency checkerboard
                    // image. This button is disabled because otherwise it
                    // is prioritized for click events which prevents our
                    // intended button that draws over it from animating on
                    // click.
                    GUILayout.Button(s_PreviewButtonContent, m_PreviewButtonStyle);
                }

                const float CheckerboardImageDeltaMinY = 0.0f;
                const float CheckerboardImageDeltaMaxY = 4.0f;

                Rect previewButtonRect = GUILayoutUtility.GetLastRect();
                previewButtonRect.yMin += CheckerboardImageDeltaMinY;
                previewButtonRect.yMax += CheckerboardImageDeltaMaxY;
                EditorGUI.DrawTextureTransparent(previewButtonRect, s_TransparencyBackground, ScaleMode.ScaleAndCrop);

                float alpha =
                    Mathf.PingPong(
                        t: (float)EditorApplication.timeSinceStartup,
                        length: 0.8f
                    );

                alpha = MelcherCommon.RemapFrom01(alpha, 0.0f, 0.95f);

                EditorGUI.DrawRect(previewButtonRect, Color.grey.WithAlpha(alpha));

                GUI.Button(
                    previewButtonRect,
                    s_PreviewButtonContent,
                    style: m_PreviewButtonStyle
                );

                EditorGUILayout.Space(20);

                GUILayout.Label(s_NonInteractablePreviewButtonContent, m_NonInteractablePreviewButtonStyle);
                Rect nonInteractablePreviewButtonRect = GUILayoutUtility.GetLastRect();
                nonInteractablePreviewButtonRect.yMin += CheckerboardImageDeltaMinY;
                nonInteractablePreviewButtonRect.yMax += CheckerboardImageDeltaMaxY;
                //EditorGUI.DrawTextureTransparent(nonInteractablePreviewButtonRect, s_TransparencyBackground, ScaleMode.ScaleAndCrop);
                //GUI.Button(
                //    nonInteractablePreviewButtonRect,
                //    s_NonInteractablePreviewButtonContent,
                //    style: m_NonInteractablePreviewButtonStyle
                //);
            }

            // We must manually redraw the inspector *every frame* in order to
            // animate our button previews in real time. This is necessary
            // because Unity's auto-refresh button hover/click effects only
            // work for styles that derive from Button (or maybe that are just
            // named "Button") AND that use Unity's default button background
            // textures. We are using custom background textures which means
            // our button previews will not highlight on mouse hover every
            // frame unless we repaint every frame.
            // https://discussions.unity.com/t/guilayout-button-hover-texture-not-changing-immediately/197385/4
            Repaint();
        }

        protected static GUIContent s_PreviewButtonContent = new GUIContent("Preview Button");
        protected static GUIContent s_NonInteractablePreviewButtonContent = new GUIContent("Non-Interactable Example");
        protected static Texture2D s_TransparencyBackground = default;

        protected GUIStyle m_PreviewButtonStyle;
        protected GUIStyle m_NonInteractablePreviewButtonStyle;
        protected bool m_ForcePreviewButtonStyleRefresh;

    }   // class ButtonStyleEditor

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-05-24
/// ===========================================================================
