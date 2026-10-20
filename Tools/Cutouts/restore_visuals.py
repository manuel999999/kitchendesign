import json, os, re, sys

ROOT = "/Users/emmanuel/Documents/Unity/kitchendesign"
HOLD = "/Users/emmanuel/Documents/Unity/kitchendesign-held-back"
GUID_RE = re.compile(rb"(?:guid: |m_AssetGUID: |GUID:)([0-9a-f]{32})")
BINARY = {".fbx", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".tif", ".tiff", ".obj", ".blend"}

args = [a for a in sys.argv[1:] if a != "--apply"]
apply = "--apply" in sys.argv
names = args

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

entries = [f for f in held if f.startswith("Assets/AssetsMelcher/ProductVisuals/") and not f.endswith(".meta")
           and any(os.path.basename(f).startswith("ProductVisual_" + n + ".") or os.path.basename(f).startswith("ProductVisual_" + n + "^.") for n in names)]

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
            restore.add(q)
            stack.append(q)

print(json.dumps({"entries": sorted(entries), "assets_to_restore": len(restore), "files": sorted(restore)}, indent=1))

if apply:
    moved = 0
    for r in sorted(restore):
        for p in (r, r + ".meta"):
            src = os.path.join(HOLD, p)
            if os.path.exists(src):
                dst = os.path.join(ROOT, p)
                if os.path.exists(dst):
                    print("exists, skipped:", p)
                    continue
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                os.rename(src, dst)
                moved += 1
    print("moved back (incl .meta):", moved)
