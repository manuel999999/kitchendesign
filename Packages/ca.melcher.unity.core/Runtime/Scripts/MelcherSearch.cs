namespace Melcher
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// <see cref="MelcherSearch"/> is a full replacement for Unity's default
    /// <see cref="Component"/> search functions. This class can do anything
    /// <see cref="GameObject.GetComponent{T}"/> and its variations do, plus
    /// more!
    ///
    /// <see cref="MelcherSearch"/> is used throughout MelcherCore. E.g., it is
    /// used in the implementation of <see cref="MelcherFindComponentAttribute"/>.
    ///
    /// Features:
    ///     - Unified API
    ///     - Can search scenes and transform hierarchies
    ///     - Can search via a generic (including interfaces) or a System.Type
    ///       argument
    ///     - Can fill a list with transforms in breadth-first order
    ///     - Hierarchy can be searched in any direction (up or down)
    ///     - Searches can be be started at an offset from a transform and/or
    ///       limited in their traversal depth
    ///     - Uses more intuitive function names*
    ///     - Provides more search options like includeInactiveGameObjects and
    ///       includeInactiveComponents to control which objects are valid
    ///       search candidates
    ///
    /// (*) As noticed by other developers, Unity's GetComponentsInChildren,
    /// GetComponentInParents, and their variations, have a sneaky quirk in that
    /// they also search the invoker's transform. The public names chosen for
    /// this class were chosen to offer more transparency -- that the hierarchy
    /// traversals do not just search parents or children.
    /// </summary>
    public static class MelcherSearch
    {
        // =====================================================================
        // FirstFromAnyScene
        // =====================================================================

        /// <summary>
        /// Find the first <see cref="Component"/> or <see langword="interface"/>
        /// of the requested type from any scene that is loaded. Scenes are
        /// checked in the order they were loaded. The scenes' game object
        /// hierarchies are searched using pre-order traversal.
        /// </summary>
        /// <param name="minDistance">
        /// The number of hierarchy levels that the search will be offset by
        /// from the root game objects. E.g., a value of 0 means start searching
        /// from the scenes' root level game objects; and a value of 10 means
        /// start searching from the root level game objects' 10th-generation
        /// children.
        /// </param>
        /// <param name="count">
        /// The maximum number of hierarchy levels that will be searched. This
        /// value is relative to the starting point identified by <paramref
        /// name="minDistance"/>. E.g., if this is 1, only one hierarchy level
        /// is searched and none of its children. A value of 0 means no
        /// hierarchy levels will be searched and thus the function will always
        /// return <see langword="null"/>. The default value is a negative
        /// number which is interpreted as "no limit on depth".
        /// </param>
        /// <param name="includeInactiveGameObjects">
        /// If <see langword="true"/>, inactive game objects (i.e. those where
        /// GameObject.isActiveInHierarchy returns false) will be included in
        /// the search. Otherwise, inactive game objects are excluded and none
        /// of their children are searched.
        /// </param>
        /// <param name="includeInactiveComponents">
        /// If <see langword="true"/>, inactive components (i.e. those where
        /// <see cref="MelcherUnityUtility.IsComponentEnabled(Component)"/>
        /// returns <see langword="false"/>) will be included in the search.
        /// Otherwise, inactive components are excluded and will never be
        /// returned by the search.
        /// </param>
        /// <typeparam name="ComponentOrInterface">
        /// Specifies the type of <see cref="Component"/> to find. Can also be
        /// an <see langword="interface"/> to find components implementing
        /// the interface.
        /// </typeparam>
        /// <returns>
        /// The first pre-traversal component from all scenes (in the order
        /// they were loaded) that matches the specified requirements about
        /// component type, hierarchy depth, and game object & component
        /// activeness.
        /// </returns>
        public static ComponentOrInterface FirstFromAnyScene<ComponentOrInterface>(
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            ComponentOrInterface result = default;

            MelcherSceneEnumerator loadedScenes = new MelcherSceneEnumerator();

            while (result == default && loadedScenes.MoveNext())
            {
                result = FirstFromScene<ComponentOrInterface>(
                    loadedScenes.Current,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return result;
        }

        /// <inheritdoc cref="FirstFromAnyScene{ComponentOrInterface}(int, int, bool, bool)"/>
        public static Component FirstFromAnyScene(
            System.Type componentType,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            Component result = default;

            MelcherSceneEnumerator loadedScenes = new MelcherSceneEnumerator();

            while (result == default && loadedScenes.MoveNext())
            {
                result = FirstFromScene(
                    componentType,
                    loadedScenes.Current,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return result;
        }

        // =====================================================================
        // FirstFromScene
        // =====================================================================

        public static ComponentOrInterface FirstFromScene<ComponentOrInterface>(
            Scene sceneToSearch,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            if (!sceneToSearch.IsValid())
            {
                throw new System.ArgumentException($"!{nameof(sceneToSearch)}.IsValid()", nameof(sceneToSearch));
            }

            ComponentOrInterface result = null;

            SceneRootGameObjectEnumerator enumerator = new SceneRootGameObjectEnumerator(sceneToSearch);

            while (null == result && enumerator.MoveNext())
            {
                result = FirstFromTransformDownward<ComponentOrInterface>(
                    searchFrom: enumerator.Current.transform,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return result;
        }

        public static Component FirstFromScene(
            System.Type componentType,
            Scene sceneToSearch,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            if (!sceneToSearch.IsValid())
            {
                throw new System.ArgumentException($"!{nameof(sceneToSearch)}.IsValid()", nameof(sceneToSearch));
            }

            Component result = null;

            SceneRootGameObjectEnumerator enumerator = new SceneRootGameObjectEnumerator(sceneToSearch);

            while (null == result && enumerator.MoveNext())
            {
                result = FirstFromTransformDownward(
                    componentType,
                    searchFrom: enumerator.Current.transform,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return result;
        }

        // =====================================================================
        // AllFromAllScenes
        // =====================================================================

        public static int AllFromAllScenes<ComponentOrInterface>(
            List<ComponentOrInterface> results,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            if (null == results)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            int initialResultsCount = results.Count;

            MelcherSceneEnumerator loadedScenes = new MelcherSceneEnumerator();

            while (loadedScenes.MoveNext())
            {
                AllFromScene(
                    results,
                    sceneToSearch: loadedScenes.Current,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return results.Count - initialResultsCount;
        }

        public static int AllFromAllScenes(
            List<Component> results,
            System.Type componentType,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            if (null == results)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            int initialResultsCount = results.Count;

            MelcherSceneEnumerator loadedScenes = new MelcherSceneEnumerator();

            while (loadedScenes.MoveNext())
            {
                AllFromScene(
                    componentType,
                    results,
                    sceneToSearch: loadedScenes.Current,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return results.Count - initialResultsCount;
        }

        // =====================================================================
        // AllFromScene
        // =====================================================================

        public static int AllFromScene<ComponentOrInterface>(
            List<ComponentOrInterface> results,
            Scene sceneToSearch,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            if (null == results)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            if (!sceneToSearch.IsValid())
            {
                throw new System.ArgumentException($"!{nameof(sceneToSearch)}.IsValid()", nameof(sceneToSearch));
            }

            int initialResultsCount = results.Count;

            SceneRootGameObjectEnumerator enumerator = new SceneRootGameObjectEnumerator(sceneToSearch);

            while (enumerator.MoveNext())
            {
                AllFromTransformDownward(
                    results,
                    searchFrom: enumerator.Current.transform,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return results.Count - initialResultsCount;
        }

        public static int AllFromScene(
            System.Type componentType,
            List<Component> results,
            Scene sceneToSearch,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            if (null == results)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            if (!sceneToSearch.IsValid())
            {
                throw new System.ArgumentException($"!{nameof(sceneToSearch)}.IsValid()", nameof(sceneToSearch));
            }

            int initialResultsCount = results.Count;

            SceneRootGameObjectEnumerator enumerator = new SceneRootGameObjectEnumerator(sceneToSearch);

            while (enumerator.MoveNext())
            {
                AllFromTransformDownward(
                    componentType,
                    results,
                    searchFrom: enumerator.Current.transform,
                    minDistance,
                    count,
                    includeInactiveGameObjects,
                    includeInactiveComponents
                );
            }

            return results.Count - initialResultsCount;
        }

        // =====================================================================
        // FirstFromTransformDownward
        // =====================================================================

        public static ComponentOrInterface FirstFromTransformDownward<ComponentOrInterface>(
            Transform   searchFrom,
            int         minDistance = 0,
            int         count = -1,
            bool        includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool        includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            GetOneComponentHandler<ComponentOrInterface> finder =
                GetOneEnabledComponent<ComponentOrInterface>;

            if (includeInactiveComponents)
            {
                finder = GetOneComponent<ComponentOrInterface>;
            }

            return FindDownOne(finder, typeof(ComponentOrInterface), searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        public static Component FirstFromTransformDownward(
            System.Type componentType,
            Transform   searchFrom,
            int         minDistance = 0,
            int         count = -1,
            bool        includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool        includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            GetOneComponentHandler<Component> finder = GetOneEnabledComponent;

            if (includeInactiveComponents)
            {
                finder = GetOneComponent;
            }

            return FindDownOne(finder, componentType, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        // =====================================================================
        // AllFromTransformDownward
        // =====================================================================

        public static int AllFromTransformDownward<ComponentOrInterface>(
            List<ComponentOrInterface> results,
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            GetAllComponentsHandler<ComponentOrInterface> finder =
                GetAllEnabledComponents<ComponentOrInterface>;

            if (includeInactiveComponents)
            {
                finder = GetAllComponents<ComponentOrInterface>;
            }

            return FindDownAll(finder, typeof(ComponentOrInterface), results, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        public static int AllFromTransformDownward(
            System.Type componentType,
            List<Component> results,
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            GetAllComponentsHandler<Component> finder = GetAllEnabledComponents;

            if (includeInactiveComponents)
            {
                finder = GetAllComponents;
            }

            return FindDownAll(finder, componentType, results, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        // =====================================================================
        // FirstFromTransformUpward
        // =====================================================================

        public static ComponentOrInterface FirstFromTransformUpward<ComponentOrInterface>(
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            GetOneComponentHandler<ComponentOrInterface> finder =
                GetOneEnabledComponent<ComponentOrInterface>;

            if (includeInactiveComponents)
            {
                finder = GetOneComponent<ComponentOrInterface>;
            }

            return FindUpOne(finder, typeof(ComponentOrInterface), searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        public static Component FirstFromTransformUpward(
            System.Type componentType,
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            GetOneComponentHandler<Component> finder = GetOneEnabledComponent;

            if (includeInactiveComponents)
            {
                finder = GetOneComponent;
            }

            return FindUpOne(finder, componentType, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        // =====================================================================
        // AllFromTransformUpward
        // =====================================================================

        public static int AllFromTransformUpward<ComponentOrInterface>(
            List<ComponentOrInterface> results,
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
            where ComponentOrInterface : class
        {
            GetAllComponentsHandler<ComponentOrInterface> finder =
                GetAllEnabledComponents<ComponentOrInterface>;

            if (includeInactiveComponents)
            {
                finder = GetAllComponents<ComponentOrInterface>;
            }

            return FindUpAll(finder, typeof(ComponentOrInterface), results, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        public static int AllFromTransformUpward(
            System.Type componentType,
            List<Component> results,
            Transform searchFrom,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = DefaultIncludeInactiveComponents)
        {
            GetAllComponentsHandler<Component> finder = GetAllEnabledComponents;

            if (includeInactiveComponents)
            {
                finder = GetAllComponents;
            }

            return FindUpAll(finder, componentType, results, searchFrom, minDistance, count, includeInactiveGameObjects);
        }

        // =====================================================================
        // GetBreadthFirstHierarchy
        // =====================================================================

        /// <summary>
        /// Perform a downward (the only meaningful direction) breadth-first
        /// traversal and append any children that conform to the requirements
        /// imposed by <paramref name="minOffset"/> and <paramref name="count"/> to
        /// <paramref name="results"/>.
        /// </summary>
        /// <param name="searchFrom">
        /// The transform where the breadth-first traversal will begin.
        /// </param>
        /// <param name="minOffset">
        /// The number of initial hierarchy steps that will be ignored from the
        /// starting point. E.g. 0 means include the starting point, 1 means
        /// exclude the starting point and start with its children, etc.
        /// </param>
        /// <param name="count">
        /// The number of hierarchy steps downward to consider, starting from
        /// the offset starting point. A negative value means consider all
        /// children regardless of depth.
        /// </param>
        /// <param name="includeInactiveGameObjects">
        /// Whether inactive game objects should be considered in the search.
        /// </param>
        /// <returns>
        /// The number of matching transforms that were appended to
        /// <paramref name="results"/>.
        /// </returns>
        public static int
        GetDownwardBreadthFirstHierarchy(
            List<Transform> results,
            Transform       searchFrom,
            int             minOffset = 0,
            int             count = -1,
            bool            includeInactiveGameObjects = DefaultIncludeInactiveGameObjects)
        {
            if (results == null)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            if (searchFrom == null)
            {
                throw new System.ArgumentNullException(nameof(searchFrom));
            }

            // To avoid extra GC, we use a static list. This optimization
            // imposes a requirement that this function is only ever called on
            // the main thread
            s_BreadthFirstInfo.Clear();

            if (count != 0 && (includeInactiveGameObjects || searchFrom.gameObject.activeInHierarchy))
            {
                int currentTraversalDepth = 0;

                s_BreadthFirstInfo.Add(new BreadthOrderInfo(searchFrom, currentTraversalDepth));

                int hierarchyStepBegin = 0;

                while (count != 0)
                {
                    ++currentTraversalDepth;

                    // Iterate over the children that were last added
                    int hierarchyStepEnd = s_BreadthFirstInfo.Count;

                    for (int i = hierarchyStepBegin; i < hierarchyStepEnd; ++i)
                    {
                        foreach (Transform child in s_BreadthFirstInfo[i].transform)
                        {
                            if (!child.gameObject.activeSelf && !includeInactiveGameObjects)
                            {
                                // Skip inactive objects if requested
                                continue;
                            }

                            s_BreadthFirstInfo.Add(new BreadthOrderInfo(child, currentTraversalDepth));
                        }
                    }

                    if (count > 0)
                    {
                        --count;
                    }

                    // Prepare the next `i` loop to begin where this loop
                    // ended
                    hierarchyStepBegin = hierarchyStepEnd;
                }
            }

            // Grab the initial length to later find the number that were added
            int initialResultsCount = results.Count;

            int breadthOrderCount = s_BreadthFirstInfo.Count;
            for (int i = 0; i < breadthOrderCount; ++i)
            {
                if (s_BreadthFirstInfo[i].relativeDepth >= minOffset)
                {
                    results.Add(s_BreadthFirstInfo[i].transform);
                }
            }

            // Return the number of transforms that were added
            return results.Count - initialResultsCount;
        }

        // =====================================================================
        // Shared implementation
        // ---------------------------------------------------------------------
        //
        // Rather than write the same boilerplate code in every public API,
        // these private functions allow the core search algorithms to be
        // re-used; swapping the "component getter" part for a generic argument
        // version or a System.Type version.
        // =====================================================================

        private static ComponentType FindDownOne<ComponentType>(
            GetOneComponentHandler<ComponentType> componentGetter,
            System.Type componentType,
            Transform searchFrom,
            int minDistance,
            int count,
            bool includeInactiveGameObjects)
        {
            ComponentType result = default;

            if (minDistance <= 0 && count != 0)
            {
                result = componentGetter.Invoke(searchFrom, componentType);
                if (result != null)
                {
                    return result;
                }
            }

            // Decrement `count` by one, but only when minDistance has also
            // been fully decremented (i.e., min distance is at or below zero)
            if (minDistance <= 0)
            {
                --count;
            }

            // Decrement `minDistance` by one, but skip if it's already at or
            // below zero
            if (minDistance > 0)
            {
                --minDistance;
            }

            // Depth < 0 means traverse everything.
            if (count != 0 && (includeInactiveGameObjects || searchFrom.gameObject.activeInHierarchy))
            {
                foreach (Transform child in searchFrom)
                {
                    if (!child.gameObject.activeSelf && !includeInactiveGameObjects)
                    {
                        // Skip inactive objects when requested
                        continue;
                    }

                    result = FindDownOne(
                        componentGetter,
                        componentType,
                        searchFrom: child,
                        minDistance: minDistance,
                        count: count,
                        includeInactiveGameObjects: includeInactiveGameObjects
                    );

                    if (result != null)
                    {
                        // Found something. Stop searching
                        break;
                    }
                }
            }

            return result;
        }

        private static int FindDownAll<ComponentType>(
            GetAllComponentsHandler<ComponentType> componentGetter,
            System.Type componentType,
            List<ComponentType> results,
            Transform searchFrom,
            int minDistance,
            int count,
            bool includeInactiveGameObjects)
        {
            if (results == null)
            {
                throw new System.ArgumentNullException(nameof(results));
            }

            if (minDistance <= 0 && count != 0)
            {
                results.AddRange(componentGetter.Invoke(searchFrom, componentType));
            }

            // Decrement `count` by one, but only when minDistance has also
            // been fully decremented (i.e., min distance is at or below zero)
            if (minDistance <= 0)
            {
                --count;
            }

            // Decrement `minDistance` by one, but skip if it's already at or
            // below zero
            if (minDistance > 0)
            {
                --minDistance;
            }

            int initialResultsCount = results.Count;

            // Depth < 0 means traverse everything.
            if (count != 0 && (includeInactiveGameObjects || searchFrom.gameObject.activeInHierarchy))
            {
                foreach (Transform child in searchFrom)
                {
                    if (!child.gameObject.activeSelf && !includeInactiveGameObjects)
                    {
                        // Skip inactive objects if requested
                        continue;
                    }

                    FindDownAll(
                        componentGetter,
                        componentType,
                        results,
                        searchFrom: child,
                        minDistance,
                        count,
                        includeInactiveGameObjects
                    );
                }
            }

            return results.Count - initialResultsCount;
        }

        private static ComponentType FindUpOne<ComponentType>(
            GetOneComponentHandler<ComponentType> componentGetter,
            System.Type componentType,
            Transform searchFrom,
            int minDistance,
            int count,
            bool includeInactiveGameObjects)
        {
            ComponentType result = default;

            // Skip over the requested distance
            while (minDistance > 0 && searchFrom)
            {
                searchFrom = searchFrom.parent;
                --minDistance;
            }

            while (count != 0 && searchFrom != null && result == null)
            {
                if (includeInactiveGameObjects || searchFrom.gameObject.activeInHierarchy)
                {
                    result = componentGetter.Invoke(searchFrom, componentType);
                }

                searchFrom = searchFrom.parent;

                if (count > 0)
                {
                    --count;
                }
            }

            return result;
        }

        private static int FindUpAll<ComponentType>(
            GetAllComponentsHandler<ComponentType> componentGetter,
            System.Type componentType,
            List<ComponentType> results,
            Transform searchFrom,
            int minDistance,
            int count,
            bool includeInactiveGameObjects)
        {
            // Skip over the requested distance
            while (minDistance > 0 && searchFrom != null)
            {
                searchFrom = searchFrom.parent;
                --minDistance;
            }

            int initialResultsCount = results.Count;

            while (count != 0 && searchFrom != null)
            {
                if (includeInactiveGameObjects || searchFrom.gameObject.activeInHierarchy)
                {
                    results.AddRange(componentGetter.Invoke(searchFrom, componentType));
                }

                searchFrom = searchFrom.parent;

                if (count > 0)
                {
                    --count;
                }
            }

            return results.Count - initialResultsCount;
        }

        // =====================================================================
        // FindOneComponentHandlers
        // ---------------------------------------------------------------------
        //
        // These are the low-level "component getters" that take a single game
        // object and return a single component attached to it. There is another
        // variation of "component getter" that searches for multiple
        // components.
        // =====================================================================

        /// <summary>
        /// Return one component from <paramref name="transformToSearch"/>'s
        /// game object that matches the generic <typeparamref
        /// name="ComponentOrInterface"/> argument.
        ///
        /// The <paramref name="componentType"/> argument is ignored.
        ///
        /// This implements <see cref="GetOneComponentHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownOne{ComponentType}"/>.
        /// </summary>
        private static ComponentOrInterface GetOneComponent<ComponentOrInterface>(Transform transformToSearch, System.Type componentType)
            where ComponentOrInterface : class
        {
            transformToSearch.TryGetComponent(out ComponentOrInterface c);
            return c;
        }

        /// <summary>
        /// Return one component from <paramref name="transformToSearch"/>'s
        /// game object that matches the type specified by <paramref
        /// name="componentType"/>.
        ///
        /// This implements <see cref="GetOneComponentHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownOne{ComponentType}"/>.
        /// </summary>
        private static Component GetOneComponent(Transform transformToSearch, System.Type componentType)
        {
            transformToSearch.TryGetComponent(componentType, out Component c);
            return c;
        }

        /// <summary>
        /// Return one *enabled* component from <paramref
        /// name="transformToSearch"/>'s game object that matches the generic
        /// <typeparamref name="ComponentOrInterface"/> argument.
        ///
        /// The components' enabled-ness is evaluated using <see
        /// cref="MelcherUnityUtility.IsComponentEnabled(Component)"/>.
        ///
        /// The <paramref name="componentType"/> argument is ignored.
        ///
        /// This implements <see cref="GetOneComponentHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownOne{ComponentType}"/>.
        /// </summary>
        private static ComponentOrInterface GetOneEnabledComponent<ComponentOrInterface>(Transform transformToSearch, System.Type componentType)
            where ComponentOrInterface : class
        {
            // We must get all components to iterate them (in case the first
            // component is disabled but another is enabled)
            IEnumerable<ComponentOrInterface> results =
                GetAllComponents<ComponentOrInterface>(transformToSearch, componentType);

            foreach (ComponentOrInterface result in results)
            {
                if (MelcherUnityUtility.IsComponentEnabled(result as Component))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Return one *enabled* component from <paramref
        /// name="transformToSearch"/>'s game object that matches the type
        /// specified by <paramref name="componentType"/>.
        ///
        /// The components' enabled-ness is evaluated using <see
        /// cref="MelcherUnityUtility.IsComponentEnabled(Component)"/>.
        ///
        /// This implements <see cref="GetOneComponentHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownOne{ComponentType}"/>.
        /// </summary>
        private static Component GetOneEnabledComponent(Transform transformToSearch, System.Type componentType)
        {
            // We must get all components to iterate them (in case the first
            // component is disabled but another is enabled)
            transformToSearch.GetComponents<Component>(s_SharedComponentResults);

            foreach (Component result in s_SharedComponentResults)
            {
                if (MelcherUnityUtility.IsComponentEnabled(result))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// A component-getter delegate type. This type of getter is intended
        /// to find *one* component of a specified type on a single game object.
        /// Different implementations can be made to define different criteria.
        /// Pass an implementation to one of the search functions to alter its
        /// behaviour.
        /// </summary>
        private delegate ComponentType GetOneComponentHandler<ComponentType>(Transform searchFrom, System.Type componentType);

        // =====================================================================
        // GetAllComponentsHandler
        // ---------------------------------------------------------------------
        //
        // The "component getters" that return multiple components from a single
        // game object. There are two variations and both implement the
        // GetAllComponentsHandler delegate. This allows them to be passed
        // interchangeably to the search algorithm functions. One version uses
        // generics and the other uses System.Type.
        // =====================================================================

        /// <summary>
        /// Return all components from <paramref name="transformToSearch"/>'s
        /// game object that match the generic <typeparamref
        /// name="ComponentOrInterface"/> argument.
        ///
        /// The <paramref name="componentType"/> argument is ignored.
        ///
        /// This implements <see cref="GetAllComponentsHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownAll{ComponentType}"/>.
        /// </summary>
        private static IEnumerable<ComponentOrInterface> GetAllComponents<ComponentOrInterface>(Transform transformToSearch, System.Type componentType)
            where ComponentOrInterface : class
        {
            return transformToSearch.GetComponents<ComponentOrInterface>();
        }

        /// <summary>
        /// Return all components from <paramref name="transformToSearch"/>'s
        /// game object that match the type specified by <paramref
        /// name="componentType"/>.
        ///
        /// This implements <see cref="GetAllComponentsHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownAll{ComponentType}"/>.
        /// </summary>
        private static IEnumerable<Component> GetAllComponents(Transform transformToSearch, System.Type componentType)
        {
            return transformToSearch.GetComponents(componentType);
        }

        /// <summary>
        /// Return all *enabled* components from <paramref
        /// name="transformToSearch"/>'s game object that match the generic
        /// <typeparamref name="ComponentOrInterface"/> argument.
        ///
        /// The components' enabled-ness is evaluated using <see
        /// cref="MelcherUnityUtility.IsComponentEnabled(Component)"/>.
        ///
        /// The <paramref name="componentType"/> argument is ignored.
        ///
        /// This implements <see cref="GetAllComponentsHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownAll{ComponentType}"/>.
        /// </summary>
        private static IEnumerable<ComponentOrInterface> GetAllEnabledComponents<ComponentOrInterface>(Transform transformToSearch, System.Type componentType)
            where ComponentOrInterface : class
        {
            return transformToSearch.GetComponents<ComponentOrInterface>()
                .Where(x => MelcherUnityUtility.IsComponentEnabled(x as Component));
        }

        /// <summary>
        /// Return all *enabled* components from <paramref
        /// name="transformToSearch"/>'s game object that match the type
        /// specified by <paramref name="componentType"/>.
        ///
        /// The components' enabled-ness is evaluated using <see
        /// cref="MelcherUnityUtility.IsComponentEnabled(Component)"/>.
        ///
        /// This implements <see cref="GetAllComponentsHandler{ComponentType}"/>
        /// and can be used in functions taking a delegate of that type. E.g.,
        /// <see cref="FindDownAll{ComponentType}"/>.
        /// </summary>
        private static IEnumerable<Component> GetAllEnabledComponents(Transform transformToSearch, System.Type componentType)
        {
            transformToSearch.GetComponents<Component>(s_SharedComponentResults);
            return s_SharedComponentResults.Where(x => MelcherUnityUtility.IsComponentEnabled(x));
        }

        /// <summary>
        /// A component-getter delegate type. This type of getter is intended
        /// to find *all* components of a specified type on a single game
        /// object. Different implementations can be made to define different
        /// criteria. Pass an implementation to one of the search functions to
        /// alter its behaviour.
        /// </summary>
        private delegate IEnumerable<ComponentType> GetAllComponentsHandler<ComponentType>(Transform searchFrom, System.Type componentType);

        // =====================================================================
        // Static fields
        // =====================================================================

        public const bool DefaultIncludeInactiveGameObjects = false;
        public const bool DefaultIncludeInactiveComponents = true;

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static List<Component> s_SharedComponentResults = new List<Component>();

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static List<BreadthOrderInfo> s_BreadthFirstInfo = new List<BreadthOrderInfo>();

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static List<Transform> s_BreadthFirstResults = new List<Transform>();

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// A helper struct for assembling the breadth-first order of a
        /// transform hierarchy. Each node memorizes a transform and its
        /// relative depth from the search's starting point.
        /// </summary>
        private readonly struct BreadthOrderInfo
        {
            public BreadthOrderInfo(Transform transform, int relativeDepth)
            {
                this.transform = transform;
                this.relativeDepth = relativeDepth;
            }

            public readonly Transform transform;
            public readonly int relativeDepth;

        }   // readonly struct BreadthOrderInfo

        /// <summary>
        ///     Iterate all root-level GameObjects in a scene.
        /// </summary>
        private struct SceneRootGameObjectEnumerator
        {
            // =====================================================================
            // Constructors
            // =====================================================================

            public SceneRootGameObjectEnumerator(Scene scene)
            {
                scene.GetRootGameObjects(s_SceneRootGameObjects);

                m_Current = null;
                m_NextIndex = 0;
            }

            // =====================================================================
            // Properties
            // =====================================================================

            public GameObject Current => m_Current;

            // =====================================================================
            // Methods
            // =====================================================================

            public bool MoveNext()
            {
                m_Current = null;

                if (m_NextIndex < s_SceneRootGameObjects.Count)
                {
                    m_Current = s_SceneRootGameObjects[m_NextIndex];
                    ++m_NextIndex;
                }

                return null != m_Current;
            }

            public void Reset()
            {
                m_Current = null;
                m_NextIndex = 0;
            }

            // =====================================================================
            // Static fields
            // =====================================================================

            [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
            private static readonly List<GameObject> s_SceneRootGameObjects =
                new List<GameObject>();

            // =====================================================================
            // Internal fields
            // =====================================================================

            private GameObject m_Current;
            private int m_NextIndex;

        }   // struct SceneRootGameObjectEnumerator

    }   // class MelcherSearch

}   // namespace Melcher

