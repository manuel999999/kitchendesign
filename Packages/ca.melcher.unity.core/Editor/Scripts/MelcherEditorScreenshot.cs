namespace Melcher
{
    using System;
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine.UI;

    /// <summary>
    /// Provides editor-time utilities for taking screenshots.
    /// </summary>
    public static class MelcherEditorScreenshotMenu
    {
        /// <summary>
        /// Automatically capture and name a screenshot for the game view.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot game view _F10")]
        public static void ScreenshotGameView()
        {
            ScreenshotGameView($"gameview_{Screen.width}x{Screen.height}");
        }

        public static void ScreenshotGameView(string screenshotName)
        {
            // Create output folder
            string screenshotFolderPath = GetScreenshotFolderPath();
            if (!Directory.Exists(screenshotFolderPath))
            {
                Directory.CreateDirectory(screenshotFolderPath);
            }

            // Get base file name
            string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffffff");
            if (!string.IsNullOrEmpty(screenshotName))
            {
                fileName = $"{screenshotName}_{fileName}";
            }

            string filePath = Path.Combine(screenshotFolderPath, fileName + ".png");
            filePath = MakeUniqueFilePath(filePath);
            CaptureGameView(filePath, verbose: true);
        }

        /// <summary>
        /// Automatically capture and name a screenshot for the game view.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot game view (with alpha) _F8")]
        public static void ScreenshotGameViewWithAlpha()
        {
            // Create output folder
            string screenshotFolderPath = GetScreenshotFolderPath();
            if (!Directory.Exists(screenshotFolderPath))
            {
                Directory.CreateDirectory(screenshotFolderPath);
            }

            Camera mainCamera = Camera.main;
            Selection.activeGameObject?.TryGetComponent(out mainCamera);

            // Get base file name
            int width = mainCamera.pixelWidth;
            int height = mainCamera.pixelHeight;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"gameview_{width}x{height}_{timestamp}";

            var filePath = Path.Combine(screenshotFolderPath, fileName + ".png");
            filePath = MakeUniqueFilePath(filePath);

            CaptureGameViewWithAlpha(mainCamera, filePath, verbose: true);
        }



        /// <summary>
        /// Automatically capture and name a screenshot for the scene view.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot game view for portrait app #F10")]
        public static void ScreenshotPortraitResolutions()
        {
            MelcherEditorCoroutine.StartEditorCoroutine(TakePortraits());

            IEnumerator TakePortraits()
            {
                // Between pictures, the canvases are forced to rebuild their
                // layout to be responsive to the new screen size
                Canvas[] canvases = MelcherUnityUtility.FindObjectsByType<Canvas>(MelcherUnityUtility.FindObjectsSortMode.None);

                void MarkAllLayoutsForRebuild()
                {
                    foreach (Canvas canvas in canvases)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                    }
                }

                int gameViewIndexToRestore = MelcherGameViewUtility.GetCurrentViewSizeIndex();

                // Restore the original time scale once the screenshots are taken
                using var restoreTimeScale = MelcherDeferred.Defer(
                    toCapture: Time.timeScale,
                    onDispose: originalTimeScale => Time.timeScale = originalTimeScale
                );

                // Freeze the game so all the pictures have the exact same
                // content
                // TODO: The TimeScale value should be managed by a class to
                //       ensure it can be modified by multiple scripts without
                //       stomping over their changes
                Time.timeScale = 0.0f;

                string screenSizeName = "Android 4K Portrait";
                MelcherGameViewUtility.UseSize(width: 2160, height: 3840, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "Android 1080p Portrait";
                MelcherGameViewUtility.UseSize(width: 1080, height: 1920, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPad 13 Portrait";
                MelcherGameViewUtility.UseSize(width: 2048, height: 2732, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPhone 6.9 Portrait";
                MelcherGameViewUtility.UseSize(width: 1290, height: 2796, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPhone 6.5 Portrait";
                MelcherGameViewUtility.UseSize(width: 1242, height: 2688, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;
            }
        }

        /// <summary>
        /// Automatically capture and name a screenshot for the scene view.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot game view for landscape app")]
        public static void ScreenshotLandscapeResolutions()
        {
            MelcherEditorCoroutine.StartEditorCoroutine(TakeLandscapes());

            IEnumerator TakeLandscapes()
            {
                // Between pictures, the canvases are forced to rebuild their
                // layout to be responsive to the new screen size
                Canvas[] canvases = MelcherUnityUtility.FindObjectsByType<Canvas>(MelcherUnityUtility.FindObjectsSortMode.None);

                void MarkAllLayoutsForRebuild()
                {
                    foreach (Canvas canvas in canvases)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                    }
                }

                int gameViewIndexToRestore = MelcherGameViewUtility.GetCurrentViewSizeIndex();

                // Restore the original time scale once the screenshots are taken
                using var restoreTimeScale = MelcherDeferred.Defer(
                    toCapture: Time.timeScale,
                    onDispose: originalTimeScale => Time.timeScale = originalTimeScale
                );

                // Freeze the game so all the pictures have the exact same
                // content
                // TODO: The TimeScale value should be managed by a class to
                //       ensure it can be modified by multiple scripts without
                //       stomping over their changes
                Time.timeScale = 0.0f;

                string screenSizeName = "Android 4K Landscape";
                MelcherGameViewUtility.UseSize(width: 3840, height: 2160, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "Android 1080p Landscape";
                MelcherGameViewUtility.UseSize(width: 1920, height: 1080, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPad 13 Landscape";
                MelcherGameViewUtility.UseSize(width: 2732, height: 2048, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPhone 6.9 Landscape";
                MelcherGameViewUtility.UseSize(width: 2796, height: 1290, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;

                screenSizeName = "iOS iPhone 6.5 Landscape";
                MelcherGameViewUtility.UseSize(width: 2688, height: 1242, createIfMissingName: screenSizeName);
                MarkAllLayoutsForRebuild();
                yield return null;
                ScreenshotGameView(screenSizeName);
                yield return null;
            }
        }


        /// <summary>
        /// Capture the game window.
        /// </summary>
        public static void CaptureGameView(string filePath, bool verbose)
        {
            // Focus the game window. This is necessary for ScreenCapture to work
            var assembly = typeof(EditorWindow).Assembly;
            var gameViewType = assembly.GetType("UnityEditor.GameView");
            var gameViewWindow = EditorWindow.GetWindow(gameViewType);
            gameViewWindow.Show();
            gameViewWindow.Focus();

            // No delays are needed to avoid capturing menu UI with ScreenCapture
            ScreenCapture.CaptureScreenshot(filePath);
            if (verbose)
            {
                MelcherLog.Log("Captured screenshot " + filePath);
            }
        }

        public static void CaptureGameViewWithAlpha(Camera camera, string filePath, bool verbose)
        {
            var rt = RenderTexture.GetTemporary(
                width: camera.pixelWidth,
                height: camera.pixelHeight,
                depthBuffer: 32
            );
            try
            {
                var oldTargetTexture = camera.targetTexture;
                camera.targetTexture = rt;
                try
                {
                    camera.enabled = false;
                    camera.Render();

                    var oldActiveRt = RenderTexture.active;
                    RenderTexture.active = rt;
                    try
                    {
                        var screenshot = new Texture2D(
                            rt.width,
                            rt.height,
                            TextureFormat.ARGB32,
                            mipChain: false
                        );
                        try
                        {
                            screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                            screenshot.Apply();

                            var bytes = screenshot.EncodeToPNG();
                            File.WriteAllBytes(filePath, bytes);
                            if (verbose)
                            {
                                MelcherLog.Log("Captured screenshot " + filePath);
                            }
                        }
                        finally
                        {
                            GameObject.DestroyImmediate(screenshot);
                        }
                    }
                    finally
                    {
                        RenderTexture.active = oldActiveRt;
                        camera.enabled = true;
                    }
                }
                finally
                {
                    camera.targetTexture = oldTargetTexture;
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// Automatically capture and name a screenshot for the scene view.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot scene view _F11")]
        public static void ScreenshotSceneView()
        {
            void DelayScreenshot()
            {
                // Create output folder
                var screenshotFolderPath = GetScreenshotFolderPath();
                if (!Directory.Exists(screenshotFolderPath))
                {
                    Directory.CreateDirectory(screenshotFolderPath);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filePath = Path.Combine(screenshotFolderPath, $"sceneview_{timestamp}.png");
                filePath = MakeUniqueFilePath(filePath);
                CaptureSceneView(filePath, verbose: true);
            }

            // Delay the screenshot by 1 frame to allow the menu UI to fade
            EditorApplication.delayCall += DelayScreenshot;
        }

        /// <summary>
        /// Screenshot the scene view. It will try to crop to just the camera
        /// element but will fallback to the entire window otherwise.
        /// </summary>
        public static void CaptureSceneView(string filePath, bool verbose)
        {
            var sceneView = SceneView.lastActiveSceneView;
            var cameraElement = FindElementByName(sceneView.rootVisualElement, "unity-scene-view-camera-rect");

            if (cameraElement != null)
            {
                // Screenshot just the camera element
                var bounds = cameraElement.worldBound;
                var width = Mathf.FloorToInt(bounds.width);
                var height = Mathf.FloorToInt(bounds.height);
                CaptureEditor(
                    filePath,
                    sceneView.position.position + bounds.position,
                    width,
                    height,
                    verbose
                );
            }
            else
            {
                // Screenshot the whole scene view window (including tabs and toolbar)
                CaptureWindow(sceneView, filePath, verbose);
            }
        }

        /// <summary>
        /// Automatically capture and name a screenshot for the focused window.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Screenshot focused window _F12")]
        public static void ScreenshotFocusedWindow()
        {
            void DelayScreenshot()
            {
                // Create output folder
                var screenshotFolderPath = GetScreenshotFolderPath();
                if (!Directory.Exists(screenshotFolderPath))
                {
                    Directory.CreateDirectory(screenshotFolderPath);
                }

                EditorWindow activeWindow = EditorWindow.focusedWindow;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string filePath = Path.Combine(screenshotFolderPath, $"{activeWindow.GetType().Name}_{timestamp}.png");
                filePath = MakeUniqueFilePath(filePath);

                CaptureWindow(activeWindow, filePath, verbose: true);
            }

            // Delay the screenshot by 1 frame to allow the menu UI to fade
            EditorApplication.delayCall += DelayScreenshot;
        }

        /// <summary>
        /// Capture a specific editor window.
        /// </summary>
        public static void CaptureWindow(EditorWindow window, string filePath, bool verbose)
        {
            window.Focus();

            // Get screen position and sizes
            var topLeftCorner = window.position.position;
            var width = Mathf.FloorToInt(window.position.width);
            var height = Mathf.FloorToInt(window.position.height);

            CaptureEditor(filePath, topLeftCorner, width, height, verbose);
        }

        /// <summary>
        /// Lowest level function used to capture a rectangular region of the
        /// editor window.
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="topLeftCorner">Top-left corner of capture</param>
        /// <param name="width">Width of capture</param>
        /// <param name="height">Height of capture</param>
        /// <param name="verbose">When true, a message is logged with the
        ///     output path.
        /// </param>
        public static void CaptureEditor(string filePath, Vector2 topLeftCorner, int width, int height, bool verbose)
        {
            byte[] bytes = null;

            var colors = InternalEditorUtility.ReadScreenPixel(topLeftCorner, width, height);
            var texture = new Texture2D(width, height);
            try
            {
                texture.SetPixels(colors);

                if (filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    bytes = texture.EncodeToPNG();
                }
                else if (filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    bytes = texture.EncodeToJPG();
                }
                else if (filePath.EndsWith(".tga", StringComparison.OrdinalIgnoreCase))
                {
                    bytes = texture.EncodeToTGA();
                }
                else
                {
                    MelcherLog.LogError(
                        tag: nameof(MelcherEditorScreenshotMenu),
                        message: $"Unhandled format: {Path.GetExtension(filePath)}"
                    );
                    return;
                }
            }
            finally
            {
                // Clean up unneeded resources
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            File.WriteAllBytes(filePath, bytes);
            if (verbose)
            {
                MelcherLog.Log("Captured screenshot " + filePath);
            }
        }

        /// <summary>
        /// Append an increasing set of numbers to the end of a file path
        /// until a path is found that doesn't exist. This function will first
        /// check and return the input path if it doesn't exist.
        /// </summary>
        public static string MakeUniqueFilePath(string filePath, int digitCount = 2)
        {
            // Try appending an increasing number to the path until it does not exist
            if (File.Exists(filePath))
            {
                int attempt = 0;

                string folderPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileName(filePath);
                string fileExt = Path.GetExtension(fileName);
                string newFilePath;

                do
                {
                    ++attempt;
                    string paddedNumber = attempt.ToString().PadLeft(digitCount, '0');
                    newFilePath = Path.Combine(folderPath, fileName + $"_{paddedNumber}{fileExt}");
                } while (File.Exists(newFilePath));

                filePath = newFilePath;
            }

            return filePath;
        }


        public static string GetScreenshotFolderPath()
        {
            return MelcherTransformFilePath.From("Screenshots", MelcherPredefinedFilePath.project)
                .ToString();
        }

        /// <summary>
        /// Recursively search a VisualElement's children for the first one
        /// matching <paramref name="name"/>. Returns null if none found.
        /// </summary>
        private static VisualElement FindElementByName(VisualElement root, string name)
        {
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            foreach (var child in root.Children())
            {
                var element = FindElementByName(child, name);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

    }   // class MelcherEditorScreenshot

}   // namespace Melcher

