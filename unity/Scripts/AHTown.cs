// Ashen Hollow: town projects. The people of Ashen Hollow are rebuilding: a market hall, a lighthouse, a forge yard, a
// healing garden and a watchtower. Each needs a pile of materials; the townsfolk and other heroes bring theirs over
// the days (up to 70% of each pile), and the rest needs you. Anyone can give, Adventurer or Artisan. A finished project
// helps everyone who plays from then on (a little more pay at shops, faster skill XP, a little more class XP from
// fights), and the biggest givers have their names cut into its plaque. Giving earns Renown (the Requests board's).
// Opened from the menu (Menu -> Town projects). Nothing here can be bought with Crowns.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHTownProj { public string id; public long start; public bool done; public List<AHKV> mine = new List<AHKV>(); }
[Serializable] public class AHTownState { public List<AHTownProj> projs = new List<AHTownProj>(); public long given; }

public static class AHTown
{
    public class Proj { public string id, name, blurb, perk; public string[] mats; public int[] need; public string[] skills; public float k; }
    public static readonly Proj[] Projects =
    {
        new Proj { id = "market", name = "The Market Hall", blurb = "A roofed hall where traders can sell in any weather.", perk = "Shops pay 5% more for what you sell",
            mats = new[] { "logs", "iron_bar", "linen" }, need = new[] { 400, 120, 80 }, k = 0.05f },
        new Proj { id = "lighthouse", name = "The Lighthouse", blurb = "A light on the point so the fishing boats come home safe.", perk = "+10% fishing and cooking XP",
            mats = new[] { "pine_logs", "bronze_bar", "coral_shard" }, need = new[] { 300, 120, 40 }, skills = new[] { "fishing", "cooking" }, k = 0.10f },
        new Proj { id = "forge", name = "The Forge Yard", blurb = "Three great hearths for the town's smiths and jewelers.", perk = "+10% mining, smithing and jewelcrafting XP",
            mats = new[] { "copper_ore", "iron_bar", "gold_ore" }, need = new[] { 500, 150, 60 }, skills = new[] { "mining", "smithing", "jewelcrafting" }, k = 0.10f },
        new Proj { id = "garden", name = "The Healing Garden", blurb = "Beds of healing herbs, beehives and a well for the sick.", perk = "+10% herblore, farming and woodcutting XP",
            mats = new[] { "sunpetal", "honey", "wheat" }, need = new[] { 300, 80, 200 }, skills = new[] { "herblore", "farming", "woodcutting" }, k = 0.10f },
        new Proj { id = "watch", name = "The Watchtower", blurb = "A tower on the hill to watch the roads for raiders.", perk = "+5% class XP from monsters",
            mats = new[] { "logs", "iron_bar", "wolf_pelt" }, need = new[] { 350, 140, 60 }, k = 0.05f },
    };
    const float RealmShare = 0.7f; const double RealmDays = 7.0;
    static readonly string[] Givers = { "Old Tam", "Bram Ironhide", "Yara Oakhand", "Mirelle", "Hollis", "Sera the Swift", "Dunstan", "Magda", "Fenwick", "Liora" };
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

    public static AHTownState S(AHPlayer p) { if (p.prog.town == null) p.prog.town = new AHTownState(); return p.prog.town; }
    public static Proj Def(string id) { return Array.Find(Projects, x => x.id == id); }
    public static AHTownProj St(AHPlayer p, Proj d)
    {
        var s = S(p); var t = s.projs.Find(x => x.id == d.id);
        if (t == null) { t = new AHTownProj { id = d.id, start = NowS }; s.projs.Add(t); }
        return t;
    }
    public static bool Done(AHPlayer p, string id) { if (p == null || p.prog == null) return false; var t = S(p).projs.Find(x => x.id == id); return t != null && t.done; }

    // the townsfolk's share so far: grows over a week to 70% of each pile
    public static int Realm(AHTownProj t, int need) { double days = Math.Max(0, NowS - t.start) / 86400.0; return Mathf.FloorToInt((float)(Math.Min(1.0, days / RealmDays) * RealmShare * need)); }
    public static int Mine(AHTownProj t, string id) { return (int)AHProgress.Get(t.mine, id); }
    public static int Have(AHTownProj t, Proj d, int i) { return Mathf.Min(d.need[i], Realm(t, d.need[i]) + Mine(t, d.mats[i])); }
    public static float Frac(AHPlayer p, Proj d)
    {
        var t = St(p, d); if (t.done) return 1f; float a = 0, b = 0;
        for (int i = 0; i < d.mats.Length; i++) { a += Have(t, d, i); b += d.need[i]; }
        return b > 0 ? a / b : 1f;
    }

    // give up to n of a material (only what you gathered or made yourself)
    public static void Give(AHGame g, Proj d, int i, int n)
    {
        var p = g.player; var t = St(p, d); if (t.done) return;
        string id = d.mats[i]; int left = d.need[i] - Have(t, d, i);
        n = Mathf.Min(n, Mathf.Min(left, p.OwnOf(id)));
        if (n <= 0) { g.ui.Toast(left <= 0 ? "That pile is full." : "You have none you gathered or made yourself."); return; }
        p.bag.Take(id, n); AHProgress.Add(t.mine, id, n);
        long val = AHComp.Value(id) * n; S(p).given += val;
        int ren = Mathf.Max(1, (int)(val / 60));
        AHRequests.State(p).renown += ren;
        g.ui.Toast("You give " + n + " " + AHRequests.ItemName(id).ToLowerInvariant() + " to " + d.name + ". +" + ren + " renown.", 3f);
        Check(g, d);
        p.bag.Touch(); g.MarkDirty();
    }

