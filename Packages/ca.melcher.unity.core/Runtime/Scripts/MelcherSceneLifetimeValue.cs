namespace Melcher
{
    using System.Collections.Generic;

    /// <summary>
    ///     The base class for values whose lifetime is scoped to a scene. A
    ///     scene lifetime value can contain any arbitrary data. The data
    ///     survives as long as the scene it belongs to is loaded.
    ///     <br /><br />
    ///
    ///     The value can be retrieved from other scenes and other scripts
    ///     using the static <see cref="Get"/> function.
    ///     <br /><br />
    ///
    ///     The data held by a <see cref="MelcherSceneLifetimeValue{DerivedType}"/>
    ///     is intended to be exposed in the inspector. This makes it easy to
    ///     view and override the value.
    /// </summary>
    /// <typeparam name="DerivedType">
    ///     This is the type that is deriving from <see
    ///     cref="MelcherSceneLifetimeValue{DerivedType}"/>.
    ///     <br /><br />
    ///
    ///     Example:
    ///     <code>
    ///     class MyClass
    ///         : MelcherSceneLifetimeValue ( MyClass )
    ///     {
    ///     }
    ///     </code>
    /// </typeparam>
    // Usage:
    //
    // Declaring a new MelcherSceneLifetimeValue looks like this:
    //
    //  public class GameObjectSceneLifetimeValue : MelcherSceneLifetimeValue ( GameObjectSceneValue )
    //  {
    //      public GameObject go;
    //  }
    //
    // And using it looks like this:
    //
    //  protected void OnEnable()
    //  {
    //      GameObjectSceneLifetimeValue value =
    //          GameObjectSceneLifetimeValue.Get();
    //
    //      value.go = gameObject;
    //  }
    //
    // This feature can be used to create a Session scene that contains
    // GameObjects that have their own MelcherSceneLifetimeValue components
    // attached. These components can be fetched from any scene as long as the
    // Session scene is loaded. The Session scene can be reloaded to wipe its
    // data.
    [System.Obsolete("This type will be replaced by MelcherMonoBehaviourRegistry")]
    // REVIEW: This type was originally designed to expose single fields in the
    //         inspector. E.g., the player's life count, money, experience,
    //         etc. However, in practice, it is often used for things more
    //         complex than simple "values" and its `Get` API lacks the useful
    //         scene-based specificity offered by MelcherMonoBehaviourRegistry.
    //         Scene-based lookup is nice for modular testing and more reliable
    //         than the index-based lookup offered here. -Ryan
    public abstract class MelcherSceneLifetimeValue<DerivedType> : MelcherMonoBehaviour
        where DerivedType : MelcherSceneLifetimeValue<DerivedType>
    {
        // =====================================================================
        // Properties
        // =====================================================================

        public static int count => s_Instances.Count;

        // ====================================================================
        // Methods
        // ====================================================================

        public static DerivedType Get()
        {
            return Get(index: 0);
        }

        public static DerivedType Get(int index)
        {
            if (index < 0 || index >= s_Instances.Count)
            {
                MelcherLog.LogError(
                    $"No value for {MelcherFormat.ConsoleTypeName<DerivedType>()} " +
                    $"is registered at index {MelcherFormat.ConsoleNumericValue(index)}. " +
                    $"Did you forget to add a {MelcherFormat.ConsoleTypeName<DerivedType>()} " +
                    $"to the scene? Or maybe its scene hasn't finished loading yet?"
                );

                return null;
            }

            return s_Instances[index];
        }

        public static bool TryGet(out DerivedType sessionObject, int index = 0)
        {
            sessionObject = null;

            if (index >= 0 && index < s_Instances.Count)
            {
                sessionObject = s_Instances[index];
            }

            return sessionObject != null;
        }

        public static DerivedType Find(System.Predicate<DerivedType> predicate)
        {
            return s_Instances.Find(predicate);
        }

        protected virtual void OnEnable()
        {
            s_Instances.Add((DerivedType)this);
        }

        protected virtual void OnDisable()
        {
            s_Instances.Remove((DerivedType)this);
        }

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.ClearCollection]
        protected static List<DerivedType> s_Instances = new List<DerivedType>();

    }   // class MelcherSceneLifetimeValue

}   // namespace Melcher
