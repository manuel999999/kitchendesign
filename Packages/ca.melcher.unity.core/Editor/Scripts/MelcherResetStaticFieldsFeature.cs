namespace Melcher
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;

    /// <summary>
    /// The <see cref="MelcherResetStaticFieldsFeature"/> is a more powerful
    /// version of <see cref="RuntimeInitializeOnLoadMethodAttribute"/>
    /// that is designed specifically for resetting class static fields. It is
    /// intended to be used when a project has the Disable Domain Reloading
    /// play-mode option enabled.
    /// <br /><br />
    ///
    /// This system has a number of useful features:
    /// <br /><br />
    ///
    ///     1.  It supports generic-types. Unity's built-in <see
    ///         cref="RuntimeInitializeOnLoadMethod"/> is not invoked when
    ///         added to a function declared in a generic type.
    ///         <br /><br />
    ///
    ///     2.  It double-checks that all static fields in a script have been
    ///         reset. The functions that the user writes are expected to return
    ///         the number of fields that were reset. This system compares that
    ///         number to the number of total static fields in the class and
    ///         warns if they don't match.
    ///         <br /><br />
    ///
    ///     3.  Robust error messages. If the attribute is misapplied or the
    ///         function doesn't return a value, the user will be warned. If the
    ///         number of fields reset doesn't match the number of static fields
    ///         in the type, the user will be warned.
    /// </summary>
    public static class MelcherResetStaticFieldsFeature
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        [MenuItem(MelcherEditorConst.Menu.ToolsAssetsPrefix + "Check and reset static fields")]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnCheckStaticFields()
        {
            bool showWarnings = EditorPrefs.GetBool(EditorPrefsKey.ShowWarnings, defaultValue: false);

            Menu.SetChecked(s_ToggleWarningsMenuItemPath, showWarnings);

            if (!EditorSettings.enterPlayModeOptionsEnabled ||
                !EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
            {
                // Domain Reloading is enabled so it's not necessary to do any
                // manual resetting of static fields. However, we do it anyway
                // because it may otherwise become a huge wave of work to do
                // (adding in functions to reset static fields) if Domain
                // Reloading is later disabled.
            }

            const BindingFlags AnythingLocal =
                BindingFlags.DeclaredOnly | // only things declared directly by the type
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            System.Type resetStaticFieldsAttributeType =
                typeof(MelcherResetStaticFieldsMethodAttribute);

            System.Type afterResetStaticFieldsAttributeType =
                typeof(MelcherAfterResetStaticFieldsMethodAttribute);

            Assembly[] projectCompiledAssemblies =
                System.AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic && assembly.Location.StartsWith(MelcherPredefinedFilePath.project))
                    .ToArray();

            // Find all non-generic type definitions across all assemblies
            // that we may have authored. This cannot find closed types of
            // generic definitions (e.g., List<int>) that are used in fields,
            // properties, function parameters and return values, base type of
            // inheritance, and method variables -- therefore those are
            // manually searched for within `RegisterUniqueClosedType`.

            // Generic type definitions are intentionally filtered out here
            // because they cannot have their methods invoked. I.e., you cannot
            // invoke methods on a List<T> because there are infinite many
            // possible type constructions of it.

            IEnumerable<System.Type> closedTypes =
                projectCompiledAssemblies
                    .SelectMany(
                        assembly =>
                        {
                            try
                            {
                                return assembly.GetTypes();
                            }
                            catch (System.Reflection.ReflectionTypeLoadException)
                            {
                                // This exception handler ensures this script doesn't
                                // fail on assemblies whose types cannot be loaded.
                                // For example, Google's Firebase.
                                // TODO: See if there's a cleaner way to detect if an
                                //       assembly's types can't be loaded. -Ryan
                                // Not logging a warning because it adds noise to the
                                // console.
                                //MelcherLog.LogWarning($"Could not load types from assembly: {assembly.Location}");
                                return System.Array.Empty<System.Type>();
                            }
                        }
                    )
                    .Where(typeInAssembly => !typeInAssembly.IsGenericTypeDefinition);

            // With the explicitly defined closed-types, we must walk their
            // hierarchy graph and inspect their fields to find closed types
            // that were possibly missed.

            HashSet<System.Type> uniqueClosedTypes = new HashSet<System.Type>();

            foreach (System.Type closedType in closedTypes)
            {
                RegisterUniqueClosedType(projectCompiledAssemblies, uniqueClosedTypes, closedType);
            }

            foreach (System.Type closedType in uniqueClosedTypes)
            {
                InvokeAllResetStaticFieldMethods(closedType, showWarnings);
                ResetMarkedFields(closedType, showWarnings);
            }

            foreach (System.Type closedType in uniqueClosedTypes)
            {
                InvokeAllAfterResetStaticFieldMethods(closedType);
            }

            void RegisterUniqueClosedType(Assembly[] assemblyWhitelist, HashSet<System.Type> uniqueClosedTypes, System.Type closedType)
            {
                while (closedType.IsArray)
                {
                    closedType = closedType.GetElementType();
                }

                while (closedType.IsByRef)
                {
                    closedType = closedType.GetElementType();
                }

                if (closedType.IsEnum ||
                    closedType.IsSubclassOf(typeof(System.Delegate)))
                {
                    // Skip types that cannot contain fields. I.e., they have
                    // nothing to reset.

                    return;
                }

                if (closedType.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null)
                {
                    // Skip compiler generated types (e.g, lambdas) because
                    // their behaviour cannot be authored.

                    return;
                }

                if (!assemblyWhitelist.Contains(closedType.Assembly))
                {
                    // Ignore types from external assemblies (System, Unity
                    // Module, etc.) because their source is not available to
                    // us.

                    return;
                }

                if (!IsAuthoredByMelcher(closedType))
                {
                    // Ignore types that we did not author. This avoids
                    // analyzing and warning about types we did not author.

                    return;
                }

                if (!uniqueClosedTypes.Add(closedType))
                {
                    // Already handled
                    return;
                }

                foreach (System.Type genericArgumentType in closedType.GenericTypeArguments)
                {
                    RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, genericArgumentType);
                }

                foreach (FieldInfo field in closedType.GetFields(AnythingLocal))
                {
                    RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, field.FieldType);
                }

                foreach (PropertyInfo property in closedType.GetProperties(AnythingLocal))
                {
                    RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, property.PropertyType);
                }

                foreach (MethodInfo method in closedType.GetMethods(AnythingLocal))
                {
                    if (method.ReturnType != null)
                    {
                        RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, method.ReturnType);
                    }

                    foreach (System.Type genericArgumentType in method.GetGenericArguments())
                    {
                        RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, genericArgumentType);
                    }

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, parameter.ParameterType);
                    }
                }

                if (closedType.BaseType != null)
                {
                    RegisterUniqueClosedType(assemblyWhitelist, uniqueClosedTypes, closedType.BaseType);
                }
            }   // RegisterUniqueClosedType

            bool IsAuthoredByMelcher(System.Type type)
            {
                string ns = type.Namespace;

                string melcherNamespace = "Melcher";

                return
                    ns != null &&
                    ns.StartsWith(melcherNamespace) &&
                    (ns.Length == melcherNamespace.Length || ns[melcherNamespace.Length - 1] == '.');
            }   // IsAuthoredByMelcher

            /// <summary>
            /// Nested function that finds all methods decorated with a
            /// <see cref="MelcherResetStaticFieldsMethodAttribute"/> and then
            /// attempts to invoke it.
            /// <br /><br />
            ///
            /// If a function is found with the attribute and it doesn't match
            /// the expected signature, i.e., static and returning an int, a
            /// warning will be logged.
            /// </summary>
            void InvokeAllResetStaticFieldMethods(System.Type concreteType, bool showWarnings)
            {
                // Find and execute all ResetStaticFields methods. These are
                // known because they are static and marked by an attribute.

                IEnumerable<MethodInfo> resetStaticFieldMethods =
                    concreteType.GetMethods(AnythingLocal)
                        .Where(method => System.Attribute.IsDefined(method, resetStaticFieldsAttributeType));

                if (!resetStaticFieldMethods.Any())
                {
                    // No static field reset methods were declared here. Ensure
                    // the type does not define any static fields that should
                    // be reset.

                    if (showWarnings)
                    {
                        MelcherEditorUtility.AssertClassStaticFieldsAreResetForDomainNonReload(
                            concreteType,
                            staticFieldResetCount: 0,
                            concreteType.Name,  // TODO: Remove the backtick if it exists?
                            callerMemberName: nameof(MelcherResetStaticFieldsMethodAttribute)
                        );
                    }

                    return;
                }

                int totalFieldsReset = 0;

                foreach (MethodInfo resetStaticFieldMethod in resetStaticFieldMethods)
                {
                    if (!resetStaticFieldMethod.IsStatic)
                    {
                        // For a better user experience, print an error when the
                        // reset-static-fields attribute is misapplied to
                        // non-static functions.

                        if (showWarnings)
                        {
                            MelcherLog.LogError(
                                $"The function {MelcherFormat.ConsoleMethodName(resetStaticFieldMethod.Name)} " +
                                $"must be {MelcherFormat.ConsoleKeyword("static")} " +
                                $"to work with {MelcherFormat.ConsoleTypeName<MelcherResetStaticFieldsMethodAttribute>()}.\n" +
                                $"The function will not be called.",
                                callerFilePath: resetStaticFieldMethod.DeclaringType.Name + ".cs"
                            );
                        }

                        continue;
                    }

                    object resetMethodResult =
                        resetStaticFieldMethod.Invoke(obj: null, parameters: System.Array.Empty<object>());

                    if (!showWarnings)
                    {
                        return;
                    }

                    if (resetStaticFieldMethod.ReturnType == typeof(int))
                    {
                        // Double-check that the number of fields the user
                        // reported were reset matches the number of static
                        // fields declared directly by the class.

                        int fieldsResetByUser = (int)resetMethodResult;

                        MelcherLog.Assert(
                            fieldsResetByUser >= 0,
                            $"{MelcherFormat.ConsoleTypeName<MelcherResetStaticFieldsMethodAttribute>()} functions should not return negative numbers."
                        );

                        checked
                        {
                            totalFieldsReset += fieldsResetByUser;
                        }
                    }
                    else
                    {
                        MelcherLog.LogWarning(
                            $"{MelcherFormat.ConsoleMethodName(MelcherCommon.PrettyTypeName(concreteType) + "." + resetStaticFieldMethod.Name)} " +
                            $"should return an {MelcherFormat.ConsoleTypeName("int")} " +
                            $"indicating the number of static fields that " +
                            $"were reset.\n" +
                            $"As is, the reset function is called but the " +
                            $"system cannot verify if all static fields were " +
                            $"reset."
                        );
                    }
                }

                MelcherEditorUtility.AssertClassStaticFieldsAreResetForDomainNonReload(
                    concreteType,
                    staticFieldResetCount: totalFieldsReset,
                    concreteType.Name,  // TODO: Remove the backtick if it exists?
                    callerMemberName: nameof(MelcherResetStaticFieldsMethodAttribute)
                    //callerFilePath: resetStaticFieldMethod.DeclaringType.Name + ".cs",
                    //callerMemberName: resetStaticFieldMethod.Name
                );
            }   // InvokeAllResetStaticFieldMethods

            /// <summary>
            ///     Find fields that are marked with <see
            ///     cref="MelcherResetStaticFieldInfoAttribute"/> and reset
            ///     them according to their internal reset mode.
            /// </summary>
            void ResetMarkedFields(System.Type concreteType, bool showWarnings)
            {
                // We assume that only one MelcherResetStaticFieldInfoAttribute
                // can be attached at a time. Let's ensure this assumption is
                // true.
                System.AttributeUsageAttribute attributeUsage =
                    typeof(MelcherStaticFieldEditorTreatment.BaseStaticFieldAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

                MelcherLog.Assert(attributeUsage != null);
                MelcherLog.Assert(attributeUsage.AllowMultiple == false);

                foreach (FieldInfo fieldInfo in concreteType.GetFields(AnythingLocal))
                {
                    MelcherStaticFieldEditorTreatment.BaseStaticFieldAttribute attribute =
                        fieldInfo.GetCustomAttribute<MelcherStaticFieldEditorTreatment.BaseStaticFieldAttribute>();

                    if (attribute == null)
                    {
                        // No "reset static field info" attribute on this field
                        // for this function to process.
                        continue;
                    }

                    if (attribute is MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessionsAttribute)
                    {
                        // Skip ignored fields. The user intends for their
                        // value to remain unmodified.
                        continue;
                    }

                    if (!fieldInfo.IsStatic)
                    {
                        // For a better user experience, print an error when the
                        // reset-static-fields attribute is misapplied to
                        // non-static functions.

                        if (showWarnings)
                        {
                            MelcherLog.LogError(
                                $"The field {MelcherFormat.ConsoleFieldName(fieldInfo.Name)} " +
                                $"must be {MelcherFormat.ConsoleKeyword("static")} " +
                                $"to work with {MelcherFormat.ConsoleTypeName(attribute.GetType())}.\n" +
                                $"The field will skipped.",
                                callerFilePath: fieldInfo.DeclaringType.Name + ".cs"
                            );
                        }

                        continue;
                    }

                    object fieldValue = fieldInfo.GetValue(obj: null);

                    if (attribute is MelcherStaticFieldEditorTreatment.ClearCollectionAttribute)
                    {
                        if (fieldValue == null)
                        {
                            // Skip null values because they can't be cleared
                            // and they don't inherently represent an error.
                            continue;
                        }

                        System.Type genericFieldValueType = fieldValue.GetType();

                        if (genericFieldValueType.IsGenericType)
                        {
                            // Convert a closed type (e.g., Queue<int>) to its
                            // open type (e.g., Queue<>). This lets us compare
                            // collection types without needing to know their
                            // element types.

                            genericFieldValueType = genericFieldValueType.GetGenericTypeDefinition();
                        }

                        if (genericFieldValueType == typeof(IList<>) ||
                            genericFieldValueType == typeof(IDictionary<,>) ||
                            genericFieldValueType == typeof(List<>) ||
                            genericFieldValueType == typeof(Dictionary<,>) ||
                            genericFieldValueType == typeof(Queue) ||
                            genericFieldValueType == typeof(Queue<>) ||
                            genericFieldValueType == typeof(Stack) ||
                            genericFieldValueType == typeof(Stack<>) ||
                            genericFieldValueType == typeof(HashSet<>))
                        {
                            System.Type fieldValueType = fieldValue.GetType();

                            MethodInfo theClearMethod =
                                fieldValueType.GetMethod(
                                    "Clear",
                                    BindingFlags.Instance | BindingFlags.Public
                                );

                            try
                            {
                                theClearMethod.Invoke(obj: fieldValue, parameters: null);
                            }
                            catch (System.Exception e)
                            {
                                MelcherLog.LogError(
                                    $"Cannot clear the collection {MelcherFormat.ConsoleFieldName(fieldInfo.Name)} " +
                                    $"because the reflection-found Clear() method threw an exception: {e}"
                                );
                            }
                        }
                        else
                        {
                            MelcherLog.LogError(
                                $"Unsupported collection type, {MelcherFormat.ConsoleTypeName(fieldInfo.FieldType)}, " +
                                $"used by {MelcherFormat.ConsoleFieldName(fieldInfo.Name)} alongside " +
                                $"static field reset mode, {MelcherFormat.ConsoleTypeName<MelcherStaticFieldEditorTreatment.ClearCollectionAttribute>()}."
                            );
                        }
                    }
                    else if (attribute is MelcherStaticFieldEditorTreatment.ResetToTypeDefaultAttribute)
                    {
                        // Conveniently, FieldInfo.SetValue() will implicitly use
                        // the type's default value for a field if we pass it a `null`
                        // value.
                        fieldInfo.SetValue(obj: null, value: null);
                    }
                }
            }   // ResetMarkedFields

            /// <summary>
            ///     Nested function that finds all methods decorated with a
            ///     <see cref="MelcherAfterResetStaticFieldsMethodAttribute"/>
            ///     and then attempts to invoke them.
            /// <br /><br />
            ///
            ///     If a function is found with the attribute and it doesn't
            ///     match the expected signature, i.e., a static void returning
            ///     function, a warning will be logged.
            /// </summary>
            void InvokeAllAfterResetStaticFieldMethods(System.Type concreteType)
            {
                // Find and execute all ResetStaticFields methods. These are
                // known because they are static and marked by an attribute.

                IEnumerable<MethodInfo> afterResetStaticFieldMethods =
                    concreteType.GetMethods(AnythingLocal)
                        .Where(method => System.Attribute.IsDefined(method, afterResetStaticFieldsAttributeType));

                if (!afterResetStaticFieldMethods.Any())
                {
                    // No "execute after static fields are reset" methods were
                    // declared in this type.

                    return;
                }

                foreach (MethodInfo afterResetStaticFieldMethod in afterResetStaticFieldMethods)
                {
                    if (!afterResetStaticFieldMethod.IsStatic)
                    {
                        // For a better user experience, print an error when
                        // the attribute was applied to non-static functions.

                        if (showWarnings)
                        {
                            MelcherLog.LogError(
                                $"The function {MelcherFormat.ConsoleMethodName(afterResetStaticFieldMethod.Name)} " +
                                $"must be {MelcherFormat.ConsoleKeyword("static")} " +
                                $"to work with {MelcherFormat.ConsoleTypeName<MelcherAfterResetStaticFieldsMethodAttribute>()}.\n" +
                                $"The function will not be called.",
                                callerFilePath: afterResetStaticFieldMethod.DeclaringType.Name + ".cs"
                            );
                        }

                        continue;
                    }
                    else if (afterResetStaticFieldMethod.ReturnType != typeof(void))
                    {
                        MelcherLog.LogWarning(
                            $"The function {MelcherFormat.ConsoleMethodName(afterResetStaticFieldMethod.Name)} " +
                            $"should return void.",
                            callerFilePath: afterResetStaticFieldMethod.DeclaringType.Name + ".cs"
                        );
                    }

                    afterResetStaticFieldMethod.Invoke(obj: null, parameters: System.Array.Empty<object>());
                }
            }   // InvokeAllAfterResetStaticFieldMethods

        }

        /// <summary>
        ///     A toggleable menu item to enable or disable the warnings this
        ///     feature emits.
        ///     <br /><br />
        ///
        ///     This is intended as a temporary feature to help migrate existing
        ///     projects over to Melcher Core.
        /// </summary>
        [MenuItem(itemName: s_ToggleWarningsMenuItemPath)]
        private static void ToggleWarnings()
        {
            bool showWarnings = EditorPrefs.HasKey(EditorPrefsKey.ShowWarnings);

            showWarnings = !showWarnings;

            Menu.SetChecked(s_ToggleWarningsMenuItemPath, showWarnings);

            if (showWarnings)
            {
                EditorPrefs.SetBool(key: EditorPrefsKey.ShowWarnings, value: showWarnings);
            }
            else
            {
                EditorPrefs.DeleteKey(key: EditorPrefsKey.ShowWarnings);
            }
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        ///     The menu item path to toggle warnings on or off.
        /// </summary>
        private const string s_ToggleWarningsMenuItemPath =
            MelcherEditorConst.Menu.ToolsEditorSettingsPrefix + "Show reset static fields feature warnings";

        public static class EditorPrefsKey
        {
            /// <summary>
            ///     An EditorPrefs key that saves whether the user wants to see
            ///     warnings emitted by this feature.
            /// </summary>
            [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
            public static readonly string ShowWarnings = typeof(MelcherResetStaticFieldsFeature).FullName + ".ShowWarnings";

        }   // static class EditorPrefsKey

    }   // static class MelcherResetStaticFieldsFeature

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-07
/// ===========================================================================
