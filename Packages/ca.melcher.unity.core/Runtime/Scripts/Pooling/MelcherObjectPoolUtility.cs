namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using Melcher.Linq;

    /// <summary>
    ///     This class contains useful functions for implementing a <see
    ///     cref="IMelcherObjectPool{ObjectType}"/>.
    /// </summary>
    public static class MelcherObjectPoolUtility
    {
        /// <summary>
        ///     Create an object pool from pre-created objects. The
        ///     constructed pool's capacity will match the length of
        ///     <paramref name="elements"/>.
        /// </summary>
        /// <param name="elements">
        ///     The pre-created objects that are available in the pool. The
        ///     number of elements in this collection determines the pool's
        ///     capacity.
        ///     <br /><br />
        ///
        ///     <see cref="System.InvalidOperationException"/> is thrown if
        ///     any element in the collection is <see langword="null"/>.
        /// </param>
        /// <param name="createCollection">
        ///     A function that creates the collection that will hold the
        ///     pooled objects.
        /// </param>
        public static CollectionType CreateForCollection<ObjectType, CollectionType>(
            IEnumerable<ObjectType>                                     elements,
            Func<int, CollectionType>                                   createCollection,
            AddElementToCollectionHandler<CollectionType, ObjectType>   addElementToCollection)
                where ObjectType : class
        {
            if (elements == null)
            {
                throw new System.ArgumentNullException(nameof(elements));
            }

            if (createCollection == null)
            {
                throw new System.ArgumentNullException(nameof(createCollection));
            }

            if (addElementToCollection == null)
            {
                throw new System.ArgumentNullException(nameof(addElementToCollection));
            }

            int elementCount;
            try
            {
                elementCount = elements.MelcherCount();
            }
            catch (System.OverflowException e)
            {
                // Rethrow a different exception with a more meaningful error
                // message. The OverflowException happens if `elements` is
                // infinite and we attempt to take its count.

                throw new System.ArgumentException($"{nameof(elements)} is infinite", nameof(elements), e);
            }

            CollectionType destination = createCollection.Invoke(elementCount);

            if (destination == null)
            {
                throw new System.ArgumentException($"{nameof(createCollection)} returned null", nameof(createCollection));
            }

            int elementIndex = 0;
            foreach (ObjectType element in elements)
            {
                if (element == null)
                {
                    throw new System.InvalidOperationException($"{nameof(elements)} contained a null value");
                }

                // TODO: Should this throw if the same element is already added?

                addElementToCollection.Invoke(destination, elementIndex, element);

                ++elementIndex;
            }

            return destination;
        }

        // =====================================================================
        // Nested types
        // =====================================================================

        public delegate void AddElementToCollectionHandler<CollectionType, ElementType>(
            CollectionType  collection,
            int             elementIndex,
            ElementType     element);


    }   // static class MelcherObjectPoolUtility

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-11
/// ===========================================================================
