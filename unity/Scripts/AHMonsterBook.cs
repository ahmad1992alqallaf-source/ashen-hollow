// Ashen Hollow: the Monster book. Every kind of beast and foe has a page. Defeat one and its page fills in; defeat 10
// and it is studied (its strength, its habits and what it drops are written down); defeat 50 and it is mastered (a gold
// border). The pages are bound in five volumes by level. Finishing a volume (every page found) pays coin and a gem;
// pages filled in all add up to looks (25, 50 and 80 pages: a hit effect, a trail and a name colour). Looks only.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHMonsterBook
{
    public const int Studied = 10, Mastered = 50;
    public static readonly string[] Volumes = { "I · The Wilds (levels 1–15)", "II · Far Lands (16–30)", "III · Hard Country (31–45)", "IV · The Burning South (46–60)", "V · The Deep and Beyond (61+)" };
    public static int VolOf(int lvl) { return lvl <= 15 ? 0 : lvl <= 30 ? 1 : lvl <= 45 ? 2 : lvl <= 60 ? 3 : 4; }

    public class Page { public string id, name; public int lvl, vol; public bool boss, elite, rare, flee, slam, calm; public float hp, dmg, speed; public List<string> drops = new List<string>(); }
    static List<Page> pages; static Dictionary<string, object> src;
    public static List<Page> All
    {
        get
        {
            var m = AHDB.Mobs;
            if (pages != null && ReferenceEquals(m, src)) return pages;
            src = m; pages = new List<Page>();
            if (m != null) foreach (var kv in m)
            {
                var d = kv.Value as Dictionary<string, object>; if (d == null) continue;
                string name = AHJson.S(d, "name"); if (string.IsNullOrEmpty(name) || AHJson.B(d, "add") || AHJson.B(d, "hidden")) continue;
                var pg = new Page { id = kv.Key, name = name, lvl = (int)AHJson.N(d, "lvl", 1), boss = AHJson.B(d, "boss"), elite = AHJson.B(d, "elite"), rare = AHJson.B(d, "rare"),
                    flee = AHJson.B(d, "flee"), slam = AHJson.B(d, "slam"), calm = AHJson.N(d, "aggro") <= 0, hp = (float)AHJson.N(d, "hp"), dmg = (float)AHJson.N(d, "dmg"), speed = (float)AHJson.N(d, "speed") };
                pg.vol = VolOf(pg.lvl);
                var dr = AHJson.A(d, "drops"); if (dr != null) foreach (var o in dr) { var r = o as List<object>; if (r != null && r.Count > 0) { var it = AHItems.Get(r[0] as string); if (it != null) pg.drops.Add(it.name); } }
                pages.Add(pg);
            }
            pages.Sort((a, b) => a.lvl != b.lvl ? a.lvl.CompareTo(b.lvl) : string.CompareOrdinal(a.name, b.name));
            return pages;
        }
    }

    public static long Kills(AHPlayer p, string id) { return p == null || p.prog == null ? 0 : AHProgress.Get(p.prog.book, id); }
    public static int Pages(AHPlayer p) { if (p == null || p.prog == null) return 0; int n = 0; foreach (var pg in All) if (Kills(p, pg.id) > 0) n++; return n; }
    public static int Count(AHPlayer p, int vol, out int found, out int studied)
    {
        int n = 0; found = 0; studied = 0;
        foreach (var pg in All) { if (pg.vol != vol) continue; n++; long k = Kills(p, pg.id); if (k > 0) found++; if (k >= Studied) studied++; }
        return n;
    }

    // everything the quest log hears (AHQuests.Event): a foe of this kind fell
    public static void OnKill(AHGame g, string id)
    {
        var p = g.player; if (p == null || p.prog == null || id == null) return;
        var pg = All.Find(x => x.id == id); if (pg == null) return;
        long before = Kills(p, id); AHProgress.Add(p.prog.book, id, 1); long k = before + 1;
        if (before == 0)
        {
            g.ui.Toast("New page in the Monster book: " + pg.name + " (" + Pages(p) + " / " + All.Count + ")", 3f);
            int found, studied, n = Count(p, pg.vol, out found, out studied);
            if (found == n)
            {
                long coin = (long)(500L * (pg.vol + 1) * (pg.vol + 1)) * AHDB.CU; p.AddMoney(coin, p.transform.position);
                string[] gems = { "sapphire", "ruby", "emerald" }; p.bag.Add(gems[UnityEngine.Random.Range(0, 3)]);
                g.ui.Banner("Volume " + Volumes[pg.vol].Split('·')[0].Trim() + " complete!", "Every page found · " + AHItems.MoneyText(coin) + " and a gem");
            }
            foreach (var d in AHLooks.All) if (d.how == "book" && Pages(p) == d.n) g.ui.Banner("New look: " + d.name, "For " + d.n + " pages of the Monster book · wear it in Menu → Looks");
        }
        else if (k == Studied) g.ui.Toast("Monster book: " + pg.name + " is studied. Its habits and drops are written down.", 3f);
        else if (k == Mastered) g.ui.Toast("Monster book: " + pg.name + " is mastered (gold border).", 3f);
        g.MarkDirty();
    }

    // a page's story, written from what is known of the beast
    public static string Story(Page pg)
    {
        string what = pg.boss ? "A great foe, feared across the land." : pg.elite ? "A beast of rare strength that few hunters have seen twice." : pg.rare ? "Seldom seen; tales of it outnumber the people who have met it." : "";
        string ways = pg.flee ? "Shy: it runs before it fights." : pg.calm ? "It keeps to itself unless it is struck first." : "It attacks anything that comes near.";
        string build = pg.speed >= 150 ? "Quick on its feet." : pg.speed <= 70 ? "Slow, but it does not tire." : "";
        string blow = pg.slam ? "Watch the ground: it marks where it will strike, then slams." : pg.dmg >= 40 ? "Its blows hit very hard." : "";
        return (what + " " + ways + " " + build + " " + blow).Trim().Replace("  ", " ");
    }
}

