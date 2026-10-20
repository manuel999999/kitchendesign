var k = Melcher.Kitchen.instance;
var skin = Melcher.ProFitGlobals.appOptions.appSkin;
Melcher.Cabinet target = null;
foreach (var entry in k.layout.EnumerateCabinets()) foreach (var occ in entry.element.occupants) if (target == null) target = entry.element;
Cysharp.Threading.Tasks.UniTaskExtensions.Forget(target.SetCabinetMaterials(skin.primarySelectionMaterial, skin.primarySelectionMaterial, null));
return "highlighted " + target.name + " with " + skin.primarySelectionMaterial.name + " (" + skin.primarySelectionMaterial.shader.name + ")";
