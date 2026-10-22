namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class RotateCamera : MelcherMonoBehaviour
    {
        private void Update()
        {
            transform.localEulerAngles += Vector3.up * Time.deltaTime * 20.0f;
        }
    
    }   // class RotateCamera

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-01
/// ===========================================================================
