namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// Adds editor support for <see cref="MelcherOnValidateAttribute"/> and the
    /// <see cref="MelcherFindComponentAttribute"/> system.
    /// </summary>
    public class MelcherMonoBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            MelcherOnValidateAttribute.ValidateFields(this);
        }
#endif  // if UNITY_EDITOR

    }   // class MelcherMonoBehaviour

}   // namespace Melcher
