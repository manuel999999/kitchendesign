#!/bin/sh
# usage: open_design.sh <client_id> <quote_id>   signs in if needed, then loads the design (editor play mode)
B=/Users/emmanuel/Documents/Unity/kitchendesign/Tools/Baseline/unity6; P=/Users/emmanuel/Documents/Unity/kitchendesign; cd $P
C=$1; Q=$2
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:1500])"; }
playing=$(unity command editor_status --json --no-banner 2>/dev/null | python3 -I -c "import json,sys; print(json.load(sys.stdin)['data']['result']['playMode'])")
if [ "$playing" != "playing" ]; then
  unity command open_scene --path Assets/AssetsMelcher/Scenes/MenuScene.unity --json --no-banner >/dev/null 2>&1
  unity command editor_play --json --no-banner >/dev/null 2>&1
  for i in $(seq 1 40); do perl -e 'select(undef,undef,undef,3)'; r=$(ev $B/state.cs); echo "$r" | grep -q "quickActive=1" && break; done
  ev $B/rib.cs; ev $B/quicklogin.cs
  for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $B/state.cs); echo "$r" | grep -qE "login=(Finished|NotStarted)" && break; done; echo "$r"
fi
sed "s/__CLIENT__/$C/g; s/__QUOTE__/$Q/g" $B/load.tmpl > $B/load.cs
ev $B/load.cs
for i in $(seq 1 30); do perl -e 'select(undef,undef,undef,2)'; ev $B/kstatus.cs | grep -q "kitchen=none" && break; done
prev=""; same=0
for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $B/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done
echo "loaded: $r"
