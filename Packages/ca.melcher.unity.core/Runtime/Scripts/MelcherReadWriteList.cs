namespace Melcher
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    ///     <see cref="MelcherReadWriteList{ElementType}"/> provides both
    ///     mutable access and read-only access to a standard <see
    ///     cref="List{T}"/>.
    ///     <br /><br />
    ///
    ///     The purpose of this type is the convenience to declare a single
    ///     variable when needing a list that is mutable within a class but
    ///     also able to be exposed for read-only access outside of it, and
    ///     without paying the a recurring cost for converting the list into a
    ///     read-only collection.
    ///     <br /><br />
    ///
    ///     In other words, this allows class writers to avoid declaring two
    ///     different variables and coordinating them.
    /// </summary>
    [System.Serializable]
    public class MelcherReadWriteList<ElementType>
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public MelcherReadWriteList()
            : this(new List<ElementType>())
        {
            m_ReadOnlyList = m_List.AsReadOnly();
        }

        public MelcherReadWriteList(int capacity)
            : this(new List<ElementType>(capacity))
        {
            m_ReadOnlyList = m_List.AsReadOnly();
        }

        public MelcherReadWriteList(IEnumerable<ElementType> listToShallowCopy)
        {
            m_List = new List<ElementType>(listToShallowCopy);
            m_ReadOnlyList = m_List.AsReadOnly();
        }

        /// <summary>
        /// An implicit conversion operator to allow the type to more seamlessly
        /// integrate with existing code. Existing List variables only need to
        /// have their type changed to <see cref="MelcherReadWriteList{ElementType}"/>
        /// and their initialization will still work.
        /// <br /><br />
        ///
        /// The resulting <see cref="MelcherReadWriteList{ElementType}"/> uses
        /// <paramref name="listToTake"/> directly and does not shallow copy
        /// like its other constructors do.
        /// </summary>
        public static implicit operator MelcherReadWriteList<ElementType>(List<ElementType> listToTake)
        {
            if (listToTake == null)
            {
                throw new System.ArgumentNullException(nameof(listToTake));
            }

            return new MelcherReadWriteList<ElementType>()
            {
                m_List = listToTake,
                m_ReadOnlyList = listToTake.AsReadOnly()
            };
        }

        // =====================================================================
        // Properties
        // =====================================================================

        // Capitalized for consistency with the property in the underlying types.
        public int Count => m_List.Count;

        /// <summary>
        /// Access to the mutable list. When the list is assigned a new value,
        /// the read-only list reference is rebuilt to point at it. If the new
        /// list is <see langword="null"/>, then so too will be <see
        /// cref="readOnly"/>.
        /// </summary>
        public List<ElementType> mutable
        {
            get => m_List;

            protected set
            {
                if (value != m_List)
                {
                    m_List = value;
                    m_ReadOnlyList = m_List != null ? m_List.AsReadOnly() : null;
                }
            }
        }

        /// <summary>
        /// Read-only access to the list.
        /// </summary>
        public ReadOnlyCollection<ElementType> readOnly => m_ReadOnlyList;

        // =====================================================================
        // Internal fields
        // =====================================================================

        /// <summary>
        /// The mutable list.
        /// </summary>
        [UnityEngine.SerializeReference]    // Using SerializeReference to allow polymorphic list drawing
        protected List<ElementType> m_List;

        /// <summary>
        /// Read-only access to <see cref="m_List"/>.
        /// </summary>
        protected ReadOnlyCollection<ElementType> m_ReadOnlyList;

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public const string ListFieldName = nameof(m_List);
#endif  // if UNITY_EDITOR

    }   // class MelcherReadWriteList

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-10-22
/// ===========================================================================
