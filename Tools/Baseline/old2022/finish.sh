#!/bin/sh
# usage: finish.sh <quote_id>  -- moves photo_*.jpg out of the old project and writes server.json
Q=$1; OUT=/Users/emmanuel/Documents/Unity/GeminiBaseline/designs/$Q
mkdir -p "$OUT/photos"
for f in /Users/emmanuel/Documents/Unity/Gemini/Unity/photo_*.jpg; do [ -e "$f" ] && mv "$f" "$OUT/photos/"; done
python3 -I -c '
import json,sys
q=int(sys.argv[1]); out=sys.argv[2]
for x in json.load(open("/Users/emmanuel/Documents/Unity/GeminiBaseline/_inventory/all-designs.json")):
    if x["quote_id"]==q: json.dump(x, open(out+"/server.json","w"), indent=1)
' "$Q" "$OUT"
ls "$OUT" "$OUT/photos" | tr '\n' ' '; echo
