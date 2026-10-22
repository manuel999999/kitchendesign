using UnityEngine;

namespace KitchenDesign.Utility
{
    public static class RendererUtility
    {
        public static void SetAllRendererMaterials(Renderer renderer, Material material)
        {
            Material[] sharedMaterials = renderer.sharedMaterials;
            for (int i = 0; i < sharedMaterials.Length; ++i)
            {
                sharedMaterials[i] = material;
            }
            renderer.sharedMaterials = sharedMaterials;
        }

        public static void SetGameObjectLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;

            foreach (Transform child in transform)
            {
                SetGameObjectLayerRecursively(child, layer);
            }
        }
    }
}
