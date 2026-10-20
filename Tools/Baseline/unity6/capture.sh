#!/bin/sh
# usage: capture.sh <client_id> <quote_id>   (Unity 6 copy, via the Unity CLI)
D=$(dirname "$0"); C=$1; Q=$2; OUT=/Users/emmanuel/Documents/Unity/GeminiBaseline/unity6/$Q; P=/Users/emmanuel/Documents/Unity/kitchendesign
ev() { (cd $P && unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1) | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:1500])"; }
sed "s/__CLIENT__/$C/g; s/__QUOTE__/$Q/g" $D/load.tmpl > $D/load.cs
ev $D/load.cs
for i in $(seq 1 30); do perl -e 'select(undef,undef,undef,2)'; ev $D/kstatus.cs | grep -q "kitchen=none" && break; done
prev=""; same=0
for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,5)'; r=$(ev $D/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done
echo "loaded: $r"
ev $D/photos.cs >/dev/null
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,3)'; n1=$(ls $P/photo_*.jpg 2>/dev/null | wc -l); perl -e 'select(undef,undef,undef,3)'; n2=$(ls $P/photo_*.jpg 2>/dev/null | wc -l); [ "$n2" -gt 0 ] && [ "$n1" = "$n2" ] && break; done
sed "s#__OUTDIR__#$OUT#; s/__QUOTE__/$Q/" $D/measure.tmpl > $D/measure.cs
echo "measured: $(ev $D/measure.cs)"
mkdir -p "$OUT/photos"; for f in $P/photo_*.jpg; do [ -e "$f" ] && mv "$f" "$OUT/photos/"; done
ls "$OUT/photos" | tr '\n' ' '; echo
