using System;
using System.Collections.Generic;
using UnityEngine;

namespace Doge.Collections
{
    public abstract class ObjectCollection<TKey, TValue> : MonoBehaviour
    {
		public abstract List<TValue> Items { get; }
        protected abstract TKey GetKey(TValue item);

        protected Dictionary<TKey,TValue> _objectCollection;

		public event Action OnCreateFinish;

		protected virtual void Start() {
			CreateItemCollection();
		}

		protected void CreateItemCollection() {
			_objectCollection = new Dictionary<TKey, TValue>();
			if(this.Items != null && this.Items.Count > 0) {
				for(int i= 0; i < this.Items.Count; i++) {
					_objectCollection[GetKey(this.Items[i])] = this.Items[i];
				}
			}

			if(OnCreateFinish != null) {
                OnCreateFinish.Invoke();
            }
        }

		public TValue GetObject(TKey key) {
			if(_objectCollection != null && _objectCollection.ContainsKey(key)) {
				return _objectCollection[key];
			} else {
                return DefaultValue();
            }
		}

		protected virtual TValue DefaultValue() {
			return default(TValue);
		}

		public bool HasKey(TKey key) {
            return _objectCollection.ContainsKey(key);
        }
    }
}
