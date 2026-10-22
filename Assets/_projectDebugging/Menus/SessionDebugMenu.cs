using UnityEditor;

namespace KitchenDesign.Debugging
{
    public static class SessionDebugMenu
    {
        private const string KickUserItem = "Tools/Kitchen Design/Kick signed-in user _F1";

        [MenuItem(KickUserItem)]
        private static void KickUser()
        {
            ProFitGlobals.KickUser("Kicked by debug hotkey.");
        }

        [MenuItem(KickUserItem, true)]
        private static bool CanKickUser()
        {
            return EditorApplication.isPlaying;
        }
    }
}
