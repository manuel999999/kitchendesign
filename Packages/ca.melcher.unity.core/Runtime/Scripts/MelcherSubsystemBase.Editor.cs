#if UNITY_EDITOR
namespace Melcher
{
    using UnityEngine;

    // Editor-only implementation details
    public abstract partial class MelcherSubsystemBase : MelcherMonoBehaviour
    {
        static partial void InternalDestroyEditorInstances<SubsystemType>()
            where SubsystemType : MonoBehaviour
        {
            SubsystemType[] systems = Resources.FindObjectsOfTypeAll<SubsystemType>();
            int systemCount = systems.Length;
            int destroyCount = 0;
            for (int i = systemCount - 1; i >= 0; --i)
            {
                GameObject go = systems[i].gameObject;

                if (go && go.hideFlags == EditorSubsystemHideFlags)
                {
                    DestroyImmediate(go);
                    ++destroyCount;
                    systems[i] = null;
                }
            }
        }

        /// <summary>
        /// <see cref="HideFlags"/> bitmask used by editor-time subsystems.
        /// </summary>
        public const HideFlags EditorSubsystemHideFlags = HideFlags.HideAndDontSave;

    }   // class MelcherSubsystemBase

}   // namespace Melcher
#endif  // if UNITY_EDITOR

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-14
/// ===========================================================================
