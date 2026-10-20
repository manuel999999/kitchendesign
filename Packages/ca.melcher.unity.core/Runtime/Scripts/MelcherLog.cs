namespace Melcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    using Object = UnityEngine.Object;

    /// <summary>
    ///     <see cref="MelcherLog"/> is a wrapper for Unity's built-in logging
    ///     API that adds a couple nice features:
    ///     <br /><br />
    ///
    ///     1.  <b>Tagging</b>
    ///
    ///         Log messages are automatically prefixed with the file
    ///         name that created it. The prefix can be specified manually
    ///         if desired. The helper function <see cref="MakeLogTag"/> can
    ///         create a tag that includes color information. This is
    ///         useful to distinguish different systems within the console
    ///         window.
    ///         <br /><br />
    ///
    ///     2.  <b>Log Filtering</b>
    ///
    ///         Messages can specify a filter level. The global <see
    ///         cref="minimumSeverityToDisplay"/> setting controls which
    ///         message priorities will actually be logged. Any message whose
    ///         severity is less than the threshold is not displayed in the
    ///         console. (See <see cref="MelcherLogSeverity"/>)
    ///         <br /><br />
    ///
    ///     3.  <b>Editor-only Logging</b>
    ///
    ///         Some functions provided here are Editor and Development-build
    ///         conditional. They will will only log if the app is running
    ///         within the editor or as a development build. This is useful to
    ///         restrict debug prints in builds where they may otherwise affect
    ///         performance or leak implementation or security details.
    ///         See the functions that end with "EditorOnly" for this behaviour.
    /// </summary>
    public static class MelcherLog
    {
        /// <summary>
        ///     A line made up of dashes. Can be logged to add separation
        ///     between parts. E.g. we use it to highlight scene changes.
        /// </summary>
        public const string DashedLine = "---------------------------------------------------------------------------------";

        // =====================================================================
        // Static fields
        // =====================================================================

        /// <summary>
        ///     An unambiguous way to allow or disallow logging.
        /// </summary>
        public static bool enabled = true;

        /// <summary>
        ///     Sets the minimum log severity level to allow. Any log call
        ///     whose severity is less than this will be ignored.
        /// </summary>
        public static int minimumSeverityToDisplay = MelcherLogSeverity.Log;

        // =====================================================================
        // Standard log functions
        // =====================================================================

        /// <summary>
        ///     Editor-time assert that a condition is true, otherwise an
        ///     error message is logged and (optionally) a callback is invoked.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <param name="onFalse">
        ///     Arbitrary callback to invoke if the assertion fails.
        /// </param>
        /// <param name="severity"></param>
        /// <param name="tag"></param>
        /// <param name="callerFilePath"></param>
        [Conditional("UNITY_ASSERTIONS")]
        public static void Assert(
            bool                    condition,
            object                  message         = null,
            Object                  context         = null,
            Action                  onFalse         = null,
            int                     severity        = MelcherLogSeverity.Error,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            if (!condition)
            {
                DoLog(LogType.Assert, message ?? "Assert failed.", context, severity, tag, callerFilePath);

                onFalse?.Invoke();
            }
        }


        /// <summary>
        /// Log a message to the console. By default, the message will be
        /// prepended with the calling file's name.
        /// </summary>
        public static void Log(
            object                  message,
            Object                  context         = null,
            int                     severity        = MelcherLogSeverity.Log,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            DoLog(LogType.Log, message, context, severity, tag, callerFilePath);
        }

        /// <summary>
        ///     Log a message with severity set to <see
        ///     cref="MelcherLogSeverity.Verbose"/>. Verbose messages are
        ///     intended for messages that may otherwise be spammy. They can be
        ///     filtered from regular log messages by changing <see
        ///     cref="minimumSeverityToDisplay"/>. By default, verbose messages
        ///     are not displayed.
        /// </summary>
        public static void LogVerbose(
            object                  message,
            Object                  context         = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            DoLog(LogType.Log, message, context, MelcherLogSeverity.Verbose, tag, callerFilePath);
        }

        /// <summary>
        /// Log a warning to the console. By default, the message will be
        /// prepended with the calling file's name.
        /// </summary>
        public static void LogWarning(
            object                  message,
            Object                  context         = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            DoLog(LogType.Warning, message, context, MelcherLogSeverity.Warning, tag, callerFilePath);
        }

        /// <summary>
        /// Log an error to the console. By default, the message will be
        /// prepended with the calling file's name.
        /// </summary>
        public static void LogError(
            object                  message,
            Object                  context         = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            DoLog(LogType.Error, message, context, MelcherLogSeverity.Error, tag, callerFilePath);
        }

        // =====================================================================
        // Editor-only log functions
        // =====================================================================

        /// <summary>
        /// Log a message to the console but only if its in the editor or a
        /// development build.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogEditorOnly(
            object                  message,
            Object                  context         = null,
            int                     severity        = MelcherLogSeverity.Log,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            Log(message, context, severity, tag, callerFilePath);
        }

        /// <summary>
        /// Log a warning to the console but only if its in the editor or a
        /// development build.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogWarningEditorOnly(
            object                  message,
            Object                  context         = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            LogWarning(message, context, tag, callerFilePath);
        }

        /// <summary>
        /// Log an error to the console but only if its in the editor or a
        /// development build.
        /// </summary>
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void LogErrorEditorOnly(
            object                  message,
            Object                  context         = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            LogError(message, context, tag, callerFilePath);
        }

        // =====================================================================
        // Log tag functions
        // =====================================================================

        /// <summary>
        /// Make a colorized tag. If omitted, the default tag will be the
        /// caller's file name without the file extension.
        /// </summary>
        /// <param name="htmlColor">
        /// An HTML-like color hexcode without the leading <c>#</c>. This can
        /// also be one of a few pre-defined color names like "red". See
        /// <see href="https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/StyledText.html">
        /// Supported Colors</see> here.
        /// </param>
        public static string MakeLogTag(
            string                  htmlColor       = null,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            bool hasCustomColor = !string.IsNullOrEmpty(htmlColor);

            string prefix = GetPrefixFromTagOrFilePath(tag, callerFilePath, colorizeParts: !hasCustomColor);

            if (Application.isEditor && hasCustomColor)
            {
                // The colour is only included in the editor to improve the
                // legibility of device logs where rich text tags are displayed
                // raw.

                prefix = $"<color=#{htmlColor}>{prefix}</color>";
            }

            return prefix;
        }

        /// <inheritdoc cref="MakeLogTag(string, string, string)"/>
        public static string MakeLogTag(
            Color                   color,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            return MakeLogTag(ColorUtility.ToHtmlStringRGB(color), tag, callerFilePath);
        }

        /// <inheritdoc cref="MakeLogTag(string, string, string)"/>
        public static string MakeLogTag(
            object                  fromObjectHashCode,
            string                  tag             = null,
            [CallerFilePath] string callerFilePath  = null)
        {
            byte[] bytes = System.BitConverter.GetBytes(fromObjectHashCode.GetHashCode());
            Color32 color = new Color32(r: bytes[0], g: bytes[1], b: bytes[2], a: 0xFF);
            return MakeLogTag(color, tag, callerFilePath);
        }

        // =====================================================================
        // Utility
        // =====================================================================

        /// <summary>
        ///     Log the caller's function name and line number. This function
        ///     is intended to be sprinkled throughout code to easily test code
        ///     flow. All of its arguments are optional.
        /// </summary>
        /// <param name="message">
        ///     An optional message to concatenate to the breadcrumb.
        /// </param>
        /// <param name="context">
        ///     A Unity object context to associate with this log message. The
        ///     object will be pinged when the log message is clicked in the
        ///     console.
        /// </param>
        public static void Breadcrumb(
            object                      message             = null,
            Object                      context             = null,
            string                      tag                 = null,
            int                         severity            = MelcherLogSeverity.Log,
            [CallerFilePath] string     callerFilePath      = null,
            [CallerMemberName] string   callerMemberName    = null,
            [CallerLineNumber] int      callerLineNumber    = 0)
        {
            const char FilledHappyFace = '\u263b';

            const string FilledHappyFaceColor = "#FFCC4D";    // Discord emoji face yellow

            string breadcrumb = $"<color={FilledHappyFaceColor}>{FilledHappyFace}</color> <b>{callerMemberName}</b>:<i>{callerLineNumber}</i>";

            if (message != null)
            {
                string messageToString = message.ToString();
                if (!string.IsNullOrEmpty(messageToString))
                {
                    breadcrumb += $" {message}";
                }
            }

            Log(breadcrumb, context, severity, tag, callerFilePath);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void BreadcrumbEditorOnly(
            object                      message             = null,
            Object                      context             = null,
            string                      tag                 = null,
            int                         severity            = MelcherLogSeverity.Log,
            [CallerFilePath] string     callerFilePath      = null,
            [CallerMemberName] string   callerMemberName    = null,
            [CallerLineNumber] int      callerLineNumber    = 0)
        {
            Breadcrumb(
                message: message,
                context: context,
                tag: tag,
                severity: severity,
                callerFilePath: callerFilePath,
                callerMemberName: callerMemberName,
                callerLineNumber: callerLineNumber
            );
        }

        /// <summary>
        ///     <inheritdoc cref="DescribeVariables(string, Expression{Func{object}}[])"/>
        ///     <br /><br />
        ///
        ///     When multiple variables are provided, the resulting string will
        ///     be comma separated.
        /// </summary>
        public static string DescribeVariables(params Expression<Func<object>>[] expressions)
        {
            return DescribeVariables(", ", expressions);
        }

        /// <summary>
        ///     Return a string of one or more variables and their
        ///     value. This is intended for debugging purposes (to easily
        ///     print their name and value to the console). The <paramref
        ///     name="expressions"/> parameter is a list of lambda expressions:
        ///
        ///     <code>
        ///     MelcherLog.DescribeVariables(
        ///         () => variableWhoseNameAndValueWillBeLogged,
        ///         () => anotherVariable
        ///     );
        ///     </code>
        /// </summary>
        /// <param name="separator">
        ///     The string that is used to separate each lambda expression's
        ///     string result.
        /// </param>
        public static string DescribeVariables(string separator, params Expression<Func<object>>[] expressions)
        {
            IEnumerable<Expression<Func<object>>> nonNulls =
                expressions.Where(static x => x != null);

            IEnumerable<string> resolvedExpressions =
                nonNulls.Select(static x => Resolve(x));

            return string.Join(separator, resolvedExpressions);

            static string Resolve(Expression<Func<object>> expression)
            {
                string memberName =
                    expression.Body switch
                    {
                        MemberExpression memberExpression => memberExpression.Member.Name,
                        UnaryExpression { Operand: MemberExpression memberExpression } => memberExpression.Member.Name,
                        UnaryExpression { Operand: ConstantExpression } or ConstantExpression => "const",
                        _ => "?"
                    };

                object value = expression.Compile().Invoke();

                string formattedValue =
                    value switch
                    {
                        Enum e => MelcherFormat.ConsoleSymbolName(e),
                        string s => MelcherFormat.ConsoleString(s),
                        Type t => MelcherFormat.ConsoleTypeName(t),
                        _ => MelcherFormat.ConsoleNumericValue(value)
                    };

                return $"{MelcherFormat.ConsoleKeyword(memberName)}={formattedValue}";
            }
        }

        // =====================================================================
        // Internal functions
        // =====================================================================

        private static void DoLog(LogType logType, object message, Object context, int incomingLogSeverity, string tag, string callerFilePath)
        {
            if (!enabled)
            {
                // Ignore all messages when logging is disabled.
                return;
            }

            if (incomingLogSeverity < minimumSeverityToDisplay)
            {
                // Ignore messages that are less severe than we are interested.
                return;
            }

            string prefix = GetPrefixFromTagOrFilePath(tag, callerFilePath);

            if (Application.isEditor)
            {
                prefix = $"<b>{prefix}</b>";
            }

            prefix = $"[{prefix}] ";

            string str = prefix + message;

            switch (logType)
            {
                case LogType.Log:
                    UnityEngine.Debug.Log(str, context);
                    break;

                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(str, context);
                    break;

                case LogType.Error:
                    UnityEngine.Debug.LogError(str, context);
                    break;

                case LogType.Assert:
                    UnityEngine.Debug.Assert(false, str, context);
                    break;

                default:
                    throw new System.Exception("Unhandled log type " + logType);
            }
        }

        /// <summary>
        ///     Return log message prefix text. This will be the tag if it is
        ///     not null and not empty. Otherwise, return the file name of
        ///     <paramref name="filePath"/> without the extension.
        /// </summary>
        private static string GetPrefixFromTagOrFilePath(string tag, string filePath, bool colorizeParts = true)
        {
            string result = tag;

            if (string.IsNullOrEmpty(result))
            {
                // A tag wasn't provided. The file path will be turned into a
                // tag.

                // The file path may contain forward or backward slashes as path
                // separators and there's no guarantee they match the path
                // separators used by the system running the app. To ensure we
                // do not leak the folder structure of the project, the slashes
                // are manually checked and the path is stripped.
                int slash = filePath.LastIndexOfAny(s_DirectorySeparatorChars);
                if (slash >= 0)
                {
                    // Trim off the leading path.
                    result = filePath.Substring(slash + 1);
                }

                // Remove the possibly remaining file extension.
                // `[CallerFilePath]` strings don't include a file extension on
                // all platforms. E.g., WebGL.
                result = System.IO.Path.GetFileNameWithoutExtension(result);

#if UNITY_EDITOR
                if (colorizeParts)
                {
                    // Colour-code each portion of the file name that is between
                    // dot characters. The goal with this is to make it easier
                    // to spot related log messages (e.g., partials), without
                    // being too vibrant or distracting.

                    // Parts is the dot separated words
                    string[] parts =
                        result.Split(
                            s_FileNameSplitters,
                            System.StringSplitOptions.RemoveEmptyEntries
                        );

                    s_ColorizedFileNameParts.Clear();

                    float saturation = 0.96f;
                    float value = 0.77f;

                    if (UnityEditor.EditorGUIUtility.isProSkin)
                    {
                        // Use brighter colours for Unity's dark theme.
                        saturation = 0.1f;
                        value = 0.9f;
                    }

                    foreach (string part in parts)
                    {
                        // To make the colors consistent between editor sessions, we
                        // use Animator.StringToHash() instead of object.GetHashCode()
                        int partHash = Animator.StringToHash(part);

                        float partHue = MelcherCommon.RemapTo01(partHash, int.MinValue, int.MaxValue);

                        Color partColor = Color.HSVToRGB(partHue, saturation, value);

                        s_ColorizedFileNameParts.Add(
                            $"<color=#{ColorUtility.ToHtmlStringRGB(partColor)}>{part}</color>"
                        );
                    }

                    result = string.Join(".", s_ColorizedFileNameParts);
                }
#endif  //  if UNITY_EDITOR
            }

            return result;
        }

        // ====================================================================
        // Internal static fields
        // ====================================================================

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly char[] s_FileNameSplitters = new [] {'.'};

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly char[] s_DirectorySeparatorChars = new [] {'/', '\\'};

        [MelcherStaticFieldEditorTreatment.PersistBetweenPlaymodeSessions]
        private static readonly List<string> s_ColorizedFileNameParts = new List<string>();

#if UNITY_EDITOR
        // =====================================================================
        // Editor-only
        // =====================================================================

        [MelcherResetStaticFieldsMethod]
        private static int OnResetStaticFields()
        {
            int fieldsReset = 0;

            ++fieldsReset;  enabled = true;
            ++fieldsReset;  minimumSeverityToDisplay = MelcherLogSeverity.Log;

            return fieldsReset;
        }

#endif  // if UNITY_EDITOR

    }   // class MelcherLog

    /// <summary>
    ///     Log severity codes. Ordered from least-critical (most verbose) to
    ///     most-critical (least verbose).
    /// </summary>
    public static class MelcherLogSeverity
    {
        /// <summary>
        ///     Indicates that a message should be completely suppressed.  If
        ///     <see cref="MelcherLog.minimumSeverityToDisplay"/> is set to
        ///     this, then all log messages will be allowed, including those
        ///     with this severity.
        /// </summary>
        public const int MinValue   = int.MinValue;

        public const int Verbose    = -1000;
        public const int Log        = 0;
        public const int Warning    = 1000;
        public const int Error      = 2000;

        /// <summary>
        ///     Indicates that a message should always be shown. If <see
        ///     cref="MelcherLog.minimumSeverityToDisplay"/> is set to this,
        ///     then all log messages except for those with this severity will
        ///     be suppressed.
        /// </summary>
        public const int MaxValue   = int.MaxValue;

    }   // class MelcherLogLevel

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-14
/// ===========================================================================
