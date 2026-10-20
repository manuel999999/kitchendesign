using System;
using UnityEngine;
using Doge.Events;

namespace Doge.Scenes
{
    public class SceneController<T> : MonoBehaviour
	{
		[SerializeField] protected T sceneID;
		[SerializeField] protected GameObject target;
		[SerializeField] protected bool disableOnStart;

        public T SceneID => this.sceneID;

        public GameObject Target => this.target;
        public event Action<bool> ActiveChanged;

		protected void Awake() {
			SceneControl.Activate.AddListener(SceneActivate);
			SceneControl.Deactivate.AddListener(SceneDeactivate);
			SceneControl.Toggle.AddListener(SceneToggle);
		}

		protected void OnDestroy() {
			SceneControl.Activate.RemoveListener(SceneActivate);
			SceneControl.Deactivate.RemoveListener(SceneDeactivate);
			SceneControl.Toggle.RemoveListener(SceneToggle);
		}

		protected void Start() {
			if(this.disableOnStart) {
				this.target.SetActive(false);
			}
		}

		protected virtual void SceneActivate(IDogeParam param) {
			if(param.TryParse(out DogeParam<T> p) && p.Value.Equals(this.sceneID)) {
				Activate(true);
			}
		}

		protected virtual void SceneDeactivate(IDogeParam param) {
			if(param.TryParse(out DogeParam<T> p) && p.Value.Equals(this.sceneID)) {
				Activate(false);
			}
		}

		protected virtual void SceneToggle(IDogeParam param) {
			if(param.TryParse(out DogeParam<T> p) && p.Value.Equals(this.sceneID)) {
				if(this.target.activeSelf) {
					Activate(false);
				} else {
					Activate(true);
				}
			}
		}

		protected virtual void Activate(bool activate) {
			this.target.SetActive(activate);

			if(ActiveChanged != null) {
				ActiveChanged.Invoke(activate);
			}
		}
	}
}
