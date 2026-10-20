var k = Melcher.Kitchen.instance;
Melcher.Occupant target = null;
Melcher.Cabinet cab = null;
foreach (var entry in k.layout.EnumerateCabinets()) foreach (var occ in entry.element.occupants) if (target == null && occ.name.Contains("__NAME__")) { target = occ; cab = entry.element; }
if (target == null) return "no occupant matching __NAME__";
var root = target.productVisualContentInstance.cabinetCutOut.stencilRoot;
var b = new UnityEngine.Bounds(root.position, UnityEngine.Vector3.zero);
foreach (var r in root.GetComponentsInChildren<UnityEngine.Renderer>(true)) b.Encapsulate(r.bounds);
var t = cab.GetTransform();
var eye = b.center + t.rotation * new UnityEngine.Vector3(__EYE__);
var go = UnityEngine.GameObject.Find("CutoutTestCam");
go.transform.position = eye;
go.transform.rotation = UnityEngine.Quaternion.LookRotation(b.center - eye, UnityEngine.Vector3.up);
go.GetComponent<UnityEngine.Camera>().fieldOfView = __FOV__f;
return "aimed at " + target.name + " " + b.center.ToString("F2");
