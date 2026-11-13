namespace KitchenDesign.Utility
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
            _root = root;
            _current = null;
            _nextChildIndex = reverse ? root.childCount - 1 : 0;
            _reverse = reverse;
        }

        // =====================================================================
        // Properties
        // =====================================================================

        public ComponentType Current => _current;

        // =====================================================================
        // Methods
        // =====================================================================

        public void Reset()
        {
            _current = null;
            _nextChildIndex = _reverse ? _root.childCount - 1 : 0;
        }

        public bool MoveNext()
        {
            _current = null;
            while (!_current && _root && _nextChildIndex >= 0 && _nextChildIndex < _root.childCount)
            {
                int index = _nextChildIndex;

                _nextChildIndex =
                    _reverse ?
                        _nextChildIndex - 1 :
                        _nextChildIndex + 1;

                _root.GetChild(index).TryGetComponent(out _current);
            }
            return _current;
        }

        // =====================================================================
        // Fields
        // =====================================================================

        private Transform _root;
        private ComponentType _current;
        private int _nextChildIndex;
        private bool _reverse;

    }   // class ComponentsInChildrenEnumerator

}   // namespace KitchenDesign.Utility

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-05-11
/// ===========================================================================
