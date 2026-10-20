namespace Melcher
{
    /// <summary>
    /// Auto-locates one or more <see cref="UnityEngine.Component">components</see>
    /// and assigns them to the decorated field during the OnValidate callback
    /// (editor only).
    /// <br /><br />
    ///
    /// This feature only works on scripts that derive from <see
    /// cref="MelcherMonoBehaviour"/>, <see cref="MelcherScriptableObject"/>,
    /// or that call <see
    /// cref="MelcherOnValidateAttribute.ValidateFields(UnityEngine.Object)"/>
    /// in their <c>OnValidate</c> method.
    /// </summary>
    public partial class MelcherFindComponentAttribute : MelcherOnValidateAttribute
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        /// <summary>
        ///     Request that a <see cref="UnityEngine.Component"/> field be
        ///     "found on validate".
        /// </summary>
        /// <param name="searchMode">
        ///     Define the type and direction of search to perform. The default
        ///     value is <see cref="MelcherFindComponentMode.Self"/>.
        /// </param>
        /// <param name="minDistance">
        ///     Define the minimum hierarchy distance of found components.
        ///     Objects whose hierarchy distance is closer than this will be
        ///     skipped.
        ///     <br /><br />
        ///
        ///     Examples: <br />
        ///     0 = Consider everything (i.e., all children or all parents).
        ///          This is the default. <br />
        ///     1 = Skip the caller and search its immediate parent or child. <br />
        ///     2 = Skip the caller, its immediate parents/children, and start
        ///         from the grandparents or grandchildren. <br />
        ///     etc.
        /// </param>
        /// <param name="count">
        ///     The limit of the number of hierarchy tiers to search.
        ///     <br /><br />
        ///
        ///     Examples: <br />
        ///     -1 = Search without limiting the hierarchy depth. This is the
        ///          default. <br />
        ///     0 = Search nothing <br />
        ///     1 = Search a single hierarchy tier. <br />
        ///     2 = Search at most 2 hierarchy tiers. <br />
        ///     etc.
        /// </param>
        /// <param name="includeInactiveGameObjects">
        ///     For downward searches (including scene searches), this value
        ///     decides if disabled game objects will be considered in the
        ///     search. Upward searches ignore this value.
        /// </param>
        /// <param name="includeInactiveComponents">
        ///     For any type of search, this value decides if disabled
        ///     components will be considered in the search.
        /// </param>
        /// <param name="prefabSearchRule">
        ///     For any prefab object that is validating, this parameter
        ///     defines its special search rules. By default, only prefab
        ///     assets will search for a value but this can be changed here.
        /// </param>
        /// <param name="errorIfFewerThan">
        ///     The number of components that must be found to prevent an error
        ///     message. If 0, it will never error. A negative value will
        ///     choose an appropriate value automatically based on the type of
        ///     search being performed: arrays and lists will default to 0
        ///     whereas a non-collection field will default to 1.
        /// </param>
        /// <param name="errorIfMoreThan">
        ///     The max number of components that can be found before an error
        ///     appears. If 0, it will error if anything is found. A negative
        ///     value will prevent it from erroring from any amount. This value
        ///     is ignored for non-collection fields.
        /// </param>
        public MelcherFindComponentAttribute(
            MelcherFindComponentMode searchMode = MelcherFindComponentMode.Self,
            int minDistance = 0,
            int count = -1,
            bool includeInactiveGameObjects = MelcherSearch.DefaultIncludeInactiveGameObjects,
            bool includeInactiveComponents = MelcherSearch.DefaultIncludeInactiveComponents,
            int errorIfFewerThan = -1,
            int errorIfMoreThan = -1,
            MelcherFindComponentSearchFromPrefabPermission prefabSearchRule = MelcherFindComponentSearchFromPrefabPermission.OnlyWithinRootAsset)
        {
            m_SearchMode = searchMode;
            m_MinDistance = minDistance;
            m_Count = count;
            m_IncludeInactiveGameObjects = includeInactiveGameObjects;
            m_IncludeInactiveComponents = includeInactiveComponents;
            m_ErrorIfFewerThan = errorIfFewerThan;
            m_ErrorIfMoreThan = errorIfMoreThan;
            m_SearchFromPrefabPermission = prefabSearchRule;
        }

        // =====================================================================
        // Internal members
        // =====================================================================

        /// <summary>
        /// The type of search to perform.
        /// </summary>
        private MelcherFindComponentMode m_SearchMode;

        /// <summary>
        /// For any search, this value indicates how many hierarchy steps away
        /// from the initial game object the search will begin. A value of
        /// 0 means "start from the calling game object." Depending on the value
        /// of <see cref="m_SearchMode"/>, this offset will either
        /// shift the search's start into the caller's parents or its children.
        /// </summary>
        private int m_MinDistance;

        /// <summary>
        /// The number of steps in the hierarchy that the search will consider,
        /// starting from the start position (and including any offset). E.g.,
        /// a value of 0 will limit the search to only the object(s) at the
        /// starting depth.
        /// </summary>
        private int m_Count;

        /// <summary>
        /// When true, disabled game objects will be included during the search.
        /// Otherwise, disabled game objects are skipped.
        /// </summary>
        private bool m_IncludeInactiveGameObjects;

        /// <summary>
        /// Whether disabled components should be found.
        /// </summary>
        private bool m_IncludeInactiveComponents;

        /// <summary>
        /// The minimum number of items the developer expects the search to
        /// find. If fewer than this amount is found, an error is reported. If
        /// this value is less than zero, a value is automatically selected
        /// based on the context (0 for collections, 1 for single items).
        /// </summary>
        private int m_ErrorIfFewerThan;

        /// <summary>
        /// The maximum number of items the developer expects the search to
        /// find. If more than this amount is found, an error is reported. If
        /// this value is less than zero, a value is automatically selected
        /// based on the context (int.MaxValue for collections). This value is
        /// ignored for single component searches.
        /// </summary>
        private int m_ErrorIfMoreThan;

        /// <summary>
        /// Special search rules that are applied when the calling object is any
        /// type of prefab. By default, prefabs will only search for a value
        /// within their own asset's hierarchy.
        /// </summary>
        private MelcherFindComponentSearchFromPrefabPermission m_SearchFromPrefabPermission;

    }   // class MelcherFindComponentAttribute

    /// <summary>
    /// Search methods within <see cref="MelcherFindComponentAttribute"/>.
    /// </summary>
    public enum MelcherFindComponentMode
    {
        /// <summary>
        /// Search the GameObject of the calling component.
        /// </summary>
        Self,

        /// <summary>
        /// Search the GameObject of the calling component and then down the
        /// hierarchy (into the children).
        /// </summary>
        Down,

        /// <summary>
        /// Search the GameObject of the calling component and then up the
        /// hierarchy (into the parents).
        /// </summary>
        Up,

        /// <summary>
        /// Search the entire scene. If the search is called within a prefab,
        /// the search will mimic <see cref="MelcherFindComponentMode.Down"/>
        /// from the object's root.
        /// </summary>
        Scene,

    }   // enum MelcherFindComponentMode

    /// <summary>
    ///     Search criteria for <see cref="MelcherFindComponentAttribute"/>
    ///     when the caller is some type of prefab.
    /// </summary>
    public enum MelcherFindComponentSearchFromPrefabPermission
    {
        /// <summary>
        ///     The most strict (and arguably) most correct behaviour.
        ///     <br /><br />
        ///
        ///     If the caller is any type of prefab, the search will only
        ///     proceed if the caller belongs to the root prefab. This
        ///     prevents prefab instances in a scene and prefab instances
        ///     within other prefab assets from overriding values that were
        ///     found in the root asset.
        /// </summary>
        OnlyWithinRootAsset,

        /// <summary>
        ///     The middle strictness. Occasionally useful with prefab variants
        ///     where a base prefab contains a script intended to locate
        ///     specialized components added by variants of the base prefab.
        ///     <br /><br />
        ///
        ///     If the caller is any type of prefab, the search will only
        ///     proceed if the caller is within a prefab asset. This prevents
        ///     prefab instances in a scene and any nested prefabs within those
        ///     from overriding values that were found in a prefab asset.
        /// </summary>
        OnlyWithinAsset,

        /// <summary>
        ///     The least strict and least recommended behaviour.
        ///     <br /><br />
        ///
        ///     If the caller is any type of prefab, regardless of whether it
        ///     is asset or instance, the search is permitted. This means
        ///     prefab assets, prefab instances within prefab assets, and
        ///     prefab instances within scenes will all search.
        /// </summary>
        Anywhere

    }   // enum MelcherFindComponentSearchFromPrefabPermission

}   // namespace Melcher
