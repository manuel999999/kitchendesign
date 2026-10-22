using KitchenDesign.Scriptables;
using KitchenDesign.Shared.Interfaces;
using UnityEngine;

namespace KitchenDesign
{
    public abstract class SceneInitializer : MonoBehaviour
    {
        public abstract void Initialize(AppProfileSO appProfile, IAppRules appRules);
    }
}
