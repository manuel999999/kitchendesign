namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     A <see cref="MelcherPrefabPoolLink">prefab pool link</see> for
    ///     prefabs that will be used in <see cref="MelcherPrefabPool"/>s.
    ///     <br /><br />
    ///
    ///     This type of link allows one or more of the prefab's components'
    ///     `enabled` property to be changed when it is borrowed or returned.
    ///     This can be more performant than turning entire GameObjects off and
    ///     on in some cases.
    ///     <br /><br />
    ///
    ///     E.g., Animators and Canvases.
    /// </summary>
    public class MelcherPrefabPoolLink_ToggleComponents : MelcherPrefabPoolLink
    {
        // =====================================================================
        // Methods
        // =====================================================================

        public override void SetIsBorrowed(bool borrowed)
        {
            foreach (Component component in m_ComponentsToToggle)
            {
                MelcherUnityUtility.SetComponentEnabled(component, borrowed, out bool componentHasEnabledProperty);

#if UNITY_EDITOR
                if (component && !componentHasEnabledProperty)
                {
                    // Warn the user if the component doesn't have an enabled
                    // flag because it looks like they may have made a mistake.
                    MelcherLog.LogWarningEditorOnly($"Component doesn't support 'enabled' property. Did you mean to select something else?: {component}", component);
                }
#endif  // if UNITY_EDITOR
            }

            if (borrowed)
            {
                onBorrowed?.Invoke();
            }
            else
            {
                onReturned?.Invoke();
            }
        }
        
        // ====================================================================
        // Events
        // ====================================================================
        public System.Action onBorrowed;
        public System.Action onReturned;

        // =====================================================================
        // Internal fields
        // =====================================================================

        [SerializeField]
        protected Component[] m_ComponentsToToggle = default;

    }   // class MelcherPrefabPoolLink_ToggleComponents

}   // namespace Melcher
