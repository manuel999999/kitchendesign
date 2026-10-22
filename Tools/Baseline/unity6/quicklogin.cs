KitchenDesign.QuickLogInWidget w = null;
foreach (var x in UnityEngine.Object.FindObjectsByType<KitchenDesign.QuickLogInWidget>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) if (x.isActiveAndEnabled) w = x;
if (w == null) return "no active quick sign-in";
typeof(KitchenDesign.QuickLogInWidget).GetMethod("OnQuickLogIn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(w, new object[] { null });
return "pressed " + w.name;
