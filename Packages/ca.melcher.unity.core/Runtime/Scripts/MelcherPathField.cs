namespace Melcher
{
    /// <inheritdoc cref="MelcherPathFieldBase"/>
    [System.Serializable]
    public class MelcherPathField<AssetType> : MelcherPathFieldBase
        where AssetType : UnityEngine.Object
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public override System.Type assetType => typeof(AssetType);

    }   // class MelcherPathField

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-03-13
/// ===========================================================================
