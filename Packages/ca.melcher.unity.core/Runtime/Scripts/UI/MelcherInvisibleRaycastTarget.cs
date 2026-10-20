namespace Melcher
{
    using UnityEngine;

    // Original code by Unity Answers user, Statement:
    // http://answers.unity.com/answers/1157876/view.html
    // This saves us having to use Image components with 0 alpha which still
    // generate invisible triangles that pass through the render pipeline.

    /// <summary>
    /// Intercepts UI raycasts without needing to render anything. Useful for
    /// extending the hitbox of a UI control or as a blocker to prevent
    /// interaction with the controls beneath it.
    /// </summary>
    [UnityEngine.AddComponentMenu("UI/Invisible Raycast Target")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class MelcherInvisibleRaycastTarget : UnityEngine.UI.Graphic
    {
        public override void SetMaterialDirty()
        {
            // Do nothing
        }

        public override void SetVerticesDirty()
        {
            // Do nothing
        }

    }   // class MelcherInvisibleRaycastTarget

}   // namespace Melcher