    static void Check(AHGame g, Proj d)
    {
        var p = g.player; var t = St(p, d); if (t.done) return;
        for (int i = 0; i < d.mats.Length; i++) if (Have(t, d, i) < d.need[i]) return;
        t.done = true; p.Recalc();
        g.ui.Banner(d.name + " is finished!", d.perk + ", for everyone");
        AHSound.Play("level");
        g.MarkDirty();
    }

    // the plaque: the biggest givers (the townsfolk's shares are always the same for a project)
    public static List<KeyValuePair<string, long>> Plaque(AHPlayer p, Proj d)
    {
        var t = St(p, d); var l = new List<KeyValuePair<string, long>>();
        long total = 0; for (int i = 0; i < d.mats.Length; i++) total += AHComp.Value(d.mats[i]) * d.need[i];
        var rnd = new System.Random(d.id.GetHashCode());
        float realmK = (float)Math.Min(1.0, Math.Max(0, NowS - t.start) / 86400.0 / RealmDays);
        foreach (var nm in Givers) l.Add(new KeyValuePair<string, long>(nm, (long)(total * (0.03 + rnd.NextDouble() * 0.09) * realmK)));
        long mine = 0; for (int i = 0; i < d.mats.Length; i++) mine += AHComp.Value(d.mats[i]) * Mine(t, d.mats[i]);
        if (mine > 0) l.Add(new KeyValuePair<string, long>(string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName, mine));
        l.Sort((a, b) => b.Value.CompareTo(a.Value));
        return l;
    }

    // ---------- what the finished projects give ----------
    public static float SellK(AHPlayer p) { return Done(p, "market") ? 1.05f : 1f; }
    public static float XpK(AHPlayer p, string skill)
    {
        float k = 1f;
        foreach (var d in Projects) if (d.skills != null && Array.IndexOf(d.skills, skill) >= 0 && Done(p, d.id)) k += d.k;
        return k;
    }
    public static float ClassXpK(AHPlayer p) { return Done(p, "watch") ? 1.05f : 1f; }
}

public partial class AHUI
{
    string townSel;
    public void OpenTown() { wkMode = "town"; townSel = null; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderTown(AHPlayer p)
    {
        var rows = new List<Action<int>>();
        var d = townSel != null ? AHTown.Def(townSel) : null;
        if (d == null)
        {
            int done = 0; foreach (var q in AHTown.Projects) if (AHTown.Done(p, q.id)) done++;
            wkTitle.text = "Town projects · " + done + " / " + AHTown.Projects.Length + " built";
            wkHint.text = "Ashen Hollow is rebuilding. The townsfolk bring materials over the days; the last part needs heroes like you. A finished project helps everyone.";
            foreach (var q in AHTown.Projects)
            {
                var qq = q; bool fin = AHTown.Done(p, q.id); int pct = Mathf.FloorToInt(AHTown.Frac(p, q) * 100f);
                rows.Add(s => Row(s, qq.name + (fin ? "  <color=#9be37a>built</color>" : "  " + pct + "%"), fin ? new Color(0.6f, 0.9f, 0.5f) : new Color(1f, 0.85f, 0.55f),
                    qq.blurb, (fin ? "Gives: " : "When built: ") + qq.perk,
                    new WkBtn { label = fin ? "Plaque" : "Give", on = true, col = fin ? Plain : Go, act = () => { townSel = qq.id; wkPageI = 0; RenderWork(); } }));
            }
        }
        else
        {
            var t = AHTown.St(p, d); bool fin = t.done;
            wkTitle.text = d.name + (fin ? " · built" : " · " + Mathf.FloorToInt(AHTown.Frac(p, d) * 100f) + "%");
            wkHint.text = fin ? d.perk + ", for everyone. The names on its plaque:" : d.blurb + " When built: " + d.perk + ". Only what you gathered or made yourself can be given.";
            if (!fin)
                for (int i = 0; i < d.mats.Length; i++)
                {
                    int ii = i; string id = d.mats[i]; int have = AHTown.Have(t, d, i), own = p.OwnOf(id), left = d.need[i] - have;
                    rows.Add(s => Row(s, AHRequests.ItemName(id) + "  " + have + " / " + d.need[ii], left <= 0 ? new Color(0.6f, 0.9f, 0.5f) : Color.white,
                        "Townsfolk " + AHTown.Realm(t, d.need[ii]) + " · you " + AHTown.Mine(t, id) + " · in your bag " + own, "",
                        left > 0 ? new WkBtn { label = "Give 10", on = own > 0, col = Plain, act = () => { AHTown.Give(g, d, ii, 10); RenderWork(); } } : null,
                        left > 0 ? new WkBtn { label = "Give all", on = own > 0, col = Go, act = () => { AHTown.Give(g, d, ii, 99999); RenderWork(); } } : null));
                }
            int rank = 0;
            foreach (var kv in AHTown.Plaque(p, d))
            {
                if (++rank > 5) break;
                int rk = rank; var e = kv; bool me = e.Key == (string.IsNullOrEmpty(p.heroName) ? "You" : p.heroName);
                rows.Add(s => Row(s, "#" + rk + "  " + e.Key, me ? new Color(1f, 0.8f, 0.4f) : new Color(1f, 1f, 1f, 0.85f), "Gave " + AHItems.MoneyText(e.Value * AHDB.CU) + " worth", ""));
            }
            rows.Add(s => Row(s, "All projects", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { townSel = null; wkPageI = 0; RenderWork(); } }));
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
