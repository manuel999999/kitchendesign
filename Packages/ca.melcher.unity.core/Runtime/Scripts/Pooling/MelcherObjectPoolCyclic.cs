namespace Melcher
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     An object pool that uses a cyclic buffer. It is best suited for
    ///     fire-and-forget allocations, where the borrowed object is needed
    ///     only briefly, and where an obligation to return it to the pool adds
    ///     more friction than necessary.
    ///     <br /><br />
    ///
    ///     Some example uses are: a pool of Lists where each list is only used
    ///     used across a single frame; a pool of particle systems where each
    ///     system will be used to burst and then stop.
    ///     <br /><br />
    ///
    ///     Objects borrowed from a cyclic pool do not need to be returned. The
    ///     will automatically be reused once all other elements have been
    ///     borrowed.
    /// </summary>
    public class MelcherObjectPoolCyclic<ObjectType>
        : IMelcherObjectPool<ObjectType>
            where ObjectType : class
    {
        // =====================================================================
        // Factory functions
        // =====================================================================

        /// <summary>
        ///     Create an object pool of with new objects created up to the
        ///     requested capacity. This uses the default constructor.
        /// </summary>
        /// <param name="capacity">
        ///     The number of objects to create and populate the pool.
        /// </param>
        public static MelcherObjectPoolCyclic<ObjectType> MakeDefault<DefaultConstructibleObjectType>(int capacity)
            where DefaultConstructibleObjectType : ObjectType, new()
        {
            return
                new MelcherObjectPoolCyclic<ObjectType>(
                    capacity,
                    MelcherCommon.DefaultConstructor<DefaultConstructibleObjectType, ObjectType>
                );
        }

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <inheritdoc cref="MelcherObjectPoolCyclic(int)"/>
        /// <param name="createElement">
        ///     A function that creates or returns an object to use in the
        ///     object pool.
        /// </param>
        public MelcherObjectPoolCyclic(
            int                 capacity,
            Func<ObjectType>    createElement)
        {
            if (capacity < 0)
            {
                throw new System.ArgumentException($"{nameof(capacity)} should be positive", nameof(capacity));
            }

            if (createElement == null)
            {
                throw new System.ArgumentNullException(nameof(createElement));
            }

            m_ObjectBuffer = new ObjectType[capacity];

            for (int i = 0; i < capacity; ++i)
            {
                ObjectType element = createElement.Invoke();

                if (element == null)
                {
                    throw new System.InvalidOperationException($"{nameof(createElement)} returned null");
                }

                m_ObjectBuffer[i] = element;
            }
        }

        /// <inheritdoc cref="MelcherObjectPoolUtility.CreateForCollection{ObjectType, CollectionType}(IEnumerable{ObjectType}, Func{int, CollectionType}, MelcherObjectPoolUtility.AddElementToCollectionHandler{ObjectType, CollectionType})"/>
        public MelcherObjectPoolCyclic(IEnumerable<ObjectType> elements)
        {
            m_ObjectBuffer =
                MelcherObjectPoolUtility.CreateForCollection(
                    elements,
                    createCollection: capacity =>
                    {
                        // `capacity` is always the result of ICollection.Count
                        // so it should never be negative

                        MelcherLog.Assert(capacity >= 0);

                        if (capacity == 0)
                        {
                            return System.Array.Empty<ObjectType>();
                        }

                        return new ObjectType[capacity];
                    },
                    addElementToCollection: (array, index, element) => array[index] = element
                );
        }

        // =====================================================================
        // IMelcherObjectPool<ObjectType>
        // =====================================================================
        #region IMelcherObjectPool<ObjectType>

        public ObjectType GetObject()
        {
            if (m_NextObjectIndex < 0 || m_NextObjectIndex >= m_ObjectBuffer.Length)
            {
                throw new System.InvalidOperationException("No pooled objects");
            }

            ObjectType result = m_ObjectBuffer[m_NextObjectIndex];
            m_NextObjectIndex = (m_NextObjectIndex + 1) % m_ObjectBuffer.Length;
            return result;
        }

        /// <summary>
        ///     As a cyclical pool, nothing is needed.
        /// </summary>
        public void ReturnObject(ObjectType obj)
        {
        }

        public IReadOnlyCollection<ObjectType> allObjects => m_ObjectBuffer;

        #endregion  // IMelcherObjectPool<ObjectType>

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        ///     A fixed array of objects. These are lent out in a cyclic
        ///     fashion.
        /// </summary>
        protected readonly ObjectType[] m_ObjectBuffer;

        /// <summary>
        ///     The index of the object in <see cref="m_ObjectBuffer"/> that
        ///     will be returned in the next call to <see cref="ReturnObject"/>.
        /// </summary>
        protected int m_NextObjectIndex;

    }   // class MelcherObjectPoolCyclic

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-29
/// ===========================================================================
