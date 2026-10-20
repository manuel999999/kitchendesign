#if ENABLE_TESTS
namespace Melcher
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    /// <summary>
    ///
    /// </summary>
    public static class MelcherJsonValueTests
    {
        [Test]
        public static void DefaultValuesAreReliable()
        {
            MelcherJsonValue value = new MelcherJsonValue();

            Assert.IsNull(value.valueType);

            Assert.IsFalse(value.isObject);
            Assert.IsFalse(value.isList);
            Assert.IsFalse(value.isText);
            Assert.IsFalse(value.isFloating);
            Assert.IsFalse(value.isInteger);
            Assert.IsFalse(value.isBoolean);

            Assert.AreEqual(null, value.jsonObject);
            Assert.AreEqual(null, value.list);
            Assert.AreEqual(null, value.text);
            Assert.AreEqual(null, value.floating);
            Assert.AreEqual(null, value.integer);
            Assert.AreEqual(null, value.boolean);
        }

        [Test]
        public static void ObjectValuesAreReliable()
        {
            TestObject(new Dictionary<string, object>());
            TestObject(null);

            void TestObject(Dictionary<string, object> inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(IDictionary<string, object>), value.valueType);

                Assert.IsTrue(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.AreEqual(inputValue, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(null, value.boolean);
            }
        }

        [Test]
        public static void ListValuesAreReliable()
        {
            TestList(new List<object>());
            TestList(null);

            void TestList(IList<object> inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(IList<object>), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsTrue(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(inputValue, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(null, value.boolean);
            }
        }

        [Test]
        public static void StringValuesAreReliable()
        {
            TestString("hello");
            TestString("");
            TestString(null);

            void TestString(string inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(string), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsTrue(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(inputValue, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(null, value.boolean);
            }
        }

        [Test]
        public static void FloatingValuesAreReliable()
        {
            TestFloat64(1.0);
            TestFloat64(0.0);
            TestFloat64(-1.0);
            TestFloat64(10.0);
            TestFloat64(double.MaxValue);
            TestFloat64(double.MinValue);
            TestFloat64(double.PositiveInfinity);
            TestFloat64(double.NegativeInfinity);
            TestFloat64(double.NaN);
            TestFloat64(null);

            TestFloat32(1.0f);
            TestFloat32(0.0f);
            TestFloat32(-1.0f);
            TestFloat32(10.0f);
            TestFloat32(float.MaxValue);
            TestFloat32(float.MinValue);
            TestFloat32(float.PositiveInfinity);
            TestFloat32(float.NegativeInfinity);
            TestFloat32(float.NaN);
            TestFloat32(null);

            void TestFloat64(double? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(double), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsTrue(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.floating.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(inputValue, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(null, value.boolean);
            }

            void TestFloat32(float? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(double), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsTrue(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.floating.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(inputValue, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(null, value.boolean);
            }
        }

        [Test]
        public static void IntegerValuesAreReliable()
        {
            TestInt64(1);
            TestInt64(0);
            TestInt64(-1);
            TestInt64(10);
            TestInt64(long.MaxValue);
            TestInt64(long.MinValue);
            TestInt64(null);

            TestInt32(1);
            TestInt32(0);
            TestInt32(-1);
            TestInt32(10);
            TestInt32(int.MaxValue);
            TestInt32(int.MinValue);
            TestInt32(null);

            TestInt16(1);
            TestInt16(0);
            TestInt16(-1);
            TestInt16(10);
            TestInt16(short.MaxValue);
            TestInt16(short.MinValue);
            TestInt16(null);

            TestInt8(1);
            TestInt8(0);
            TestInt8(-1);
            TestInt8(10);
            TestInt16(sbyte.MaxValue);
            TestInt16(sbyte.MinValue);
            TestInt8(null);

            void TestInt64(long? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(long), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsTrue(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.integer.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(inputValue, value.integer);
                Assert.AreEqual(null, value.boolean);
            }

            void TestInt32(int? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(long), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsTrue(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.integer.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(inputValue, value.integer);
                Assert.AreEqual(null, value.boolean);
            }

            void TestInt16(short? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(long), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsTrue(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.integer.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(inputValue, value.integer);
                Assert.AreEqual(null, value.boolean);
            }

            void TestInt8(sbyte? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(long), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsTrue(value.isInteger);
                Assert.IsFalse(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.integer.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(inputValue, value.integer);
                Assert.AreEqual(null, value.boolean);
            }
        }

        [Test]
        public static void BooleanValuesAreReliable()
        {
            TestBool(true);
            TestBool(false);
            TestBool(null);

            void TestBool(bool? inputValue)
            {
                MelcherJsonValue value = new MelcherJsonValue(inputValue);

                Assert.AreEqual(typeof(bool), value.valueType);

                Assert.IsFalse(value.isObject);
                Assert.IsFalse(value.isList);
                Assert.IsFalse(value.isText);
                Assert.IsFalse(value.isFloating);
                Assert.IsFalse(value.isInteger);
                Assert.IsTrue(value.isBoolean);

                Assert.IsTrue(inputValue.HasValue == value.boolean.HasValue);

                Assert.AreEqual(null, value.jsonObject);
                Assert.AreEqual(null, value.list);
                Assert.AreEqual(null, value.text);
                Assert.AreEqual(null, value.floating);
                Assert.AreEqual(null, value.integer);
                Assert.AreEqual(inputValue, value.boolean);
            }
        }

    }   // class MelcherJsonValueTests

}   // namespace Melcher

#endif  // if ENABLE_TESTS

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-18
/// ===========================================================================
