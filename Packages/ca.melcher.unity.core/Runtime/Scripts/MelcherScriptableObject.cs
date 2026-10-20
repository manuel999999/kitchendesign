namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// Base class for ScriptableObjects that want to react to editor play-mode
    /// changes (e.g. to reset their data when the editor exits play-mode).
    /// </summary>
    public abstract class MelcherScriptableObject : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>
        /// Called when the editor's play mode changes. Can be used to
        /// initialize or reset data between play sessions.
        /// </summary>
        public virtual void OnEditorPlayModeStateChanged(UnityEditor.PlayModeStateChange mode)
        {
            // Does nothing by default
        }

        protected virtual void OnValidate()
        {
            MelcherOnValidateAttribute.ValidateFields(this);
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherScriptableObject

}   // namespace Melcher
