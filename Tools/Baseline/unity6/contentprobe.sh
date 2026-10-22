#!/bin/sh
# Play-mode probe of ProductVisualContentStatus (area 3, B3.3): signs in, opens two designs and
# goes back to the menu, printing the content states each time. Read-only. Back up the user's
# files first (see README); editor open, not playing.
D=$(dirname "$0"); P=/Users/emmanuel/Documents/Unity/kitchendesign; cd $P
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:800])"; }
stable() { prev=""; same=0; for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,4)'; r=$(ev $D/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done; echo "loaded: $r"; }
unity command open_scene --path Assets/AssetsMelcher/Scenes/MenuScene.unity --json --no-banner >/dev/null 2>&1
unity command editor_play --json --no-banner >/dev/null 2>&1
for i in $(seq 1 40); do perl -e 'select(undef,undef,undef,3)'; ev $D/state.cs | grep -q "quickActive=1" && break; done
ev $D/rib.cs >/dev/null; ev $D/quicklogin.cs >/dev/null
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,5)'; ev $D/state.cs | grep -qE "login=Finished clients=[1-9]" && break; done
echo "menu:     $(ev $D/contentprobe.cs)"
for p in 10191:27755 10192:32569; do
  sed "s/__CLIENT__/${p%%:*}/g; s/__QUOTE__/${p##*:}/g" $D/load.tmpl > $D/load.cs; ev $D/load.cs >/dev/null; stable
  echo "${p##*:}:    $(ev $D/contentprobe.cs)"
  ev $D/tomenu.cs >/dev/null
  for i in $(seq 1 30); do perl -e 'select(undef,undef,undef,2)'; ev $D/state.cs | grep -q "^scene=MenuScene" && break; done; perl -e 'select(undef,undef,undef,3)'
  echo "menu:     $(ev $D/contentprobe.cs)"
done
unity command editor_stop --json --no-banner >/dev/null 2>&1
