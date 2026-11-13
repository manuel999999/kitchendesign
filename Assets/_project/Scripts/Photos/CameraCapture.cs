namespace KitchenDesign.Photos
{
    using Cysharp.Threading.Tasks;
    using KitchenDesign.Cabinets;
    using KitchenDesign.Geometry;
    using KitchenDesign.KitchenModel;
    using KitchenDesign.Measures;
    using KitchenDesign.Occupancy;
    using KitchenDesign.Products;
    using KitchenDesign.Shared.Common;
    using KitchenDesign.Shared.Enums;
    using Melcher;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    public static class CameraCapture
    {
        public static byte[] TakeScreenshotFromCamera(Camera screenshotCamera, Vector2Int screenshotResolution, System.Action beforeRender = null, System.Action afterRender = null)
        {
            if (Application.isEditor && KitchenScreenshotControl.SkipScreenshotInEditor)
            {
                return null;
            }

            Utility.MemoryUtility.GarbageCollectOnLowMemoryDevices();

            RenderTextureDescriptor descriptor =
                new RenderTextureDescriptor(
                    screenshotResolution.x,
                    screenshotResolution.y
                );

            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                descriptor.depthBufferBits = 16;
            }
            else
            {
                descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            }

            descriptor.msaaSamples = 8;
            descriptor.mipCount = 0;
            descriptor.useMipMap = false;

            RenderTexture screenshotTargetTexture = new RenderTexture(descriptor);

            using var releaseRenderTexture =
                MelcherDeferred.Defer(
                    toCapture: screenshotTargetTexture,
                    onDispose: static renderTexture =>
                    {
                        if (renderTexture)
                        {
                            Object.DestroyImmediate(renderTexture);
                        }
                    }
                );

            Debug.Assert(screenshotTargetTexture.width == screenshotResolution.x);
            Debug.Assert(screenshotTargetTexture.height == screenshotResolution.y);

            RenderTexture targetTextureToRestore = screenshotCamera.targetTexture;

            using var restoreCameraTarget =
                MelcherDeferred.Defer(
                    () => screenshotCamera.targetTexture = targetTextureToRestore
                );

            screenshotCamera.targetTexture = screenshotTargetTexture;

            beforeRender?.Invoke();
            screenshotCamera.Render();
            afterRender?.Invoke();

            Texture2D screenshot =
                new Texture2D(
                    screenshotResolution.x,
                    screenshotResolution.y,
                    TextureFormat.ARGB32,
                    mipChain: false
                );

            if (!screenshot)
            {
                throw new System.InvalidOperationException("Failed to create texture for reading screenshot.");
            }

            using var releaseScreenshot =
                MelcherDeferred.Defer(
                    toCapture: screenshot,
                    onDispose: static screenshot =>
                    {
                        if (screenshot)
                        {
                            Object.DestroyImmediate(screenshot);
                        }
                    }
                );

            RenderTexture renderTextureToRestore = RenderTexture.active;
            RenderTexture.active = screenshotTargetTexture;

            using var restoreRenderTexture =
                MelcherDeferred.Defer(
                    toCapture: renderTextureToRestore,
                    onDispose: static renderTextureToRestore => RenderTexture.active = renderTextureToRestore
                );

            Debug.Assert(screenshotTargetTexture.width == screenshot.width);
            Debug.Assert(screenshotTargetTexture.height == screenshot.height);

            Rect rect = new Rect(0, 0, screenshotResolution.x, screenshotResolution.y);
            screenshot.ReadPixels(rect, 0, 0);
            screenshot.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            byte[] screenshotData = screenshot.EncodeToJPG();

            return screenshotData;
        }
    }
}
