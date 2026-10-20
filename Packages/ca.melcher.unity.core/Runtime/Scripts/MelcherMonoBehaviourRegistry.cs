namespace Melcher
{
    using System.Collections.ObjectModel;
    using UnityEngine.SceneManagement;

    /// <summary>
    ///     Provides quick, global access to instances of <typeparamref
    ///     name="TDerived"/>. Instances automatically register in their
    ///     <c>Awake</c> method and unregister in <c>OnDestroy</c>.
    ///     <br /><br />
    ///
    ///     This is essentially the service locator pattern. It holds all
    ///     registered instances in a static list, specific for <typeparamref
    ///     name="TDerived"/>, making it quicker to locate an instance versus
    ///     calling <see cref="UnityEngine.Object.FindObjectOfType{T}()"/>.
    /// </summary>
    public class MelcherMonoBehaviourRegistry<TDerived> : MelcherMonoBehaviour
        where TDerived : MelcherMonoBehaviourRegistry<TDerived>
    {
        // ====================================================================
        // Static properties
        // ====================================================================

        /// <summary>
        ///     All instances. These are added during their <c>Awake</c> and
        ///     removed during their <c>OnDestroy</c>.
        /// </summary>
        public static ReadOnlyCollection<TDerived> instances => s_Instances.readOnly;

        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     Return the first instance that was registered. This depends on
        ///     the order the scenes were loaded, the position of the object in
        ///     the hierarchy, etc.
        /// </summary>
        /// <returns>
        ///     The oldest instance still registered, otherwise <see
        ///     langword="null"/>.
        /// </returns>
        public static TDerived First()
        {
            return s_Instances.Count > 0 ? s_Instances.readOnly[0] : null;
        }

        /// <summary>
        ///     Return the first instance that passes the predicate.
        /// </summary>
        /// <returns>
        ///     The oldest instance still registered that satisfies the
        ///     predicate, otherwise <see langword="null"/>.
        /// </returns>
        public static TDerived First(System.Predicate<TDerived> predicate)
        {
            if (predicate == null)
            {
                throw new System.ArgumentNullException(nameof(predicate));
            }

            foreach (TDerived instance in s_Instances.readOnly)
            {
                if (predicate.Invoke(instance))
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        ///     <inheritdoc cref="First(System.Predicate{TDerived})"/>
        ///     <br /><br />
        ///
        ///     The predicate also receives <paramref name="arg"/> which
        ///     allows for data to be passed without the GC overhead of a
        ///     capturing lambda.
        /// </summary>
        /// <param name="arg">
        ///     Data needed inside <paramref name="predicate"/> to determine
        ///     the suitable instance. This allows the predicate to be non-
        ///     capturing which is better for performance (no GC overhead).
        /// </param>
        /// <returns>
        ///     <inheritdoc cref="First(System.Predicate{TDerived})"/>
        /// </returns>
        public static TDerived First<TArg>(TArg arg, System.Func<TArg, TDerived, bool> predicate)
        {
            if (predicate == null)
            {
                throw new System.ArgumentNullException(nameof(predicate));
            }

            foreach (TDerived instance in s_Instances.readOnly)
            {
                if (predicate.Invoke(arg, instance))
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        ///     Find the first instance in the specified scene.
        /// </summary>
        /// <returns>
        ///     The oldest instance in <paramref name="scene"/> that is still
        ///     registered, otherwise <see langword="null"/>.
        /// </returns>
        public static TDerived FirstFromScene(Scene scene)
        {
            foreach (TDerived instance in s_Instances.readOnly)
            {
                if (instance.gameObject.scene == scene)
                {
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        ///     Find the first instance in the scene returned by <see
        ///     cref="SceneManager.GetActiveScene()"/>.
        /// </summary>
        /// <returns>
        ///     The oldest instance in the active scene that is still
        ///     registered, otherwise <see langword="null"/>.
        /// </returns>
        public static TDerived FirstFromActiveScene()
        {
            return FirstFromScene(SceneManager.GetActiveScene());
        }

        // ====================================================================
        // Methods
        // ====================================================================

        protected virtual void Awake()
        {
            s_Instances.mutable.Add((TDerived)this);
        }

        protected virtual void OnDestroy()
        {
            s_Instances.mutable.Remove((TDerived)this);
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        ///     Contains all alive instances of <see cref="TDerived"/>. Alive
        ///     means the component has called <c>Awake</c> and hasn't yet
        ///     called <c>OnDestroy</c>.
        /// </summary>
        private static MelcherReadWriteList<TDerived> s_Instances = new ();

    }   // class MelcherMonoBehaviourRegistry

}   // namespace Melcher

