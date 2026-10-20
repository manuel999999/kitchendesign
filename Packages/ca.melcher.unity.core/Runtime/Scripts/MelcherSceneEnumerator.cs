namespace Melcher
{
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Iterate scenes that are known to the <see cref="SceneManager"/>. This
    /// does *not* iterate the scenes-in-build list. Rather, this iterates
    /// scenes that are currently loaded using <see
    /// cref="SceneManager.GetSceneAt(int)"/>.
    /// <br /><br />
    ///
    /// The <see langword="default"/> enumerator traverses the scenes in the
    /// order they were loaded and only returns scenes whose <see
    /// cref="Scene.isLoaded"/> property returns <see langword="true"/>.
    /// <br /><br />
    ///
    /// The argument-accepting constructor allows the enumerator to be
    /// configured to return non-loaded scenes (e.g., loading and unloading)
    /// and/or alter the traversal order. Sometimes it is useful to iterate over
    /// scenes in the opposite order they were loaded. E.g., to find a scene
    /// that was just created.
    /// <br /><br />
    ///
    /// <code>
    /// // Iterate over loaded scenes in the order of oldest loaded to most recent
    /// MelcherSceneEnumerator scenes = new MelcherSceneEnumerator();
    /// while (scenes.MoveNext())
    /// {
    ///     Scene loadedScene = scenes.Current;
    /// }
    ///
    /// // or
    ///
    /// // Iterate over loaded and loading scenes in reverse order
    /// MelcherSceneEnumerator scenes =
    ///     new MelcherSceneEnumerator(includeNonLoaded: false, order: MelcherSceneEnumerator.Order.NewestToOldest);
    ///
    /// while (scenes.MoveNext())
    /// {
    ///     Scene scene = scenes.Current;
    /// }
    /// </code>
    /// </summary>
    public struct MelcherSceneEnumerator
    {
        // ====================================================================
        // Properties
        // ====================================================================

        public Scene Current => m_Current;

        // ====================================================================
        // Constructors
        // ====================================================================

        public MelcherSceneEnumerator(bool includeNonLoaded = false, Ordering order = Ordering.OldestToNewest)
        {
            m_IncludeNonLoaded = includeNonLoaded;
            m_SceneOrder = order;
            m_Current = default;
            m_NextIndex = 0;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        public bool MoveNext()
        {
            m_Current = default;

            while (m_NextIndex < SceneManager.sceneCount)
            {
                int sceneIndex = SceneIndexBySceneOrder(m_NextIndex, m_SceneOrder);
                ++m_NextIndex;

                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if (!m_IncludeNonLoaded && !scene.isLoaded)
                {
                    continue;
                }

                m_Current = scene;
                break;
            }

            return m_Current.IsValid();
        }

        public void Reset()
        {
            m_NextIndex = 0;
            m_Current = default;
        }

        public static int SceneIndexBySceneOrder(int index, Ordering order)
        {
            int sceneIndex;

            switch (order)
            {
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(m_SceneOrder), $"Unsupported {nameof(Ordering)} mode: {order}");

                case Ordering.OldestToNewest:
                    sceneIndex = index;
                    break;

                case Ordering.NewestToOldest:
                    sceneIndex = SceneManager.sceneCount - index - 1;
                    break;
            }

            return sceneIndex;
        }

        // ====================================================================
        // Internal fields
        // ====================================================================

        /// <summary>
        /// When this is <see langword="true"/>, the enumerator will return
        /// scenes that are not loaded (<see cref="Scene.isLoaded"/>).
        /// Otherwise, only scenes that are loaded are returned.
        /// </summary>
        private bool m_IncludeNonLoaded;

        /// <summary>
        /// This determines the order the scenes are enumerated in.
        /// </summary>
        private Ordering m_SceneOrder;

        /// <summary>
        /// The value of the most recent iteration. Its <see
        /// cref="Scene.IsValid"/> will return <see langword="true"/> if it is
        /// valid.
        /// </summary>
        private Scene m_Current;

        private int m_NextIndex;

        // ====================================================================
        // Nested types
        // ====================================================================

        public enum Ordering
        {
            /// <summary>
            /// Iterate over scenes in the order they were loaded.
            /// Oldest -> Newest
            /// </summary>
            OldestToNewest,

            /// <summary>
            /// Iterate over scenes in the reverse order they were loaded.
            /// Newest -> Oldest
            /// </summary>
            NewestToOldest,

        }   // enum Ordering

    }   // struct MelcherSceneEnumerator

}   // namespace Melcher
