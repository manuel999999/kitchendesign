var k = Melcher.Kitchen.instance;
var cabMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/AssetsMelcher/Finishes/Cabinet_Endura_WhiteOak.mat");
var topMat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/AssetsMelcher/Finishes/Countertop_Challenger_SilverVein.mat");
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(k.layout.SetCabinetMaterials(cabMat, topMat, null));
bool showProducts = __SHOW__;
Melcher.Cabinet target = null;
foreach (var entry in k.layout.EnumerateCabinets())
{
    foreach (var occ in entry.element.occupants)
    {
        occ.productVisualContentInstance.SetProductVisibility(showProducts);
        if (occ.TryGetComponent(out Melcher.CabinetShelf shelf)) foreach (var nested in shelf.occupants) nested.productVisualContentInstance.SetProductVisibility(showProducts);
        if (target == null) target = entry.element;
    }
}
var go = UnityEngine.GameObject.Find("CutoutTestCam");
if (go == null)
{
    go = new UnityEngine.GameObject("CutoutTestCam");
    var c = go.AddComponent<UnityEngine.Camera>();
    c.enabled = false;
    c.fieldOfView = 40f;
    c.CopyFrom(UnityEngine.Camera.main);
    c.enabled = false;
}
var cam = go.GetComponent<UnityEngine.Camera>();
var t = target.GetTransform();
var center = t.position + t.rotation * new UnityEngine.Vector3(target.sizeInMeters.x * 0.5f, target.sizeInMeters.y * 0.6f, target.sizeInMeters.z * 0.5f);
var eye = center + t.rotation * new UnityEngine.Vector3(__EYE__);
go.transform.position = eye;
go.transform.rotation = UnityEngine.Quaternion.LookRotation(center - eye, UnityEngine.Vector3.up);
cam.fieldOfView = 45f;
return "cab=" + cabMat.shader.name + " top=" + topMat.shader.name + " target=" + target.name + " size=" + target.sizeInMeters.ToString("F3") + " center=" + center.ToString("F2");
