using UnityEngine;

namespace KitchenDesign
{
    public sealed class IntermediateDesignerToMenuSceneInitializer : SceneInitializer
    {
        [SerializeField] private MemoryFlushControl memoryFlush;

        public override void Initialize(AppSystems systems)
        {
            this.memoryFlush.Initialize();
        }
    }
}
