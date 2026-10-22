# Compares two runs of runall.sh (folders under GeminiBaseline/): resave.json must be identical,
# measured.json may differ only in camera angles by less than the tolerance (run-to-run noise).
# Usage: python3 -I compare_runs.py <reference folder> <new folder> [camera tolerance in degrees]
import json
import os
import sys

base = "/Users/emmanuel/Documents/Unity/GeminiBaseline"
ref, new = (os.path.join(base, a) for a in sys.argv[1:3])
tol = float(sys.argv[3]) if len(sys.argv) > 3 else 0.01
designs = ["27755", "32569", "28316", "27763", "27764", "27740", "44392", "36449"]


def walk(a, b, path, out):
    if isinstance(a, dict) and isinstance(b, dict):
        for k in sorted(set(a) | set(b)):
            walk(a.get(k), b.get(k), path + "." + str(k), out)
    elif isinstance(a, list) and isinstance(b, list):
        if len(a) != len(b):
            out.append((path, "length %d vs %d" % (len(a), len(b))))
            return
        for i, (u, v) in enumerate(zip(a, b)):
            walk(u, v, "%s[%d]" % (path, i), out)
    elif a != b:
        out.append((path, a, b))


failed = False
for q in designs:
    r = open(os.path.join(ref, q, "resave.json"), "rb").read()
    n = open(os.path.join(new, q, "resave.json"), "rb").read()
    diffs = []
    walk(json.load(open(os.path.join(ref, q, "measured.json"))),
         json.load(open(os.path.join(new, q, "measured.json"))), "", diffs)
    noise = [d for d in diffs if d[0].startswith(".cameras") and len(d) == 3
             and isinstance(d[1], (int, float)) and isinstance(d[2], (int, float)) and abs(d[1] - d[2]) <= tol]
    real = [d for d in diffs if d not in noise]
    ok = r == n and not real
    failed |= not ok
    print("%s resave %s, measured: %d camera noise, %d other%s" % (
        q, "identical" if r == n else "DIFFERENT", len(noise), len(real), "" if ok else "  <-- CHECK"))
    for d in real[:10]:
        print("    ", d)
print("RESULT:", "same" if not failed else "DIFFERENT")
