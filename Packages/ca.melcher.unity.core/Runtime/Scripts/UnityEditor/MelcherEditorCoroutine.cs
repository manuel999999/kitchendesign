#if UNITY_EDITOR
namespace Melcher
{
    using System.Collections;
    using UnityEditor;

    /// <summary>
    /// An editor-only coroutine implementation. It is limited to single-frame
    /// yields and other IEnumerator coroutines, but being defined outside of
    /// any editor assembly, has the advantage that it can be referenced within
    /// runtime code; the official Unity Editor Coroutines package cannot.
    /// </summary>
    public static class MelcherEditorCoroutine
    {
        /// <summary>
        /// Start a coroutine and execute it to completion. The coroutine
        /// function can only yield single frames (<c>yield return null</c>)
        /// and other <see cref="IEnumerator"/> coroutines.
        /// </summary>
        public static void StartEditorCoroutine(IEnumerator routine, System.Action onComplete = null)
        {
            PumpRoutine();

            void PumpRoutine()
            {
                if (!routine.MoveNext())
                {
                    // The coroutine is complete
                    onComplete?.Invoke();
                    return;
                }

                if (routine.Current is IEnumerator subroutine)
                {
                    // The coroutine yielded a sub-coroutine. Finish executing
                    // it before continuing
                    StartEditorCoroutine(
                        subroutine,
                        onComplete: () => EditorApplication.delayCall += PumpRoutine
                    );
                    return;
                }

                if (routine.Current != null)
                {
                    MelcherLog.LogWarning(
                        $"Can only yield single frames (yield return null) or IEnumerator coroutines. Unhandled yield type: {routine.Current.GetType()}. Yielding one frame instead..."
                    );
                }

                EditorApplication.delayCall += PumpRoutine;
            }
        }

    }   // class MelcherEditorCoroutine

}   // namespace Melcher

#endif  // if UNITY_EDITOR
