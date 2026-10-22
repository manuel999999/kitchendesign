"""Compare two edit probe runs (editprobe.sh): python3 -I compare_probe.py <ref> <new>
(folder names under GeminiBaseline/). Snapshots, log.txt and resave.json must be identical;
measured.json may differ only under "cameras" (the idle photo vcam keeps its last pose, the orbit
camera varies slightly between runs)."""
import json, os, sys

BASE = '/Users/emmanuel/Documents/Unity/GeminiBaseline'


def walk(a, b, path, out):
    if isinstance(a, dict) and isinstance(b, dict):
        for k in sorted(set(a) | set(b)):
            walk(a.get(k), b.get(k), path + '.' + str(k), out)
    elif isinstance(a, list) and isinstance(b, list) and len(a) == len(b):
        for i, (x, y) in enumerate(zip(a, b)):
            walk(x, y, path + '[' + str(i) + ']', out)
    elif a != b:
        out.append(path)


ref, new = (os.path.join(BASE, x) for x in sys.argv[1:3])
problems = []
for sub in ['.', 'standard']:
    r, n = os.path.join(ref, sub), os.path.join(new, sub)
    files = sorted(set(os.listdir(r)) | set(os.listdir(n)))
    for f in files:
        rp, np_ = os.path.join(r, f), os.path.join(n, f)
        if os.path.isdir(rp) or os.path.isdir(np_):
            continue
        if not (os.path.exists(rp) and os.path.exists(np_)):
            problems.append(f'{sub}/{f}: only in one run')
            continue
        if f == 'measured.json':
            out = []
            walk(json.load(open(rp)), json.load(open(np_)), '', out)
            out = [p for p in out if not p.startswith('.cameras')]
            problems += [f'{sub}/{f}: {p}' for p in out]
        elif open(rp, 'rb').read() != open(np_, 'rb').read():
            problems.append(f'{sub}/{f}: differs')
for p in problems:
    print(p)
print('identical (cameras aside)' if not problems else f'{len(problems)} differences')
