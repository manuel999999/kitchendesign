namespace Melcher
{
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    ///
    /// </summary>
    // This type must be a reference type (i.e., a class) because
    // ISerializationCallbackReceiver does not support structs:
    // https://forum.unity.com/threads/iserializationcallbackreceiver-working-with-structs-now.590482/#post-3942985
    [System.Serializable]
    public class MelcherStableEnum<EnumType>
        : MelcherStableEnumBase
        , ISerializationCallbackReceiver
        where EnumType : struct, System.Enum
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public MelcherStableEnum(EnumType enumValue)
        {
            m_EnumValue = enumValue;

#if UNITY_EDITOR
            m_EnumNames = System.Enum.GetName(typeof(EnumType), enumValue);
#endif  // if UNITY_EDITOR
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     Get or set the underlying enum value.
        /// </summary>
        public EnumType value
        {
            get => m_EnumValue;
            set => m_EnumValue = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Implicit conversion from <see
        ///     cref="MelcherStableEnum{EnumType}"/> to <see cref="EnumType"/>.
        /// </summary>
        public static implicit operator EnumType(MelcherStableEnum<EnumType> me)
            => me.m_EnumValue;

        // =====================================================================
        // Inspector-serialized fields
        // =====================================================================

        [SerializeField]
        protected string m_EnumNames = default;

        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected EnumType m_EnumValue;

        // =====================================================================
        // MelcherStableEnumBase
        // =====================================================================

#if UNITY_EDITOR

        public override System.Enum underlyingValue
        {
            get => m_EnumValue;
            set => m_EnumValue = (EnumType)value;
        }

#endif  // if UNITY_EDITOR

        // =====================================================================
        // ISerializationCallbackReceiver
        // =====================================================================

        public void OnBeforeSerialize()
        {
            if (m_ErrorSource == ErrorSource.Serialize)
            {
                // Exit early to avoid spamming the same "serialization
                // failed" error message.
                return;
            }

            if (m_ErrorSource == ErrorSource.Deserialize)
            {
                // Don't overwrite the saved name if the value could not be
                // restored from the name. It is kept intact so a developer is
                // always free to review it and determine the best solution.

                Object context = null;

#if UNITY_EDITOR
                context = UnityEditor.Selection.activeObject;
#endif  // if UNITY_EDITOR

                MelcherLog.LogError(
                    $"Will not serialize the enum of type " +
                    $"{MelcherFormat.ConsoleTypeName<EnumType>(useFullName: true)} " +
                    $"because one or more of its recorded names are no " +
                    $"longer valid." +
                    context
                );

                m_ErrorSource = ErrorSource.Serialize;

                return;
            }

            string enumNames = m_EnumValue.ToString();

            Match nameNumericMatch = Regex.Match(enumNames, @"^-?\d+");

            if (nameNumericMatch.Success &&
                nameNumericMatch.Value != s_DefaultNothingName &&
                nameNumericMatch.Value != s_DefaultEverythingName)
            {
                // A numeric name (e.g., "1", "10", "-4", etc.) is an error
                // because it indicates there is no matching name for the enum.
                // However, "0" and "-1" are special names  that mean "no bits"
                // and "all bits", respectively.

                MelcherLog.LogError(
                    $"Could not lookup a name in enum of type " +
                    $"{MelcherFormat.ConsoleTypeName<EnumType>(useFullName: true)}" +
                    $"for the value {MelcherFormat.ConsoleNumericValue(m_EnumValue)}.\n" +
                    $"\n" +
                    $"Perhaps its enum entry was renumbered or removed?\n" +
                    $"\n" +
                    MelcherUnityUtility.ExtractSerializationContext()
                );

                m_ErrorSource = ErrorSource.Serialize;

                return;
            }

            m_EnumNames = enumNames;
        }

        public void OnAfterDeserialize()
        {
            if (m_ErrorSource == ErrorSource.Deserialize)
            {
                // Exit early to avoid spamming the same "deserialization
                // failed" error message.
                return;
            }

            string errorContext =
                MelcherUnityUtility.ExtractSerializationContext();

            // Clear non-deserialization errors: Everything is assumed to be
            // successful starting now unless an error is detected within this
            // function.
            m_ErrorSource = ErrorSource.None;

            if (string.IsNullOrEmpty(m_EnumNames))
            {
                // Unity may skip serializing an enum value if it is the
                // default value (0). In these cases, we will just fall back to
                // a string version of 0.
                m_EnumNames = "0";
                return;
            }

            // Parse the enum name which may contain multiple names in the form:
            // "Name1, Name2"
            // TODO: Should this be matching/splitting on commas instead of
            //       whitespace characters? What if the enum value is
            //       tagged with a display name that contains a space or
            //       comma? -Ryan
            MatchCollection enumNameMatches =
                Regex.Matches(m_EnumNames, @"\w+");

            int reassembledEnumValue = default;

            foreach (Match enumNameMatch in enumNameMatches)
            {
                string enumName = enumNameMatch.Value;

                if (!System.Enum.TryParse(enumName, out EnumType valueForName))
                {
                    MelcherLog.LogError(
                        $"Could not parse enum of type " +
                        $"{MelcherFormat.ConsoleTypeName<EnumType>(useFullName: true)} " +
                        $"from the name {MelcherFormat.ConsoleString(m_EnumNames)}.\n" +
                        $"\n" +
                        $"Perhaps the enum entry was renamed or removed?\n" +
                        $"\n" +
                        errorContext
                    );

                    // Could not recover the value for this enum name.
                    m_ErrorSource = ErrorSource.Deserialize;

                    continue;
                }

                // In our current C# version, it is not possible to bitwise-OR
                // a generic enum type (e.g., EnumType in this function).
                // We use UnsafeUtility to cast the parsed EnumType to an int
                // which can be bitwise-OR'd to build up the correct value from
                // multiple names, without incurring boxing costs.

                reassembledEnumValue |=
                    Unity.Collections.LowLevel.Unsafe.UnsafeUtility
                        .As<EnumType, int>(ref valueForName);
            }

            m_EnumValue =
                (EnumType)System.Enum.ToObject(
                    typeof(EnumType),
                    reassembledEnumValue
                );
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        protected const string s_DefaultNothingName = "0";
        protected const string s_DefaultEverythingName = "-1";

    }   // class MelcherStableEnum

    /// <summary>
    ///     Abstract base type for <see cref="MelcherStableEnum{EnumType}"/>.
    ///     Its purpose is to allow a property drawer to access the underlying
    ///     enum value for any generic enum argument.
    /// </summary>
    public abstract class MelcherStableEnumBase
    {
        // =====================================================================
        // Non-serialized fields
        // =====================================================================

        protected ErrorSource m_ErrorSource = ErrorSource.None;

        // =====================================================================
        // Nested types
        // =====================================================================

        public enum ErrorSource
        {
            None,
            Serialize,
            Deserialize

        }   // enum ErrorSource

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        public abstract System.Enum underlyingValue { get; set; }

        public void ClearErrorSource()
        {
            m_ErrorSource = ErrorSource.None;
        }
#endif  // if UNITY_EDITOR


    }   // class MelcherStableEnumBase

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic
/// Initial creation date: 2023-11-02
/// ===========================================================================
