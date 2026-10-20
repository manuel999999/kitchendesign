namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// This class is intended for utility functions that are mostly free from
    /// non-System APIs. I.e., it tries to minimize the number of namespaces it
    /// includes. The Mathf library is the only non-System API currently used
    /// here because it lends itself to these pure functions and doesn't have
    /// a lot dependency.
    /// </summary>
    public static class MelcherCommon
    {
        // ====================================================================
        // IsAlmostEqual
        //
        // Compare floats and vectors using a tolerance. Prefer these over
        // direct float comparisons to avoid floating point inaccuracy
        // ====================================================================

        /// <summary>
        /// Return <see langword="true"/> if two floats are within a tolerance
        /// distance of each other; otherwise, return <see langword="false"/>.
        /// </summary>
        public static bool IsAlmostEqual(float a, float b, float tolerance)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// Return true if the two vectors are approximately the same (i.e.
        /// their difference is less than <paramref name="tolerance"/>)
        /// </summary>
        public static bool IsAlmostEqual(Vector2 a, Vector2 b, float tolerance)
        {
            return (a - b).magnitude < tolerance;
        }

        /// <inheritdoc cref="Approximately(Vector2 a, Vector2 b)"/>
        public static bool IsAlmostEqual(Vector3 a, Vector3 b, float tolerance)
        {
            return (a - b).magnitude < tolerance;
        }

        /// <inheritdoc cref="Approximately(Vector2 a, Vector2 b)"/>
        public static bool IsAlmostEqual(Vector4 a, Vector4 b, float tolerance)
        {
            return (a - b).magnitude < tolerance;
        }

        // =====================================================================
        // Vector/Padding
        //
        // Unity stores RectOffet's as Vector4's. These functions pack and
        // unpack those values to avoid needing to remember which channel
        // corresponds to which edge.
        // =====================================================================

        public static void UnpackPadding(Vector4 vector, out float left, out float right, out float top, out float bottom)
        {
            left = vector.x;
            right = vector.y;
            top = vector.z;
            bottom = vector.w;
        }

        public static Vector4 PackPadding(float left, float right, float top, float bottom)
        {
            return new Vector4(left, right, top, bottom);
        }

        // ====================================================================
        // Vector - Abs
        // ====================================================================

        /// <summary>
        /// Return a vector whose components have all been made their absolute
        /// value (Mathf.Abs).
        /// </summary>
        public static Vector2 Abs(Vector2 v2)
        {
            return new Vector2(
                Mathf.Abs(v2.x),
                Mathf.Abs(v2.y)
            );
        }

        /// <inheritdoc cref="Abs(Vector2 v2)"/>
        public static Vector3 Abs(Vector3 v3)
        {
            return new Vector3(
                Mathf.Abs(v3.x),
                Mathf.Abs(v3.y),
                Mathf.Abs(v3.z)
            );
        }

        /// <inheritdoc cref="Abs(Vector2 v2)"/>
        public static Vector4 Abs(Vector4 v4)
        {
            return new Vector4(
                Mathf.Abs(v4.x),
                Mathf.Abs(v4.y),
                Mathf.Abs(v4.z),
                Mathf.Abs(v4.w)
            );
        }

        // =====================================================================
        // ComponentWiseDivide
        // =====================================================================

        /// <summary>
        /// Like <see cref="Vector2.Scale(Vector2, Vector2)"/>, but it divides
        /// each component of <paramref name="a"/> by the matching component
        /// from <paramref name="b"/>.
        /// </summary>
        public static Vector2 ComponentWiseDivide(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.x / b.x,
                a.y / b.y
            );
        }

        /// <inheritdoc cref="ComponentWiseDivide(Vector2 a, Vector2 b)"/>
        public static Vector3 ComponentWiseDivide(Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.x / b.x,
                a.y / b.y,
                a.z / b.z
            );
        }

        /// <inheritdoc cref="ComponentWiseDivide(Vector2 a, Vector2 b)"/>
        public static Vector4 ComponentWiseDivide(Vector4 a, Vector4 b)
        {
            return new Vector4(
                a.x / b.x,
                a.y / b.y,
                a.z / b.z,
                a.w / b.w
            );
        }

        // =====================================================================
        // Swap
        // =====================================================================

        public static void Swap<T>(ref T first, ref T second)
        {
            (first, second) = (second, first);
        }

        // =====================================================================
        // Delegate
        // =====================================================================

        /// <summary>
        /// This can convert a delegate from one type to another which is
        /// useful for void or no arg delegates.
        /// </summary>
        public static CastType DuplicateDelegateAs<CastType>(System.MulticastDelegate source)
        {
            System.Delegate result = null;

            foreach (System.Delegate sourceItem in source.GetInvocationList())
            {
                System.Delegate copy = System.Delegate.CreateDelegate(
                    typeof(CastType),
                    sourceItem.Target,
                    sourceItem.Method
                );
                result = System.Delegate.Combine(result, copy);
            }

            return (CastType) (object) result;
        }

        // =====================================================================
        // Collections
        // =====================================================================

        /// <summary>
        /// A generic IndexOf function to fill the gap in LINQ. Built for
        /// IReadOnlyList but may be useful elsewhere.
        /// </summary>
        public static int IndexOf<ElementType>(System.Collections.Generic.IEnumerable<ElementType> enumerable, ElementType value)
        {
            int index = 0;
            foreach (ElementType element in enumerable)
            {
                if (element.Equals(value))
                {
                    return index;
                }

                ++index;
            }

            return -1;
        }

        public static int FindIndex<ElementType>(System.Collections.Generic.IEnumerable<ElementType> enumerable, System.Func<ElementType, bool> predicate)
        {
            int index = 0;
            foreach (ElementType element in enumerable)
            {
                if (predicate(element))
                {
                    return index;
                }

                ++index;
            }

            return -1;
        }

        /// <summary>
        /// Generic Find function. Built for IReadOnlyList but may be useful
        /// elsewhere.
        /// </summary>
        public static ElementType Find<ElementType>(System.Collections.Generic.IEnumerable<ElementType> enumerable, System.Func<ElementType, bool> predicate)
        {
            foreach (ElementType element in enumerable)
            {
                if (predicate.Invoke(element))
                {
                    return element;
                }
            }

            return default;
        }

        /// <summary>
        /// Generic function to check if any element in an enumeration has a
        /// field that matches a given value.
        /// </summary>
        /// <param name="enumerable">
        /// The collection to search.
        /// </param>
        /// <param name="toFind">
        /// The field value to find.
        /// </param>
        /// <param name="predicate">
        /// The lambda that compares the element's field to the requested value.
        /// </param>
        /// <typeparam name="ElementType">
        /// The element type of the collection.
        /// </typeparam>
        /// <typeparam name="FieldType">
        /// The field type of the element to inspect.
        /// </typeparam>
        /// <returns>
        /// Return <see langword="true"/> if any element in the collection has a
        /// field that matches the input value (as determined by the predicate);
        /// otherwise, return <see langword="false"/>.
        /// </returns>
        public static bool CollectionContainsMatchingField<ElementType, FieldType>(System.Collections.Generic.IEnumerable<ElementType> enumerable, FieldType toFind, System.Func<ElementType, FieldType> predicate)
        {
            foreach (ElementType element in enumerable)
            {
                if (object.Equals(predicate.Invoke(element), toFind))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fisher-Yates shuffle.
        /// </summary>
        public static void Shuffle<ElementType>(IList<ElementType> list)
        {
            for (int i = list.Count - 1; i > 0; --i)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                ElementType toSwap = list[i];
                list[i] = list[r];
                list[r] = toSwap;
            }
        }

        // =====================================================================
        // Frac
        // =====================================================================

        /// <summary>
        /// Return the decimal part of a float. The sign of the returned decimal
        /// will match the sign of <paramref name="f">.
        /// </summary>
        public static float Frac(float f)
        {
            return f - (int)f;
        }

        /// <summary>
        /// Modulus operation that behaves like Python's modulus: the return
        /// value will be between 0 and <paramref name="m"/>. Where negative
        /// return values are are normally returned from modulus, this function
        /// wraps those values around from the second argument.
        /// </summary>
        /// <remarks>
        /// Examples:<br />
        ///         AbsMod( 2,  6) =>  2<br />
        ///         AbsMod(-2,  6) =>  4<br />
        ///         AbsMod( 2, -6) => -4<br />
        ///         AbsMod(-2, -6) => -2
        /// </remarks>
        public static int AbsMod(int x, int m)
        {
            return ((x % m) + m) % m;
        }

        // ====================================================================
        // Remap
        // ====================================================================

        public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            float t = (value - oldMin) / (oldMax - oldMin);
            return (newMax - newMin) * t + newMin;
        }

        public static float RemapFrom01(float value, float newMin, float newMax)
        {
            return Remap(value, 0.0f, 1.0f, newMin, newMax);
        }

        public static float RemapTo01(float value, float oldMin, float oldMax)
        {
            return Remap(value, oldMin, oldMax, 0.0f, 1.0f);
        }

        // =====================================================================
        // Events
        // =====================================================================

        public static ResultType Invoke<ResultType>(Func<ResultType> action)
        {
            return action.Invoke();
        }

        public static ResultType Invoke<Argument1Type, ResultType>(Func<Argument1Type, ResultType> action, Argument1Type arg1)
        {
            return action.Invoke(arg1);
        }

        public static ResultType Invoke<Argument1Type, Argument2Type, ResultType>(Func<Argument1Type, Argument2Type, ResultType> action, Argument1Type arg1, Argument2Type arg2)
        {
            return action.Invoke(arg1, arg2);
        }

        /// <summary>
        /// Invoke all of a delegate's listeners. This continue invoking even if
        /// one throws an exception. Any exception raised is logged but
        /// otherwise ignored.
        ///
        /// This is useful to invoke callbacks or events whose success or
        /// failure should not affect the remainder of a function. E.g., if a
        /// function creates a scene and, as a side-effect, notifies objects
        /// listening for scene load events, it may be desirable to notify the
        /// listeners using this function to ensure any exceptions in the
        /// listeners' code doesn't block other listers or interrupt the
        /// rest of the function.
        ///
        /// <code>
        /// Action myAction = // ...
        /// myAction?.InvokeAndIgnoreExceptions();
        /// </code>
        /// </summary>
        public static void InvokeAndIgnoreExceptions<DelegateType>(this DelegateType action)
            where DelegateType : System.Delegate
        {
            if (action == null)
            {
                throw new System.ArgumentNullException(nameof(action), "delegate has no listeners");
            }

            foreach (System.Delegate listener in action.GetInvocationList())
            {
                try
                {
                    // Minimizing GC allocation by using Array.Empty
                    listener.DynamicInvoke(System.Array.Empty<object>());
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// <inheritdoc cref="InvokeAndIgnoreExceptions{DelegateType}(DelegateType)"/>
        /// This version invokes the listener with arguments.
        /// </summary>
        public static void InvokeAndIgnoreExceptions<DelegateType>(this DelegateType action, params object[] args)
            where DelegateType : System.Delegate
        {
            if (action == null)
            {
                throw new System.ArgumentNullException(nameof(action), "delegate has no listeners");
            }

            foreach (System.Delegate listener in action.GetInvocationList())
            {
                try
                {
                    listener.DynamicInvoke(args);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // =====================================================================
        // File paths
        // =====================================================================

        public static string GetCallerSourceCodePath(
            [System.Runtime.CompilerServices.CallerFilePath]
            string path = null)
        {
            return path;
        }

        public static bool IsPlatformPathsCaseSensitive()
        {
#if UNITY_STANDALONE_WIN
            return false;
#else
            return true;
#endif
        }

        // =====================================================================
        // Unsorted
        // =====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        public static readonly string[] LineEndings =
            new[]
            {
                "\r\n",
                "\r",
                "\n"
            };

        public static string[] SplitNewLines(string input, int count = int.MaxValue, System.StringSplitOptions splitOptions = System.StringSplitOptions.None)
        {
            return input.Split(LineEndings, count, splitOptions);
        }

        public static int ArithmeticSignAsInt(float f)
        {
            return (int)Mathf.Sign(f);
        }

        /// <inheritdoc cref="PrettyTypeName(System.Type, bool)"/>
        public static string PrettyTypeName(System.Type type)
        {
            return PrettyTypeName(type, useFullName: false);
        }

        /// <summary>
        ///     Given a type, return a string representing the types name. The
        ///     string is formatted to look similar to what would be shown in
        ///     C# source code.
        ///     <br /><br />
        ///
        ///     E.g., "List<int>" instead of "List`1".
        /// </summary>
        public static string PrettyTypeName(System.Type type, bool useFullName)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsArray)
            {
                return PrettyTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsByRef)
            {
                return PrettyTypeName(type.GetElementType()) + "&";
            }

            if (type.IsGenericType)
            {
                // Format the generic arguments so the type name looks less
                // cryptic and more like it does in source code.
                // E.g., "List<int>" instead of "List`1"

                string typeName = type.Name;

                int backtickIndex =
                    typeName.IndexOf("`", System.StringComparison.InvariantCulture);

                MelcherLog.Assert(backtickIndex >= 0, "Generic types should have a backtick in their reflected type name");

                string nonBacktickTypeName = typeName.Substring(0, backtickIndex);

                return
                    string.Format(
                        "{0}<{1}>",
                        arg0: nonBacktickTypeName,
                        arg1: string.Join(", ", System.Linq.Enumerable.Select(type.GenericTypeArguments, PrettyTypeName))
                    );
            }

            if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: true))
            {
                // The type was generated by the compiler and doesn't have an
                // intuitive name. Its declaring type is prepended to make it
                // more clear where it is coming from, but the type name will
                // still be cryptic.

                return type.DeclaringType + ".[CompilerGenerated]" + type.Name;
            }

            // System.Type.FullName will return null if the type is a generic
            // type parameter, a generic type definition, or an array type with
            // element type that is a generic type parameter or a generic type
            // definition. Additionally, if the type is a pointer type or a
            // by-ref type, then the FullName property will return null.

            return useFullName ? type.FullName : type.Name;
        }

        /// <summary>
        ///     Create <typeparamref name="ObjectType"/> by calling its default
        ///     constructor.
        /// </summary>
        public static ObjectType DefaultConstructor<ObjectType>()
            where ObjectType : new()
        {
            return new ObjectType();
        }

        /// <summary>
        ///     Create a <typeparamref name="DefaultConstructibleObjectType"/>
        ///     by calling its default constructor, but cast it to an
        ///     <typeparamref name="ObjectType"/> on return.
        ///     <br /><br />
        ///
        ///     This is useful for converting from a type that has the
        ///     <c>new()</c> generic restraint to one that doesn't.
        /// </summary>
        public static ObjectType DefaultConstructor<DefaultConstructibleObjectType, ObjectType>()
            where DefaultConstructibleObjectType : ObjectType, new()
        {
            return new DefaultConstructibleObjectType();
        }

    }   // class MelcherCommon

}   // namespace Melcher

