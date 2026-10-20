namespace Melcher
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Adds option to anchor RectTransforms to RectTransform component's context
    /// menu. Anchoring a RectTransform will move it's anchor points to match the
    /// UI object's actual edges.
    ///
    /// Based on this implementation:
    /// http://answers.unity.com/answers/1149446/view.html
    /// </summary>
    [InitializeOnLoad]
    public class MelcherAnchorToolsEditor : EditorWindow
    {
        [MenuItem("/CONTEXT/RectTransform/Pin RectTransform Corners", priority = 1020)]
        private static void ObjectAnchor(MenuCommand command)
        {
            Anchor(false, Selection.objects);
        }

        [MenuItem("/CONTEXT/RectTransform/Pin RectTransform Center", priority = 1020)]
        private static void PinObjectCenter()
        {
            Anchor(true, Selection.objects);
        }

        public static void Anchor(bool toCenter, params UnityEngine.Object[] targets)
        {
            if (targets == null)
            {
                throw new ArgumentNullException("targets");
            }

            int count = 0;
            RectTransform[] rectTransforms = new RectTransform[targets.Length];
            foreach (var obj in targets)
            {
                RectTransform rectTransform = obj as RectTransform;
                if (!rectTransform && obj is GameObject)
                {
                    rectTransform = ((GameObject)obj).GetComponent<RectTransform>();
                }

                if (rectTransform)
                {
                    rectTransforms[count] = rectTransform;
                    ++count;
                }
            }

            Undo.RecordObjects(rectTransforms, "Anchor RectTransform");
            foreach (RectTransform rectTransform in rectTransforms)
            {
                AnchorsToCorners(rectTransform);

                if (toCenter)
                {
                    Vector2 halfway = (rectTransform.anchorMin + rectTransform.anchorMax) * 0.5f;

                    Rect rect = rectTransform.rect;

                    rectTransform.anchorMin = halfway;
                    rectTransform.anchorMax = halfway;

                    Vector2 halfRect = new Vector2(rect.width, rect.height) * 0.5f;
                    rectTransform.offsetMin = -halfRect;
                    rectTransform.offsetMax = halfRect;
                }
                EditorUtility.SetDirty(rectTransform);
            }
        }

        public static Vector2 Divide(Vector2 first, Vector2 second)
        {
            if (!(Mathf.Approximately(second.x, 0.0f) || Mathf.Approximately(second.y, 0.0f)))
            {
                first = new Vector2(first.x / second.x, first.y / second.y);
            }
            return first;
        }

        private static void AnchorsToCorners(RectTransform rectTransform)
        {
            Rect rect = rectTransform.rect;

            // Calculate parent rectangle
            Rect parentRect;
            RectTransform parentRectTransform = null;
            if (rectTransform.parent && (parentRectTransform = rectTransform.parent.GetComponent<RectTransform>()) != null)
            {
                parentRect = parentRectTransform.rect;
            }
            else
            {
                Resolution resolution = Screen.currentResolution;
                Vector2 screenSize = new Vector2(resolution.width, resolution.height);
                parentRect = new Rect(Vector2.zero, screenSize);
            }

            Vector2 actualMin = Vector2.Scale(rectTransform.anchorMin, parentRect.size) + rectTransform.offsetMin;
            Vector2 actualMax = Vector2.Scale(rectTransform.anchorMax, parentRect.size) + rectTransform.offsetMax;

            Vector2 localScale = rectTransform.localScale;
            Vector2 scaleCorrection = Vector2.Scale(rect.size, Vector2.one - localScale) * 0.5f;

            rectTransform.anchorMin = Divide(actualMin + scaleCorrection, parentRect.size);
            rectTransform.anchorMax = Divide(actualMax - scaleCorrection, parentRect.size);
            rectTransform.offsetMin = -scaleCorrection;
            rectTransform.offsetMax = scaleCorrection;
        }
    }   // class MelcherAnchorToolsEditor

}   // namespace Melcher
