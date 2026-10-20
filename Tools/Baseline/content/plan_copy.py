import json, os, re, subprocess, sys
from collections import Counter, defaultdict

REPO = "/Users/emmanuel/Documents/Unity/Gemini"
OUT = sys.argv[1]
FILTERED = ("Unity/Assets/AssetsMelcher/ProductVisuals/", "Unity/Assets/AssetsMelcher/CabinetSectionModels/")
DROP_PREFIXES = (
    "Unity/Assets/KitWright/", "Unity/Assets/KitWright.meta",
    "Unity/Packages/com.unity.render-pipelines.core/", "Unity/Packages/com.unity.render-pipelines.universal/",
    "Unity/Packages/com.unity.shadergraph/", "Unity/Packages/packages-lock.json",
)
NOT_ROOTS = ("Unity/Assets/AddressableAssetsData/AssetGroups/",)
DB = "Unity/Assets/AssetsMelcher/ProductVisualDatabase.asset"
GUID_RE = re.compile(rb"(?:guid: |m_AssetGUID: |GUID:)([0-9a-f]{32})")
TEXT_EXT = {".asset", ".prefab", ".mat", ".unity", ".controller", ".anim", ".overrideController", ".mask",
            ".playable", ".spriteatlas", ".lighting", ".preset", ".physicMaterial", ".renderTexture",
            ".shadergraph", ".shadersubgraph", ".mixer", ".terrainlayer", ".signal", ".asmdef", ".asmref",
            ".flare", ".guiskin", ".fontsettings", ".brush", ".giparams", ".uss", ".uxml", ".tss", ".json"}

def git(*a):
    return subprocess.run(["git", "-C", REPO, *a], check=True, capture_output=True).stdout

files = [f for f in git("ls-files", "-z", "Unity/Assets", "Unity/ProjectSettings", "Unity/Packages").decode().split("\0") if f]
staged = git("diff", "--cached", "--name-only", "-z").decode().split("\0")
assert not [s for s in staged if s], "old repo has staged changes"
modified = set(f for f in git("diff", "--name-only", "-z", "HEAD").decode().split("\0") if f)

def read(path):
    if path in modified:
        return git("show", f"HEAD:{path}")
    with open(os.path.join(REPO, path), "rb") as fh:
        return fh.read()

files = [f for f in files if not f.startswith(DROP_PREFIXES)]
guid_to_path = {}
for f in files:
    if f.endswith(".meta") and f.startswith("Unity/Assets/"):
        m = re.search(rb"^guid: ([0-9a-f]{32})", read(f), re.M)
        if m:
            guid_to_path[m.group(1).decode()] = f[:-5]

def refs(path):
    ext = os.path.splitext(path)[1]
    out = set()
    if ext in TEXT_EXT:
        out.update(g.decode() for g in GUID_RE.findall(read(path)))
    meta = path + ".meta"
    if meta in fileset:
        out.update(g.decode() for g in GUID_RE.findall(read(meta)))
    return out

fileset = set(files)
in_filtered = lambda p: p.startswith(FILTERED)
roots = [f for f in files if not f.endswith(".meta") and not in_filtered(f) and not f.startswith(NOT_ROOTS)]
db_guids = [g.decode() for g in GUID_RE.findall(read(DB))]
missing_db = [g for g in db_guids if g not in guid_to_path]

reached, stack, reached_from = set(), [], {}
for r in roots:
    for g in refs(r):
        p = guid_to_path.get(g)
        if p and in_filtered(p) and p not in reached:
            reached.add(p); stack.append(p); reached_from[p] = r
while stack:
    p = stack.pop()
    for g in refs(p):
        q = guid_to_path.get(g)
        if q and in_filtered(q) and q not in reached:
            reached.add(q); stack.append(q); reached_from[q] = reached_from[p]

root_hits = Counter(reached_from[p] for p in reached)
filtered_assets = [f for f in files if in_filtered(f) and not f.endswith(".meta")]
folder_metas = [f for f in files if in_filtered(f) and f.endswith(".meta") and f[:-5] not in fileset]
dropped = [f for f in filtered_assets if f not in reached and "NatureKast" in os.path.basename(f)]
keep_filtered = set(filtered_assets) - set(dropped)

keep = [f for f in files if not in_filtered(f)]
keep += sorted(keep_filtered) + [p + ".meta" for p in sorted(keep_filtered) if p + ".meta" in fileset] + folder_metas

def brand(p):
    n = os.path.basename(p)
    parts = re.split(r"[_.@]", n.replace("ProductVisual_", ""))
    return parts[1] if len(parts) > 2 else parts[0]

report = {
    "tracked_after_fixed_drops": len(files),
    "db_entries": len(db_guids), "db_entries_not_in_project": len(missing_db),
    "filtered_assets": len(filtered_assets), "filtered_kept": len(keep_filtered), "filtered_dropped": len(dropped),
    "roots_reaching_filtered": root_hits.most_common(15),
    "dropped_by_folder_and_brand": {
        fo: Counter(brand(p) for p in dropped if p.startswith(fo)).most_common(25) for fo in FILTERED},
    "kept_by_folder_and_brand_top": {
        fo: Counter(brand(p) for p in keep_filtered if p.startswith(fo)).most_common(40) for fo in FILTERED},
    "modified_taken_from_head": sorted(modified & set(keep)),
    "copy_file_count": len(keep),
}
os.makedirs(OUT, exist_ok=True)
json.dump(report, open(os.path.join(OUT, "report.json"), "w"), indent=1)
open(os.path.join(OUT, "keep.txt"), "w").write("\n".join(keep) + "\n")
open(os.path.join(OUT, "dropped.txt"), "w").write("\n".join(dropped) + "\n")
print(json.dumps({k: v for k, v in report.items() if k not in ("kept_by_folder_and_brand_top",)}, indent=1))
