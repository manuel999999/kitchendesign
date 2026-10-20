namespace Melcher
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public abstract class MelcherOnValidateAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        //
        // These functions are meant to be called within an OnValidate() message
        // which is something that only happens within the editor.
        // =====================================================================

        /// <summary>
        /// Return <see langword="true"/> if <paramref name="callerType"/> is
        /// a usable type to be validated by this validator. The method is used
        /// to catch misapplied <see cref="MelcherOnValidateAttribute"/>.
        /// </summary>
        /// <param name="callerType">
        /// The type that is using the attribute.
        /// </param>
        protected abstract bool IsCompatibleCallerType(System.Type callerType);

        /// <summary>
        /// Perform the OnValidate-logic.
        /// </summary>
        /// <param name="caller">
        /// The <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/>
        /// that is validating.
        /// </param>
        /// <param name="field">
        /// The field within <paramref name="caller"/> that is being validated.
        /// </param>
        protected abstract void DoValidate(Object caller, FieldInfo field);

        // =====================================================================
        // Editor-only - Static functions
        // =====================================================================

        /// <summary>
        /// Find and validate all <see cref="MelcherOnValidateAttribute"/>
        /// attributes that are defined in the calling object.
        /// </summary>
        public static void ValidateFields(Object toValidate)
        {
            if (Application.isPlaying)
            {
                return;
            }

            // Step through the inheritance hierarchy one class at a time. This
            // allows us to find private fields in parent types. I.e.,
            // Type.GetFields() does not return private fields from parent
            // classes and so we use BindingFlags.DeclaredOnly and fetch fields
            // one class at a time

            for (System.Type currentType = toValidate.GetType();
                currentType != null;
                currentType = currentType.BaseType)
            {
                const BindingFlags LocalMemberFlags =
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly;  // only local

                foreach (FieldInfo field in currentType.GetFields(LocalMemberFlags))
                {
                    IEnumerable<MelcherOnValidateAttribute> attributes =
                        field.GetCustomAttributes<MelcherOnValidateAttribute>(inherit: true);

                    foreach (MelcherOnValidateAttribute attribute in attributes)
                    {
                        if (!attribute.IsCompatibleCallerType(toValidate.GetType()))
                        {
                            MelcherLog.LogError(
                                $"{GetFieldFullName(toValidate, field)} uses the attribute {MelcherFormat.ConsoleTypeName(attribute)} " +
                                $"which is not compatible with the field's owning type's class hierarchy.\n" +
                                $"Refer to the attribute's {MelcherFormat.ConsoleMethodName(nameof(IsCompatibleCallerType))} method to learn more.",
                                context: toValidate
                            );
                            continue;
                        }

                        attribute.DoValidate(toValidate, field);
                    }
                }
            }
        }

        /// <summary>
        /// Construct a string that contains a field's name prefixed by its
        /// owning-type's name. This is used in log messages to unambiguously
        /// refer to a field defined in a script.
        /// </summary>
        public static string GetFieldFullName(Object caller, FieldInfo field)
        {
            return MelcherFormat.ConsoleMethodName($"{caller.GetType().FullName}.{field.Name}");
        }

        /// <summary>
        /// Log an error if the field is not serialized. I.e., it is not public
        /// or it is not decorated with <see cref="SerializeField"/>.
        /// </summary>
        public static void LogErrorIfFieldIsNotSerialized(
                                Object      caller,
                                FieldInfo   field,
            [CallerFilePath]    string      callerFilePath = null)
        {
            if (!field.IsPublic && !IsDefined(field, typeof(SerializeField)))
            {
                MelcherLog.LogError(
                    $"{GetFieldFullName(caller, field)} won't persist because it's non-public and isn't marked with [{MelcherFormat.ConsoleTypeName(nameof(SerializeField))}]",
                    context: caller,
                    callerFilePath: callerFilePath
                );
            }
        }

#endif

    }   // class MelcherOnValidateAttribute

}   // namespace Melcher
