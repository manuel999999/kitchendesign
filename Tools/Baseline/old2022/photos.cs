using Cysharp.Threading.Tasks;
public static class K { public static string Run() {
  var m = UnityEngine.Object.FindObjectOfType<Melcher.KitchenScreenshotManager>();
  m.TakeScreenshots(Melcher.Kitchen.instance).Forget();
  return "started";
} }
