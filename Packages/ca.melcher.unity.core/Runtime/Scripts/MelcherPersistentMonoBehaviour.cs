namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     <see cref="MelcherPersistentMonoBehaviour{DerivedType}"/> is basically a
    ///     singleton for <see cref="MonoBehaviour"/>s. The instance lives in
    ///     the DontDestroyOnLoad scene. This class provides a static function,
    ///     <see cref="GetInstance"/>, to return the singleton instance.
    /// </summary>
    // To use this class:
    //
    //      public class MyPersistentThing : MelcherPersistentMonoBehaviour<MyPersistentThing>
    //      {
    //      }
    //
    // And then elsewhere, (for example, in your Main.cs):
    //
    //      protected void Awake()
    //      {
    //          MyPersistentThing.GetInstance();
    //      }
    //
    public abstract class MelcherPersistentMonoBehaviour<DerivedType> : MelcherMonoBehaviour
        where DerivedType : MelcherPersistentMonoBehaviour<DerivedType>
    {
        public static DerivedType GetInstance()
        {
#if UNITY_EDITOR
            if (!MelcherUnityUtility.IsApplicationPlaying())
            {
                MelcherLog.LogErrorEditorOnly("Can only be created during play mode.");

                return null;
            }
#endif  // if UNITY_EDITOR

            if (s_Instance == null)
            {
                GameObject go = new GameObject(typeof(DerivedType).Name);
                s_Instance = go.AddComponent<DerivedType>();
                DontDestroyOnLoad(go);
            }

            return s_Instance;
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        protected static DerivedType s_Instance = default;

    }   // class MelcherPersistentInstance

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-04-04
/// ===========================================================================
