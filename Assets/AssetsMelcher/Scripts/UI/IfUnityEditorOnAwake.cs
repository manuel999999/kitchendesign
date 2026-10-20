namespace Melcher
{
    using UnityEngine;
    using UnityEngine.Events;

    public class IfUnityEditorOnAwake : MelcherMonoBehaviour
    {
        [SerializeField] private UnityEvent unityEvent;

        private void Awake()
        {
#if UNITY_EDITOR
            this.unityEvent?.Invoke();
#endif
        }

    }   // class IfUnityEditorOnAwake

}   // namespace Melcher

