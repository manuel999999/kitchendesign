#!/bin/sh
# usage: capture.sh <client_id> <quote_id>
D=$(dirname "$0"); C=$1; Q=$2; OUT=/Users/emmanuel/Documents/Unity/GeminiBaseline/designs/$Q; P=/Users/emmanuel/Documents/Unity/Gemini/Unity
sed "s/__CLIENT__/$C/; s/__QUOTE__/$Q/" $D/load.tmpl > $D/load.cs
$D/kw.sh code $D/load.cs
for i in $(seq 1 30); do perl -e 'select(undef,undef,undef,2)'; $D/kw.sh code $D/kstatus.cs | grep -q "kitchen=none" && break; done
prev=""; same=0
for i in $(seq 1 90); do perl -e 'select(undef,undef,undef,5)'; r=$($D/kw.sh code $D/kstatus.cs); if [ "$r" = "$prev" ] && echo "$r" | grep -q "^DesignerScene spans"; then same=$((same+1)); else same=0; fi; prev="$r"; [ $same -ge 3 ] && break; done
echo "loaded: $r"
$D/kw.sh code $D/photos.cs >/dev/null
for i in $(seq 1 60); do perl -e 'select(undef,undef,undef,3)'; n1=$(ls $P/photo_*.jpg 2>/dev/null | wc -l); perl -e 'select(undef,undef,undef,3)'; n2=$(ls $P/photo_*.jpg 2>/dev/null | wc -l); [ "$n2" -gt 0 ] && [ "$n1" = "$n2" ] && break; done
sed "s#__OUTDIR__#$OUT#; s/__QUOTE__/$Q/" $D/measure.tmpl > $D/measure.cs
echo "measured: $($D/kw.sh code $D/measure.cs)"
$D/finish.sh $Q
