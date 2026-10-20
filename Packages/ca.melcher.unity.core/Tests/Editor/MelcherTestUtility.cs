#if ENABLE_TESTS
namespace Melcher.Tests
{
    using System.Runtime.CompilerServices;

    public static class MelcherTestUtility
    {
        public static string FormatFailedMessage(
                                object message,
            [CallerMemberName]  string methodName = null,
            [CallerLineNumber]  int lineNumber = 0)
        {
            const string RightArrow = "\u2192";
            return $"[{methodName}] line {lineNumber} {RightArrow} {message}";
        }

        public static string GetPrintableString(object value)
        {
            return value != null ? $"\"{value}\"" : "null";
        }

    }   // class MelcherTestUtility

}   // namespace Melcher.Tests

#endif  // if UNITY_TESTS
