namespace Melcher
{
    public partial class MelcherFindAssetAttribute : MelcherOnValidateAttribute
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public MelcherFindAssetAttribute(string assetPathOrSearchKeyword = null)
        {
            m_AssetPathOrSearchKeyword = assetPathOrSearchKeyword;
        }

        // =====================================================================
        // Internal fields
        // =====================================================================

        private string m_AssetPathOrSearchKeyword = null;

    }   // class MelcherFindAssetAttribute

}   // namespace Melcher
