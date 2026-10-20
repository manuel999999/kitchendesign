namespace Melcher
{
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     <see cref="MelcherTransformFilePath"/> is designed to take paths
    ///     (relative or absolute) and transform them to another path (relative
    ///     or absolute). It is useful when dealing with asset paths like those
    ///     in Resources or StreamingAssets.
    ///     <br /><br />
    ///
    ///     <code>
    ///         string projectRelativePath = "Assets/Images/MyImage.png";
    ///
    ///         string assetsRelativePath =
    ///             MelcherTransformFilePath.From(projectRelativePath, relativeTo: MelcherPredefinedFilePath.project)
    ///                 .To(relativeTo: MelcherPredefinedFilePath.data);
    ///     </code>
    /// </summary>
    public struct MelcherTransformFilePath
    {
        // ====================================================================
        // Static methods
        // ====================================================================

        /// <summary>
        ///     Start transforming a file path.
        ///     <br /><br />
        ///
        ///     If <paramref name="relativeTo"/> is provided, <paramref
        ///     name="path"/> will be treated as a path relative to it;
        ///     otherwise, <paramref name="path"/> will be treated as an
        ///     absolute path.
        /// </summary>
        /// <param name="path">
        ///     A relative or absolute path. If the path is relative, you must
        ///     pass the path it is relative to in <paramref name="relativeTo"/>.
        /// </param>
        /// <param name="relativeTo">
        ///     To make <paramref name="path"/> relative, pass the absolute path
        ///     it is relative to here. Omit this argument or pass <see
        ///     langword="null"/> if <paramref name="path"/> is absolute. See
        ///     <see cref="MelcherPredefinedFilePath"/> for some common Unity
        ///     paths.
        /// </param>
        public static MelcherTransformFilePath From(string path, string relativeTo = null)
        {
            if (relativeTo != null)
            {
                path = Path.Combine(relativeTo, path);
            }

            return new MelcherTransformFilePath(path);
        }

        /// <summary>
        ///     Calculate the full path of a file. I.e., make the path absolute
        ///     and collapse any "." and "..".
        /// </summary>
        public static string GetFullPath(string path)
        {
            // This function exists to address an issue with Path.GetFullPath()
            // on Linux. On Linux, Path.GetFullPath() was taking an absolute
            // path to one folder and returning a path pointing at a completely
            // different folder.
            //
            // The issue appears to be a known limitation of Path.GetFullPath
            // and its documentation recommends calling other functions to
            // pre-check the path. Unfortunately, the pre-check functions are
            // not available in the current .NET version supported by Unity.
            //
            // This function fixes the issue by skipping Path.GetFullPath if the
            // path appears to already be an absolute path.
            //
            // For future reference, the underlying issue was experienced while
            // trying to get a full path of an asset that belonged to a local
            // package. I.e., a file outside the project root.
#if UNITY_STANDALONE_LINUX
            if (!Path.IsPathRooted(path))
#endif  // if UNITY_STANDALONE_LINUX
            {
                path = Path.GetFullPath(path);
            }

            return path;
        }

        // ====================================================================
        // Methods
        // ====================================================================

        /// <summary>
        ///     Combine/append a new part of the fullPath.
        /// </summary>
        public MelcherTransformFilePath Combine(string path)
        {
            m_Path = Path.Combine(m_Path, path);
            return this;
        }

        /// <summary>
        ///     Get the path in absolute format.
        /// </summary>
        public override string ToString()
        {
            return GetFullPath(m_Path);
        }

        public string ToAbsolute()
        {
            return ToString();
        }

        /// <summary>
        ///     Return the path but relative to <paramref name="relativeTo"/>.
        ///     For example, if the current path is "C:\1\2\3" and
        ///     <paramref name="relativeTo"/> is "C:\1", the result will be
        ///     "2\3".
        /// </summary>
        /// <param name="relativeTo">
        ///     The path to become relative to.
        /// </param>
        public string To(string relativeTo, bool throwIfNotWithinFolder = false)
        {
            if (relativeTo == null)
            {
                throw new System.ArgumentNullException(nameof(relativeTo));
            }
            else if (string.IsNullOrEmpty(relativeTo))
            {
                throw new System.ArgumentException("Cannot be empty.", nameof(relativeTo));
            }

            // RelativeTo should be a folder. A URI to a folder needs a
            // trailing slash on the end, otherwise the MakeRelativeUri
            // function will assume the last segment refers to a file and
            // drop it from the path
            if (!relativeTo.EndsWith("/") && !relativeTo.EndsWith("\\"))
            {
                relativeTo += Path.DirectorySeparatorChar;
            }

            //MelcherLog.Breadcrumb("relativeTo=" + relativeTo);

            // This is the folder the returned path will be relative to
            System.Uri relativeToUri = new System.Uri(relativeTo);

            if (!relativeToUri.IsAbsoluteUri)
            {
                throw new System.ArgumentException($"Must be an absolute path: {relativeTo}", nameof(relativeTo));
            }

            string absolutePath = ToAbsolute();

            // We must manually handle the case where `relativeTo` is identical
            // to the current path. This is because Uri.MakeRelativePath will
            // handle it in a way that is less ideal: returning a string that
            // steps up one level and then re-enters the folder.

            // The user may or may not provide a trailing slash (on either the
            // internal string or the 'relative to' string). Therefore, when
            // determining if the paths are identical, we ignore all trailing
            // slashes.
            int lengthRelativeTo = LastIndexNotOfDirectorySeparator(relativeTo);
            int lengthAbsolutePath = LastIndexNotOfDirectorySeparator(absolutePath);

            if (lengthRelativeTo == lengthAbsolutePath &&
                string.Compare(relativeTo, indexA: 0, absolutePath, indexB: 0, length: lengthRelativeTo, ignoreCase: MelcherCommon.IsPlatformPathsCaseSensitive()) == 0)
            {
                return ".";
            }

            // This is the current path (absolute) as a URI
            System.Uri currentPathUri = new System.Uri(absolutePath);

            if (throwIfNotWithinFolder && !relativeToUri.IsBaseOf(currentPathUri))
            {
                throw new System.InvalidOperationException(
                    $"The resulting path exist outside the requested " +
                    $"'relative to' path but the caller explicitly asks " +
                    $"that such an outcome be treated as an error.\n" +
                    $"From path: {MelcherFormat.ConsoleString(ToAbsolute())}\n" +
                    $"Relative to: {MelcherFormat.ConsoleString(relativeTo)}\n"
                );
            }

            // Make the current path relative to `relativeTo`
            System.Uri relativeUri = relativeToUri.MakeRelativeUri(currentPathUri);

            // Unescape space characters that were escaped during
            // MakeRelativeUri(): "%20" => " "
            string relativePath =
                System.Uri.UnescapeDataString(relativeUri.OriginalString);

            // Use the platform's directory separator. This solves the issue of
            // `MakeRelativeUri` introducing forward slashes on Windows. This
            // approach of replacing all slashes is valid because
            // `Path.GetFullPath` automatically normalizes slashes anyway, and
            // its use is rampant throughout this class.
            return
                Regex.Replace(
                    relativePath,
                    @"[/\\]",
                    Path.DirectorySeparatorChar.ToString()
                );

            static int LastIndexNotOfDirectorySeparator(string s)
            {
                int index;

                for (index = s.Length - 1; index >= 0; --index)
                {
                    char c = s[index];

                    if (c != '/' && c != '\\')
                    {
                        break;
                    }
                }

                return index;
            }
        }

        /// <summary>
        ///     Private constructor. Use the static factory function <see
        ///     cref="From"/> to create a <see cref="MelcherTransformFilePath"/>.
        /// </summary>
        private MelcherTransformFilePath(string absolutePath)
        {
            m_Path = GetFullPath(absolutePath);
        }

        // ====================================================================
        // Internal fields
        // ====================================================================

        private string m_Path;

    }   // struct MelcherTransformFilePath

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-07-14
/// ===========================================================================
