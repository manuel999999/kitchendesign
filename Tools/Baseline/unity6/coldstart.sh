#!/bin/sh
# Cold start: plays from the given scene, records every active scene on the way, waits until the
# menu has been stable for three polls, then prints the sign-in state, the visible menu screens and
# the console counts. Read-only. Usage: coldstart.sh <scene path>   (editor open, not playing)
D=$(dirname "$0"); P=/Users/emmanuel/Documents/Unity/kitchendesign; cd $P; T=$(mktemp -t coldstart).cs
ev() { unity command eval_file --file "$1" --json --no-banner --timeout 120 2>&1 | python3 -I -c "import json,sys; d=json.load(sys.stdin); r=(d.get('data') or {}).get('result') or {}; print(r.get('result') if d.get('success') and r.get('success') else json.dumps(d)[:1500])"; }
errs() { unity command console_status --json --no-banner 2>&1 | python3 -I -c "import json,sys; r=(json.load(sys.stdin).get('data') or {}).get('result') or {}; g=r.get('groundTruth') or {}; print('errors=%s warnings=%s' % (g.get('consoleErrors'), g.get('consoleWarnings')))"; }
unity command open_scene --path "$1" --json --no-banner >/dev/null 2>&1
unity command clear_console --json --no-banner >/dev/null 2>&1
unity command editor_play --json --no-banner >/dev/null 2>&1
ev $D/rib.cs >/dev/null
seen=""; prev=""; same=0
for i in $(seq 1 120); do
    perl -e 'select(undef,undef,undef,1)'
    r=$(ev $D/state.cs)
    sc=$(echo "$r" | sed -n 's/^scene=\([^ ]*\).*/\1/p')
    case " $seen " in *" $sc "*) ;; *) seen="$seen $sc";; esac
    if [ "$r" = "$prev" ] && [ "$sc" = "MenuScene" ] && ! echo "$r" | grep -q "login=Running"; then same=$((same+1)); else same=0; fi
    prev="$r"; [ $same -ge 3 ] && break
done
echo "scenes:$seen"
echo "state: $r"
sed "s/__STEP__/state/g; s/__CLIENT__/0/g; s/__QUOTE__/0/g" $D/menu.tmpl > $T; echo "menu: $(ev $T)"; rm -f $T
echo "console: $(errs)"
unity command editor_stop --json --no-banner >/dev/null 2>&1
