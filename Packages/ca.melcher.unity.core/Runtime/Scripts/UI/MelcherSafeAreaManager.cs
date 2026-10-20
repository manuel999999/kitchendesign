namespace Melcher
{
    using System.Collections.Generic;
    using UnityEngine;
    using MEC;

    public static class MelcherSafeAreaManager
    {
        // ====================================================================
        // Static functions
        // ====================================================================

        public static void Init()
        {
            MelcherScreenEvents.onChanged -= ForceLayout;
            MelcherSafeAreaConformer.onEnable -= AddSafeArea;
            MelcherSafeAreaConformer.onDisable -= RemoveSafeArea;
            MelcherSafeAreaConformer.onParentChanged -= ReorderArea;
            Application.quitting -= IgnoreLayoutRequests;

            MelcherScreenEvents.onChanged += ForceLayout;
            MelcherSafeAreaConformer.onEnable += AddSafeArea;
            MelcherSafeAreaConformer.onDisable += RemoveSafeArea;
            MelcherSafeAreaConformer.onParentChanged += ReorderArea;
            Application.quitting += IgnoreLayoutRequests;

            s_AllowLayoutRequests = true;
        }

        private static void ForceLayout(ScreenOrientation old, ScreenOrientation now)
        {
            ForceLayout();
        }

        private static void ForceLayout()
        {
            foreach (MelcherSafeAreaConformer safeArea in s_SafeAreaConformers)
            {
                safeArea.ForceLayout();
            }
        }

        private static void AddSafeArea(MelcherSafeAreaConformer conformer)
        {
            int depth = conformer.GetLayoutOrder();

            int index = 0;
            foreach (MelcherSafeAreaConformer existing in s_SafeAreaConformers)
            {
                if (depth < existing.GetLayoutOrder())
                {
                    break;
                }

                ++index;
            }

            s_SafeAreaConformers.Insert(index, conformer);
            MarkForLayoutRebuild();
        }

        private static void RemoveSafeArea(MelcherSafeAreaConformer conformer)
        {
            s_SafeAreaConformers.Remove(conformer);
            MarkForLayoutRebuild();
        }

        private static void ReorderArea(MelcherSafeAreaConformer conformer)
        {
            RemoveSafeArea(conformer);
            AddSafeArea(conformer);
        }

        private static void IgnoreLayoutRequests()
        {
            s_AllowLayoutRequests = false;
        }

        private static void MarkForLayoutRebuild()
        {
            IEnumerator<float> DelayedLayoutRebuild()
            {
                yield return Timing.WaitForOneFrame;

                ForceLayout();
            }

            if (!s_AllowLayoutRequests)
            {
                return;
            }

            if (!s_QueuedLayoutRebuild.IsValid || !s_QueuedLayoutRebuild.IsRunning)
            {
                s_QueuedLayoutRebuild = Timing.RunCoroutine(DelayedLayoutRebuild());
            }
        }

        // =====================================================================
        // Static fields
        // =====================================================================

        private static List<MelcherSafeAreaConformer> s_SafeAreaConformers =
            new List<MelcherSafeAreaConformer>();

        private static CoroutineHandle s_QueuedLayoutRebuild;

        private static bool s_AllowLayoutRequests;

#if UNITY_EDITOR
        // =====================================================================
        // Unity-editor
        // =====================================================================

        [MelcherResetStaticFieldsMethod]
        private static int OnResetStaticFields()
        {
            int fieldsReset = 0;

            ++fieldsReset;  s_SafeAreaConformers.Clear();
            ++fieldsReset;  s_QueuedLayoutRebuild = default;
            ++fieldsReset;  s_AllowLayoutRequests = false;

            return fieldsReset;
        }
#endif  // if UNITY_EDITOR

    }   // class MelcherSafeAreaManager

}   // namespace Melcher
