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
    public static class MelcherObjectPoolCyclicTests
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
        public static void TestCreateElementFunctionIsCalledCapacityTimes()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                int createElementCalls = 0;

                var pool =
                    new MelcherObjectPoolCyclic<object>(
                        capacity,
                        createElement: () =>
                        {
                            ++createElementCalls;

                            return new object();
                        }
                    );

                Assert.AreEqual(capacity, createElementCalls);
            }
        }

        [Test]
        public static void TestAllObjectsIsNotNull()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                var pool = CreatePool(capacity);

                var elements = pool.allObjects;

                Assert.IsNotNull(elements);
            }
        }

        [Test]
        public static void TestAllObjectsCountMatchesCapacity()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                var pool = CreatePool(capacity);

                var elements = pool.allObjects;

                Assert.AreEqual(capacity, elements.Count);
            }
        }


        [Test]
        public static void TestAllObjectsContainsNoNulls()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                var pool = CreatePool(capacity);

                var elements = pool.allObjects;

                foreach (var element in pool.allObjects)
                {
                    Assert.NotNull(element);
                }
            }
        }

        [Test]
        public static void TestGetObjectNeverReturnsNull()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                var pool = CreatePool(capacity);

                int maxBorrowAttempts = capacity * 4;

                for (int borrowAttempt = 0; borrowAttempt < maxBorrowAttempts; ++borrowAttempt)
                {
                    var borrowed = pool.GetObject();

                    Assert.NotNull(borrowed);
                }
            }
        }

        [Test]
        public static void TestGetObjectWrapsBackToStart()
        {
            foreach (int capacity in PoolCapacitiesToTest)
            {
                DoTest(capacity);
            }

            void DoTest(int capacity)
            {
                var pool = CreatePool(capacity);

                int maxBorrowAttempts = capacity * 4;

                for (int borrowAttempt = 0; borrowAttempt < maxBorrowAttempts; ++borrowAttempt)
                {
                    var borrowed = pool.GetObject();

                    if (borrowed == null)
                    {
                        continue;
                    }

                    // Calculate the wrapped pool index
                    int expectedPoolIndex = borrowAttempt % capacity;

                    var expectedBorrowed = pool.allObjects.ElementAt(expectedPoolIndex);

                    Assert.AreEqual(expectedBorrowed, borrowed);
                }
            }

        }

        private static MelcherObjectPoolCyclic<object> CreatePool(int capacity = 128)
        {
            return MelcherObjectPoolCyclic<object>.MakeDefault<object>(capacity);
        }

    }   // static class MelcherObjectPoolCyclicTests

}   // namespace Melcher

#endif  // if ENABLE_TESTS

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2023-02-11
/// ===========================================================================
