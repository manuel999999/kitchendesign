namespace KitchenDesign
{
    using Melcher;
    public class ListControllerSortMode
    {
        /// <summary>
        /// A-Z
        /// </summary>
        public static readonly ListControllerSortMode NameAscending =
            new ListControllerSortMode("Name <size=125%>$(arrow-down-a-z)</size>");

        /// <summary>
        /// Z-A
        /// </summary>
        public static readonly ListControllerSortMode NameDescending =
            new ListControllerSortMode("Name <size=125%>$(arrow-up-a-z)</size>");

        /// <summary>
        /// Oldest to newest
        /// </summary>
        public static readonly ListControllerSortMode DateAscending =
            new ListControllerSortMode("Oldest");

        /// <summary>
        /// Newest to oldest
        /// </summary>
        public static readonly ListControllerSortMode DateDescending =
            new ListControllerSortMode("Newest");

        private ListControllerSortMode(string phrase)
        {
            this.phrase = phrase;
        }

        public readonly string phrase;

    }   // class ListControllerSortMode

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-06-02
/// ===========================================================================
