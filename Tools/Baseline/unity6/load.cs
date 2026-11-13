int ci, qi;
var clients = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.AppInitializer>().SystemsForEditor.ClientList;
if (!clients.TryGetClientAndQuoteIndex(10191, 27755, out ci, out qi)) return "not found";
clients.ActiveClientId = 10191;
clients.ActiveQuoteId = 27755;
Melcher.MelcherSceneManager.LoadSceneAsync(Melcher.MelcherSceneRequest.FromName("DesignerScene"), Melcher.MelcherSceneLoadMode.Single);
return "loading quote 27755";
