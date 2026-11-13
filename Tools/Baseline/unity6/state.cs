var w = UnityEngine.Object.FindObjectsByType<KitchenDesign.UI.MenuScreens.QuickLogInWidget>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
int active = 0; foreach (var x in w) if (x.isActiveAndEnabled) active++;
var ai = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.AppInitializer>();
var sys = ai == null ? null : ai.SystemsForEditor;
if (sys == null) return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " no app systems";
return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " user=" + sys.Session.User.userType + " login=" + sys.Session.LogInState + " clients=" + (sys.ClientList.UserClients == null ? -1 : sys.ClientList.UserClients.Count) + " quickActive=" + active + " rib=" + UnityEngine.Application.runInBackground;
