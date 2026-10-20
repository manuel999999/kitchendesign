namespace Melcher
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     An object pool that uses a stack buffer. It is intended for use
    ///     cases where the object borrower may need the object for longer
    ///     periods of time, or where it must be ensured that no two borrowers
    ///     receive the same object at the same time.
    /// </summary>
    public class MelcherObjectPoolStack<ObjectType>
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
            return new MelcherObjectPoolCyclic<ObjectType>(capacity, MelcherCommon.DefaultConstructor<DefaultConstructibleObjectType, ObjectType>);
        }

        // =====================================================================
        // Constructors
        // =====================================================================

        /// <inheritdoc cref="MelcherObjectPoolStack(int)"/>
        /// <param name="createElement">
        ///     A function that creates or returns an object to use in the
        ///     object pool.
        /// </param>
        public MelcherObjectPoolStack(
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

            m_AllObjects = new ObjectType[capacity];
            m_AvailableObjects = new Stack<ObjectType>(capacity);

            for (int i = 0; i < capacity; ++i)
            {
                ObjectType element = createElement.Invoke();

                if (element == null)
                {
                    throw new System.InvalidOperationException($"{nameof(createElement)} returned null");
                }

                m_AllObjects[i] = element;
                m_AvailableObjects.Push(element);
            }
        }

        /// <inheritdoc cref="MelcherObjectPoolUtility.CreateForCollection{ObjectType, CollectionType}(IEnumerable{ObjectType}, Func{int, CollectionType}, MelcherObjectPoolUtility.AddElementToCollectionHandler{ObjectType, CollectionType})"/>
        public MelcherObjectPoolStack(IEnumerable<ObjectType> elements)
        {
            m_AvailableObjects =
                MelcherObjectPoolUtility.CreateForCollection(
                    elements,
                    createCollection: capacity => new Stack<ObjectType>(capacity),
                    addElementToCollection: (stack, elementIndex, element) => stack.Push(element)
                );
        }

        // =====================================================================
        // IMelcherObjectPool<ObjectType>
        // =====================================================================
        #region IMelcherObjectPool<ObjectType>

        public ObjectType GetObject()
        {
            if (m_AvailableObjects.Count <= 0)
            {
                throw new System.InvalidOperationException("Out of pooled objects");
            }

            return m_AvailableObjects.Pop();
        }

        public void ReturnObject(ObjectType obj)
        {
            if (obj == null)
            {
                throw new System.ArgumentNullException(nameof(obj));
            }

            // TODO: Add an option to check if items are being returned more
            //       than once?
            m_AvailableObjects.Push(obj);
        }

        public IReadOnlyCollection<ObjectType> allObjects => m_AllObjects;

        #endregion  // IMelcherObjectPool<ObjectType>

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        ///     The collection of all objects that were created as part of this
        ///     pool. Some of these objects may be lent out and therefore won't
        ///     appear in <see cref="m_AvailableObjects"/>.
        /// </summary>
        protected ObjectType[] m_AllObjects;

        /// <summary>
        ///     The collection of available objects ready to be borrowed. This
        ///     is a stack to minimize the amount of memory shuffling needed as
        ///     objects are borrowed and returned.
        /// </summary>
        protected Stack<ObjectType> m_AvailableObjects;

    }   // class MelcherObjectPoolStack

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-01-29
/// ===========================================================================
