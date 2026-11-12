using UnityEngine;

namespace KitchenDesign
{
    public abstract class SceneInitializer : MonoBehaviour
    {
        public abstract void Initialize(AppSystems systems);
    }
}
