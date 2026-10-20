#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Melcher
{
    using System;
    using System.Reflection;

    // Editor-only functionality of MelcherSignals
    public partial class MelcherSignal : MelcherScriptableObject
    {
        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Help implement type-safety for <see cref="Signal(object)"/>.
        ///     This function validates if a signal should receive data and, if
        ///     it should, delegates the data's type validation to <see
        ///     cref="AssertDataTypeIsExpected(Type)"/>. If validation fails,
        ///     an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        partial void AssertDataTypeSafety(object data)
        {
            if (m_ExpectedArgumentType == null)
            {
                throw new ArgumentException($"MelcherSignal '{name}' has unknown data type.");
            }
            else if (m_ExpectedArgumentType == typeof(void) && data != null)
            {
                throw new ArgumentException($"MelcherSignal '{name}' should not receive data.");
            }
            else if (data != null)
            {
                AssertDataTypeIsExpected(data.GetType());
            }
        }

        /// <summary>
        ///     Help implement type-safety for <see cref="Signal(object)"/>.
        ///     This function is responsible for validating the event argument
        ///     type conforms to the signal's expected type. E.g., if a Signal
        ///     has an expected argument type of <see
        ///     cref="UnityEngine.Texture"/>, this method will throw an
        ///     exception if the actual type of the argument does not belong to
        ///     the <see cref="UnityEngine.Texture"/> class hierarchy (i.e.,
        ///     the argument is not a <see cref="UnityEngine.Texture2D"/> or
        ///     <see cref="UnityEngine.RenderTexture"/>.
        /// </summary>
        private void AssertDataTypeIsExpected(Type dataType)
        {
            if (!m_ExpectedArgumentType.IsAssignableFrom(dataType))
            {
                throw new ArgumentException(
                    $"MelcherSignal '{name}' received data type '{dataType.FullName}' but expects '{m_QualifiedArgumentTypeName}"
                );
            }
        }

        public Type GetExpectedArgumentType()
        {
            return m_ExpectedArgumentType;
        }

        protected void InitializeExpectedArgumentType()
        {
            // Default to void (no arguments)
            m_ExpectedArgumentType = typeof(void);

            // Convert argument type from text to actual type
            if (!string.IsNullOrWhiteSpace(m_QualifiedArgumentTypeName))
            {
                m_ExpectedArgumentType = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    m_ExpectedArgumentType = assembly.GetType(m_QualifiedArgumentTypeName);
                    if (m_ExpectedArgumentType != null)
                    {
                        break;
                    }
                }

                if (m_ExpectedArgumentType == null)
                {
                    MelcherLog.LogError(
                        message: $"'{name}' uses unknown type: {m_QualifiedArgumentTypeName}",
                        context: this
                    );
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            InitializeExpectedArgumentType();
        }
#endif  // if UNITY_EDITOR

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private Type m_ExpectedArgumentType;

    }   // class MelcherSignal

}   // namespace Melcher
#endif  // if UNITY_EDITOR || DEVELOPMENT_BUILD
