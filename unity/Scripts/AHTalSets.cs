// Ashen Hollow: talent sets. Save the talents you have learned into one of three sets (per class) and switch between
// them in one tap, free, outside a fight: a damage set for the Deep, a tough set for the raid, and so on. Ranks are
// only restored as far as your points and each talent's limit allow. MENU → Class → Talent sets (or the hub row).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHTalSets
{
    public const int N = 3;
    static string Key(AHPlayer p, int i) { return "ah_talset" + i + ":" + p.cls.id; }
    public static string Name(AHPlayer p, int i) { return AHPrefs.GetString(Key(p, i) + ":name", "Set " + (i + 1)); }
    public static bool Has(AHPlayer p, int i) { return AHPrefs.GetString(Key(p, i), "") != ""; }
    public static int Count(AHPlayer p, int i) { int n = 0; foreach (var e in Parse(AHPrefs.GetString(Key(p, i), ""))) n += (int)e.Value; return n; }
    static List<KeyValuePair<string, long>> Parse(string s)
    {
        var l = new List<KeyValuePair<string, long>>();
        foreach (var part in s.Split(',')) { int c = part.LastIndexOf(':'); long v; if (c > 0 && long.TryParse(part.Substring(c + 1), out v) && v > 0) l.Add(new KeyValuePair<string, long>(part.Substring(0, c), v)); }
        return l;
    }
    public static void Save(AHGame g, int i)
    {
        var p = g.player; var parts = new List<string>();
        foreach (var e in AHEvo.CP(p).tal) if (e.v > 0) parts.Add(e.k + ":" + e.v);
        if (parts.Count == 0) { g.ui.Toast("Learn some talents first, then save them here."); return; }
        AHPrefs.SetString(Key(p, i), string.Join(",", parts.ToArray()));
        g.ui.Toast(Name(p, i) + " saved (" + AHEvo.Spent(p) + " points).", 2.5f);
    }
    public static bool InFight(AHGame g) { foreach (var m in g.mobs) if (m != null && !m.dead && m.Chasing && !AHJuice.IsDummy(m) && (m.transform.position - g.player.transform.position).magnitude < 16f) return true; return false; }
    public static void Use(AHGame g, int i)
    {
        var p = g.player; if (!Has(p, i)) return;
        if (InFight(g)) { g.ui.Toast("You can't change talents in the middle of a fight."); return; }
        // which talents exist now, and their limits
        var max = new Dictionary<string, int>();
        foreach (var t in AHEvo.Tiers(p)) if (t.open && t.nodes != null) foreach (var n in t.nodes) max[AHJson.S(n, "id")] = (int)AHJson.N(n, "max", 3);
        var tal = AHEvo.CP(p).tal; tal.Clear(); int left = AHEvo.Points(p);
        foreach (var e in Parse(AHPrefs.GetString(Key(p, i), "")))
        {
            int m; if (!max.TryGetValue(e.Key, out m) || left <= 0) continue;
            int r = Mathf.Min(m, Mathf.Min(left, (int)e.Value)); if (r <= 0) continue;
            AHProgress.Add(tal, e.Key, r); left -= r;
        }
        p.Recalc(); g.MarkDirty(); g.SaveProgress();
        AHFx.Ring(p.transform.position, 0.4f, 2f, new Color(1f, 0.8f, 0.4f, 0.8f), 0.6f);
        g.ui.Toast("Talents: " + Name(p, i) + " (" + AHEvo.Spent(p) + " points).", 2.5f);
    }
}

public partial class AHUI
{
    public void OpenTalSets() { wkMode = "talsets"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderTalSets(AHPlayer p)
    {
        wkTitle.text = "Talent sets";
        wkHint.text = "Save the talents you have now into a set, and switch sets in one tap (free, not during a fight). Points in use: " + AHEvo.Spent(p) + " of " + AHEvo.Points(p) + ".";
        Paged(AHTalSets.N + 1);   // one page: keeps the page counter right
        for (int i = 0; i < AHTalSets.N; i++)
        {
            int ii = i; bool has = AHTalSets.Has(p, i);
            Row(i, AHTalSets.Name(p, i), has ? new Color(1f, 0.8f, 0.45f) : new Color(1f, 1f, 1f, 0.6f), has ? AHTalSets.Count(p, i) + " points saved" : "Empty", "",
                new WkBtn { label = "Save", on = true, col = Plain, act = () => { AHTalSets.Save(g, ii); RenderWork(); } },
                new WkBtn { label = "Use", on = has, col = Go, act = () => { AHTalSets.Use(g, ii); RenderWork(); } });
        }
        Row(AHTalSets.N, "Talents", p.cls.color, "Learn or reset talents on the class page", "", new WkBtn { label = "Open", on = true, col = Plain, act = () => OpenClassWin() });
    }
}
