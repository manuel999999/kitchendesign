using UnityEngine;

namespace KitchenDesign.Debugging
{
    public static class DebugSystems
    {
        public static bool TryGet(out AppSystems systems)
        {
            systems = null;

            if (!Application.isPlaying)
            {
                return false;
            }

            AppInitializer appInitializer = Object.FindFirstObjectByType<AppInitializer>();
            if (appInitializer == null)
            {
                return false;
            }

            systems = appInitializer.SystemsForEditor;
            return systems != null;
        }
    }
}
