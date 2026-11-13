namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class SpinBehavior : MelcherMonoBehaviour
    {
        private void Update()
        {
            transform.localEulerAngles += Vector3.up * Time.deltaTime * 20.0f;
        }
    
    }   // class SpinBehavior

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-01
/// ===========================================================================
