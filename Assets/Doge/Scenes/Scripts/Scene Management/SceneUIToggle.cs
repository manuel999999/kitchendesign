using UnityEngine;
using UnityEngine.Events;
using Doge.Events;

namespace Doge.Scenes
{
    public abstract class SceneUIToggle<T> : MonoBehaviour
    {
        protected abstract UnityEvent ClickEvent { get; }
        protected abstract T Target { get; }

        protected void Awake() {
            ClickEvent.AddListener(Toggle);
        }

        protected void OnDestroy() {
            ClickEvent.RemoveListener(Toggle);
        }

        protected void Toggle() {
            SceneControl.Toggle.Raise(new DogeParam<T>(Target));
        }
    }
}
