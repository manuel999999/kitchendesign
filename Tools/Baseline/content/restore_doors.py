import json, os, re, sys

ROOT = "/Users/emmanuel/Documents/Unity/kitchendesign"
HOLD = "/Users/emmanuel/Documents/Unity/kitchendesign-held-back"
BASE = "/Users/emmanuel/Documents/Unity/GeminiBaseline/designs/"
DESIGNS = [27755, 32569, 28316, 27763, 27764, 27740, 44392, 36449]
GUID_RE = re.compile(rb"(?:guid: |m_AssetGUID: |GUID:)([0-9a-f]{32})")
BINARY = {".fbx", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".tif", ".tiff", ".obj", ".blend"}
apply = len(sys.argv) > 1 and sys.argv[1] == "--apply"

used = set()
for q in DESIGNS:
    m = json.load(open(BASE + f"{q}/measured.json"))
    used |= {c["visual"].replace("ProductVisual_", "").rstrip("^") for c in m["cabinets"] if c["visual"]}

mapping = {}
mdir = os.path.join(ROOT, "Assets/AssetsMelcher/CabinetSectionMappings")
for f in os.listdir(mdir):
    if f.endswith(".asset"):
        txt = open(os.path.join(mdir, f)).read()
        for k, v in re.findall(r"- key: (\S+)\n\s+value: (\S+)", txt):
            mapping[k] = v
door_prefixes = {"ProductVisual_" + mapping[u] + "@" for u in used if u in mapping}

held = []
for dp, dn, fn in os.walk(HOLD):
    for f in fn:
        held.append(os.path.relpath(os.path.join(dp, f), HOLD))
held_set = set(held)
g2held = {}
for f in held:
    if f.endswith(".meta"):
        m = re.search(rb"^guid: ([0-9a-f]{32})", open(os.path.join(HOLD, f), "rb").read(), re.M)
        if m:
            g2held[m.group(1).decode()] = f[:-5]

entries = [f for f in held if f.startswith("Assets/AssetsMelcher/ProductVisuals/") and f.endswith("^.asset")
           and any(os.path.basename(f).startswith(p) for p in door_prefixes)]

def refs(path):
    out = set()
    for p in (path, path + ".meta"):
        full = os.path.join(HOLD, p)
        if p in held_set and (p.endswith(".meta") or os.path.splitext(p)[1].lower() not in BINARY):
            out.update(g.decode() for g in GUID_RE.findall(open(full, "rb").read()))
    return out

restore, stack = set(entries), list(entries)
while stack:
    p = stack.pop()
    for g in refs(p):
        q = g2held.get(g)
        if q and q not in restore:
            restore.add(q); stack.append(q)

print(json.dumps({
    "used_cabinets": len(used), "with_door_mapping": len([u for u in used if u in mapping]),
    "door_entries": len(entries), "assets_to_restore": len(restore),
    "by_folder": {k: sum(1 for r in restore if r.split("/")[2] == k) for k in {r.split("/")[2] for r in restore}},
}, indent=1))
open(os.path.join(os.path.dirname(__file__), "restored_door_entries.txt"), "w").write("\n".join(sorted(entries)) + "\n")

if apply:
    moved = 0
    for r in sorted(restore):
        for p in (r, r + ".meta"):
            src = os.path.join(HOLD, p)
            if os.path.exists(src):
                dst = os.path.join(ROOT, p)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                os.rename(src, dst)
                moved += 1
    print("moved back (incl .meta):", moved)
