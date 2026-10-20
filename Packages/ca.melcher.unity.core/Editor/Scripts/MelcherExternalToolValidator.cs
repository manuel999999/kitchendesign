namespace Melcher
{
    using System.IO;
    using UnityEngine;
    using UnityEditor;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Validate the local git repo has the proper Unity YAML merge tool path
    /// in its config. If the path is incorrect or missing, an error message
    /// is reported that contains the proper config code that should be
    /// entered. This is useful for keeping it synced when a project is updated
    /// to a newer Unity version.
    /// </summary>
    public static class MelcherExternalToolValidator
    {
        /// <summary>
        /// Automatically try to validate the git config when the project
        /// loads. It uses the non-verbose validation mode so nothing is
        /// displayed if the config is good to go.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void ValidateOnEditorStart()
        {
            ValidateGitMergeTool(verbose: false);
        }

        /// <summary>
        /// A menu-accessible function for validating the git config. Uses
        /// the verbose validation mode so it will print a message if the
        /// config is good or bad.
        /// </summary>
        [MenuItem(MelcherEditorConst.Menu.ToolsProjectSettingsPrefix + "Validate git config")]
        public static void ExplicitValidateGitMergeTool()
        {
            ValidateGitMergeTool(verbose: true);
        }

        /// <summary>
        /// Check and print whether the git config has the proper Unity YAML
        /// merge tool version for the running editor version.
        /// </summary>
        /// <param name="verbose">
        /// If <see langword="true"/>, a message will be logged if the
        /// git config is good or bad. Otherwise, a message will only be
        /// logged if the config is bad.
        /// </param>
        private static void ValidateGitMergeTool(bool verbose)
        {
            MelcherProcessRunner.Result result = MelcherProcessRunner.RunCmd(
                "git rev-parse --show-toplevel",
                timeoutSeconds: 3.0f
            );

            const int ExitCode_NotAGitRepo = 128;
            if (result.exitCode.HasValue && result.exitCode.Value == ExitCode_NotAGitRepo)
            {
                if (verbose)
                {
                    MelcherLog.Log("Not a git repo.");
                }
                return;
            }
            else if (!result.exitCode.HasValue || result.exitCode.Value != 0)
            {
                string error = result.error;
                if (result.exitCode.HasValue)
                {
                    error = result.exitCode.Value + " :: " + result.error;
                }
                throw new System.Exception(error);
            }

            string gitRepoPath = result.output.Trim();

            string gitFolderPath = Path.Combine(gitRepoPath, ".git");
            gitFolderPath = Path.GetFullPath(gitFolderPath);

            string gitConfigPath = Path.Combine(gitFolderPath, "config");

            if (!File.Exists(gitConfigPath))
            {
                MelcherLog.LogError("git config doesn't exist for this project");
                return;
            }

            string[] gitConfigLines = File.ReadAllLines(gitConfigPath);

            string currentUnityVersion = Application.unityVersion;

            string unityYamlMergeExtension = ".exe";

            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                unityYamlMergeExtension = "";
            }

            // Regular expression patterns expected to be found in order. They
            // don't need to be immediately one after the other though
            string[] requiredPatternsToFindInOrder =
            {
                // [merge]
                @"\[merge\]",

                //     tool = unityyamlmerge
                @"tool\s*=\s*unityyamlmerge",

                // [mergetool = "unityyamlmerge"]
                @"\[mergetool\s*""unityyamlmerge""\]",

                //     trustExitCode = false
                @"trustExitCode\s*=\s*false",

                //     cmd =
                //     E:/UnityHubInstalls/2019.4.4f1/Editor/Data/Tools/UnityYAMLMerge.exe merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
                $@"cmd\s*=\s*'{Regex.Escape(EditorApplication.applicationContentsPath)}/Tools/UnityYAMLMerge{Regex.Escape(unityYamlMergeExtension)}' merge -p ""\$BASE"" ""\$REMOTE"" ""\$LOCAL"" ""\$MERGED"""
            };

            int gitConfigLineCount = gitConfigLines.Length;
            int requiredLines = requiredPatternsToFindInOrder.Length;
            int requiredLinesFound = 0;

            for (int i = 0; i < gitConfigLineCount && requiredLinesFound < requiredLines; ++i)
            {
                var match = Regex.Match(gitConfigLines[i], requiredPatternsToFindInOrder[requiredLinesFound], RegexOptions.IgnoreCase);
                if (match != null && match.Success)
                {
                    ++requiredLinesFound;
                }
            }

            if (requiredLinesFound != requiredPatternsToFindInOrder.Length)
            {
                var sb = new System.Text.StringBuilder();

                sb.AppendLine($"RegEx pattern \"{requiredPatternsToFindInOrder[requiredLinesFound]}\" did not match git config");
                sb.AppendLine();
                sb.AppendLine("Edit your git config with the following:");
                sb.AppendLine("[merge]");
                sb.AppendLine("    tool = unityyamlmerge");
                sb.AppendLine("[mergetool \"unityyamlmerge\"]");
                sb.AppendLine("    trustExitCode = false");
                sb.AppendLine(@$"    cmd = '{EditorApplication.applicationContentsPath}/Tools/UnityYAMLMerge{unityYamlMergeExtension}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED""");

                MelcherLog.LogError(sb.ToString());
            }
            else if (verbose)
            {
                MelcherLog.Log("git config is good");
            }

        }
    }   // class MelcherExternalToolValidator

}   // namespace Melcher
