namespace KitchenDesign
{
    using Melcher;
    /// <summary>
    ///     A type that pairs any type with an index. It is originally intended
    ///     to be used as a type argument for <see cref="System.Predicate{T}"/>,
    ///     to include the element and index for the filter criteria. In that
    ///     case, having explicitly named member fields is much nicer than using
    ///     tuples.
    /// </summary>
    /// <typeparam name="ElementType"></typeparam>
    public struct ElementIndex<ElementType>
    {
        public ElementType element;
        public int index;
    }   // struct ElementIndex

    public class ElementIndex
    {
        public static ElementIndex<ElementType> From<ElementType>(ElementType element, int index)
        {
            return
                new ElementIndex<ElementType>()
                {
                    element = element,
                    index = index,
                };
        }

    }   // class ElementIndex

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2023-11-10
/// ===========================================================================
