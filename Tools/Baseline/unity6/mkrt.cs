foreach (var db in UnityEngine.Resources.FindObjectsOfTypeAll<Melcher.ProductVisualDatabase>())
  typeof(Melcher.ProductVisualDatabase).GetMethod("MakeRuntimeEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(db, null);
return "made";
