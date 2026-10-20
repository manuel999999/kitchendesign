namespace Melcher
{
    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Custom Hierarchy tool that puts a small button on the right edge of the hierarchy window
    ///     that can be used to show/hide any <see cref="MelcherUI"/> class or derived class that has
    ///     a <see cref="MelcherUIExplode"/> component.
    ///
    ///     The icon works in an opposite state to the m_ExplodeInEditor value of the MelcherUIExplode
    ///     component, in that when it is lit up and active, it is visible in the game view (therefore
    ///     exploded=true on the component.
    /// </summary>

    [InitializeOnLoad]
    public class MelcherUIExplodeHierarchy
    {
        static Texture2D s_TextureShow;
        static Texture2D s_TextureHide;
        static List<MelcherUIExplode> s_ExplodeUIs = new List<MelcherUIExplode>();
        const string ExplodeHierarchyIconEnabled = "ExplodeHierarchyIconEnabled";

        // Initialize a static version of this class, getting the paths to the icons, and subscribing
        // to relevant Editor events to draw the hierarchy windows and update them.
        static MelcherUIExplodeHierarchy()
        {
            string codePath = MelcherCommon.GetCallerSourceCodePath();
            string combineWithIconPath = Path.Combine(codePath, "..", "..", "EditorIcons");
            string fullPath = Path.GetFullPath(combineWithIconPath);
            string relativePath = MelcherTransformFilePath.From(fullPath).To(MelcherPredefinedFilePath.project);
            string slashTrimmedPath = relativePath.Replace("\\", "/");
            string packagePath = slashTrimmedPath.Replace("Library/PackageCache", "Packages");
            string finalPath = Regex.Replace(packagePath, @"@[^\\/]*(?=[\\/])", "");

            s_TextureShow = AssetDatabase.LoadAssetAtPath<Texture2D>(finalPath + "/explode_ui_icon_show.png");
            s_TextureHide = AssetDatabase.LoadAssetAtPath<Texture2D>(finalPath + "/explode_ui_icon_hide.png");

            EditorApplication.hierarchyChanged -= OnHierarchyChange;
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HandleHierarchyWindow;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HandleHierarchyWindow;
        }

        private static void HandleHierarchyWindow(EntityId instanceID, Rect selectionRect)
        {
            if (!EditorPrefs.GetBool(ExplodeHierarchyIconEnabled, true))
            {
                return;
            }
            if (s_TextureShow == null || s_TextureHide == null)
            {
                return;
            }

            Rect rect = new Rect(selectionRect);
            rect.x = rect.width + 48;
            rect.width = 18;

            GUIContent guiContent = new GUIContent();
            guiContent.tooltip = "Toggles this MelcherUI on/off in the Game View using MelcherUIExplode to move it out of the way, instead of toggling active state.";

            GUIStyle guiStyle = new GUIStyle();
            guiStyle.stretchHeight = true;
            guiStyle.stretchWidth = true;

            MelcherUIExplode singleExplodingUi = null;
            foreach (MelcherUIExplode melcherUI in s_ExplodeUIs)
            {
                if (melcherUI == null)
                {
                    continue;
                }
                if (melcherUI.gameObject.GetEntityId().Equals(instanceID))
                {
                    singleExplodingUi = melcherUI;
                    break;
                }
            }

            if (singleExplodingUi == null)
            {
                return;
            }

            if (singleExplodingUi.isExploded)
            {
                guiContent.image = s_TextureHide;
            }
            else
            {
                guiContent.image = s_TextureShow;
            }

            if (GUI.Button(rect, guiContent, guiStyle))
            {
                bool isContainedInSelection = false;

                if (Selection.count <= 0)
                {
                    SetSingleExplodingUI();
                    return;
                }

                foreach (object obj in Selection.objects)
                {
                    GameObject gObj = obj as GameObject;

                    if (gObj == null)
                    {
                        break;
                    }

                    if (gObj.TryGetComponent(out MelcherUIExplode groupedExplodingUi))
                    {
                        if (groupedExplodingUi == singleExplodingUi)
                        {
                            isContainedInSelection = true;
                            break;
                        }
                    }
                }

                foreach (object obj in Selection.objects)
                {
                    if (!isContainedInSelection)
                    {
                        break;
                    }

                    GameObject gObj = obj as GameObject;

                    if (gObj == null)
                    {
                        break;
                    }

                    if (gObj.TryGetComponent(out MelcherUIExplode groupedExplodingUi))
                    {
                        if (groupedExplodingUi == singleExplodingUi)
                        {
                            continue;
                        }

                        groupedExplodingUi.isExploded = !singleExplodingUi.isExploded;
                        groupedExplodingUi.Validate();
                        EditorUtility.SetDirty(groupedExplodingUi);
                    }
                }

                SetSingleExplodingUI();

                void SetSingleExplodingUI()
                {
                    singleExplodingUi.isExploded = !singleExplodingUi.isExploded;
                    singleExplodingUi.Validate();
                    EditorUtility.SetDirty(singleExplodingUi);
                }
            }
        }

        static void OnHierarchyChange()
        {
            GameObject[] go = MelcherUnityUtility.FindObjectsByType<GameObject>(MelcherUnityUtility.FindObjectsSortMode.None);

            s_ExplodeUIs.Clear();

            foreach (GameObject obj in go)
            {
                if (obj.TryGetComponent(out MelcherUIExplode exploder))
                {
                    s_ExplodeUIs.Add(exploder);
                }
            }
        }

        [MenuItem("Show MelcherUIExplode Icons", menuItem = "Tools/Melcher Studios/Show MelcherUIExplode Icons")]
        static void OnToggleActive()
        {
            bool enabled = !EditorPrefs.GetBool(ExplodeHierarchyIconEnabled);
            EditorPrefs.SetBool(ExplodeHierarchyIconEnabled, enabled);
        }

    }  // class MelcherUIExplodeEditor

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender (jbender.ie@me.com)
/// Initial creation date: 2024-04-18
/// ===========================================================================
