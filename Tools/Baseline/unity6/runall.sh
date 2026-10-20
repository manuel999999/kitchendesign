#!/bin/sh
D=$(dirname "$0"); P=/Users/emmanuel/Documents/Unity/kitchendesign; cd $P
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:1500])"; }
unity command open_scene --path Assets/AssetsMelcher/Scenes/MenuScene.unity --json --no-banner >/dev/null 2>&1
unity command editor_play --json --no-banner >/dev/null 2>&1
for i in $(seq 1 40); do perl -e 'select(undef,undef,undef,3)'; r=$(ev $D/state.cs); echo "$r" | grep -q "quickActive=1" && break; done
ev $D/rib.cs; ev $D/dbstate.cs; ev $D/quicklogin.cs
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $D/state.cs); echo "$r" | grep -qE "login=(Finished|NotStarted)" && break; done; echo "$r"
rm -rf /Users/emmanuel/Documents/Unity/GeminiBaseline/unity6
for p in 10191:27755 10192:32569 10192:28316 10194:27763 10194:27764 10191:27740 15387:44392 10192:36449; do echo "== $p"; $D/capture.sh ${p%%:*} ${p##*:}; done
ev $D/dbstate.cs
unity command editor_stop --json --no-banner >/dev/null 2>&1
python3 -I $D/../compare.py 27755 32569 28316 27763 27764 27740 44392 36449
