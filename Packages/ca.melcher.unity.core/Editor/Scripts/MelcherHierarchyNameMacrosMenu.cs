namespace Melcher
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEditor;
    using UnityEditor.SceneManagement;

    /// <summary>
    /// This script adds a menu item that renames game objects in the hierarchy
    /// according to special tags.
    /// 
    /// The main purpose of this renaming behaviour is to help disambiguate game
    /// objects with the same name in different parts of the hierarchy. For
    /// example, consider different UI menus that have similarly named Next and
    /// Cancel buttons. With proper tags, the buttons can be renamed from
    /// their original name on the left to the names on the right:
    ///
    /// <code>
    ///     // BEFORE                   // AFTER
    ///     + OptionsUI #               + OptionsUI #
    ///         + Cancel ^                  + Cancel ^OptionsUI
    ///         + Next ^                    + Next ^OptionsUI
    ///     + SetupUI #                 + SetupUI #
    ///         + Cancel ^                  + Cancel ^SetupUI
    ///         + Untagged                  + Untagged
    ///         + Next ^                    + Next ^SetupUI
    /// </code>
    ///
    /// There are two tags:
    ///
    ///     #       This tag indicates the text after the # symbol will be
    ///             copied after any ^ symbols in its children. If no text is
    ///             follows the # symbol, the text before it will be used.
    ///
    ///     ^       This tag indicates where its parents that have a # symbol
    ///             will have their names appended.
    /// </summary>
    public static class MelcherHierarchyNameMacrosMenu
    {
        [MenuItem(MelcherEditorConst.Menu.ToolsPrefix + "Apply hierarchy name macros... %#h")]   // ctrl+shift+h
        private static void RenameHierarchy(MenuCommand context)
        {
            int editorSceneCount = EditorSceneManager.sceneCount;

            int dialogResult = MelcherEditorConst.DialogResult.Cancel;

            if (editorSceneCount > 1)
            {
                dialogResult =
                    EditorUtility.DisplayDialogComplex(
                        title: s_DialogPromptTitle,
                        message: s_DialogPromptMessage,
                        ok: s_DialogPromptOk,
                        cancel: "Cancel",
                        alt: $"Only: {EditorSceneManager.GetActiveScene().name}"
                    );
            }
            else
            {
                bool didAccept = 
                    EditorUtility.DisplayDialog(
                        title: s_DialogPromptTitle,
                        message: s_DialogPromptMessage,
                        ok: s_DialogPromptOk
                    );

                if (didAccept)
                {
                    dialogResult = MelcherEditorConst.DialogResult.OK;
                }
            }

            if (dialogResult == MelcherEditorConst.DialogResult.Cancel)
            {
                return;
            }

            List<GameObject> rootSceneObjects = new List<GameObject>();
            Stack<string> anchorNameStack = new Stack<string>();

            string tagDefinition    = Regex.Escape(new string(s_TagDefinitionChar, count: 1));
            string tagReference     = Regex.Escape(new string(s_TagReferenceChar, count: 1));

            Regex copyAnchorNamePattern     = new Regex($@"{tagDefinition}([^{tagReference}]*)");   // anything after a definition character except for a reference character
            Regex pasteAnchorNamePattern    = new Regex($@"{tagReference}([^{tagDefinition}]*)");   // anything after a reference character except for an definition character
            Regex fallbackAnchorNamePattern = new Regex($@"^[^{tagDefinition}{tagReference}]*");    // everything before the first tag character, starting from beginning of name

            if (dialogResult == MelcherEditorConst.DialogResult.OK)
            {
                // Process every scene
                for (int sceneIndex = 0; sceneIndex < editorSceneCount; ++sceneIndex)
                {
                    ProcessScene(EditorSceneManager.GetSceneAt(sceneIndex));
                }
            }
            else
            {
                ProcessScene(EditorSceneManager.GetActiveScene());

            }

            void ProcessScene(Scene scene)
            {
                scene.GetRootGameObjects(rootSceneObjects);

                foreach (GameObject root in rootSceneObjects)
                {
                    RecursivelyUpdateNames(root.transform);
                }
            }

            void RecursivelyUpdateNames(Transform current)
            {
                bool didPushName = false;

                // Setup a defer to automatically pop the pushed name.
                // This ensures we properly handle clean up if an exception
                // occurs
                using var autoPop = MelcherDeferred.Defer(
                    () =>
                    {
                        if (didPushName)
                        {
                            anchorNameStack.Pop();
                        }
                    }
                );

                string gameObjectName = current.name;

                // Apply the current anchor name
                {
                    // Build the whole chain of names that are on the stack.
                    // Reverse the stack so it's in first-to-last order. Ignore
                    // blank names
                    string currentAnchorName = string.Join(".", anchorNameStack.Reverse().Where(x => !string.IsNullOrWhiteSpace(x)));

                    gameObjectName =
                        pasteAnchorNamePattern.Replace(gameObjectName, x => s_TagReferenceChar + currentAnchorName);

                    // Update the name if it changed
                    if (gameObjectName != current.name)
                    {
                        current.name = gameObjectName;
                    }
                }

                // Look for a new anchor name
                {
                    Match match = copyAnchorNamePattern.Match(gameObjectName);
                    if (match != null && match.Success)
                    {
                        string newAnchorName = match.Groups[1].Value;

                        if (string.IsNullOrWhiteSpace(newAnchorName))
                        {
                            // If the anchor name is blank, attempt to use the
                            // first part of the game object name
                            match = fallbackAnchorNamePattern.Match(gameObjectName);
                            if (match != null && match.Success)
                            {
                                newAnchorName = match.Value.Trim();
                            }
                        }

                        anchorNameStack.Push(newAnchorName);
                        didPushName = true;
                    }
                }

                foreach (Transform child in current)
                {
                    RecursivelyUpdateNames(child);
                }
            }
        }

        private const char s_TagDefinitionChar = '#';
        private const char s_TagReferenceChar = '^';

        private const string s_DialogPromptTitle = "Rename game objects using macro tags?";

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly string s_DialogPromptMessage =
            $"This will rename game objects that have special patterns in their names ({s_TagReferenceChar}).\n" +
            $"\n" +
            $"(Example) Before:\n" +
            $" - The Root {s_TagDefinitionChar}Root\n" +
            $" - - The Child {s_TagReferenceChar}\n" +
            $"\n" +
            $"(Example) After:\n" +
            $" - The Root {s_TagDefinitionChar}Root\n" +
            $" - - The Child {s_TagReferenceChar}Root";

        private const string s_DialogPromptOk = "All scenes";

    }   // class MelcherHierarchyNameMacrosMenu

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-11-27
/// ===========================================================================
