var m = UnityEngine.Object.FindFirstObjectByType<Melcher.KitchenScreenshotManager>();
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(m.TakeScreenshots(Melcher.Kitchen.instance));
return "started";
