using KitchenDesign.Scriptables;
using KitchenDesign.Shared.Interfaces;
using UnityEngine;
using KitchenDesign.Products;

namespace KitchenDesign
{
    public abstract class SceneInitializer : MonoBehaviour
    {
        public abstract void Initialize(AppProfileSO appProfile, IAppRules appRules, ProductVisualContentStatus productContent);
    }
}
