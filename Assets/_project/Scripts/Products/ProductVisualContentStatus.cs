namespace KitchenDesign.Products
{
    using KitchenDesign.Scriptables;
    using KitchenDesign.Serializables;
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public sealed class ProductVisualContentStatus
    {
        public delegate void ContentLoadedHandler(
            AssetAddress<ProductVisualContent> address,
            LoadedProductContent newlyLoadedContent);

        public event ContentLoadedHandler ContentLoaded;

        private readonly Dictionary<AssetAddress<ProductVisualContent>, ContentState> _states = new();

        public async UniTask<ProductVisualContent> LoadContent(ProductVisualSO productVisual)
        {
            if (productVisual == null)
            {
                throw new System.ArgumentNullException(nameof(productVisual));
            }

            if (!this._states.TryGetValue(productVisual.contentAddress, out ContentState state))
            {
                state = new ContentState(Addressables.LoadAssetAsync<GameObject>(productVisual.contentAddress));

                state.Handle.Completed += this.OnContentLoadCompleted;

                this._states.Add(productVisual.contentAddress, state);
            }

            await state.Handle;

            return
                productVisual.isRuntimeInfo ?
                    state.Content.RuntimeContentPrefab :
                    state.Content.SourceContentPrefab;
        }

        public bool IsLoadingOrLoaded(ProductVisualSO productVisual)
        {
            if (this._states.TryGetValue(productVisual.contentAddress, out ContentState state))
            {
                return state.Handle.Status != AsyncOperationStatus.Failed;
            }

            return false;
        }

        public void Purge()
        {
            foreach (ContentState state in this._states.Values)
            {
                Addressables.Release(state.Handle);
            }

            this._states.Clear();
        }

        private void OnContentLoadCompleted(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            foreach (KeyValuePair<AssetAddress<ProductVisualContent>, ContentState> entry in this._states)
            {
                if (entry.Value.Handle.Result != handle.Result)
                {
                    continue;
                }

                entry.Value.Content = new LoadedProductContent(handle.Result);

                this.ContentLoaded?.Invoke(entry.Key, entry.Value.Content);

                break;
            }
        }

        private sealed class ContentState
        {
            public ContentState(AsyncOperationHandle<GameObject> handle)
            {
                this.Handle = handle;
            }

            public AsyncOperationHandle<GameObject> Handle { get; }

            public LoadedProductContent Content { get; set; }
        }
    }
}
