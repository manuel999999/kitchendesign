namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Editor-time script. Helps create an "exploded view" of UI's so
    ///     they're not all stacked.
    /// </summary>
    public class MelcherUIExplode : MelcherMonoBehaviour
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public bool isExploded { get => m_ExplodeInEditor; set => m_ExplodeInEditor = value; }
        
        // =====================================================================
        // Methods
        // =====================================================================

        public void SetExploded(bool explode)
        {
            Vector2 anchorMin;
            Vector2 anchorMax;

            if (explode)
            {
                anchorMin = new Vector2(m_GridCoord.x, m_GridCoord.y);
                anchorMax = new Vector2(m_GridCoord.x + 1, m_GridCoord.y + 1);
            }
            else
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
            }

            const float Tolerance = 0.001f;
            if (!Application.isPlaying &&
                MelcherCommon.IsAlmostEqual(m_RectTransform.anchorMin, anchorMin, Tolerance) &&
                MelcherCommon.IsAlmostEqual(m_RectTransform.anchorMax, anchorMax, Tolerance))
            {
                // Avoid re-dirtying the scene because the requested values are
                // so similar to the current values.
                return;
            }

            m_RectTransform.anchorMin = anchorMin;
            m_RectTransform.anchorMax = anchorMax;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(m_RectTransform);
#endif  // if UNITY_EDITOR
        }

#if UNITY_EDITOR

        /// <summary>
        ///     Explode or un-explode according to the <see
        ///     cref="m_ExplodeInEditor"/> field.
        /// </summary>
        private void RefreshInEditor()
        {
            // Change the UI's exploded-ness, but only during edit mode and
            // while the UI object exists. The second check is necessary
            // because this function may be delayed (e.g., OnValidate() uses
            // UnityEditor.delayCall) and the scene and UI may be unloading.
            if (!Application.isPlaying && this)
            {
                SetExploded(m_ExplodeInEditor);
            }
        }

        private void OnTransformParentChanged()
        {
            RefreshInEditor();
        }

        protected override void OnValidate()
        {
            // Do the default MelcherMonoBehaviour validation first
            base.OnValidate();

            if (!m_RectTransform)
            {
                return;
            }

            // Don't explode UI's inside prefab stages. This avoids overwriting
            // the prefabs anchors.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) ||
                MelcherEditorUtility.IsPartOfCurrentPrefabStage(gameObject))
            {
                return;
            }

            if (m_RectTransform == m_RectTransform.root)
            {
                MelcherLog.LogWarning($"{nameof(MelcherUIExplode)} does not work on a root-level game object.", this);
                return;
            }

            // Don't refresh the UI when the game is (or will be) running. This
            // fixes an issue where the UI would un-explode/anchor correctly
            // upon starting playmode but then, immediately upon the next frame,
            // explode back.
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode ||
                UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += RefreshInEditor;
        }

        public void Validate()
        {
            OnValidate();
        }
#endif  // if UNITY_EDITOR

        // =====================================================================
        // Inspector-serialized members
        // =====================================================================

        [Tooltip("When true, the UI will be exploded to it's grid coordinate " +
            "position in the editor. Disable this field to move the UI " +
            "back within frame of the game view.")]
        [SerializeField]
        protected bool m_ExplodeInEditor = true;

        [SerializeField]
        protected Vector2Int m_GridCoord = Vector2Int.zero;

        // =====================================================================
        // OnValidate-serialized members
        // =====================================================================

        [Space]

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected RectTransform m_RectTransform = null;

    }   // class MelcherUIExplode

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
