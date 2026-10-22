#!/bin/sh
# Read-only walk through the menu screens and back from the designer. Never creates, edits,
# deletes or saves anything on the server. Needs the 6.6 editor open on this project, not playing.
D=$(dirname "$0"); P=/Users/emmanuel/Documents/Unity/kitchendesign; cd $P
C=10191; Q=27755
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:1500])"; }
wait3() { perl -e 'select(undef,undef,undef,3)'; }
errs() { unity command console_status --json --no-banner 2>&1 | python3 -I -c "import json,sys; r=(json.load(sys.stdin).get('data') or {}).get('result') or {}; g=r.get('groundTruth') or {}; print('errors=%s warnings=%s' % (g.get('consoleErrors'), g.get('consoleWarnings')))"; }
step() { sed "s/__STEP__/$1/g; s/__CLIENT__/$C/g; s/__QUOTE__/$Q/g" $D/menu.tmpl > $D/menu.cs; ev $D/menu.cs >/dev/null; wait3; sed "s/__STEP__/state/g; s/__CLIENT__/$C/g; s/__QUOTE__/$Q/g" $D/menu.tmpl > $D/menu.cs; echo "$1: $(ev $D/menu.cs)"; }
unity command open_scene --path Assets/AssetsMelcher/Scenes/MenuScene.unity --json --no-banner >/dev/null 2>&1
unity command clear_console --json --no-banner >/dev/null 2>&1
unity command editor_play --json --no-banner >/dev/null 2>&1
for i in $(seq 1 40); do wait3; r=$(ev $D/state.cs); echo "$r" | grep -q "quickActive=1" && break; done
ev $D/rib.cs >/dev/null; ev $D/quicklogin.cs
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $D/state.cs); echo "$r" | grep -qE "login=Finished clients=[1-9]" && break; done; echo "signed in: $r"
echo "console after sign-in: $(errs)"
for s in home clients quotes clientinfo home clients quoteinfo quoteinfo_close profile logout logout_close home; do step $s; done
echo "console after menu: $(errs)"
sed "s/__CLIENT__/$C/g; s/__QUOTE__/$Q/g" $D/load.tmpl > $D/load.cs; ev $D/load.cs
prev=""; same=0
for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $D/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done
echo "designer: $r"
ev $D/tomenu.cs
for i in $(seq 1 40); do wait3; r=$(ev $D/state.cs); echo "$r" | grep -q "scene=MenuScene" && break; done; wait3; wait3
echo "back: $r"
step state
step home
echo "console at end: $(errs)"
unity command editor_stop --json --no-banner >/dev/null 2>&1
