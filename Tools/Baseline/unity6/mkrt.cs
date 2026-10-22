foreach (var db in UnityEngine.Resources.FindObjectsOfTypeAll<KitchenDesign.ProductVisualDatabase>())
  typeof(KitchenDesign.ProductVisualDatabase).GetMethod("MakeRuntimeEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(db, null);
return "made";
