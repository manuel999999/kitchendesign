// This is using a two-layer deep namespace to make the use of these extension
// methods more explicit in scripts. E.g., a `using Melcher.Linq` statement at
// the top of the file.
//
// This is intentional and is meant to make the programmer mindful that they are
// using methods with similar performance impact to those added by System.Linq.
namespace Melcher.Linq
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// This extension class is used to define functions that are more preferred
    /// than the default LINQ extension functions.
    /// </summary>
    public static class MelcherIEnumerableExtensions
    {
        /// <summary>
        ///     An optimized version of <see
        ///     cref="System.Linq.Enumerable.Count{TSource}(IEnumerable{TSource})"/>.
        ///     <br /><br />
        ///
        ///     This implementation can take advantage of the O(1) access to
        ///     <see cref="IReadOnlyCollection{T}.Count"/>. It also prioritizes
        ///     casts to the non-generic <see cref="IEnumerable"/> for up to
        ///     100x execution speed.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        ///     Thrown if <paramref name="enumerable"/> is <see
        ///     langword="null"/>.
        /// </exception>
        /// <exception cref="System.OverflowException">
        ///     Thrown if <paramref name="enumerable"/> is infinite or appears
        ///     infinite; i.e., it has more elements that can be counted using
        ///     an <see cref="int"/>.
        /// </exception>
        /// <param name="enumerable">
        ///     The sequence whose element count will be returned.
        /// </param>
        public static int MelcherCount<ElementType>(this IEnumerable<ElementType> enumerable)
        {
            // The implementation here is based on an example provided by SLaks:
            // https://blog.slaks.net/2015-01-12/linq-count-considered-occasionally-harmful

            if (enumerable == null)
            {
                throw new System.ArgumentNullException(nameof(enumerable));
            }

            if (enumerable is ICollection collection)
            {
                // The cast to a non-generic collection is prioritized because
                // it can be 100x faster than casting to the generic type.
                // https://www.danielcrabtree.com/blog/191/casting-to-ienumerable-t-is-two-orders-of-magnitude-slower

                return collection.Count;
            }
            else if (enumerable is ICollection<ElementType> genericCollection)
            {
                return genericCollection.Count;
            }
            else if (enumerable is IReadOnlyCollection<ElementType> readOnlyGenericCollection)
            {
                return readOnlyGenericCollection.Count;
            }

            int count = 0;

            using (var enumerator = enumerable.GetEnumerator())
            {
                checked
                {
                    while (enumerator.MoveNext())
                    {
                        ++count;
                    }
                }
            }

            return count;
        }
    }   // class MelcherIEnumerableExtensions

}   // namespace Melcher.Linq

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-11
/// ===========================================================================
