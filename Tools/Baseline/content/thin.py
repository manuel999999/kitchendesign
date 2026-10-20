import json, os, re, sys
from collections import Counter

ROOT = "/Users/emmanuel/Documents/Unity/kitchendesign"
HOLD = "/Users/emmanuel/Documents/Unity/kitchendesign-held-back"
BASE = "/Users/emmanuel/Documents/Unity/GeminiBaseline/designs/"
DESIGNS = [27755, 32569, 28316, 27763, 27764, 27740, 44392, 36449]
PRODUCT_BRANDS = {"TwinEagles", "Bull"}
THIN = ("Assets/AssetsMelcher/ProductVisuals/", "Assets/AssetsMelcher/CabinetSectionModels/", "Assets/AssetsMelcher/ProductModels/")
DB = "Assets/AssetsMelcher/ProductVisualDatabase.asset"
NOT_ROOTS = ("Assets/AddressableAssetsData/AssetGroups/", DB)
GUID_RE = re.compile(rb"(?:guid: |m_AssetGUID: |GUID:)([0-9a-f]{32})")
BINARY = {".fbx", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".tif", ".tiff", ".dll", ".ttf", ".otf",
          ".wav", ".mp3", ".ogg", ".mp4", ".mov", ".pdf", ".zip", ".obj", ".blend", ".bytes", ".so", ".a", ".gif", ".bmp"}
apply = len(sys.argv) > 1 and sys.argv[1] == "--apply"

files = []
for top in ("Assets", "Packages", "ProjectSettings"):
    for dp, dn, fn in os.walk(os.path.join(ROOT, top)):
        for f in fn:
            files.append(os.path.relpath(os.path.join(dp, f), ROOT))
fileset = set(files)
g2p = {}
for f in files:
    if f.endswith(".meta"):
        with open(os.path.join(ROOT, f), "rb") as fh:
            m = re.search(rb"^guid: ([0-9a-f]{32})", fh.read(), re.M)
        if m:
            g2p[m.group(1).decode()] = f[:-5]

def refs(path):
    out = set()
    if os.path.splitext(path)[1].lower() not in BINARY and os.path.isfile(os.path.join(ROOT, path)):
        with open(os.path.join(ROOT, path), "rb") as fh:
            out.update(g.decode() for g in GUID_RE.findall(fh.read()))
    if path + ".meta" in fileset:
        with open(os.path.join(ROOT, path + ".meta"), "rb") as fh:
            out.update(g.decode() for g in GUID_RE.findall(fh.read()))
    return out

thin = lambda p: p.startswith(THIN)

with open(os.path.join(ROOT, DB), "rb") as fh:
    db_guids = [g.decode() for g in GUID_RE.findall(fh.read())][1:]
db_paths = [g2p[g] for g in db_guids if g in g2p]

used = set()
for q in DESIGNS:
    m = json.load(open(BASE + f"{q}/measured.json"))
    used |= {c["visual"] for c in m["cabinets"] if c["visual"]}
    used |= {o["visual"] for o in m["occupants"] if o["visual"]}
used_bases = {u.rstrip("^") for u in used}

def keep_entry(path):
    n = os.path.basename(path)[:-len(".asset")].rstrip("^")
    if n.startswith(("ProductVisual_Cabinet_", "ProductVisual_Flooring_", "ProductVisual_Countertop_")):
        return "finish"
    base = n.split("@")[0]
    if base in used_bases:
        return "design"
    if n.startswith("ProductVisual_CabinetSection_"):
        return "generic" if ("_Generic_" in n or "_Filler_" in n) else None
    parts = n.replace("ProductVisual_", "").split("_")
    return "brand" if len(parts) > 2 and parts[1] in PRODUCT_BRANDS else None

kept_entries = {}
for p in db_paths:
    k = keep_entry(p)
    if k:
        kept_entries[p] = k

roots = [f for f in files if not f.endswith(".meta") and not thin(f) and not f.startswith(NOT_ROOTS)] + list(kept_entries)
reached, stack = set(), []
for r in roots:
    if thin(r) and r not in reached:
        reached.add(r); stack.append(r)
    for g in refs(r):
        p = g2p.get(g)
        if p and thin(p) and p not in reached:
            reached.add(p); stack.append(p)
while stack:
    p = stack.pop()
    for g in refs(p):
        q = g2p.get(g)
        if q and thin(q) and q not in reached:
            reached.add(q); stack.append(q)

thin_assets = [f for f in files if thin(f) and not f.endswith(".meta")]
move = [f for f in thin_assets if f not in reached]
kept_db_after = [p for p in db_paths if p in reached]

report = {
    "db_entries": len(db_paths),
    "entries_selected_by_rule": Counter(kept_entries.values()),
    "db_entries_still_reachable": len(kept_db_after),
    "db_entries_kept_by_kind": Counter(
        "finish" if os.path.basename(p).startswith(("ProductVisual_Cabinet_", "ProductVisual_Flooring_", "ProductVisual_Countertop_"))
        else "cabinet" if os.path.basename(p).startswith("ProductVisual_CabinetSection_") else "product" for p in kept_db_after),
    "thin_folder_assets": len(thin_assets),
    "kept": len(thin_assets) - len(move),
    "to_move": len(move),
    "to_move_by_folder": Counter(f.split("/")[2] for f in move),
    "used_visuals_missing_from_db": sorted(u for u in used if not any(os.path.basename(p).startswith(u) for p in db_paths)),
}
print(json.dumps(report, indent=1, default=dict))
open(os.path.join(os.path.dirname(__file__), "thin_move.txt"), "w").write("\n".join(move) + "\n")

if apply:
    moved = 0
    for f in move:
        for p in (f, f + ".meta"):
            src = os.path.join(ROOT, p)
            if os.path.exists(src):
                dst = os.path.join(HOLD, p)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                os.rename(src, dst)
                moved += 1
    print("moved files (incl .meta):", moved)
