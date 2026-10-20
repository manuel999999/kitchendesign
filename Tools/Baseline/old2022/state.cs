using System.Linq;
public static class K { public static string Run() {
  var w = UnityEngine.Object.FindObjectsOfType<Melcher.QuickLogInWidget>(true);
  var l = UnityEngine.Object.FindObjectsOfType<Melcher.LogInUI>(true);
  return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
    + " user=" + Melcher.ProFitGlobals.user.userType.ToString()
    + " login=" + Melcher.ProFitGlobals.logInCoroutineState
    + " clients=" + (Melcher.ProFitGlobals.userClients?.Count ?? -1)
    + " quickWidgets=" + w.Length + " active=" + string.Join(",", w.Select(x => x.isActiveAndEnabled))
    + " loginUI active=" + string.Join(",", l.Select(x => x.isActiveAndEnabled))
    + " app=" + Melcher.ProFitGlobals.appOptions?.name;
} }
