// Ashen Hollow: the Collection book. Every fish of every land, and every grade of every ore, log, hide and herb has a
// place in it. Catch or gather one and it is ticked off; fill a page (a land's five fish, or one material in all
// five grades) and take its reward: money and XP in that craft. The book is in the Menu.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHCollection
{
    public class Page { public string id, title, skill; public List<string> items = new List<string>(); public Color col; }
    static List<Page> pages;

    public static List<Page> Pages
    {
        get
        {
            if (pages != null) return pages;
            pages = new List<Page>();
            // the waters: one page per land with its own five fish (lands sharing a list share a page)
            var seen = new HashSet<string>();
            foreach (var land in new[] { "meadow", "mill", "fenwick", "silkwood", "kingsroad", "whitepine", "sands", "tide", "ember", "fossil" })
            {
                var fish = AHFish.Of(land); string key = "";
                foreach (var f in fish) key += f.id + ",";
                if (!seen.Add(key)) continue;
                var pg = new Page { id = "fish_" + land, title = "Waters of " + AHAuto.LandName(land), skill = "fishing", col = new Color(0.55f, 0.85f, 1f) };
                foreach (var f in fish) pg.items.Add(f.id);
                pages.Add(pg);
            }
            // the materials: each base in its five grades
            AHItems.Get("copper_ore");   // makes sure the grades are in the item table
            var bases = new List<string>();
            foreach (var kv in AHDB.Items) if (AHGrades.Of(kv.Key) == 0 && AHGrades.Gradable(kv.Key)) bases.Add(kv.Key);
            string[] order = { "ore", "bar", "log", "hide", "herb" };
            foreach (var kind in order)
                foreach (var b in bases)
                {
                    if (AHGrades.Kind(b) != kind) continue;
                    var d = AHItems.Get(b); if (d == null) continue;
                    var pg = new Page { id = "mat_" + b, title = d.name, skill = kind == "ore" ? "mining" : kind == "bar" ? "smithing" : kind == "log" ? "woodcutting" : kind == "hide" ? "skinning" : "herblore", col = d.color };
                    for (int k = 0; k <= 4; k++) pg.items.Add(AHGrades.Id(b, k));
                    pages.Add(pg);
                }
            return pages;
        }
    }

    // something caught or gathered (from fishing, gathering, skinning, smelting)
    public static void Add(AHGame g, AHPlayer p, string id)
    {
        if (p == null || id == null) return;
        bool isNew = AHProgress.Get(p.prog.caught, id) == 0;
        AHProgress.Add(p.prog.caught, id, 1);
        if (!isNew) return;
        foreach (var pg in Pages)
        {
            if (!pg.items.Contains(id)) continue;
            int have = Have(p, pg);
            if (g != null && g.ui != null)
            {
                if (have == pg.items.Count) g.ui.Banner("Collection page complete: " + pg.title, "Claim the reward in Menu → Collection book");
                else g.ui.Toast("Collection book: " + AHItems.Get(id).name + " (" + pg.title + " " + have + " / " + pg.items.Count + ")", 2.5f);
            }
        }
    }
    public static int Have(AHPlayer p, Page pg) { int n = 0; foreach (var id in pg.items) if (AHProgress.Get(p.prog.caught, id) > 0) n++; return n; }
    public static bool Done(AHPlayer p, Page pg) { return Have(p, pg) == pg.items.Count; }
    public static bool Claimed(AHPlayer p, Page pg) { return p.prog.bookPages.Contains(pg.id); }
    public static int Filled(AHPlayer p) { int n = 0; foreach (var pg in Pages) if (Done(p, pg)) n++; return n; }
    public static int Ready(AHPlayer p) { int n = 0; foreach (var pg in Pages) if (Done(p, pg) && !Claimed(p, pg)) n++; return n; }

    public static long Reward(Page pg)
    {
        double v = 0; foreach (var id in pg.items) v += AHComp.Value(id);
        return (long)Math.Round(Math.Max(20, v * 3) * AHDB.CU);
    }
    public static void Claim(AHGame g, AHPlayer p, Page pg)
    {
        if (!Done(p, pg) || Claimed(p, pg)) return;
        p.prog.bookPages.Add(pg.id);
        long m = Reward(pg); p.AddMoney(m, p.transform.position);
        p.GainXp(pg.skill, 250 + 40 * Filled(p), false);
        g.ui.Banner(pg.title, "Page reward: " + AHItems.MoneyText(m));
        AHSound.Play("level"); g.MarkDirty();
    }
}

public partial class AHUI
{
    public void OpenCatchBook() { wkMode = "catch"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderCatchBook(AHPlayer p)
    {
        var all = AHCollection.Pages;
        wkTitle.text = "Collection book · " + AHCollection.Filled(p) + " / " + all.Count + " pages";
        wkHint.text = "Every fish of every land, and every grade of every ore, bar, log, hide and herb. Fill a page for its reward.";
        int from = Paged(all.Count);
        for (int i = from; i < Mathf.Min(all.Count, from + RowsPerPage); i++)
        {
            var pg = all[i]; int have = AHCollection.Have(p, pg);
            var parts = new List<string>();
            foreach (var id in pg.items)
            {
                var d = AHItems.Get(id); if (d == null) continue;
                bool got = AHProgress.Get(p.prog.caught, id) > 0;
                parts.Add(got ? "<color=#" + ColorUtility.ToHtmlStringRGB(AHItems.Quality(d)) + ">" + ShortName(d.name, pg) + "</color>" : "<color=#6a6258>?</color>");
            }
            bool done = have == pg.items.Count, claimed = AHCollection.Claimed(p, pg);
            var pp = pg;
            rowItem = AHItems.Get(pg.items[pg.items.Count - 1]);
            Row(i - from, pg.title + "  " + have + " / " + pg.items.Count + (claimed ? "  <color=#9be37a>✓</color>" : ""), pg.col,
                string.Join(" · ", parts.ToArray()), done ? (claimed ? "Reward taken" : "Reward: " + AHItems.MoneyText(AHCollection.Reward(pg)) + " and " + AHDB.SkillName(pg.skill) + " XP") : "",
                done && !claimed ? new WkBtn { label = "Claim", on = true, col = Go, act = () => { AHCollection.Claim(g, p, pp); RenderWork(); } } : null);
        }
    }
    // in a material's page the grades are told by their first word (Rich, Pure...), the plain one as "Common"
    static string ShortName(string name, AHCollection.Page pg)
    {
        if (!pg.id.StartsWith("mat_")) return name;
        return name == pg.title ? "Common" : name.Split(' ')[0];
    }
}
