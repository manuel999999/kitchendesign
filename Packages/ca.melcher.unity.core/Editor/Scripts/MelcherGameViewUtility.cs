namespace Melcher
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Exposes programmatic control of the Game View's resolution.
    /// This is a modified version of the script found here:
    /// https://answers.unity.com/questions/956123/add-and-select-game-view-resolution.html?childToView=1368989#comment-1368989
    /// </summary>
    public static class MelcherGameViewUtility
    {
        public static bool UseSize(int width, int height, GameViewSizeMode viewSizeMode = GameViewSizeMode.FixedResolution, string createIfMissingName = null)
        {
            int viewSizeIndex = IndexOfViewSize(width, height);

            if (viewSizeIndex < 0 && createIfMissingName != null)
            {
                RuntimePlatform targetPlatform = MelcherUnityUtility.targetPlatform;

                GameViewSizeGroupType viewSizesGroupType = PlatformToViewSizeGroupType(targetPlatform);

                viewSizeIndex = AddCustomSize(viewSizesGroupType, viewSizeMode, width, height, createIfMissingName);
            }

            if (viewSizeIndex >= 0)
            {
                UseSize(viewSizeIndex);
            }

            return viewSizeIndex >= 0;
        }

        public static void UseSize(int viewSizeIndex)
        {
            Type gameViewType = GetEditorType("GameView");

            PropertyInfo selectedSizeIndexProp =
                gameViewType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            selectedSizeIndexProp.SetValue(gameView, viewSizeIndex, null);

            // Update the Game View's zoom setting
            MethodInfo sizeSelectionCallback =
                gameViewType.GetMethod(
                    "SizeSelectionCallback",
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                 );
            sizeSelectionCallback.Invoke(gameView, new object[] { viewSizeIndex, null });
        }

        public static int AddCustomSize(GameViewSizeGroupType sizeGroupType, GameViewSizeMode viewSizeMode, int width, int height, string name)
        {
            object platformViewSizeGroup = GetViewSizeGroupForPlatform(sizeGroupType);

            // or group.GetType().
            MethodInfo addCustomSize = s_GetGroup.ReturnType.GetMethod("AddCustomSize");

            Type gameViewSizeType = GetEditorType("GameViewSize");

            ConstructorInfo gameViewSizeTypeConstructor =
                gameViewSizeType.GetConstructor(
                    new Type[] {
                        GetEditorType("GameViewSizeType"),  // mode
                        typeof(int),    // width
                        typeof(int),    // height
                        typeof(string)  // name
                    }
                );

            //Type newGameViewSizeTypeType = null;//= GetEditorType("GameViewSizeType");

            //if (viewSizeMode == GameViewSizeMode.AspectRatio)
            //{
            //    newGameViewSizeTypeType = GetEditorType("GameViewSizeType.AspectRatio");
            //}
            //else
            //{
            //    newGameViewSizeTypeType = GetEditorType("GameViewSizeType.FixedResolution");
            //}

            object newSize =
                gameViewSizeTypeConstructor.Invoke(
                    new object[]
                    {
                        (int)viewSizeMode,
                        width,
                        height,
                        name
                    }
                );

            addCustomSize.Invoke(platformViewSizeGroup, new object[] { newSize });

            int totalViewSizeCount = GetEntryCountInViewSizeGroup(platformViewSizeGroup);
            return totalViewSizeCount - 1;
        }

        public static int IndexOfViewSize(string name)
        {
            RuntimePlatform targetPlatform = MelcherUnityUtility.targetPlatform;

            GameViewSizeGroupType viewSizesGroupType = PlatformToViewSizeGroupType(targetPlatform);

            return IndexOfViewSize(viewSizesGroupType, name);
        }

        public static int IndexOfViewSize(GameViewSizeGroupType viewSizesGroupType, string name)
        {
            var platformViewSizeGroup = GetViewSizeGroupForPlatform(viewSizesGroupType);
            var getDisplayTexts = platformViewSizeGroup.GetType().GetMethod("GetDisplayTexts");
            var displayTexts = (string[])getDisplayTexts.Invoke(platformViewSizeGroup, null);

            for (int i = 0; i < displayTexts.Length; i++)
            {
                string display = displayTexts[i];

                // the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
                // so if we're querying a custom size text we substring to only get the name
                // You could see the outputs by just logging
                // Debug.Log(display);
                int pren = display.IndexOf('(');
                if (pren != -1)
                {
                    display = display.Substring(0, pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
                }

                if (display == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOfViewSize(int width, int height)
        {
            RuntimePlatform targetPlatform = MelcherUnityUtility.targetPlatform;

            GameViewSizeGroupType viewSizesGroupType = PlatformToViewSizeGroupType(targetPlatform);

            return IndexOfViewSize(viewSizesGroupType, width, height);
        }

        public static int IndexOfViewSize(GameViewSizeGroupType viewSizesGroupType, int width, int height)
        {
            object platformViewSizeGroup = GetViewSizeGroupForPlatform(viewSizesGroupType);

            int totalViewSizeCount = GetEntryCountInViewSizeGroup(platformViewSizeGroup);

            Type groupType = platformViewSizeGroup.GetType();
            MethodInfo getGameViewSize = groupType.GetMethod("GetGameViewSize");
            Type gameViewSizeType = getGameViewSize.ReturnType;
            PropertyInfo widthProp = gameViewSizeType.GetProperty("width");
            PropertyInfo heightProp = gameViewSizeType.GetProperty("height");

            object[] getGameViewSizeArguments = new object[1];
            ref object viewIndexArgument = ref getGameViewSizeArguments[0];

            for (int i = 0; i < totalViewSizeCount; i++)
            {
                viewIndexArgument = i;

                object size = getGameViewSize.Invoke(platformViewSizeGroup, getGameViewSizeArguments);

                int sizeWidth = (int)widthProp.GetValue(size, null);
                int sizeHeight = (int)heightProp.GetValue(size, null);

                if (sizeWidth == width && sizeHeight == height)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int GetCurrentViewSizeIndex()
        {
            Type gameViewType = GetEditorType("GameView");

            PropertyInfo selectedSizeIndexProp =
                gameViewType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            return (int)selectedSizeIndexProp.GetValue(gameView);
        }

        private static int GetEntryCountInViewSizeGroup(object viewSizeGroup)
        {
            Type groupType = viewSizeGroup.GetType();

            MethodInfo getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
            MethodInfo getCustomCount = groupType.GetMethod("GetCustomCount");

            int builtInViewCount = (int)getBuiltinCount.Invoke(viewSizeGroup, null);
            int customViewCount = (int)getCustomCount.Invoke(viewSizeGroup, null);

            return builtInViewCount + customViewCount;
        }

        static object GetViewSizeGroupForPlatform(GameViewSizeGroupType platform)
        {
            return s_GetGroup.Invoke(s_GameViewSizesInstance, new object[] { (int)platform });
        }

        static GameViewSizeGroupType PlatformToViewSizeGroupType(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return GameViewSizeGroupType.Android;

                case RuntimePlatform.IPhonePlayer:
                    return GameViewSizeGroupType.iOS;

                case RuntimePlatform.WebGLPlayer:
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.OSXPlayer:
                    return GameViewSizeGroupType.Standalone;
            }

            throw new NotImplementedException($"Unhandled platform: {platform}");
        }

        private static Type GetEditorType(string typeName)
        {
            string qualifiedType = $"UnityEditor.{typeName}";
            Type type = typeof(Editor).Assembly.GetType(qualifiedType);
            MelcherLog.Assert(type != null, $"Unknown type: {qualifiedType}");
            return type;
        }

        static MelcherGameViewUtility()
        {
            Type sizesType = GetEditorType("GameViewSizes");
            s_GetGroup = sizesType.GetMethod("GetGroup");

            Type singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            PropertyInfo instanceProp = singleType.GetProperty("instance");
            s_GameViewSizesInstance = instanceProp.GetValue(null, null);
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static object s_GameViewSizesInstance;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static MethodInfo s_GetGroup;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum GameViewSizeMode
        {
            AspectRatio,

            FixedResolution

        }   // enum GameViewSizeMode


    }   // class MelcherGameViewUtils

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-16
/// ===========================================================================
