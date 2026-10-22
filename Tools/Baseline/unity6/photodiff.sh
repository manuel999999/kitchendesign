#!/bin/sh
# Mean per-channel pixel difference (/255) of every save photo between two runall.sh runs.
# Usage: photodiff.sh <reference folder> <new folder>   (folders under GeminiBaseline/; editor open)
D=$(dirname "$0"); T=$(mktemp -t photodiff).cs
for q in 27755 32569 28316 27763 27764 27740 44392 36449; do
    sed "s#__REF__#$1#; s#__NEW__#$2#; s#__QUOTE__#$q#" $D/photodiff.tmpl > $T
    unity command eval_file --file $T --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:600])"
done
rm -f $T
