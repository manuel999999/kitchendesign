var m = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.KitchenScreenshotManager>();
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(m.TakeScreenshots(KitchenDesign.Kitchen.instance));
return "started";
