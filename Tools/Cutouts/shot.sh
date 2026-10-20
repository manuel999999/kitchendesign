#!/bin/sh
# usage: shot.sh <output.png> [camera name]   renders a camera (default MainCamera) to a PNG anywhere on disk
P=/Users/emmanuel/Documents/Unity/kitchendesign; T=$(mktemp -t cutshot).cs
sed "s#__PATH__#$1#g; s#__CAMERA__#${2:-MainCamera}#g" $P/Tools/Cutouts/shot.cs > $T
cd $P && unity command eval_file --file $T --json --no-banner --timeout 60 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:800])"
rm -f $T
