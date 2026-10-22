namespace KitchenDesign.Products
{
    using UnityEngine;

    public readonly struct LoadedProductContent
    {
        public LoadedProductContent(GameObject sourceContent)
        {
            this.SourceContentPrefab = sourceContent.GetComponent<ProductVisualContent>();
            this.RuntimeContentPrefab = Utility.RuntimePrefabs.InstantiatePrefabAsRuntimePrefab(this.SourceContentPrefab);
        }

        public ProductVisualContent SourceContentPrefab { get; }

        public ProductVisualContent RuntimeContentPrefab { get; }
    }
}
