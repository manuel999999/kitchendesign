namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public static class MelcherUnityExtensions
    {
        // ====================================================================
        // Color
        // ====================================================================

        public static Color WithAlpha(this Color color, float alpha)
        {
            Color copy = color;
            copy.a = alpha;
            return copy;
        }

        // ====================================================================
        // Quaternion
        // ====================================================================

        public static void ToSwingTwist(this Quaternion rotation, Vector3 axis, out float swing, out float twist)
        {
            rotation.ToAngleAxis(out swing, out Vector3 rotationAxis);
            Vector3 projection = Vector3.Project(rotationAxis, axis);
            //Vector3.SignedAngle(axis, projection);
            twist = projection.magnitude * 180.0f;
        }

        // ====================================================================
        // GameObject
        // ====================================================================

        public static ComponentType GetOrAddComponent<ComponentType>(this GameObject gameObject)
            where ComponentType : Component
        {
            if (!gameObject.TryGetComponent(out ComponentType component))
            {
                component = gameObject.AddComponent<ComponentType>();
            }

            return component;
        }

        // ====================================================================
        // Transform
        // ====================================================================

        public static void SetLayerRecursively(this Transform target, int layer, bool includeSelf)
        {
            if (includeSelf)
            {
                target.gameObject.layer = layer;
            }

            foreach (Transform child in target)
            {
                SetLayerRecursively(child, layer, includeSelf: true);
                child.gameObject.layer = layer;
            }
        }

        // ====================================================================
        // RectTransform
        // ====================================================================

        /// <summary>
        /// Search up the transform hierarchy for the first active
        /// <see cref="CanvasScaler"/>.
        /// </summary>
        public static CanvasScaler FindCanvasScaler(RectTransform rectTransform)
        {
            return MelcherSearch.FirstFromTransformUpward<CanvasScaler>(rectTransform, includeInactiveComponents: false);
        }

        public static float GetCanvasScaleFactor(this RectTransform rectTransform)
        {
            if (rectTransform is null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            CanvasScaler canvasScaler = FindCanvasScaler(rectTransform);
            return GetCanvasScaleFactor(canvasScaler);
        }

        public static float GetCanvasScaleFactor(CanvasScaler canvasScaler)
        {
            float scale = 1.0f;

            if (canvasScaler)
            {
                switch (canvasScaler.uiScaleMode)
                {
                    case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                        Vector2 referenceSize = canvasScaler.referenceResolution;
                        float widthHeightRatio = canvasScaler.matchWidthOrHeight;
                        scale = (Screen.width / referenceSize.x) * (1.0f - widthHeightRatio) +
                            (Screen.height / referenceSize.y) * widthHeightRatio;
                        break;

                    case CanvasScaler.ScaleMode.ConstantPhysicalSize:
                    case CanvasScaler.ScaleMode.ConstantPixelSize:
                    default:
                        scale = canvasScaler.transform.localScale.x;
                        break;
                }
            }

            return scale;
        }

        /// <summary>
        /// Calculate the rect relative to its parent's bottom-left corner.
        ///
        /// This differs from <see cref="RectTransform.rect" /> because the
        /// returned rect's position is relative to its parent's bottom-left
        /// corner (instead of its own pivot).
        /// </summary>
        public static Rect GetRectInParentSpace(this RectTransform rectTransform)
        {
            if (rectTransform is null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            // Rect in pivot space
            Rect rect = rectTransform.rect;

            // Offset of child's bottom-left corner from its pivot
            Vector2 offset = rectTransform.offsetMin + Vector2.Scale(rectTransform.pivot, rect.size);

            // Offset of child's bottom-left corner from its parent's
            // bottom-left corner
            RectTransform parent = rectTransform.parent as RectTransform;
            if (parent)
            {
                offset += Vector2.Scale(rectTransform.anchorMin, parent.rect.size);
            }

            // Convert from pivot space to parent space
            rect.position += offset;

            return rect;
        }

        /// <summary>
        /// Calculate the rect relative to its canvas' bottom-left corner.
        ///
        /// This can be thought of being similar to a world position.
        /// </summary>
        public static Rect GetRectInCanvasSpace(this RectTransform rectTransform)
        {
            if (rectTransform is null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            Rect rect = GetRectInParentSpace(rectTransform);

            // Iterate over the parents (accumulating their offsets) until a
            // canvas or null is reached
            RectTransform parent = rectTransform.parent as RectTransform;
            while (parent && !parent.TryGetComponent(out Canvas _))
            {
                // Include the offset of each parent
                rect.position += parent.offsetMin;

                Vector2 anchorMin = parent.anchorMin;
                parent = parent.parent as RectTransform;

                // Offset of last bottom-left corner from its parent's
                // bottom-left corner
                if (parent)
                {
                    rect.position += Vector2.Scale(anchorMin, parent.rect.size);
                }
            }

            return rect;
        }

        /// <summary>
        /// Assign a RectTransform's rect using a rect in its parent's space.
        /// </summary>
        public static void SetRectFromParentSpaceRect(this RectTransform rectTransform, Rect parentSpaceRect)
        {
            if (rectTransform is null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            Vector2 position = parentSpaceRect.position;

            position -= rectTransform.offsetMin;

            Vector2 parentSize = default;
            RectTransform parentTransform = rectTransform.parent as RectTransform;
            if (parentTransform)
            {
                parentSize = parentTransform.rect.size;
                position -= Vector2.Scale(rectTransform.anchorMin, parentSize);
            }

            rectTransform.offsetMin += position;

            Vector2 anchorMaxPos = Vector2.Scale(rectTransform.anchorMax, parentSize);
            Vector2 offsetToMax = anchorMaxPos - parentSpaceRect.max;
            rectTransform.offsetMax = -offsetToMax;
        }

        /// <summary>
        /// Assign a RectTransform's rect using a rect in its canvas' space.
        /// </summary>
        public static void SetRectFromCanvasSpaceRect(this RectTransform rectTransform, Rect canvasSpaceRect)
        {
            if (rectTransform is null)
            {
                throw new ArgumentNullException(nameof(rectTransform));
            }

            Vector2 positionOffset = Vector2.zero;

            RectTransform currentTransform = rectTransform;
            while (currentTransform && !currentTransform.TryGetComponent(out Canvas canvas))
            {
                positionOffset -= currentTransform.offsetMin;
                Vector2 anchorMin = currentTransform.anchorMin;
                currentTransform = currentTransform.parent as RectTransform;
                if (currentTransform)
                {
                    positionOffset -= Vector2.Scale(anchorMin, currentTransform.rect.size);
                }
            }

            Vector2 parentSize = default;
            RectTransform parentTransform = rectTransform.parent as RectTransform;
            if (parentTransform)
            {
                parentSize = parentTransform.rect.size;
            }

            rectTransform.offsetMin += (canvasSpaceRect.position + positionOffset);

            Vector2 topRight = rectTransform.offsetMin + canvasSpaceRect.size;
            Vector2 anchorDelta = Vector2.Scale(parentSize, rectTransform.anchorMax - rectTransform.anchorMin);
            rectTransform.offsetMax = topRight - anchorDelta;
        }


        // ====================================================================
        // RectOffset
        // ====================================================================

        public static RectOffset FromVector4(this RectOffset rectOffset, Vector4 vector)
        {
            rectOffset.left = Mathf.RoundToInt(vector.x);
            rectOffset.right = Mathf.RoundToInt(vector.y);
            rectOffset.top = Mathf.RoundToInt(vector.z);
            rectOffset.bottom = Mathf.RoundToInt(vector.w);
            return rectOffset;
        }

        public static Vector4 ToVector4(this RectOffset rectOffset)
        {
            return new Vector4(rectOffset.left, rectOffset.right, rectOffset.top, rectOffset.bottom);
        }

        // =====================================================================
        // ParticleSystem
        // =====================================================================

        // TODO: Is this even necessary? I tried to simplify this name to
        //       Evaluate but it turns out there is an Evaluate function on
        //       MinMaxCurve's. Does the built-in version use the curve's mode?
        public static float EvaluateMinMaxCurve(this ParticleSystem.MinMaxCurve c, float t)
        {
            float value = 0.0f;

            switch (c.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    value = c.constant;
                    break;

                case ParticleSystemCurveMode.TwoConstants:
                    value = UnityEngine.Random.Range(c.constantMin, c.constantMax);
                    break;

                case ParticleSystemCurveMode.Curve:
                    value = c.curve.Evaluate(t);
                    break;

                case ParticleSystemCurveMode.TwoCurves:
                    float min = c.curveMin.Evaluate(t);
                    float max = c.curveMax.Evaluate(t);
                    if (min > max)
                    {
                        MelcherCommon.Swap(ref min, ref max);
                    }
                    value = UnityEngine.Random.Range(min, max);
                    break;

                default:
                    MelcherLog.LogError("Unhandled mode: " + c.mode);
                    break;
            }

            return value;
        }

        // ====================================================================
        // Collections
        // ====================================================================

        /// <summary>
        /// Return a tuple containing the element of the original collection, and an index value.
        /// Examples of calling function:
        /// foreach (var myVar in MyList.Enumerate())
        /// foreach ((int index, GameObject element) in MyList.Enumrate())
        /// </summary>
        public static IEnumerable<(int index, T element)> Enumerate<T>(this IEnumerable<T> collection)
        {
            int index = -1;
            foreach (T element in collection)
            {
                index++;
                yield return (index, element);
            }
        }

    }   // class MelcherUnityExtensions

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2021-12-07
/// ===========================================================================
