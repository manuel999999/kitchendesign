namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;

    /// <summary>
    /// A garbage-free enumerator that allows iterating over a transform's
    /// immediate children for components of a specific type. Children that
    /// lack the component are skipped. This does not handle grandchildren nor
    /// multiple components on a single child (it will only return the first
    /// component).
    /// </summary>
    // TODO: Should this implement IDisposable? This type is treated like a
    //       IEnumerator and those implement IDisposable. -Ryan
    //       Possible reference: https://antao-almada.medium.com/performance-of-value-type-vs-reference-type-enumerators-820ab1acc291
    // TODO: Consider renaming to `FirstComponentsInImmediateChildrenEnumerator`
    //       for improved clarity. -Ryan
    public struct ComponentsInChildrenEnumerator<ComponentType>

        where ComponentType : Component
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public ComponentsInChildrenEnumerator(Transform root, bool reverse = false)
        {
            m_Root = root;
            m_Current = null;
            m_NextChildIndex = reverse ? root.childCount - 1 : 0;
            m_Reverse = reverse;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public ComponentType Current => m_Current;

        // =====================================================================
        // Methods
        // =====================================================================

        public void Reset()
        {
            m_Current = null;
            m_NextChildIndex = m_Reverse ? m_Root.childCount - 1 : 0;
        }

        public bool MoveNext()
        {
            m_Current = null;
            while (!m_Current && m_Root && m_NextChildIndex >= 0 && m_NextChildIndex < m_Root.childCount)
            {
                int index = m_NextChildIndex;

                m_NextChildIndex =
                    m_Reverse ?
                        m_NextChildIndex - 1 :
                        m_NextChildIndex + 1;

                m_Root.GetChild(index).TryGetComponent(out m_Current);
            }
            return m_Current;
        }

        // =====================================================================
        // Fields
        // =====================================================================

        private Transform m_Root;
        private ComponentType m_Current;
        private int m_NextChildIndex;
        private bool m_Reverse;

    }   // class ComponentsInChildrenEnumerator

}   // namespace KitchenDesign

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-11
/// ===========================================================================
