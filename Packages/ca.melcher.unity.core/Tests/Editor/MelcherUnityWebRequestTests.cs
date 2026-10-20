#if ENABLE_TESTS
namespace Melcher.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.Networking;
    using UnityEditor;

    /// <summary>
    /// Tests assumptions made about Unity's <see cref="UnityWebRequest"/>.
    /// </summary>
    public class MelcherUnityWebRequestTests
    {
        public const string KnownGoodUrl = "https://www.google.ca";

        // =====================================================================
        // WebRequest
        // =====================================================================

        [Test]
        public static void SetHeaderThrowsArgumentExceptionIfValueIsNull()
        {
            Assert.Throws<System.ArgumentException>(
                () =>
                {
                    UnityWebRequest request = UnityWebRequest.Get(KnownGoodUrl);
                    request.SetRequestHeader("HeaderKey", null);
                }
            );
        }

        [Test]
        public static void SetHeaderThrowsArgumentExceptionIfValueIsNull2()
        {
                    UnityWebRequest request = UnityWebRequest.Get(KnownGoodUrl);
                    request.SetRequestHeader("HeaderKey", "");
            // Assert.Throws<System.ArgumentException>(
            //     () =>
            //     {
            //     }
            // );
        }


        // =====================================================================
        // HTTP Get
        // =====================================================================

        /// <summary>
        /// Test assumptions about passing a <see langword="null"/> URL to a
        /// <see cref="UnityWebRequest"/>.
        /// </summary>
        [UnityTest]
        public static IEnumerator TestGetUrlNullRules()
        {
            string url = null;

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.FormatFailedMessage(url)} not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = UnityWebRequest.Get(url);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestGetUrlEmptyRules()
        {
            string url = "";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = UnityWebRequest.Get(url);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestGetUrlSingleSlashRules()
        {
            string url = "/";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Cannot connect to destination host";

                using var r = UnityWebRequest.Get(url);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestGetUrlNoHostNameRules()
        {
            string url = "http://";

            Assert.Throws<System.UriFormatException>(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should throw")
            );

            yield break;
        }

        [UnityTest]
        public static IEnumerator TestGetUrlUnknownProtocolRules()
        {
            string url = "unknown://";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = UnityWebRequest.Get(url);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestGetUrlIncompleteProtocolRules()
        {
            string url = "http:/";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = UnityWebRequest.Get(url);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Cannot connect to destination host";

                using var r = UnityWebRequest.Get(url);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        // =====================================================================
        // HTTP Post
        // =====================================================================

        /// <summary>
        /// Test assumptions about passing a <see langword="null"/> URL to a
        /// <see cref="UnityWebRequest"/>.
        /// </summary>
        [UnityTest]
        public static IEnumerator TestPostUrlNullRules()
        {
            string url = null;

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.FormatFailedMessage(url)} not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = Post(url, null);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with a url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestPostUrlEmptyRules()
        {
            string url = "";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = Post(url, null);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestPostUrlSingleSlashRules()
        {
            string url = "/";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Cannot connect to destination host";

                using var r = Post(url, null);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestPostUrlNoHostNameRules()
        {
            string url = "http://";

            Assert.Throws<System.UriFormatException>(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should throw")
            );

            yield break;
        }

        [UnityTest]
        public static IEnumerator TestPostUrlUnknownProtocolRules()
        {
            string url = "unknown://";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Malformed URL";

                using var r = Post(url, null);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        [UnityTest]
        public static IEnumerator TestPostUrlIncompleteProtocolRules()
        {
            string url = "http:/";

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                },
                MelcherTestUtility.FormatFailedMessage($"Creating a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            Assert.DoesNotThrow(
                () =>
                {
                    using var r = Post(url, null);
                    r.SendWebRequest();
                },
                MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should not throw")
            );

            {
                const string ExpectedRequestError = "Cannot connect to destination host";

                using var r = Post(url, null);
                r.SendWebRequest();
                yield return null;

                Assert.AreEqual(
                    ExpectedRequestError,
                    r.error,
                    MelcherTestUtility.FormatFailedMessage($"Sending a {nameof(UnityWebRequest)} with url {MelcherTestUtility.GetPrintableString(url)} should have a {MelcherTestUtility.GetPrintableString(ExpectedRequestError)} error string")
                );
            }
        }

        static private UnityWebRequest Post(string url, string form)
        {
#if UNITY_2022_2_OR_NEWER
            return UnityWebRequest.PostWwwForm(url, form);
#else
            return UnityWebRequest.Post(url, form);
#endif  // if UNITY_2022_2_OR_NEWER else
        }

    }   // class MelcherUnityWebRequestTests

}   // namespace Melcher.Tests

#endif  // if ENABLE_TESTS
