namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     A <see cref="Component"/> to be attached to GameObjects that used
    ///     in by a <see cref="MelcherPrefabPool"/>. This class defines how the
    ///     GameObject behaves when it is borrowed (and returned to) its pool.
    /// <summary>
    /// <verbose>
    ///     Pooled GameObjects are expected to have a component of this type
    ///     attached to their root. If a pooled object doesn't have a <see
    ///     cref="MelcherPrefabPoolLink"/>, one is added when the prefab is
    ///     instantiated by <see cref="MelcherPrefabPool"/>.
    ///     <br /><br />
    ///
    ///     The default prefab pool link is <see
    ///     cref="MelcherPrefabPoolLink_ToggleGameObject"/> which turns a
    ///     GameObject on/off when it is borrowed/returned.
    ///     <br /><br />
    ///
    ///     This base class prefab link does nothing on borrow/return. This is
    ///     not ideal in the majority of cases, however it can be for some
    ///     component types like particle systems. This class can be extended to
    ///     define the desired borrow/return behaviour.
    /// </verbose>
    public class MelcherPrefabPoolLink : MelcherMonoBehaviour
    {
        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Return this prefab to its pool. By default, this will move the
        ///     object back to its prefab pool scene.
        /// </summary>
        public bool ReturnToPool()
        {
            return MelcherPrefabPool.ReturnObjectToPool(m_PoolInstanceId, this);
        }

        /// <summary>
        ///     A <see cref="MelcherPrefabPoolLink"/> that does nothing when
        ///     the pooled object is borrowed or returned. This is useful for
        ///     some types of components: e.g., particle prefabs benefit from
        ///     this because their emitted particles will persist after the
        ///     pooled object is returned.
        /// </summary>
        public virtual void SetIsBorrowed(bool borrowed)
        {
            // Do nothing
        }

        /// <summary>
        /// If we need to do any initialization in a link sub class. 
        /// </summary>
        public virtual void SetIsReady()
        {
            // Do nothing
        }

        // ====================================================================
        // Methods used internally by PrefabPool
        // ====================================================================
        #region PrefabPool internal methods

        /// <summary>
        /// Return the pool that owns this prefab.
        /// </summary>
        public EntityId GetPoolInstanceID()
        {
            return m_PoolInstanceId;
        }

        /// <summary>
        /// Link the prefab to a specific pool.
        /// </summary>
        public void SetPoolInstanceID(EntityId poolInstanceId)
        {
            m_PoolInstanceId = poolInstanceId;
        }

        #endregion  // PrefabPool internal methods

        // ====================================================================
        // Members
        // ====================================================================

        protected EntityId m_PoolInstanceId;

    }   // class MelcherPrefabPoolLink

}   // namespace Melcher
