namespace KitchenDesign.Utility
{
    public static class MemoryUtility
    {
        public static void GarbageCollectOnLowMemoryDevices()
        {
#if UNITY_IOS
            System.GC.Collect();
#endif
        }
    }
}
