#!/bin/sh
# usage: kw.sh <method> <params-json>
#        kw.sh code <file.cs> [timeout-ms]   runs a C# snippet via execute_code (skip_refresh)
D=$(dirname "$0")
if [ "$1" = "code" ]; then
  python3 -I -c 'import json,sys; print(json.dumps({"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"execute_code","arguments":{"code":open(sys.argv[1]).read(),"skip_refresh":True,"safety_checks":False}}}))' "$2" > "$D/.req.json"
  curl -s -m 900 -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' -X POST http://127.0.0.1:8765/ --data-binary "@$D/.req.json" \
    | python3 -I -c 'import json,sys
d=json.load(sys.stdin)
if "error" in d: print("ERROR", d["error"]); sys.exit(1)
t=d["result"]["content"][0]["text"]
try:
    j=json.loads(t); print(j.get("data",{}).get("result", j.get("message")) if j.get("success") else json.dumps(j)[:4000])
except Exception: print(t[:4000])'
  exit 0
fi
curl -s -m 600 -H 'Content-Type: application/json' -H 'Accept: application/json, text/event-stream' -X POST http://127.0.0.1:8765/ -d "{\"jsonrpc\":\"2.0\",\"id\":$(date +%s),\"method\":\"$1\",\"params\":$2}"
