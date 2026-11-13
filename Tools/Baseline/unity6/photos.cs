var m = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.Photos.KitchenScreenshotControl>();
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(m.TakeScreenshots(UnityEngine.Object.FindFirstObjectByType<KitchenDesign.KitchenModel.Kitchen>()));
return "started";
