namespace Melcher
{
    using UnityEditor;

    /// <summary>
    ///     Adds a context menu to easily ping any <see
    ///     cref="UnityEngine.Object"/>.
    /// </summary>
    internal static class MelcherPingContextMenu
    {
        [MenuItem("CONTEXT/Object/Ping")]
        private static void Ping(MenuCommand command)
        {
            EditorGUIUtility.PingObject(command.context);
        }

    }   // class MelcherPingContextMenu

}   // namespace Melcher
