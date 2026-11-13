KitchenDesign.UI.MenuScreens.QuickLogInWidget w = null;
foreach (var x in UnityEngine.Object.FindObjectsByType<KitchenDesign.UI.MenuScreens.QuickLogInWidget>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) if (x.isActiveAndEnabled) w = x;
if (w == null) return "no active quick sign-in";
typeof(KitchenDesign.UI.MenuScreens.QuickLogInWidget).GetMethod("OnQuickLogIn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(w, new object[] { null });
return "pressed " + w.name;
