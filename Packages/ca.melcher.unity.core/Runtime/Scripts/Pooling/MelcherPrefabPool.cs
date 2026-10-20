namespace Melcher
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    ///     A <see cref="MelcherPrefabPool"/> is a ScriptableObject that
    ///     represents a prefab pool. An instance of this asset can be
    ///     referenced by multiple objects and they will all share access to the
    ///     same pool of objects.
    /// </summary>
    /// <verbose>
    ///     To use a <see cref="MelcherPrefabPool"/>:
    ///     <br /><br />
    ///
    ///     1.  Create a prefab pool asset in your project.
    ///         "Assets > Create > Melcher Studios > Prefab Pool"
    ///         <br /><br />
    ///
    ///     2.  Assign the pool's prefab and capacity.
    ///         <br /><br />
    ///
    ///     3.  Reference the pool in your MonoBehaviour. This is usually done
    ///         using a serialized field.
    ///         <br /><br />
    ///
    ///     4.  Call the pool's <see cref="AddPoolUser"/> function in your
    ///         script's OnEnable() or Awake(). This ensures the pool is
    ///         initialized before it is needed.
    ///         <br /><br />
    ///
    ///     5.  Call the pool's <see cref="RemovePoolUser"/> function in your
    ///         script's OnDisable() or OnDestroy(). This frees the pool scene
    ///         and all of its objects if this is the last use case of the pool.
    ///         <br /><br />
    ///
    ///     The number of objects in a pool is fixed. This is by design
    ///     because runtime instantiation affects performance and is otherwise
    ///     solvable with a larger pool size. It may be simplest to start a
    ///     prefab pool with a large number of objects and refine it down
    ///     to the smallest usable value after testing.
    ///     <br /><br />
    ///
    ///     Prefab pools create their own scene to instantiate the pooled
    ///     objects. The "pool scene" provides a few benefits:
    ///     <br /><br />
    ///
    ///     1.  It disassociates the pooled objects from any user designed
    ///         scene. Pooled objects are automatically returned to the prefab
    ///         scene when they're finished being used which enables the pool
    ///         to be reused between scenes transitions.
    ///         <br /><br />
    ///
    ///     2.  It offers better performance than nesting the prefabs under
    ///         an empty parent transform. Keeping pooled objects in their own
    ///         scene makes targeted <see cref="MelcherSearch"/> searches more
    ///         performant.
    ///         <br /><br />
    ///
    ///     One caveat with prefab pools is accessing them from
    ///     ScriptableObjects: Due to initialization timing of
    ///     ScriptableObjects, it is possible for the scene that a prefab
    ///     pool creates to be destroyed unexpectedly if a pool calls <see
    ///     cref="AddPoolUser"/> before the initial game scene loads. To be safe,
    ///     try to avoid invoking any prefab pool functions within a
    ///     ScriptableObject's Awake and OnEnable functions.
    /// </verbose>
    [CreateAssetMenu(fileName = "PrefabPool", menuName = "Melcher Studios/Prefab Pool")]
    public class MelcherPrefabPool : MelcherScriptableObject
    {
        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>
        ///     The prefab that is pooled.
        /// </summary>
        public GameObject prefabToPool => m_PrefabToPool;

        /// <summary>
        ///     The maximum number of prefabs available. This is a fixed limit
        ///     that does not resize.
        /// </summary>
        public int capacity => m_Capacity;

        /// <summary>
        ///     <see langword="true"/> if the pool is cyclical. Cyclical pools
        ///     are intended for borrow-and-forget type use and do not need to
        ///     have to have their pooled objects returned.
        /// </summary>
        public bool isCyclical => m_IsCyclical;

        /// <summary>
        ///     The unique scene created to hold this pool's objects.
        /// </summary>
        public Scene poolScene => m_PoolScene;

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Notify the pool that it is being used by something new and
        ///     therefore the pool should ready itself. This initializes the
        ///     pool (if it is not already) by creating a scene and all of the
        ///     pooled objects within it. This increments the use count so that
        ///     additional use cases skip the initialization.
        ///     <br /><br />
        ///
        ///     This must be called before the pool is used; otherwise, its
        ///     internal state will be invalid and throw errors. The best
        ///     practice is to call this in your script's `OnEnable` and to
        ///     call <see cref="RemovePoolUser"/> in your script's `OnDisable`.
        /// </summary>
        public void AddPoolUser()
        {
            bool isFirstUseCase = m_UseCaseCount == 0;

            ++m_UseCaseCount;

            if (isFirstUseCase)
            {
                bool didRegisterPool = s_InUsePrefabPools.Add(this);
                MelcherLog.Assert(didRegisterPool, "This pool already existed in the in-use pool list before it was marked in-use.");

                string sceneName = name;

                m_PoolScene = MelcherSceneManager.CreateScene(sceneName);

                MelcherSceneManager.sceneUnloading -= OnSceneUnloading;
                MelcherSceneManager.sceneUnloading += OnSceneUnloading;

                PreInstantiatePooledPrefabs();

                if (m_IsCyclical)
                {
                    m_ObjectPool =
                        new MelcherObjectPoolCyclic<GameObject>(
                            m_Capacity,
                            InstantiatePrefab
                        );
                }
                else
                {
                    m_ObjectPool =
                        new MelcherObjectPoolStack<GameObject>(
                            m_Capacity,
                            InstantiatePrefab
                        );
                }

                PostInstantiatePooledPrefabs();

                RenamePooledPrefabs();

                // Ensure all of the prefabs are properly initialized as
                // "not-borrowed" by going through their prefab pool link

                IReadOnlyCollection<GameObject> prefabs = m_ObjectPool.allObjects;
                foreach (GameObject prefab in prefabs)
                {
                    MelcherPrefabPoolLink poolLink =
                        prefab.GetComponent<MelcherPrefabPoolLink>();

                    poolLink.SetIsBorrowed(false);

                    // REVIEW: Still necessary? I suggest we move any Readying
                    //         behaviour to the pool link. E.g., either in its
                    //         awake or in SetIsBorrowed() and using its own
                    //         `isReady` field. This keeps the pool agnostic to
                    //         pooled objects implementation. -Ryan
                    poolLink.SetIsReady();
                }
            }
        }

        /// <summary>
        ///     Notify the pool that one of its use cases is no longer needed.
        ///     If the pool has only one use case when this is called, the
        ///     pools's scene and instantiated objects will be destroyed.
        /// </summary>
        public void RemovePoolUser()
        {
            MelcherSceneManager.sceneUnloading -= OnSceneUnloading;

            MelcherLog.Assert(m_UseCaseCount > 0);

            if (m_UseCaseCount > 0)
            {
                --m_UseCaseCount;

                if (m_UseCaseCount <= 0)
                {
                    MelcherSceneRequest poolSceneRequest =
                        MelcherSceneRequest.FromScene(m_PoolScene);

                    MelcherSceneManager.EnqueueUnloadScene(
                        poolSceneRequest,
                        _ =>
                        {
                            MelcherSceneManager.sceneUnloading -= OnSceneUnloading;
                            m_PoolScene = default;
                        }
                    );

                    bool didUnregisterPool = s_InUsePrefabPools.Remove(this);
                    MelcherLog.Assert(didUnregisterPool, "The pool did not exist in the in-use pool list while it was in-use.");
                }
            }
        }

        /// <summary>
        ///     Borrow a pooled prefab and get a component from it.
        /// </summary>
        public ComponentType GetPrefab<ComponentType>()
            where ComponentType : Component
        {
            GameObject borrowedPrefab = m_ObjectPool.GetObject();

            if (!borrowedPrefab.TryGetComponent(out ComponentType component))
            {
                // Immediately return the prefab if it doesn't have the
                // requested component
                m_ObjectPool.ReturnObject(borrowedPrefab);

                MelcherLog.LogError(
                    $"Cannot borrow and get component. Prefab doesn't have {typeof(ComponentType).Name}.",
                    context: this
                );

                return null;
            }

            OnBorrow(borrowedPrefab);

            return component;
        }

        [System.Obsolete("Use GetPrefab<ComponentType>() to improve code clarity.")]
        public ComponentType GetObject<ComponentType>()
            where ComponentType : Component
        {
            return GetPrefab<ComponentType>();
        }

        /// <summary>
        ///     Borrow a pooled prefab.
        /// </summary>
        // REVIEW: Should we just axe this function to simplify the API?
        //         GetObject<ComponentType> feels more robust. -Ryan
        [System.Obsolete("Use GetPrefab<ComponentType>()")]
        public GameObject GetObject()
        {
            GameObject borrowedPrefab = m_ObjectPool.GetObject();

            if (borrowedPrefab)
            {
                OnBorrow(borrowedPrefab);
            }

            return borrowedPrefab;
        }

        /// <summary>
        ///     Return a previously borrowed prefab back to the pool.
        /// </summary>
        public bool ReturnPrefab(MelcherPrefabPoolLink poolLink)
        {
            if (!poolLink.GetPoolInstanceID().Equals(GetEntityId()))
            {
                MelcherLog.LogError(
                    $"Cannot return prefab. Prefab belongs to a different pool.",
                    context: poolLink
                );

                return false;
            }

            OnReturn(poolLink);

            m_ObjectPool.ReturnObject(poolLink.gameObject);

            return true;
        }

        [System.Obsolete("Use ReturnPrefab() to improve code clarity.")]
        public bool ReturnObject(MelcherPrefabPoolLink poolLink)
        {
            return ReturnPrefab((Component)poolLink);
        }

        /// <summary>
        ///     Return a previously borrowed prefab back to the pool.
        /// </summary>
        public bool ReturnPrefab(Component previouslyPooledPrefab)
        {
            if (!previouslyPooledPrefab.TryGetComponent(out MelcherPrefabPoolLink poolLink))
            {
                MelcherLog.LogError(
                    $"Cannot return prefab. Prefab is missing {nameof(MelcherPrefabPoolLink)}",
                    context: previouslyPooledPrefab
                );

                return false;
            }

            return ReturnPrefab(poolLink);
        }

        /// <summary>
        ///     Return a borrowed <see cref="GameObject"/> back to the pool.
        /// </summary>
        public bool ReturnPrefab(GameObject previouslyPooledPrefab)
        {
            return ReturnPrefab(previouslyPooledPrefab.transform);
        }

        /// <summary>
        ///     Return a borrowed <see cref="GameObject"/> back to the pool.
        /// </summary>
        [System.Obsolete("Use ReturnPrefab() for improved code clarity.")]
        public bool ReturnObject(GameObject previouslyPooledPrefab)
        {
            return ReturnPrefab(previouslyPooledPrefab.transform);
        }

        /// <summary>
        /// Called immediately after a prefab is removed from the available
        /// prefabs list but before it is returned to the caller/borrower.
        /// </summary>
        protected virtual void OnBorrow(GameObject borrowedPrefab)
        {
            MelcherPrefabPoolLink poolLink =
                borrowedPrefab.GetComponent<MelcherPrefabPoolLink>();

            poolLink?.SetIsBorrowed(true);
        }

        /// <summary>
        /// Called immediately as as prefab is returned to its pool but before
        /// it is inserted back into the available prefab pool.
        /// </summary>
        /// <param name="poolLink">The PrefabPoolLink of the prefab being returned.</param>
        protected virtual void OnReturn(MelcherPrefabPoolLink poolLink)
        {
            if (poolLink != null)
            {
                poolLink.SetIsBorrowed(false);

                // Detach the unborrowed game object from its parent so it can
                // be moved back to the pool scene. This is done after
                // `SetIsBorrowed(false)` so the pool link can check the context
                // of the pool's transform, should it be needed.
                poolLink.gameObject.transform.SetParent(null, worldPositionStays: false);
                SceneManager.MoveGameObjectToScene(poolLink.gameObject, m_PoolScene);
            }
        }

        /// <summary>
        ///     Prepare the to-be-pooled prefab for instantiation.
        /// </summary>
        protected virtual void PreInstantiatePooledPrefabs()
        {
            // Activate the prefab before instantiating it. This ensures all
            // its Awake() methods are called instantly to avoid loading
            // hitches that would occur if it were waked during gameplay.
            m_WasPrefabActive = m_PrefabToPool.activeSelf;

            m_PrefabToPool.SetActive(true);
        }

        protected virtual void PostInstantiatePooledPrefabs()
        {
            m_PrefabToPool.SetActive(m_WasPrefabActive);
        }

        protected virtual GameObject InstantiatePrefab()
        {
            GameObject pooledObject = GameObject.Instantiate(m_PrefabToPool);
            SceneManager.MoveGameObjectToScene(pooledObject, m_PoolScene);

            if (!pooledObject.TryGetComponent(out MelcherPrefabPoolLink poolLink))
            {
                poolLink = pooledObject.AddComponent<MelcherPrefabPoolLink_ToggleGameObject>();
            }

            poolLink.SetPoolInstanceID(GetEntityId());

            return pooledObject;
        }

        /// <summary>
        ///     Rename all prefabs in the pool to clean up the "(clone)"
        ///     suffix. The prefab instances are named after the prefab asset
        ///     with their index appended. E.g. "&lt;PrefabName&gt;_0"
        /// </summary>
        protected virtual void RenamePooledPrefabs()
        {
            string prefabName = m_PrefabToPool.name;

            int stringBufferSize = prefabName.Length + 8;

            // Make a StringBuilder to use for renaming
            StringBuilder sb = new StringBuilder(prefabName, stringBufferSize);
            sb.Append("_");

            IReadOnlyCollection<GameObject> prefabs = m_ObjectPool.allObjects;

            int prefixLength = sb.Length;
            int i = 0;
            foreach (GameObject prefab in prefabs)
            {
                sb.Length = prefixLength;
                sb.Append(i);
                prefab.name = sb.ToString();
                ++i;
            }
        }

        /// <summary>
        ///     This function prevents unloading scenes from destroying pooled
        ///     objects that were borrowed, moved into the unloading scene and
        ///     never moved back.
        ///     <br /><br />
        ///
        ///     It is necessary this happens before before a scene begins
        ///     unloading because Unity will throw an exception when an object
        ///     is re-parented (e.g., to move it back to the pool scene),
        ///     after an unload has started. This avoids the error:
        ///     "Cannot change GameObject hierarchy while activating or
        ///     deactivating the parent."
        /// </summary>
        /// <param name="unloadingScene"></param>
        protected virtual void OnSceneUnloading(Scene unloadingScene)
        {
            SceneHandle unloadingSceneHandle = unloadingScene.handle;

            foreach (GameObject go in m_ObjectPool.allObjects)
            {
                if (go.scene.handle == unloadingSceneHandle)
                {
                    ReturnPrefab(go);

                    go.transform.SetParent(null, worldPositionStays: false);
                    SceneManager.MoveGameObjectToScene(go, m_PoolScene);
                }
            }
        }

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [SerializeField]
        protected GameObject m_PrefabToPool = default;

        [SerializeField]
        protected int m_Capacity = 64;

        [SerializeField]
        protected bool m_IsCyclical;

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        /// <summary>
        /// The unique scene created to hold this pool's objects.
        /// </summary>
        protected Scene m_PoolScene = default;

        /// <summary>
        /// The underlying pool strategy. Currently this is either a stack-based
        /// pool (first-in, last-out) or a cyclic pool where objects are
        /// reused even if they haven't been returned to the pool yet.
        /// </summary>
        protected IMelcherObjectPool<GameObject> m_ObjectPool = default;

        /// <summary>
        /// Whether the seed prefab is marked active. The pool forces the
        /// prefab to be active as its instantiating prefabs. This is used to
        /// reset the prefab to its original activeness state.
        /// </summary>
        protected bool m_WasPrefabActive;

        /// <summary>
        ///     The number of users of this pool. Its purpose is to monitor
        ///     how many places the pool is used and to appropriately create or
        ///     destroy the pooled objects when the user count transitions
        ///     between 0 and 1.
        /// </summary>
        protected int m_UseCaseCount;

        // ====================================================================
        // Static methods
        // ====================================================================

        public static bool ReturnObjectToPool(EntityId poolInstanceId, MelcherPrefabPoolLink borrowedObject)
        {
            MelcherPrefabPool prefabPool =
                FindInUsePoolByInstanceID(poolInstanceId);

            if (prefabPool != null)
            {
                return prefabPool.ReturnPrefab(borrowedObject);
            }

            return false;
        }

        public static MelcherPrefabPool FindInUsePoolByInstanceID(EntityId poolInstanceId)
        {
            int poolCount = s_InUsePrefabPools.Count;

            foreach (MelcherPrefabPool pool in s_InUsePrefabPools)
            {
                if (pool.GetEntityId().Equals(poolInstanceId))
                {
                    return pool;
                }
            }

            return null;
        }

        // ====================================================================
        // Static fields
        // ====================================================================

        /// <summary>
        ///     The prefab pools that are currently loaded. I.e., they called
        ///     OnEnable() but not OnDisable() yet. The elements are stored as
        ///     WeakReferences to avoid keeping the prefab pool alive beyond
        ///     their intended scope.
        /// </summary>
        [MelcherStaticFieldEditorTreatment.ClearCollection]
        protected static HashSet<MelcherPrefabPool> s_InUsePrefabPools =
            new HashSet<MelcherPrefabPool>();

    }   // class MelcherPrefabPool

}   // namespace Melcher
