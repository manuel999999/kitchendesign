namespace Melcher
{
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using UnityEngine;

    /// <summary>
    /// Use this script to run an exe or execute a command from the command
    /// line in the editor.
    /// </summary>
    public static class MelcherProcessRunner
    {
        /// <summary>
        /// Run a command through the command line. This can be used to run
        /// programs already on the path.
        /// </summary>
        public static Result RunCmd(string command, float timeoutSeconds)
        {
#if UNITY_EDITOR_WIN
            return RunExe("cmd.exe", "/C " + command, timeoutSeconds);
#elif UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX
            return RunExe("/bin/bash", $"-c \"{command}\"", timeoutSeconds);
#else
#error Please add support for this platform to MelcherProcessRunner
#endif
        }

        /// <summary>
        /// Run an executable.
        /// </summary>
        public static Result RunExe(string exePath, string arguments, float timeoutSeconds)
        {
            Result result = new Result();

            const float Seconds2Milliseconds = 1000.0f;
            int timeoutMs = Mathf.RoundToInt(timeoutSeconds * Seconds2Milliseconds);

            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                using (var process = new Process())
                {
                    // Run the process without a window
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    // Use string builders to concatenate long output
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    void PipeData(DataReceivedEventArgs e, StringBuilder builder, AutoResetEvent eventHandle)
                    {
                        if (e.Data == null)
                        {
                            eventHandle.Set();
                        }
                        else
                        {
                            builder.AppendLine(e.Data);
                        }
                    }

                    process.OutputDataReceived += (sender, e) =>
                    {
                        PipeData(e, outputBuilder, outputWaitHandle);
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        PipeData(e, errorBuilder, errorWaitHandle);
                    };

                    try
                    {
                        // Start the process with a timeout
                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit(timeoutMs))
                        {
                            result.exitCode = process.ExitCode;
                        }
                        else
                        {
                            errorBuilder.AppendLine($"<{nameof(MelcherProcessRunner)} timed out>");
                        }
                    }
                    finally
                    {
                        outputWaitHandle.WaitOne(timeoutMs);
                        errorWaitHandle.WaitOne(timeoutMs);

                        result.output = outputBuilder.ToString();
                        result.error = errorBuilder.ToString();
                    }
                }
            }

            return result;
        }

        public struct Result
        {
            public int? exitCode;
            public string output;
            public string error;
        }   // struct Result

    }   // class MelcherProcessRunner

}   // namespace Melcher
