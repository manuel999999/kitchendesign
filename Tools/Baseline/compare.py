import json, sys
B = "/Users/emmanuel/Documents/Unity/GeminiBaseline"
TOL = 0.0002
def walk(a, b, path, out):
    if isinstance(a, dict) and isinstance(b, dict):
        for k in sorted(set(a) | set(b)):
            if k in ("render_bounds",) and path.endswith("]"):
                pass
            if k not in a or k not in b:
                out.append(f"{path}.{k}: only in {'old' if k in a else 'new'}")
            else:
                walk(a[k], b[k], f"{path}.{k}", out)
    elif isinstance(a, list) and isinstance(b, list) and a and all(isinstance(x, dict) and "name" in x for x in a + b):
        walk({x["name"]: x for x in a}, {x["name"]: x for x in b}, path, out)
    elif isinstance(a, list) and isinstance(b, list):
        if len(a) != len(b):
            out.append(f"{path}: length {len(a)} -> {len(b)}")
        for i, (x, y) in enumerate(zip(a, b)):
            walk(x, y, f"{path}[{i}]", out)
    elif isinstance(a, (int, float)) and isinstance(b, (int, float)) and not isinstance(a, bool):
        if abs(a - b) > TOL:
            out.append(f"{path}: {a} -> {b}")
    elif a != b:
        out.append(f"{path}: {str(a)[:80]} -> {str(b)[:80]}")
for q in sys.argv[1:]:
    old = json.load(open(f"{B}/designs/{q}/measured.json")); new = json.load(open(f"{B}/unity6/{q}/measured.json"))
    out = []; walk(old, new, "measured", out)
    ro = json.load(open(f"{B}/designs/{q}/resave.json")); rn = json.load(open(f"{B}/unity6/{q}/resave.json"))
    rout = []
    for k in sorted(set(ro) | set(rn)):
        if k in ("timeStamp", "fileName"): continue
        if ro.get(k) != rn.get(k): rout.append(k)
    groups = {}
    for line in out:
        key = line.split(":")[0]
        for part in ("render_bounds", "cameras", "ground", "renderer_count", "drawn_in_span_space"):
            if part in key: key = part; break
        else: key = "other"
        groups.setdefault(key, []).append(line)
    print(f"== {q}: {len(out)} measurement differences; resave keys differing: {rout or 'none'}")
    for g, lines in groups.items():
        print(f"  {g}: {len(lines)}")
        for l in lines[:6]: print("    ", l)
