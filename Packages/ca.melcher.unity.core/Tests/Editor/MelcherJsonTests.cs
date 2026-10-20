#if ENABLE_TESTS
namespace Melcher.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    using JsonObject = System.Collections.Generic.IDictionary<string, object>;

    public class MelcherJsonTests
    {
        private static JsonObject s_JsonRoot;

        public static JsonObject GetOrCreateJsonRoot()
        {
            if (s_JsonRoot == null)
            {
                CreateJsonRoot();
            }

            return s_JsonRoot;
        }

        [Test]
        public static void CreateJsonRoot()
        {
            s_JsonRoot = new Dictionary<string, object>()
            {
                { "null", null },

                { "string", "string" },
                { "string_spaces", "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." },
                { "string_empty", "" },

                { "bool_true", true },
                { "bool_false", false },

                { "enum", System.StringComparison.CurrentCulture },

                { "sbyte", (sbyte)32 },
                { "sbyte_zero", (sbyte)0 },
                { "sbyte_negative", unchecked((sbyte)-200) },
                { "sbyte_max", sbyte.MaxValue },
                { "sbyte_min", sbyte.MinValue },

                { "short", (short)32 },
                { "short_zero", (short)0 },
                { "short_negative", (short)-400 },
                { "short_max", short.MaxValue },
                { "short_min", short.MinValue },

                { "int", 32 },
                { "int_zero", 0 },
                { "int_negative", -400 },
                { "int_max", int.MaxValue },
                { "int_min", int.MinValue },

                { "long", 32L },
                { "long_zero", 0L },
                { "long_negative", -400L },
                { "long_max", long.MaxValue },
                { "long_min", long.MinValue },

                { "byte", (byte)32 },
                { "byte_zero", (byte)0 },
                { "byte_negative", unchecked((byte)-200) },
                { "byte_max", byte.MaxValue },
                { "byte_min", byte.MinValue },

                { "ushort", (ushort)32 },
                { "ushort_zero", (ushort)0 },
                { "ushort_negative", unchecked((ushort)-400) },
                { "ushort_max", ushort.MaxValue },
                { "ushort_min", ushort.MinValue },

                { "uint", 32u },
                { "uint_zero", 0u },
                { "uint_negative", unchecked((uint)-400) },
                { "uint_max", uint.MaxValue },
                { "uint_min", uint.MinValue },

                { "ulong", 32ul },
                { "ulong_zero", 0ul },
                { "ulong_negative", unchecked((ulong)-400) },
                { "ulong_max", ulong.MaxValue },
                { "ulong_min", ulong.MinValue },

                { "float", 32.32f },
                { "float_zero", 0.0f },
                { "float_negative", -400.0f },
                { "float_max", float.MaxValue },
                { "float_min", float.MinValue },
                { "float_nan", float.NaN },
                { "float_posinf", float.PositiveInfinity },
                { "float_neginf", float.NegativeInfinity },

                { "double", 32.32 },
                { "double_zero", 0.0 },
                { "double_negative", -400.0 },
                { "double_max", double.MaxValue },
                { "double_min", double.MinValue },
                { "double_nan", double.NaN },
                { "double_posinf", double.PositiveInfinity },
                { "double_neginf", double.NegativeInfinity },

                {
                    "object",
                    new Dictionary<string, object>()
                },

                {
                    "list",
                    new List<object>()
                    {
                        new Dictionary<string, object>()
                        {
                            { "a", "A" },
                        },
                        new Dictionary<string, object>()
                        {
                            { "b", "B" },
                        },
                        new Dictionary<string, object>()
                        {
                            { "c", "C" },
                        },
                    }
                }
            };
        }

        private static string GetFailedPrefix(string methodName, int lineNumber)
        {
            const string RightArrow = "\u2192";
            return $"[{methodName}] line {lineNumber} {RightArrow} ";
        }

        private static string GetPrintableString(object value)
        {
            return value != null ? $"\"{value}\"" : "null";
        }

        private static void CheckExpectations(
            in  MelcherJsonKeyResult    result,
                string                  expectedKey,
                bool                    expectedKeyExists,
                System.Type             expectedRequestedValueType,
                System.Type             expectedActualValueType,
                object                  expectedActualValue,
                System.Type             expectedExceptionType,
            [CallerMemberName] string   memberName                  = null,
            [CallerLineNumber] int      lineNumber                  = 0)
        {
            string prefix = GetFailedPrefix(memberName, lineNumber);

            Assert.IsTrue(
                result.key == expectedKey,
                $"{prefix} Expected {nameof(result)}.{nameof(result.key)} == {GetPrintableString(expectedKey)}. Got {GetPrintableString(result.key)}"
            );

            Assert.IsTrue(
                result.keyExists == expectedKeyExists,
                $"{prefix} Expected {nameof(result)}.{nameof(result.keyExists)} == {expectedKeyExists}. Got {result.keyExists}"
            );

            Assert.IsTrue(
                result.requestedType == expectedRequestedValueType,
                $"{prefix} Expected {nameof(result)}.{nameof(result.requestedType)} == {GetPrintableString(expectedRequestedValueType)}. Got {GetPrintableString(result.requestedType)}"
            );

            if (result.sourceValue == null)
            {
                // Can't know the value type if it's null
                expectedActualValueType = null;
            }

            Assert.IsTrue(
                result.sourceValueType == expectedActualValueType,
                $"{prefix} Expected {nameof(result)}.{nameof(result.sourceValueType)} == {GetPrintableString(expectedActualValueType)}. Got {GetPrintableString(result.sourceValueType)}"
            );

            Assert.IsTrue(
                object.Equals(result.sourceValue, expectedActualValue),
                $"{prefix} Expected {nameof(result)}.{nameof(result.sourceValue)} == {GetPrintableString(expectedActualValue)}. Got {GetPrintableString(result.sourceValue)}"
            );

            System.Type exceptionType = result.exception?.GetType();
            Assert.IsTrue(
                exceptionType == expectedExceptionType,
                $"{prefix} Expected typeof({nameof(result)}.{nameof(result.exception)}) == {GetPrintableString(expectedExceptionType)}. Got {GetPrintableString(result.exception)}"
            );
        }

        [Test]
        public void MelcherJsonKeyResult_DefaultCtor()
        {
            // Test assumptions about a default MelcherJsonKeyResult

            MelcherJsonKeyResult result = default;

            CheckExpectations(
                in result,
                expectedKey: null,
                expectedKeyExists: false,
                expectedRequestedValueType: null,
                expectedActualValueType: null,
                expectedActualValue: null,
                expectedExceptionType: null
            );
        }

        // =====================================================================
        // MelcherJsonKeyResult.Success
        // =====================================================================

        [Test]
        public void MelcherJsonKeyResult_Success()
        {
            // bool
            MelcherJsonKeyResult_Success_TestForType("bool_true", true);
            MelcherJsonKeyResult_Success_TestForType("bool_false", false);

            // ints
            MelcherJsonKeyResult_Success_TestForType(null, 0);
            MelcherJsonKeyResult_Success_TestForType("", 0);
            MelcherJsonKeyResult_Success_TestForType("int", 1);
            MelcherJsonKeyResult_Success_TestForType("int_neg", -100);
            MelcherJsonKeyResult_Success_TestForType("int_max", int.MaxValue);
            MelcherJsonKeyResult_Success_TestForType("int_min", int.MinValue);

            // unsigned ints
            MelcherJsonKeyResult_Success_TestForType(null, 0u);
            MelcherJsonKeyResult_Success_TestForType("", 0u);
            MelcherJsonKeyResult_Success_TestForType("uint", 1u);
            MelcherJsonKeyResult_Success_TestForType("uint_neg", unchecked((uint)-100));
            MelcherJsonKeyResult_Success_TestForType("uint_max", uint.MaxValue);
            MelcherJsonKeyResult_Success_TestForType("uint_min", uint.MinValue);

            // floats
            MelcherJsonKeyResult_Success_TestForType(null, 0.0f);
            MelcherJsonKeyResult_Success_TestForType("", 0.0f);
            MelcherJsonKeyResult_Success_TestForType("float", 1.0f);
            MelcherJsonKeyResult_Success_TestForType("float_neg", -100.0f);
            MelcherJsonKeyResult_Success_TestForType("float_max", float.MaxValue);
            MelcherJsonKeyResult_Success_TestForType("float_min", float.MinValue);
            MelcherJsonKeyResult_Success_TestForType("float_nan", float.NaN);
            MelcherJsonKeyResult_Success_TestForType("float_posinf", float.PositiveInfinity);
            MelcherJsonKeyResult_Success_TestForType("float_neginf", float.NegativeInfinity);

            // doubles
            MelcherJsonKeyResult_Success_TestForType(null, 0.0);
            MelcherJsonKeyResult_Success_TestForType("", 0.0);
            MelcherJsonKeyResult_Success_TestForType("double", 1.0);
            MelcherJsonKeyResult_Success_TestForType("double_neg", -100.0);
            MelcherJsonKeyResult_Success_TestForType("double_max", double.MaxValue);
            MelcherJsonKeyResult_Success_TestForType("double_min", double.MinValue);
            MelcherJsonKeyResult_Success_TestForType("double_nan", double.NaN);
            MelcherJsonKeyResult_Success_TestForType("double_posinf", double.PositiveInfinity);
            MelcherJsonKeyResult_Success_TestForType("double_neginf", double.NegativeInfinity);

            // enum
            MelcherJsonKeyResult_Success_TestForType(null, System.StringComparison.CurrentCulture);
            MelcherJsonKeyResult_Success_TestForType("", System.StringComparison.CurrentCulture);
            MelcherJsonKeyResult_Success_TestForType("enum", System.StringComparison.CurrentCulture);

            // base object type
            MelcherJsonKeyResult_Success_TestForType(null, (object)null);
            MelcherJsonKeyResult_Success_TestForType("", (object)null);
            MelcherJsonKeyResult_Success_TestForType("object", (object)null);

            // another object type
            MelcherJsonKeyResult_Success_TestForType(null, this);
            MelcherJsonKeyResult_Success_TestForType("", this);
            MelcherJsonKeyResult_Success_TestForType("object", this);

            // string
            MelcherJsonKeyResult_Success_TestForType(null, "hello 1");
            MelcherJsonKeyResult_Success_TestForType("", "hello 2");
            MelcherJsonKeyResult_Success_TestForType("string", "hello 3");

            // string[]
            string[] stringArrayNull = null;
            string[] stringArray0 = new string[] {};
            string[] stringArray3 = new string[] {"one", "two", "three"};
            MelcherJsonKeyResult_Success_TestForType(null, stringArrayNull);
            MelcherJsonKeyResult_Success_TestForType("", stringArray0);
            MelcherJsonKeyResult_Success_TestForType("strings", stringArray3);
        }

        private static void MelcherJsonKeyResult_Success_TestForType<T>(
            string key,
            T value,
            [CallerMemberName] string overrideMemberName = null,
            [CallerLineNumber] int overrideLineNumber = 0)
        {
            // Test assumptions about a success MelcherJsonKeyResult

            MelcherJsonKeyResult result = MelcherJsonKeyResult.Success<T>(key, value);

            CheckExpectations(
                in result,
                expectedKey: key,
                expectedKeyExists: true,
                expectedRequestedValueType: typeof(T),
                expectedActualValueType: typeof(T),
                expectedActualValue: value,
                expectedExceptionType: null,
                memberName: overrideMemberName,
                lineNumber: overrideLineNumber
            );
        }

        // =====================================================================
        // MelcherJsonKeyResult.KeyNotFound
        // =====================================================================

        [Test]
        public void MelcherJsonKeyResult_KeyNotFound()
        {
            // bool
            MelcherJsonKeyResult_KeyNotFound_TestForType("bool_true", true);
            MelcherJsonKeyResult_KeyNotFound_TestForType("bool_false", false);

            // ints
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, 0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", 0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("int", 1);
            MelcherJsonKeyResult_KeyNotFound_TestForType("int_neg", -100);
            MelcherJsonKeyResult_KeyNotFound_TestForType("int_max", int.MaxValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("int_min", int.MinValue);

            // unsigned ints
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, 0u);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", 0u);
            MelcherJsonKeyResult_KeyNotFound_TestForType("uint", 1u);
            MelcherJsonKeyResult_KeyNotFound_TestForType("uint_neg", unchecked((uint)-100));
            MelcherJsonKeyResult_KeyNotFound_TestForType("uint_max", uint.MaxValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("uint_min", uint.MinValue);

            // floats
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, 0.0f);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", 0.0f);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float", 1.0f);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_neg", -100.0f);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_max", float.MaxValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_min", float.MinValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_nan", float.NaN);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_posinf", float.PositiveInfinity);
            MelcherJsonKeyResult_KeyNotFound_TestForType("float_neginf", float.NegativeInfinity);

            // doubles
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, 0.0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", 0.0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double", 1.0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_neg", -100.0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_max", double.MaxValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_min", double.MinValue);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_nan", double.NaN);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_posinf", double.PositiveInfinity);
            MelcherJsonKeyResult_KeyNotFound_TestForType("double_neginf", double.NegativeInfinity);

            // enum
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, System.StringComparison.CurrentCulture);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", System.StringComparison.CurrentCulture);
            MelcherJsonKeyResult_KeyNotFound_TestForType("enum", System.StringComparison.CurrentCulture);

            // base object type
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, (object)null);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", (object)null);
            MelcherJsonKeyResult_KeyNotFound_TestForType("object", (object)null);

            // another object type
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, this);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", this);
            MelcherJsonKeyResult_KeyNotFound_TestForType("object", this);

            // string
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, "hello 1");
            MelcherJsonKeyResult_KeyNotFound_TestForType("", "hello 2");
            MelcherJsonKeyResult_KeyNotFound_TestForType("string", "hello 3");

            // string[]
            string[] stringArrayNull = null;
            string[] stringArray0 = new string[] {};
            string[] stringArray3 = new string[] {"one", "two", "three"};
            MelcherJsonKeyResult_KeyNotFound_TestForType(null, stringArrayNull);
            MelcherJsonKeyResult_KeyNotFound_TestForType("", stringArray0);
            MelcherJsonKeyResult_KeyNotFound_TestForType("strings", stringArray3);
        }

        private static void MelcherJsonKeyResult_KeyNotFound_TestForType<T>(string key, T value, [CallerMemberName] string overrideMemberName = null, [CallerLineNumber] int overrideLineNumber = 0)
        {
            // Test assumptions about a "key not found" MelcherJsonKeyResult

            MelcherJsonKeyResult result = MelcherJsonKeyResult.KeyNotFound<T>(key);

            CheckExpectations(
                in result,
                expectedKey: key,
                expectedKeyExists: false,
                expectedRequestedValueType: typeof(T),
                expectedActualValueType: null,
                expectedActualValue: null,
                expectedExceptionType: typeof(System.Collections.Generic.KeyNotFoundException),
                memberName: overrideMemberName,
                lineNumber: overrideLineNumber
            );
        }

        // =====================================================================
        // MelcherJsonObjectParser_TryGetValueFromJson
        // =====================================================================

        [Test]
        public void MelcherJsonObjectExtensions_TryGetValueFromJson()
        {
            GetOrCreateJsonRoot();
            Assert.NotNull(s_JsonRoot, "No JSON root");

            MelcherJsonObjectExtensions_TryGetValueFromJson_TestForClass<object>("null", expectedReturnValue: true, expectedKeyExists: true, expectedExceptionType: null);
            MelcherJsonObjectExtensions_TryGetValueFromJson_TestForClass<object>("not exists", expectedReturnValue: false, expectedKeyExists: false, expectedExceptionType: typeof(KeyNotFoundException));

            MelcherJsonObjectExtensions_TryGetValueFromJson_TestForStruct<int>("int", expectedReturnValue: true, expectedKeyExists: true, expectedExceptionType: null);
        }

        private static void MelcherJsonObjectExtensions_TryGetValueFromJson_TestForStruct<T>(
            string key,
            bool expectedReturnValue,
            bool expectedKeyExists,
            System.Type expectedExceptionType,
            [CallerMemberName] string memberName = null,
            [CallerLineNumber] int lineNumber = 0)
            where T : struct
        {
            bool returnValue = s_JsonRoot.TryGetValueFromJson(key, out T value, out MelcherJsonKeyResult result);

            DoTest(returnValue, in value, in result, key, expectedReturnValue, expectedKeyExists, expectedExceptionType, memberName, lineNumber);
        }

        private static void MelcherJsonObjectExtensions_TryGetValueFromJson_TestForClass<T>(
            string key,
            bool expectedReturnValue,
            bool expectedKeyExists,
            System.Type expectedExceptionType,
            [CallerMemberName] string memberName = null,
            [CallerLineNumber] int lineNumber = 0)
            where T : class
        {
            bool returnValue = s_JsonRoot.TryGetValueFromJson(key, out T value, out MelcherJsonKeyResult result);

            DoTest(returnValue, in value, in result, key, expectedReturnValue, expectedKeyExists, expectedExceptionType, memberName, lineNumber);
        }

        private static void DoTest<T>(
            bool returnValue,
            in T value,
            in MelcherJsonKeyResult result,
            string key,
            bool expectedReturnValue,
            bool expectedKeyExists,
            System.Type expectedExceptionType,
            string memberName,
            int lineNumber)
        {
            string prefix = GetFailedPrefix(memberName, lineNumber);

            Assert.IsTrue(
                returnValue == expectedReturnValue,
                $"{prefix} Expected {nameof(s_JsonRoot)}.TryGetValueFromJson({GetPrintableString(key)}) == {expectedReturnValue}. Got {returnValue}"
            );

            s_JsonRoot.TryGetValue(key, out object expectedValue);

            Assert.IsTrue(
                object.Equals(expectedValue, value),
                $"{prefix} Expected {nameof(s_JsonRoot)}.TryGetValueFromJson({GetPrintableString(key)})'s out value == {GetPrintableString(expectedValue)}. Got {GetPrintableString(value)}"
            );

            CheckExpectations(
                in result,
                expectedKey: key,
                expectedKeyExists: expectedKeyExists,
                expectedRequestedValueType: typeof(T),
                expectedActualValueType: typeof(T),
                expectedActualValue: expectedValue,
                expectedExceptionType: expectedExceptionType,
                memberName: memberName,
                lineNumber: lineNumber
            );
        }

        [Test]
        public void MelcherJsonObjectParser_GetValue()
        {
        }

    }   // class MelcherJsonTests

}   // namespace Melcher.Tests

#endif  // if ENABLE_TESTS
