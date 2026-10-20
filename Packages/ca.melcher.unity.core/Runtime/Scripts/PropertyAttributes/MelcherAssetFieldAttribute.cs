namespace Melcher
{
    using UnityEngine;

    /// <summary>
    /// Ensure that a field references an asset. The field is cleared if the
    /// user attempts to assign an object from a scene.
    /// </summary>
    public class MelcherAssetFieldAttribute : PropertyAttribute
    {
    }   // class MelcherAssetFieldAttribute

}   // namespace Melcher