public partial class AHUI
{
    int bookVol;
    public void OpenMonsterBook() { wkMode = "book"; wkPageI = 0; bookVol = -1; ShowWork(true); RenderWork(); }

    void RenderMonsterBook(AHPlayer p)
    {
        var rows = new List<Action<int>>(); var all = AHMonsterBook.All;
        if (bookVol < 0)
        {
            wkTitle.text = "Monster book · " + AHMonsterBook.Pages(p) + " / " + all.Count + " pages";
            wkHint.text = "Defeat a beast to fill in its page; defeat " + AHMonsterBook.Studied + " to study it, " + AHMonsterBook.Mastered + " to master it. Finish a volume for coin and a gem; pages add up to looks.";
            for (int v = 0; v < AHMonsterBook.Volumes.Length; v++)
            {
                int vv = v, found, studied, n = AHMonsterBook.Count(p, v, out found, out studied); if (n == 0) continue;
                rows.Add(sl => Row(sl, "Volume " + AHMonsterBook.Volumes[vv], found == n ? new Color(1f, 0.85f, 0.4f) : Color.white, found + " / " + n + " pages found · " + studied + " studied", found == n ? "<color=#9be37a>Complete</color>" : "",
                    new WkBtn { label = "Read", on = true, col = Plain, act = () => { bookVol = vv; wkPageI = 0; RenderWork(); } }));
            }
            foreach (var d in AHLooks.All)
            {
                if (d.how != "book") continue; var dd = d; bool own = AHMonsterBook.Pages(p) >= d.n;
                rows.Add(sl => Row(sl, dd.name + " · " + (dd.kind == "hit" ? "hit effect" : dd.kind == "trail" ? "trail" : dd.kind == "skin" ? "weapon skin" : "name colour"), own ? new Color(0.62f, 0.9f, 0.55f) : dd.col,
                    own ? "Yours · wear it in Menu → Looks" : dd.n + " pages (" + AHMonsterBook.Pages(p) + " so far)", dd.desc));
            }
        }
        else
        {
            wkTitle.text = "Monster book · Volume " + AHMonsterBook.Volumes[bookVol];
            wkHint.text = "Pages of this volume. A page you haven't found yet shows only the beast's level.";
            rows.Add(sl => Row(sl, "Back to the volumes", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { bookVol = -1; wkPageI = 0; RenderWork(); } }));
            foreach (var pg in all)
            {
                if (pg.vol != bookVol) continue;
                var pp = pg; long k = AHMonsterBook.Kills(p, pg.id);
                string tag = pp.boss ? " · boss" : pp.elite ? " · elite" : pp.rare ? " · rare" : "";
                if (k <= 0) rows.Add(sl => Row(sl, "??? · level " + pp.lvl + tag, new Color(1f, 1f, 1f, 0.45f), "Not found yet", ""));
                else rows.Add(sl => Row(sl, pp.name + " · level " + pp.lvl + tag, k >= AHMonsterBook.Mastered ? new Color(1f, 0.82f, 0.3f) : k >= AHMonsterBook.Studied ? new Color(0.62f, 0.9f, 0.55f) : Color.white,
                    AHMonsterBook.Story(pp),
                    k >= AHMonsterBook.Studied ? Mathf.RoundToInt(pp.hp) + " health · hits for about " + Mathf.RoundToInt(pp.dmg) + (pp.drops.Count > 0 ? " · drops " + string.Join(", ", pp.drops.ToArray()) : "") + " · " + k + " defeated"
                                               : k + " defeated · " + (AHMonsterBook.Studied - k) + " more to study it"));
            }
        }
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
