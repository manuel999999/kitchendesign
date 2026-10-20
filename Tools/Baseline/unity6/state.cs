var w = UnityEngine.Object.FindObjectsByType<Melcher.QuickLogInWidget>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
int active = 0; foreach (var x in w) if (x.isActiveAndEnabled) active++;
return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " user=" + Melcher.ProFitGlobals.user.userType + " login=" + Melcher.ProFitGlobals.logInCoroutineState + " clients=" + (Melcher.ProFitGlobals.userClients == null ? -1 : Melcher.ProFitGlobals.userClients.Count) + " quickActive=" + active + " rib=" + UnityEngine.Application.runInBackground;
