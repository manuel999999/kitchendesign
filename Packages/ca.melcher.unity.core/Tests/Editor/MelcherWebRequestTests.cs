#if ENABLE_TESTS
namespace Melcher.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.Networking;

    public class MelcherWebRequestTests
    {
        [Test]
        public static void CanFormDataBeWhiteSpace()
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

            // Will error?
            formData.Add(
                new MultipartFormDataSection("", " ")
            );

            UnityWebRequest request = UnityWebRequest.Post("127.0.0.1/nowhere", formData);

            string payload = System.Text.Encoding.UTF8.GetString(request.uploadHandler.data);

            MelcherLog.Log("Payload=\n" + payload);
        }

        [UnityTest]
        public static IEnumerator TestIt()
        {
            Dictionary<string, object> sentJson =
                new Dictionary<string, object>()
                {
                    { "hello", "world" },
                    { "a", 1 }
                };

            yield return MelcherWebRequestBuilder.Get("https://postman-echo.com/get")
                .SetJsonPayload(new MelcherJsonValue(sentJson))
                .Send();
        }

        [Test]
        public static void TestFactoryFunctionGetIsNeverNull()
        {
            int cycleCount = 1000;
            for (int i = 0; i < cycleCount; ++i)
            {
                MelcherWebRequestBuilder request = MelcherWebRequestBuilder.Get("a");

                Assert.NotNull(request);
            }
        }

        // =====================================================================
        // Url Parameters - Without Values
        // =====================================================================

        private static string GetEscapedUrlWithParameterKeysWithoutValues(string baseUrl, params string[] urlParameterKeys)
        {
            MelcherWebRequestBuilder request = MelcherWebRequestBuilder.Get(baseUrl);

            foreach (string parameter in urlParameterKeys)
            {
                request
                    .AddUrlParameter(parameter);
            }

            return request.GetUrlWithUrlParameters();
        }

        [Test]
        public static void UrlParameterSpacesAreEscapedAsPluses()
        {
            string url = GetEscapedUrlWithParameterKeysWithoutValues("host", "a b");

            Assert.AreEqual("host?a+b", url);
        }


        [Test]
        public static void UrlParameterNullKeyThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => GetEscapedUrlWithParameterKeysWithoutValues("host", new string[] { null })
            );
        }

        [Test]
        public static void UrlParameterEmptyKeyThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithoutValues("host", "")
            );
        }

        [Test]
        public static void UrlParameterWhiteSpaceKeyThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithoutValues("host", "  ")
            );
        }

        [Test]
        public static void UrlParameterSpecialCharactersAreEscaped()
        {
            string url = GetEscapedUrlWithParameterKeysWithoutValues("host", "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~");

            Assert.AreEqual("host?%21%22%23%24%25%26%27()*%2b%2c-.%2f0123456789%3a%3b%3c%3d%3e%3f%40ABCDEFGHIJKLMNOPQRSTUVWXYZ%5b%5d%5e_%60abcdefghijklmnopqrstuvwxyz%7b%7c%7d~", url);
        }

        [Test]
        public static void UrlParametersAreAppendedWithAmpersandWhenUrlAlreadyHasUrlParameters()
        {
            string url = GetEscapedUrlWithParameterKeysWithoutValues("host?first", "second");

            Assert.AreEqual("host?first&second", url);
        }

        [Test]
        public static void UrlParametersAreJoinedWithQuestionMarkFirstAndThenAmpersands()
        {
            string url = GetEscapedUrlWithParameterKeysWithoutValues("host", "a", "b", "c", "d");

            Assert.AreEqual("host?a&b&c&d", url);
        }

        // =====================================================================
        // Url Parameters - With Values
        // =====================================================================

        private static string GetEscapedUrlWithParameterKeysWithValues(string baseUrl, params KeyValuePair<string, string>[] urlParameters)
        {
            MelcherWebRequestBuilder request = MelcherWebRequestBuilder.Get(baseUrl);

            foreach (KeyValuePair<string, string> parameter in urlParameters)
            {
                request
                    .AddUrlParameter(parameter.Key, parameter.Value);
            }

            return request.GetUrlWithUrlParameters();
        }

        [Test]
        public static void UrlParameterSpacesAreEscapedAsPlusesWithValues()
        {
            string url = GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("a b", "c d"));

            Assert.AreEqual("host?a+b=c+d", url);
        }

        [Test]
        public static void UrlParameterNullKeyThrowsArgumentNullExceptionWithValues()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>(null, null))
            );

            Assert.Throws<System.ArgumentNullException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>(null, "value"))
            );
        }

        [Test]
        public static void UrlParameterNullValueDoesNotThrow()
        {
            string url = GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("a", null));

            Assert.AreEqual("host?a", url);
        }

        [Test]
        public static void UrlParameterEmptyKeyThrowsArgumentExceptionWithValues()
        {
            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("", null))
            );

            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("", "value"))
            );
        }

        [Test]
        public static void UrlParameterWhiteSpaceKeyThrowsArgumentExceptionWithValues()
        {
            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("  ", null))
            );

            Assert.Throws<System.ArgumentException>(
                () => GetEscapedUrlWithParameterKeysWithValues("host", new KeyValuePair<string, string>("  ", "value"))
            );
        }

        [Test]
        public static void UrlParameterSpecialCharactersAreEscapedWithValues()
        {
            string url =
                GetEscapedUrlWithParameterKeysWithValues(
                    "host",
                    new KeyValuePair<string, string>(
                        "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~",
                        "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~")
                );

            Assert.AreEqual("host?%21%22%23%24%25%26%27()*%2b%2c-.%2f0123456789%3a%3b%3c%3d%3e%3f%40ABCDEFGHIJKLMNOPQRSTUVWXYZ%5b%5d%5e_%60abcdefghijklmnopqrstuvwxyz%7b%7c%7d~=%21%22%23%24%25%26%27()*%2b%2c-.%2f0123456789%3a%3b%3c%3d%3e%3f%40ABCDEFGHIJKLMNOPQRSTUVWXYZ%5b%5d%5e_%60abcdefghijklmnopqrstuvwxyz%7b%7c%7d~", url);
        }

        [Test]
        public static void UrlParametersAreAppendedWithAmpersandWhenUrlAlreadyHasUrlParametersWithValues()
        {
            string url = GetEscapedUrlWithParameterKeysWithValues("host?first", new KeyValuePair<string, string>("second", "2"));

            Assert.AreEqual("host?first&second=2", url);
        }

        [Test]
        public static void UrlParametersAreJoinedWithQuestionMarkFirstAndThenAmpersandsWithValues()
        {
            string url =
                GetEscapedUrlWithParameterKeysWithValues(
                    "host",
                    new KeyValuePair<string, string>("a", "1"),
                    new KeyValuePair<string, string>("b", "2"),
                    new KeyValuePair<string, string>("c", "3"),
                    new KeyValuePair<string, string>("d", "4")
                );

            Assert.AreEqual("host?a=1&b=2&c=3&d=4", url);
        }

        // =====================================================================
        //
        // =====================================================================

        [Test]
        public static void TestEmptyRequests()
        {
            string message = "Empty request should not throw.";

            DoTest(() => MelcherWebRequestBuilder.Get(""), message);
            DoTest(() => MelcherWebRequestBuilder.Post(""), message);
            DoTest(() => MelcherWebRequestBuilder.Put(""), message);
            DoTest(() => MelcherWebRequestBuilder.Patch(""), message);
            DoTest(() => MelcherWebRequestBuilder.Delete(""), message);
            DoTest(() => MelcherWebRequestBuilder.Head(""), message);
        }

        [UnityTest]
        public static IEnumerator TestEscaping()
        {
            using var request = UnityWebRequest.Get("https://postman-echo.com/get?hello world");

            yield return request.SendWebRequest();

            if (request.error != null)
            {
                MelcherLog.LogError(request.error);
                yield break;
            }

            MelcherLog.Log($"{request.url} - {request.responseCode}\n{request.downloadHandler.text}");
        }

        [Test]
        public static void TestBasicGet()
        {
            string message = "Hello!";

            DoTest(
                () =>
                {
                    MelcherWebRequestBuilder.Get("")
                        .Send();
                },
                message
            );
        }

        [Test]
        public static void TestTimeoutIs0()
        {
            MelcherWebRequestBuilder.Get("192.168.200.200");
        }


        private static void DoTest(TestDelegate test, string message, [CallerMemberName] string methodName = null, [CallerLineNumber] int lineNumber = 0)
        {
            Assert.DoesNotThrow(
                test,
                $"{GetFailedPrefix(methodName, lineNumber)}{message}"
            );
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

    }   // class MelcherJsonTests

}   // namespace Melcher.Tests

#endif  // if UNITY_TESTS
