namespace Melcher
{
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>
    ///     The purpose of this class is to focus the scene asset in the
    ///     inspector when the user selects a scene object in the hierarchy.
    ///     Unity's default behavior is to blank the inspector when the scene
    ///     object in the hierarchy is selected.
    /// </summary>
    public static class MelcherHierarchySceneSelect
    {
#if false
        [InitializeOnLoadMethod]
        public static void Init()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            MelcherLog.Breadcrumb($"selection changed is null = {Selection.activeObject == null}");

            if (Selection.activeObject == null &&
                TryGetTreeViewFromFocusedWindow(out object treeView))
            {
                MelcherLog.Breadcrumb("found tree view");

                // The user possibly selected a scene object in the hierarchy.

                //IList<int> selection = treeView.GetSelection();

                //if (selection != null &&
                //    selection.Count == 1 &&
                //    IsRootTreeItem(treeView, selection[0]))
                //{
                //    MelcherLog.Breadcrumb("root");
                //}

            }
        }

        private static bool IsRootTreeItem(object treeView, int itemId)
        {
            if (treeView == null)
            {
                return false;
            }

            FieldInfo dataSourceField =
                treeView.GetType().GetField(
                    "m_DataSource",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

            object dataSource = dataSourceField?.GetValue(treeView);

            if (dataSource == null)
            {
                return false;
            }

            MethodInfo getItemMethod =
                dataSource.GetType().GetMethod(
                    "FindItem",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

            TreeViewItem item =
                getItemMethod?.Invoke(dataSource, new object[] { itemId }) as TreeViewItem;

            return item != null && item.depth == -1;

        }

        private static bool TryGetTreeViewFromFocusedWindow(out object treeViewController)
        {
            treeViewController = null;

            EditorWindow focusedWindow = EditorWindow.focusedWindow;

            if (!focusedWindow)
            {
                MelcherLog.Breadcrumb("no focused window");
                return false;
            }

            MelcherLog.Breadcrumb($"focused window type is {focusedWindow.GetType().FullName}");

            if (focusedWindow.GetType() != SceneHierarchyWindowType())
            {
                MelcherLog.Breadcrumb("not the scene hierarchy window");
                return false;
            }

            PropertyInfo sceneHierarchyProperty =
                focusedWindow.GetType().GetProperty("sceneHierarchy");

            if (sceneHierarchyProperty == null)
            {
                MelcherLog.LogError("no sceneHierarchy property.");
                return false;
            }

            object sceneHierarchy =
                sceneHierarchyProperty.GetValue(focusedWindow);

            if (sceneHierarchy == null)
            {
                MelcherLog.LogError("no sceneHierarchy");
                return false;
            }

            MelcherLog.Breadcrumb($"sceneHierarchy type is {sceneHierarchy.GetType().FullName}");

            FieldInfo treeViewField =
                sceneHierarchy.GetType().GetField(
                    "m_TreeView",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

            if (treeViewField == null)
            {
                MelcherLog.LogError("no tree view field.");
                return false;
            }
            object t = treeViewField.GetValue(sceneHierarchy);

            MelcherLog.Log($"t is {(t != null ? t.GetType().FullName : null)}");

            treeViewController = t;

            if (treeViewController == null)
            {
                MelcherLog.LogError("no tree view controller.");
                return false;
            }

            MelcherLog.Log($"treeView is {(treeViewController != null ? treeViewController.GetType().FullName : null)}");

            //MelcherLog.Log($"item 0 is {treeViewController.itemsSource[0].GetType().FullName}");

            return treeViewController != null;
        }

        private static bool IsRootSceneItem(object treeView, int itemId)
        {
            if (treeView == null) return false;

            var dataSourceField = treeView.GetType().GetField("m_DataSource", BindingFlags.NonPublic | BindingFlags.Instance);
            var dataSource = dataSourceField?.GetValue(treeView);

            if (dataSource == null) return false;

            var getItemMethod = dataSource.GetType().GetMethod("FindItem", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var item = getItemMethod?.Invoke(dataSource, new object[] { itemId }) as TreeViewItem;

            return item != null && item.depth == -1; // Root items typically have depth -1
        }

        private static System.Type SceneHierarchyWindowType()
        {
            if (s_SceneHierarchyWindowType == null)
            {
                s_SceneHierarchyWindowType =
                    typeof(Editor).Assembly.GetType(
                        "UnityEditor.SceneHierarchyWindow",
                        throwOnError: true
                    );
            }

            return s_SceneHierarchyWindowType;
        }

        private static System.Type s_SceneHierarchyWindowType;
#endif
    }   // class MelcherHierarchySceneSelect

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2025-02-19
/// ===========================================================================
