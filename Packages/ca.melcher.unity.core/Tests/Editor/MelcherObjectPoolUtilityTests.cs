#if ENABLE_TESTS
namespace Melcher
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.Networking;

    /// <summary>
    ///
    /// </summary>
    public static class MelcherObjectPoolUtilityTests
    {
        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly int[] PoolCapacitiesToTest =
            new []
            {
                0,
                1,
                100,
                10000
            };

        [Test]
        public static void TestCreateForCollectionThrowsIfCreateCollectionReturnsNull()
        {
            Assert.Throws<System.ArgumentException>(
                code: () =>
                {
                    MelcherObjectPoolUtility.CreateForCollection(
                        System.Array.Empty<object>(),
                        createCollection: capacity => (object[])null,
                        addElementToCollection: (collection, elementIndex, element) =>
                        {
                            collection[elementIndex] = element;
                        }
                    );
                }
            );
        }

        [Test]
        public static void TestCreateForCollectionThrowsOnInfiniteElements()
        {
            Assert.Throws<System.ArgumentException>(
                code: () =>
                {
                    MelcherObjectPoolUtility.CreateForCollection(
                        InfiniteCollection(),
                        createCollection: capacity => new object[capacity],
                        addElementToCollection: (collection, elementIndex, element) =>
                        {
                            collection[elementIndex] = element;
                        }
                    );
                }
            );

        }

        [Test]
        public static void TestConstructFromIEnumerableMakesNewCollection()
        {
            DoTest(System.Array.Empty<object>());

            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(InfiniteCollection().Take(capacity));
            }

            void DoTest(IEnumerable<object> elements)
            {
                var pool = new MelcherObjectPoolCyclic<object>(elements);

                if (pool.allObjects.Count == 0)
                {
                    Assert.AreSame(System.Array.Empty<object>(), pool.allObjects);
                }
                else
                {
                    Assert.AreNotSame(pool.allObjects, elements);
                }
            }
        }

        private static IEnumerable<object> InfiniteCollection()
        {
            while (true)
            {
                // To avoid consuming a ton of memory, return the same object
                // for every element. This may cause errors in the future if
                // checks are added to ObjectPools that ensure all elements are
                // unique.

                yield return s_InfinitelyReusedObject;
            }
        }

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static object s_InfinitelyReusedObject = new object();

    }   // static class MelcherObjectPoolUtilityTests

}   // namespace Melcher

#endif  // if ENABLE_TESTS

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-11
/// ===========================================================================
