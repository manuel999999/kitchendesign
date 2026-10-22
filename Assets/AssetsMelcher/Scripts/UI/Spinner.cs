namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    ///     Intended to spin any loading images at a consistent rate throughout
    ///     the app.
    /// </summary>
    // TODO: Replace previous spinners in other UI with this component.
    public class Spinner : MelcherMonoBehaviour
    {
        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        private void Update()
        {
            const float DegreesPerSecond = 120.0f;

            m_Transform.localEulerAngles += Time.deltaTime * DegreesPerSecond * Vector3.back;
        }

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent]
        protected Transform m_Transform = default;
    }   // class Spinner

}   // namespace KitchenDesign

