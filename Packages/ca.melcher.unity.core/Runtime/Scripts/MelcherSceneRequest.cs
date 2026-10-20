namespace Melcher
{
    using UnityEngine.SceneManagement;

    /// <summary>
    /// A <see cref="MelcherSceneRequest"/> is an encapsulated way to refer
    /// to scenes by one of many identifiers (e.g., by build index, name,
    /// path, etc.).
    /// <br /><br />
    ///
    /// The advantage of this pattern is it allows us to write a single
    /// function that is capable of handling scenes referenced by any method.
    /// </summary>
    public readonly struct MelcherSceneRequest
    {
        // =====================================================================
        // Factory functions
        // =====================================================================

        public static MelcherSceneRequest FromBuildIndex(int buildIndex)
        {
            return new MelcherSceneRequest(Mode.BuildIndex, intValue: buildIndex);
        }

        /// <summary>
        /// Reference an already loaded scene. (<see cref="Scene.handle"/>)
        /// </summary>
        public static MelcherSceneRequest FromHandle(SceneHandle handle)
        {
            return new MelcherSceneRequest(Mode.Handle, sceneHandle: handle);
        }

        public static MelcherSceneRequest FromName(string sceneName)
        {
            return new MelcherSceneRequest(Mode.Name, stringValue: sceneName);
        }

        /// <summary>
        ///     <paramref name="scenePath"/> should not be prepended with
        ///     "Assets/" and it should not have an extension (e.g., ".unity").
        /// </summary>
        public static MelcherSceneRequest FromPath(string scenePath)
        {
            return new MelcherSceneRequest(Mode.Path, stringValue: scenePath);
        }

        public static MelcherSceneRequest FromScene(Scene scene)
        {
            return FromHandle(scene.handle);
        }

        // =====================================================================
        // Methods
        // =====================================================================

        public override int GetHashCode()
        {
            return new MelcherHashCodeMixer()
                .Mix(mode.GetHashCode())
                .Mix(UsesStringValue() ? stringValue.GetHashCode() : mode == Mode.Handle ? sceneHandle.GetHashCode() : intValue.GetHashCode())
                .GetHashCode();
        }

        public override string ToString()
        {
            if (UsesStringValue())
            {
                return $"[{mode}: {stringValue}]";
            }

            if (mode == Mode.Handle)
            {
                return $"[{mode}: {sceneHandle}]";
            }

            return $"[{mode}: {intValue}]";
        }

        public bool UsesStringValue()
        {
            return mode == Mode.Name || mode == Mode.Path;
        }

        // =====================================================================
        // Constructors
        // =====================================================================

        private MelcherSceneRequest(Mode mode, int intValue = -1, string stringValue = null, SceneHandle sceneHandle = default)
        {
            this.mode = mode;
            this.intValue = intValue;
            this.stringValue = stringValue;
            this.sceneHandle = sceneHandle;
        }

        // =====================================================================
        // Members
        // =====================================================================

        public readonly Mode mode;
        public readonly int intValue;
        public readonly string stringValue;
        public readonly SceneHandle sceneHandle;

        // =====================================================================
        // Nested types
        // =====================================================================

        /// <summary>
        /// An enum of the various ways scenes can be referenced.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// The scene is referenced by its build index.
            /// </summary>
            BuildIndex,

            /// <summary>
            /// The scene is referenced by its handle. Unity scenes are
            /// assigned handles when they begin loading and keep those
            /// handles until they are unloaded.
            /// </summary>
            Handle,

            /// <summary>
            /// The scene is referenced by its name. Scene name's are the
            /// scene's file name without their ".unity" extension.
            /// </summary>
            Name,

            /// <summary>
            /// The scene is referenced by its project-relative path. The
            /// path should include the scene file's file extension
            /// </summary>
            Path
        }

    }   // struct MelcherSceneRequest

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-01-19
/// ===========================================================================
