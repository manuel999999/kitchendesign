namespace Melcher
{
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    ///     Manages the loading (and querying of loaded) addressable <see
    ///     cref="ProductVisualContent"/>s.
    /// </summary>
    public class ProductVisualContentLoader :
        MelcherMonoBehaviourRegistry<ProductVisualContentLoader>
    {
        // ====================================================================
        // Events
        // ====================================================================

        public delegate void
            ProductVisualContentLoadedHandler(
                AssetAddress<ProductVisualContent> address,
                LoadedContent newlyLoadedContent
            );

        /// <summary>
        ///     Provides a callback where newly loaded visual content can be
        ///     updated according to the app's current state. For example,
        ///     depending on which <see cref="KitchenBuildingMethod"/> is
        ///     active, products may need to be tweaked slightly and this needs
        ///     to be handled any time a product loads at runtime.
        /// </summary>
        public event ProductVisualContentLoadedHandler onProductVisualContentLoaded;

        // ====================================================================
        // Methods
        // ====================================================================

        public async UniTask<ProductVisualContent> ProductVisualToContent(ProductVisual productVisual)
        {
            if (productVisual == null)
            {
                throw new System.ArgumentNullException(nameof(productVisual));
            }

            if (!m_ProductVisualContentAssetStates.TryGetValue(productVisual.contentAddress, out AssetState assetState))
            {
                assetState = new AssetState(Addressables.LoadAssetAsync<GameObject>(productVisual.contentAddress));

                assetState.handle.Completed += OnLoadProductVisualPrefabCompleted;

                m_ProductVisualContentAssetStates.Add(productVisual.contentAddress, assetState);
            }

            await assetState.handle;

            return
                productVisual.isRuntimeInfo ?
                    assetState.loadedContent.runtimeContentPrefab :
                    assetState.loadedContent.sourceContentPrefab;
        }

        public bool IsProductVisualLoadingOrLoaded(ProductVisual productVisual)
        {
            if (m_ProductVisualContentAssetStates.TryGetValue(productVisual.contentAddress, out AssetState assetState))
            {
                return assetState.handle.Status != AsyncOperationStatus.Failed;
            }

            return false;
        }

        /// <summary>
        ///     Release all of the assets this instance loaded.
        /// </summary>
        public void Purge()
        {
            foreach (AssetState entry in m_ProductVisualContentAssetStates.Values)
            {
                //Destroy(entry.loadedContent.runtimeContentPrefab.gameObject);
                //Destroy(entry.loadedContent.sourceContentPrefab.gameObject);

                Addressables.Release(entry.handle);
            }

            m_ProductVisualContentAssetStates.Clear();
        }

        protected override void OnDestroy()
        {
            Purge();

            base.OnDestroy();
        }

        private void OnLoadProductVisualPrefabCompleted(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            // Find the ProductVisual that was used to request this prefab so
            // it the prefab can be meaningfully completed with its associated
            // ProductVisualPrefabInfo.
            foreach (var keyValuePair in m_ProductVisualContentAssetStates)
            {
                if (keyValuePair.Value.handle.Result == handle.Result)
                {
                    keyValuePair.Value.loadedContent = new LoadedContent(handle.Result);

                    onProductVisualContentLoaded?.Invoke(
                        address: keyValuePair.Key,
                        newlyLoadedContent: keyValuePair.Value.loadedContent
                    );

                    break;
                }
            }
        }

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        private Dictionary<AssetAddress<ProductVisualContent>, AssetState> m_ProductVisualContentAssetStates = new ();

        // ====================================================================
        // Types
        // ====================================================================

        /// <summary>
        ///     Holds a reference to the source prefab and runtime prefab of a
        ///     <see cref="ProductVisualContent"/>.
        /// </summary>
        public struct LoadedContent
        {
            public LoadedContent(GameObject sourceContent)
            {
                sourceContentPrefab = sourceContent.GetComponent<ProductVisualContent>();
                runtimeContentPrefab = ProFitUtility.InstantiatePrefabAsRuntimePrefab(sourceContentPrefab);
            }

            public readonly ProductVisualContent sourceContentPrefab;
            public readonly ProductVisualContent runtimeContentPrefab;

        }   // class LoadedContent

        /// <summary>
        ///     The internal state of the addressable operation, paired with
        ///     references to the loaded data (assigned when it eventually
        ///     loads).
        /// </summary>
        private class AssetState
        {
            public AssetState(AsyncOperationHandle<GameObject> handle)
            {
                this.handle = handle;
                loadedContent = default;
            }

            /// <summary>
            ///     The addressable operation. Provides access to the
            ///     addressable's status, asset object, and the ability to
            ///     release the addressable.
            /// </summary>
            public readonly AsyncOperationHandle<GameObject> handle;

            /// <summary>
            ///     Combines the two types of content data (source/original and
            ///     runtime) into one view.
            /// </summary>
            public LoadedContent loadedContent;

        }   // struct AssetState

    }   // class ProductVisualContentManager

}   // namespace Melcher

