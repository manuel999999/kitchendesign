namespace Melcher
{
    /// <summary>
    /// Turn the whole GameObject on (borrowed) or off (returned). This
    /// can be expensive for some component types like Animators and
    /// Canvases.
    /// </summary>
    public class MelcherPrefabPoolLink_ToggleGameObject : MelcherPrefabPoolLink
    {
        public override void SetIsBorrowed(bool borrowed)
        {
            gameObject.SetActive(borrowed);
        }

    }   // class MelcherPrefabPoolLink_ToggleGameObject

}   // namespace Melcher
