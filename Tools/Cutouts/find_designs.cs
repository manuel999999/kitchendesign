var lines = new System.Collections.Generic.List<string>();
int n = 0;
foreach (var c in Melcher.ProFitGlobals.userClients)
{
    if (c.quotes == null) continue;
    foreach (var q in c.quotes)
    {
        n++;
        string s = q.saveData == null ? "" : MiniJSON.Json.Serialize(q.saveData);
        int grill = System.Text.RegularExpressions.Regex.Matches(s, "(?i)grill").Count;
        int burner = System.Text.RegularExpressions.Regex.Matches(s, "(?i)burner").Count;
        int shelf = System.Text.RegularExpressions.Regex.Matches(s, "(?i)shelf").Count;
        int sink = System.Text.RegularExpressions.Regex.Matches(s, "(?i)sink").Count;
        if (grill + burner + shelf > 0) lines.Add(c.id + ":" + q.id + " grill=" + grill + " burner=" + burner + " shelf=" + shelf + " sink=" + sink + " len=" + s.Length);
    }
}
lines.Sort((a, b) => b.Length.CompareTo(a.Length));
return "designs=" + n + " matching=" + lines.Count + "\n" + string.Join("\n", lines.GetRange(0, System.Math.Min(40, lines.Count)));
