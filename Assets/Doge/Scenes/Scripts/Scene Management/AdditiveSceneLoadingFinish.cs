using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Doge.Scenes
{
    public class AdditiveSceneLoadingFinish : MonoBehaviour
    {
        [SerializeField] protected UnityEvent onScenesLoadingFinish;
        [SerializeField] protected float invokeDelay;

        protected void Awake() {
            Scenes.LoadFinished.AddListener(ScenesLoadingFinish);
        }

        protected void OnDestroy() {
            Scenes.LoadFinished.RemoveListener(ScenesLoadingFinish);
        }

        protected virtual void ScenesLoadingFinish() {
            if(this.onScenesLoadingFinish != null) {
                if(this.invokeDelay > 0) {
                    StartCoroutine(SceneLoadingFinishDelay());
                } else {
                    this.onScenesLoadingFinish.Invoke();
                }
            }
        }

        protected virtual IEnumerator SceneLoadingFinishDelay() {
            yield return new WaitForSeconds(this.invokeDelay);
            this.onScenesLoadingFinish.Invoke();
        }
    }
}
