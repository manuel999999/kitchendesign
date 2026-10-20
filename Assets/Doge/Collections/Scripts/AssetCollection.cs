using System;
using System.Collections.Generic;
using UnityEngine;

namespace Doge.Collections
{
    public abstract class AssetCollection<TKey, TValue> : MonoBehaviour
	{
		protected abstract AssetCollectionItem<TKey, TValue>[] AssetsSource { get; }

        public event Action OnCreateFinish;

        protected Dictionary<TKey,TValue> _assetCollection;
		
		public AssetCollectionItem<TKey, TValue>[] AssetsInCollection { 
			get {
				return AssetsSource;
			}
		}

		protected virtual void Start() {
			CreateAssetCollection();
		}

		protected virtual void CreateAssetCollection() {
			_assetCollection = new Dictionary<TKey, TValue>();
			if(AssetsSource != null && AssetsSource.Length > 0) {
				for(int i= 0; i < AssetsSource.Length; i++) {
					_assetCollection[AssetsSource[i].Key] = AssetsSource[i].Value;
				}
			}

			CreateFinish();
        }

		protected void CreateFinish() {
			if(OnCreateFinish != null) {
                OnCreateFinish.Invoke();
            }
		}

		public TValue GetAsset(TKey key) {
			if(_assetCollection != null && _assetCollection.ContainsKey(key)) {
				return _assetCollection[key];
			} else {
                return DefaultValue();
            }
		}

		protected virtual TValue DefaultValue() {
			return default(TValue);
		}

		public bool HasKey(TKey key) {
            return _assetCollection.ContainsKey(key);
        }
	}
}