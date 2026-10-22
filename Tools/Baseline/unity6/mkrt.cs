foreach (var db in UnityEngine.Resources.FindObjectsOfTypeAll<KitchenDesign.Scriptables.ProductVisualDatabaseSO>())
  typeof(KitchenDesign.Scriptables.ProductVisualDatabaseSO).GetMethod("MakeRuntimeEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(db, null);
return "made";
