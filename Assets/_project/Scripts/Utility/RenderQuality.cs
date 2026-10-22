using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace KitchenDesign.Utility
{
    public static class RenderQuality
    {
        public static bool TrySetRenderQuality(UniversalRenderPipelineAsset requestedQuality)
        {
            int qualityCount = QualitySettings.names.Length;

            for (int i = 0; i < qualityCount; ++i)
            {
                UniversalRenderPipelineAsset qualityPipeline =
                    (UniversalRenderPipelineAsset)
                        QualitySettings.GetRenderPipelineAssetAt(i);

                if (qualityPipeline != requestedQuality)
                {
                    continue;
                }

                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality != i)
                {
                    QualitySettings.SetQualityLevel(i);
                }

                return true;
            }

            return false;
        }
    }
}
