var status = KitchenDesign.ProFitGlobals.productContent;
var f = status.GetType().GetField("_states", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var states = (System.Collections.IDictionary)f.GetValue(status);
int ok = 0, failed = 0, withCopies = 0, other = 0;
foreach (var s in states.Values)
{
    var h = (UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.GameObject>)s.GetType().GetProperty("Handle").GetValue(s);
    var c = (KitchenDesign.Products.LoadedProductContent)s.GetType().GetProperty("Content").GetValue(s);
    if (h.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded) ok++; else if (h.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed) failed++; else other++;
    if (c.SourceContentPrefab != null && c.RuntimeContentPrefab != null) withCopies++;
}
if (System.AppDomain.CurrentDomain.GetData("probeSubscribed") == null)
{
    System.AppDomain.CurrentDomain.SetData("probeSubscribed", true);
    System.AppDomain.CurrentDomain.SetData("probeEvents", 0);
    status.ContentLoaded += (a, c) => System.AppDomain.CurrentDomain.SetData("probeEvents", (int)System.AppDomain.CurrentDomain.GetData("probeEvents") + 1);
}
return "scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " states=" + states.Count + " succeeded=" + ok + " failed=" + failed + " pending=" + other + " withCopies=" + withCopies + " eventsSinceSubscribe=" + System.AppDomain.CurrentDomain.GetData("probeEvents");
