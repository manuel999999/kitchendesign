#!/bin/sh
# Edit probe (area 4): signs in, opens design 28316 and makes a fixed series of edits through the
# EventBridge.Designer calls the toolbar makes (highlight, nudge, center, clone, drag, delete, widget
# resize, clone/reorder/add/delete a section, toe kicks, finish, delete a backing, add a product).
# After each edit it waits until the kitchen is stable and writes a snapshot; at the end measured.json
# and resave.json (SaveToJson, never sent). Nothing is saved to the server. Output:
# GeminiBaseline/editprobe/ (deleted first; copy it to a named folder to keep it). Compare two runs
# with diff -r. A second phase then builds a new Standard (ProFit-style) L kitchen in the same session
# and repeats the edits there (free placement, dragging and the resize widgets, which Catalog
# designs refuse); its measured.json and resave.json are in standard/. Back up the user's files first (see README); editor open, not playing. No options.
D=$(dirname "$0"); P=/Users/emmanuel/Documents/Unity/kitchendesign; OUT=/Users/emmanuel/Documents/Unity/GeminiBaseline/editprobe; cd $P
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:800])"; }
sig() { sed "s#__OUTDIR__#$OUT#; s#__NAME__#$1#" $D/editsig.tmpl > $D/editsig.cs; ev $D/editsig.cs; }
settle() { prev=""; same=0; for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,1.5)'; r=$(sig ""); if [ "$r" = "$prev" ]; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done; }
step() { sed "s#__OUTDIR__#$OUT#; s#__STEP__#$2#" $D/editstep.tmpl > $D/editstep.cs; echo "$1 $2: $(ev $D/editstep.cs)"; settle; echo "   $(sig $1_$2)"; }
unity command open_scene --path Assets/AssetsMelcher/Scenes/MenuScene.unity --json --no-banner >/dev/null 2>&1
unity command editor_play --json --no-banner >/dev/null 2>&1
for i in $(seq 1 40); do perl -e 'select(undef,undef,undef,3)'; ev $D/state.cs | grep -q "quickActive=1" && break; done
ev $D/rib.cs >/dev/null; ev $D/quicklogin.cs >/dev/null
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,5)'; ev $D/state.cs | grep -qE "login=Finished clients=[1-9]" && break; done
rm -rf $OUT; mkdir -p $OUT
sed "s/__CLIENT__/10192/g; s/__QUOTE__/28316/g" $D/load.tmpl > $D/load.cs; ev $D/load.cs >/dev/null
prev=""; same=0; for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,4)'; r=$(ev $D/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done; echo "loaded: $r"
step 00 setup
n=1
for s in hl_occ nudge nudge_up center clone_occ drag_begin drag_move drag_end destroy_occ hl_cab resize_begin resize_move resize_end clone_cab reorder add_cab destroy_cab toekick finish destroy_backing add_occ hl_none; do
  step $(printf %02d $n) $s; n=$((n+1))
done
sed "s#__OUTDIR__#$OUT#; s/__QUOTE__/28316/" $D/measure.tmpl > $D/measure.cs
echo "measured: $(ev $D/measure.cs)"
for s in new_standard add_grill add_drawer hl_occ nudge nudge_up center clone_occ drag_begin drag_move drag_end destroy_occ hl_cab resize_begin resize_move resize_end clone_cab reorder add_cab destroy_cab toekick finish add_occ hl_none; do
  step $(printf %02d $n) $s; n=$((n+1))
done
sed "s#__OUTDIR__#$OUT/standard#; s/__QUOTE__/0/" $D/measure.tmpl > $D/measure.cs
echo "measured standard: $(ev $D/measure.cs)"
grep -c "" $OUT/log.txt | sed 's/^/log lines: /'
unity command editor_stop --json --no-banner >/dev/null 2>&1
