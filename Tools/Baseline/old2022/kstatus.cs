using System.Linq;
public static class K { public static string Run() {
  var k = Melcher.Kitchen.instance;
  string sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
  if (k == null || k.layout == null) return sc + " kitchen=none";
  int cabs = k.layout.EnumerateCabinets().Count();
  int occ = UnityEngine.Object.FindObjectsOfType<Melcher.Occupant>().Length;
  int rend = k.GetComponentsInChildren<UnityEngine.Renderer>(false).Length;
  return sc + " spans=" + k.layout.spans.Count + " cabs=" + cabs + " occ=" + occ + " rend=" + rend + " layout=" + k.layout.name;
} }
