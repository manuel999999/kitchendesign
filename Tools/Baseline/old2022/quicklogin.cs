using System.Linq; using System.Reflection;
public static class K { public static string Run() {
  var w = UnityEngine.Object.FindObjectsOfType<Melcher.QuickLogInWidget>(true).First(x => x.isActiveAndEnabled);
  var m = typeof(Melcher.QuickLogInWidget).GetMethod("OnQuickLogIn", BindingFlags.NonPublic | BindingFlags.Instance);
  m.Invoke(w, new object[] { null });
  return "pressed " + w.name;
} }
