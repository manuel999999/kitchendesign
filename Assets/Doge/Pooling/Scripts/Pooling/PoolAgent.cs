using System;
using System.Collections.Generic;
using UnityEngine;

namespace Doge.Pooling
{
    public abstract class PoolAgent<T> : MonoBehaviour where T : MonoBehaviour
	{
		[SerializeField] protected T poolObject;
		[SerializeField] protected int poolCount;
		[SerializeField] protected Transform poolContainer;

		protected List<T> _poolObjects;
		public List<T> PoolObjects {
            get {
                return _poolObjects;
            }
        }
		public event Action OnPoolFinish;

		private void Start() {
			StartPool();
		}

		protected virtual void StartPool() {
			_poolObjects = new List<T>();
			
			for(int i = 0; i < poolCount; i++) {
				var item = Instantiate(poolObject, poolContainer);
				item.gameObject.SetActive(false);
				Before(item, i);
				_poolObjects.Add(item);
			}
			
			if(OnPoolFinish != null) {
				OnPoolFinish.Invoke();
			}
		}
		
		public virtual T Get() {
			if(_poolObjects == null || _poolObjects.Count == 0) {
				return null;
			}
			for(int i = 0; i < _poolObjects.Count; i++) {
				if(IsAvailable(_poolObjects[i])) {
					return _poolObjects[i];
				}
			}

			return null;
		}

		public virtual void Clean() {
			if(_poolObjects == null || _poolObjects.Count == 0) {
				return;
			}

			for(int i = 0; i < _poolObjects.Count; i++) {
				Disable(_poolObjects[i]);
			}
		}

		protected virtual void Before(T item, int index) { }

		protected abstract bool IsAvailable(T item);
		protected abstract void Disable(T item);
	}
}
