namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     A <see cref="ProductVisual"/> represents a preset appearance for a
    ///     a product. Visuals are often reused by several <see
    ///     cref="ProductInfo"/>. A visual is split into two parts to minimize
    ///     their memory overhead: the lightweight <see
    ///     cref="ProductVisualInfo">info</see> and the heavier <see
    ///     cref="ProductVisualContent">content</see> loaded via addressables.
    ///     <br /><br />
    ///
    ///     The <see cref="ProductVisualDatabase"/> holds an array of every
    ///     <see cref="ProductVisual"/>, including the runtime copies used to
    ///     allow their data to be changed without persisting outside of
    ///     playmode, which is why its so important their heavier data is
    ///     gated behind addressables.
    /// </summary>
    [CreateAssetMenu(fileName ="", menuName = "Melcher Studios/Pro-Fit/Product Visual")]
    public class ProductVisual : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public ProductVisualInfo info => m_Info;

        public AssetAddress<ProductVisualContent> contentAddress => m_ContentAddress;

        public bool isRuntimeInfo => m_SourceProductVisual != null;

        public ProductVisual sourceProductVisual => m_SourceProductVisual;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Creates a copy of this entry. Intended to be called at runtime.
        ///     <br /><br />
        ///
        ///     This allows the copy (its info and prefab) to be modified
        ///     independently of the originals, which is needed for some
        ///     building methods and brand catalogs.
        /// </summary>
        /// <returns></returns>
        public ProductVisual MakeRuntimeClone()
        {
            if (isRuntimeInfo)
            {
                throw new System.InvalidOperationException(
                    "Trying to clone a runtime product visual but only " +
                    "source product visuals are intended to be cloned."
                );
            }

            ProductVisual clone = Instantiate(this);

            clone.name = name;  // remove the (clone) suffix.
            clone.m_Info = Instantiate(clone.m_Info);
            clone.m_Info.name = m_Info.name;
            clone.m_SourceProductVisual = this;

            return clone;
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        private ProductVisualInfo m_Info;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_PrefabAddress")]
        private AssetAddress<ProductVisualContent> m_ContentAddress;

        // ====================================================================
        // Non-serialized fields
        // ====================================================================

        /// <summary>
        ///     The <see cref="ProductVisual"/> that was used to
        ///     instantiate this object, if this object is a runtime instance.
        ///     Otherwise, this will be <see langword="null"/> to indicate it
        ///     is a source/original entry that was created in the editor.
        /// </summary>
        private ProductVisual m_SourceProductVisual;

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        public void EditorOnlySetInfo(ProductVisualInfo newInfo)
        {
            m_Info = newInfo;
        }

        public void EditorOnlySetContentAddress(ProductVisualContent content)
        {
            AssetAddress<ProductVisualContent>.TryFromAsset(content, out m_ContentAddress);
        }

        [UnityEditor.MenuItem("CONTEXT/" + nameof(ProductVisual) + "/Link to script")]
        private static void LinkToScript(UnityEditor.MenuCommand command)
        {
            ProductVisual target = (ProductVisual)command.context;

            string path = UnityEditor.AssetDatabase.GetAssetPath(target);

            path = System.IO.Path.ChangeExtension(path, extension: ".prefab");

            path = path.Replace("^", "");

            ProductVisualContent content =
                UnityEditor.AssetDatabase.LoadAssetAtPath<ProductVisualContent>(path);
            MelcherLog.Breadcrumb("path=" + path, content);

            MelcherLog.Breadcrumb(AssetAddress<ProductVisualContent>.TryFromAsset(content, out target.m_ContentAddress));

            UnityEditor.EditorUtility.SetDirty(target);
        }

#endif  // if UNITY_EDITOR

    }   // class ProductVisual

}   // namespace Melcher

