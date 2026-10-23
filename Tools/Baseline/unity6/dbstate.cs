var dbs = UnityEngine.Resources.FindObjectsOfTypeAll<KitchenDesign.Scriptables.ProductVisualDatabaseSO>();
string r = "databases in memory=" + dbs.Length;
foreach (var db in dbs) {
  var f = typeof(KitchenDesign.Scriptables.ProductVisualDatabaseSO).GetField("m_VisualIdToRuntimeProductVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var d = (System.Collections.IDictionary)f.GetValue(db);
  r += " | " + db.name + " source=" + db.sourceEntries.Count + " runtime=" + d.Count + " filler=" + (db.FindRuntimeVisualByID("CabinetSection_Filler_Generic_Filler") != null);
}
var k = KitchenDesign.KitchenModel.Kitchen.instance;
r += " | kitchen db=" + (k != null && k.productVisualDatabase != null ? k.productVisualDatabase.name : "none");
return r;
