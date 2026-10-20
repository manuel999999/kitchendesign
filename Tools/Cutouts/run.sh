#!/bin/sh
# usage: run.sh <snippet.cs> [sed expressions...]   runs an eval_file snippet with optional placeholder substitutions
P=/Users/emmanuel/Documents/Unity/kitchendesign; F=$1; shift; T=$(mktemp -t cutrun).cs
cp "$F" $T; for e in "$@"; do sed -i '' "$e" $T; done
cd $P && unity command eval_file --file $T --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:2500])"
rm -f $T
