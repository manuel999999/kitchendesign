var k = Melcher.Kitchen.instance;
string sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
if (k == null || k.layout == null) return sc + " kitchen=none";
int cabs = 0; foreach (var e in k.layout.EnumerateCabinets()) cabs++;
int occ = UnityEngine.Object.FindObjectsByType<Melcher.Occupant>(UnityEngine.FindObjectsSortMode.None).Length;
int rend = k.GetComponentsInChildren<UnityEngine.Renderer>(false).Length;
return sc + " spans=" + k.layout.spans.Count + " cabs=" + cabs + " occ=" + occ + " rend=" + rend + " layout=" + k.layout.name;
