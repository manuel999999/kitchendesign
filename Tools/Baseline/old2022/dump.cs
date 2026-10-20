using System.Collections.Generic;
public static class K { public static string Run() {
  var all = new List<object>(); int n = 0;
  foreach (var c in Melcher.ProFitGlobals.userClients) {
    if (c.quotes == null) continue;
    foreach (var q in c.quotes) {
      n++;
      all.Add(new Dictionary<string, object> {
        {"client_id", c.id}, {"quote_id", q.id}, {"save_name", q.saveName}, {"pdf", q.pdfEndpoint},
        {"cover", q.coverImageName}, {"top", q.topDownImageName}, {"front", q.frontImageNames == null ? new List<object>() : new List<object>(q.frontImageNames)},
        {"save_data", q.saveData} });
    }
  }
  System.IO.Directory.CreateDirectory("/Users/emmanuel/Documents/Unity/GeminiBaseline/_inventory");
  System.IO.File.WriteAllText("/Users/emmanuel/Documents/Unity/GeminiBaseline/_inventory/all-designs.json", MiniJSON.Json.Serialize(all));
  return "clients=" + Melcher.ProFitGlobals.userClients.Count + " designs=" + n + " login=" + Melcher.ProFitGlobals.logInCoroutineState;
} }
