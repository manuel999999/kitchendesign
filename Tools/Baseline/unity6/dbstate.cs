var dbs = UnityEngine.Resources.FindObjectsOfTypeAll<Melcher.ProductVisualDatabase>();
string r = "databases in memory=" + dbs.Length;
foreach (var db in dbs) {
  var f = typeof(Melcher.ProductVisualDatabase).GetField("m_VisualIdToRuntimeProductVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var d = (System.Collections.IDictionary)f.GetValue(db);
  r += " | " + db.name + " source=" + db.sourceEntries.Count + " runtime=" + d.Count + " filler=" + (db.FindRuntimeVisualByID("CabinetSection_Filler_Generic_Filler") != null);
}
var k = Melcher.Kitchen.instance;
r += " | kitchen db=" + (k != null && k.productVisualDatabase != null ? k.productVisualDatabase.name : "none");
return r;
