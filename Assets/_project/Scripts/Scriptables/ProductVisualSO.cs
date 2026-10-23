namespace KitchenDesign.Scriptables
{
    using Melcher;
    using KitchenDesign.Serializables;
    using UnityEngine;
    using KitchenDesign.Products;

    [CreateAssetMenu(fileName ="", menuName = "Melcher Studios/Pro-Fit/Product Visual")]
    public class ProductVisualSO : MelcherScriptableObject
    {
        public ProductVisualInfoSO info => m_Info;

        public AssetAddress<ProductVisualContent> contentAddress => m_ContentAddress;

        public bool isRuntimeInfo => m_SourceProductVisual != null;

        public ProductVisualSO sourceProductVisual => m_SourceProductVisual;

        public ProductVisualSO MakeRuntimeClone()
        {
            if (isRuntimeInfo)
            {
                throw new System.InvalidOperationException(
                    "Trying to clone a runtime product visual but only " +
                    "source product visuals are intended to be cloned."
                );
            }

            ProductVisualSO clone = Instantiate(this);

            clone.name = name;
            clone.m_Info = Instantiate(clone.m_Info);
            clone.m_Info.name = m_Info.name;
            clone.m_SourceProductVisual = this;

            return clone;
        }

        [SerializeField]
        private ProductVisualInfoSO m_Info;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_PrefabAddress")]
        private AssetAddress<ProductVisualContent> m_ContentAddress;

        private ProductVisualSO m_SourceProductVisual;

#if UNITY_EDITOR

        public void EditorOnlySetInfo(ProductVisualInfoSO newInfo)
        {
            m_Info = newInfo;
        }

        public void EditorOnlySetContentAddress(ProductVisualContent content)
        {
            AssetAddress<ProductVisualContent>.TryFromAsset(content, out m_ContentAddress);
        }

        [UnityEditor.MenuItem("CONTEXT/" + nameof(ProductVisualSO) + "/Link to script")]
        private static void LinkToScript(UnityEditor.MenuCommand command)
        {
            ProductVisualSO target = (ProductVisualSO)command.context;

            string path = UnityEditor.AssetDatabase.GetAssetPath(target);

            path = System.IO.Path.ChangeExtension(path, extension: ".prefab");

            path = path.Replace("^", "");

            ProductVisualContent content =
                UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualContent>(path);
            MelcherLog.Breadcrumb("path=" + path, content);

            MelcherLog.Breadcrumb(AssetAddress<ProductVisualContent>.TryFromAsset(content, out target.m_ContentAddress));

            UnityEditor.EditorUtility.SetDirty(target);
        }

#endif
    }
}
