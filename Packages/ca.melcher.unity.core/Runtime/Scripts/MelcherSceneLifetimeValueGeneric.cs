namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     Intermediary class for <see cref="MelcherSceneLifetimeValue{DerivedType}"/>
    ///     that allows us to specify value type and get an <see
    ///     cref="onValueChanged"/> callback, specific to this exact
    ///     LifeTimeValue.
    /// </summary>
    /// <typeparam name="DerivedType">
    ///     This is the type that is deriving from <see
    ///     cref="MelcherSceneLifetimeValueGeneric{DerivedType, ValueType}"/>.
    ///     <br /><br />
    ///
    ///     Example:
    ///     <code>
    ///     class MyClass
    ///         : MelcherSceneLifetimeValueGeneric &lt; MyClass, Material &gt;
    ///     {
    ///     }
    ///     </code>
    /// </typeparam>
    /// <typeparam name="ValueType">
    ///     The type of the <see cref="value"/> property.
    /// </typeparam>
    public class MelcherSceneLifetimeValueGeneric<DerivedType, ValueType>
        : MelcherSceneLifetimeValue<DerivedType>
        where DerivedType : MelcherSceneLifetimeValueGeneric<DerivedType, ValueType>
    {
        // ====================================================================
        // Events
        // ====================================================================

        /// <summary>
        ///     Invoked when <see cref="value"/> changes. It may not always
        ///     be invoked when the setter is called, depending on what the
        ///     subclass returns in <see cref="OnPreSetValue(ValueType)"/>.
        /// </summary>
        public static event ValueChangedHandler onValueChanged;

        // ====================================================================
        // Static functions
        // ====================================================================

        /// <summary>
        ///     A shortcut to fetch <see cref="value"/> on the first found
        ///     object.
        /// </summary>
        public static bool TryGetValue(out ValueType value, int index = 0)
        {
            if (!TryGet(out var firstInstance, index))
            {
                value = default;
                return false;
            }

            value = firstInstance.value;
            return true;
        }

        /// <summary>
        ///     A shortcut to set the value of the first found object.
        /// </summary>
        public static bool TrySetValue(ValueType value, int index = 0)
        {
            if (!TryGet(out var firstInstance, index))
            {
                return false;
            }

            firstInstance.value = value;
            return true;
        }

        // ====================================================================
        // Properties
        // ====================================================================

        public ValueType value
        {
            get => m_Value;

            set
            {
                if (!OnPreSetValue(value))
                {
                    // Allow scene lifetime subclasses to intercept and
                    // allow/disallow an assignment.

                    return;
                }

                ValueType oldValue = m_Value;
                m_Value = value;

                // Allow subclasses to perform validation or event propagation
                // after the value is modified:

                OnPostSetValue();

                onValueChanged?.Invoke(this, in oldValue, newValue: in m_Value);
            }
        }

        /// <summary>
        ///     This function is called before a value is assigned in <see
        ///     cref="value"/>'s setter. The return value controls whether the
        ///     setter will perform the assignment. Override this function to
        ///     change the conditions for when the value is allowed to be
        ///     changed or to invoke custom events.
        /// </summary>
        /// <returns>
        ///     Return <see langword="true"/> if <paramref name="value"/> is
        ///     allowed to be assigned to <see cref="m_Value"/>; otherwise,
        ///     return <see langword="false"/>.
        /// </returns>
        protected virtual bool OnPreSetValue(ValueType value)
        {
            // Only permit the value to be set if it is different from the
            // current value

            return !Equals(m_Value, value);
        }

        /// <summary>
        ///     This function is called after a value has been assigned in the
        ///     setter of <see cref="value"/>.
        /// </summary>
        protected virtual void OnPostSetValue()
        {
            // Does nothing by default.
        }

        // ====================================================================
        // Inspector-serialized fields
        // ====================================================================

        [SerializeField]
        protected ValueType m_Value;

        // ====================================================================
        // Nested types
        // ====================================================================

        /// <summary>
        ///     The delegate used by <see cref="onValueChanged"/>.
        ///     <br /><br />
        ///
        ///     The parameters are passed using the <see langword="in"/>
        ///     modifier to prevent them from being modified outside of the
        ///     <see cref="value"/> setter.
        ///     <br /><br />
        ///
        ///     The <paramref name="caller"/> parameter is passed as an opaque
        ///     <see cref="object"/> to avoid the verbosity of the actual type,
        ///     "MelcherSceneLifetimeValueGeneric(DerivedType, ValueType)".
        ///     This seems fine because it will conceivably mainly be used just
        ///     to disambiguate which object is calling it.
        /// </summary>
        public delegate void ValueChangedHandler(object caller, in ValueType oldValue, in ValueType newValue);

#if UNITY_EDITOR
        // ====================================================================
        // Editor-only
        // ====================================================================

        [MelcherEasyButton]
        private void ForceOnValueChanged()
        {
            onValueChanged?.Invoke(this, in m_Value, in m_Value);
        }
#endif
    }   // class MelcherSceneLifetimeValueGeneric

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Justin Bender (jbender.ie@me.com)
/// Initial creation date: 2023-02-15
/// ===========================================================================
