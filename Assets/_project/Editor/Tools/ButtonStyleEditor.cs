namespace KitchenDesign.ToolsEditor
{
    using Melcher;
    using UnityEngine;
    using UnityEditor;
    using KitchenDesign.Scriptables;

    [CustomEditor(typeof(ButtonStyleSO))]
    public class ButtonStyleEditor : Editor
    {
        public void OnEnable()
        {
            _forcePreviewButtonStyleRefresh = true;
        }

        protected void RefreshPreviewButtonStyle()
        {
            if (!_transparencyBackground)
            {
                string pathContainingThisScript =
                    MelcherCommon.GetCallerSourceCodePath();

                pathContainingThisScript = System.IO.Path.GetDirectoryName(pathContainingThisScript);

                string transparencyBackgroundImagePath =
                    MelcherTransformFilePath.From("TransparencyBackground.png", relativeTo: pathContainingThisScript)
                        .To(MelcherPredefinedFilePath.project);

                _transparencyBackground =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        transparencyBackgroundImagePath
                    );
            }
            ButtonStyleSO buttonStyleToReplicate = (ButtonStyleSO)target;

            _previewButtonStyle = new GUIStyle(GUI.skin.button);
            _previewButtonStyle.fontStyle |= FontStyle.Bold;
            _previewButtonStyle.fontSize = 20;
            _previewButtonStyle.padding = new RectOffset(5, 5, 5, 5);

            Color foreground;
            Color background;

            // Normal:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Normal);

            _previewButtonStyle.normal.textColor = foreground;
            _previewButtonStyle.normal.background =
                CreateBackgroundTexture(background);

            // Hovered:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Hovered);

            _previewButtonStyle.hover.textColor = foreground;
            _previewButtonStyle.hover.background =
                CreateBackgroundTexture(background);

            // Pressed/active:
            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.Pressed);

            _previewButtonStyle.active.textColor = foreground;
            _previewButtonStyle.active.background =
                CreateBackgroundTexture(background);

            // Noninteractable
            _nonInteractablePreviewButtonStyle = new GUIStyle(_previewButtonStyle);

            (foreground, background) =
                buttonStyleToReplicate.ColorsForAppearance(MelcherSelectable.Appearance.NonInteractable);

            _nonInteractablePreviewButtonStyle.normal.textColor = foreground;
            _nonInteractablePreviewButtonStyle.normal.background =
                CreateBackgroundTexture(background);

            _nonInteractablePreviewButtonStyle.hover = _nonInteractablePreviewButtonStyle.normal;
            _nonInteractablePreviewButtonStyle.active = _nonInteractablePreviewButtonStyle.normal;

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

                if (defaultInspectorControls.changed || _forcePreviewButtonStyleRefresh)
                {
                    RefreshPreviewButtonStyle();

                    _forcePreviewButtonStyleRefresh = false;
                }

                using (new EditorGUI.DisabledGroupScope(disabled: true))
                {
                    // Draw a disabled button. Its size will be used to
                    // determine the size of the transparency checkerboard
                    // image. This button is disabled because otherwise it
                    // is prioritized for click events which prevents our
                    // intended button that draws over it from animating on
                    // click.
                    GUILayout.Button(_previewButtonContent, _previewButtonStyle);
                }

                const float CheckerboardImageDeltaMinY = 0.0f;
                const float CheckerboardImageDeltaMaxY = 4.0f;

                Rect previewButtonRect = GUILayoutUtility.GetLastRect();
                previewButtonRect.yMin += CheckerboardImageDeltaMinY;
                previewButtonRect.yMax += CheckerboardImageDeltaMaxY;
                EditorGUI.DrawTextureTransparent(previewButtonRect, _transparencyBackground, ScaleMode.ScaleAndCrop);

                float alpha =
                    Mathf.PingPong(
                        t: (float)EditorApplication.timeSinceStartup,
                        length: 0.8f
                    );

                alpha = MelcherCommon.RemapFrom01(alpha, 0.0f, 0.95f);

                EditorGUI.DrawRect(previewButtonRect, Color.grey.WithAlpha(alpha));

                GUI.Button(
                    previewButtonRect,
                    _previewButtonContent,
                    style: _previewButtonStyle
                );

                EditorGUILayout.Space(20);

                GUILayout.Label(_nonInteractablePreviewButtonContent, _nonInteractablePreviewButtonStyle);
                Rect nonInteractablePreviewButtonRect = GUILayoutUtility.GetLastRect();
                nonInteractablePreviewButtonRect.yMin += CheckerboardImageDeltaMinY;
                nonInteractablePreviewButtonRect.yMax += CheckerboardImageDeltaMaxY;
                //EditorGUI.DrawTextureTransparent(nonInteractablePreviewButtonRect, _transparencyBackground, ScaleMode.ScaleAndCrop);
                //GUI.Button(
                //    nonInteractablePreviewButtonRect,
                //    _nonInteractablePreviewButtonContent,
                //    style: _nonInteractablePreviewButtonStyle
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

        protected static GUIContent _previewButtonContent = new GUIContent("Preview Button");
        protected static GUIContent _nonInteractablePreviewButtonContent = new GUIContent("Non-Interactable Example");
        protected static Texture2D _transparencyBackground = default;

        protected GUIStyle _previewButtonStyle;
        protected GUIStyle _nonInteractablePreviewButtonStyle;
        protected bool _forcePreviewButtonStyleRefresh;

    }   // class ButtonStyleEditor

}   // namespace KitchenDesign.ToolsEditor

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2024-05-24
/// ===========================================================================
