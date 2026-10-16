// Ashen Hollow: dungeon looters. The two paths help each other in the dungeons:
//  - An Adventurer can hire a looter, an Artisan of the guild, for the next dungeon run. While you fight, the looter
//    skins, digs or picks what the fallen leave behind: a share of hides, ore or herbs from each foe, more from a
//    mini-boss and a good bundle from the boss (sometimes a gem). The looter goes home when the boss falls.
//    Three looters, one for each trade: Pell (skinner: hides and pelts), Marra (miner: ore and shards) and Old Fen
//    (herbalist: herbs, dust and relics). The fee grows with the dungeon.
//  - An Artisan (whom the monsters leave alone) can sign up as the looter of an adventuring party: choose a dungeon you
//    are ready for, and come back in a while with the party's leftovers, a cut of their coin, and sometimes a gem.
//    One contract at a time. Time runs while the game is closed.
// Opened from Companions (Menu -> Companions -> Dungeon looters).
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHLootJob { public string dung; public long start, end; public bool told; }
[Serializable] public class AHLooterState { public string hired = ""; public int runs; public List<AHLootJob> jobs = new List<AHLootJob>(); public int contracts; }

public static class AHLooters
{
    public class Looter { public string id, name, trade, blurb; }
    public static readonly Looter[] Looters =
    {
        new Looter { id = "pell",  name = "Pell Thistledown", trade = "skin", blurb = "a skinner: hides, pelts and scales" },
        new Looter { id = "marra", name = "Marra Coppervein", trade = "mine", blurb = "a miner: ore, shards and cores" },
        new Looter { id = "fen",   name = "Old Fen",          trade = "herb", blurb = "a herbalist: herbs, dust and relics" },
    };
    public static Looter Get(string id) { return Array.Find(Looters, l => l.id == id); }

    // what each dungeon leaves behind, by trade
    public class Haul { public string dung, name; public int lvl; public float tier; public string[] skin, mine, herb; }
    public static readonly Haul[] Hauls =
    {
        new Haul { dung = "d_warrens", name = "The Mire Warrens",      lvl = 14, tier = 1f,   skin = new[] { "boar_hide", "wolf_pelt", "croc_hide" },          mine = new[] { "copper_ore", "iron_ore", "tin_ore" },         herb = new[] { "mireroot", "sunpetal", "bone_dust" } },
        new Haul { dung = "d_frost",   name = "Frostpeak Caverns",     lvl = 22, tier = 1.45f, skin = new[] { "frost_pelt", "wolf_fang", "wolf_pelt" },         mine = new[] { "iron_ore", "gold_ore", "iron_ore" },          herb = new[] { "frostbloom", "frostbloom", "bone_dust" } },
        new Haul { dung = "d_tomb",    name = "Tomb of the Sand King", lvl = 30, tier = 1.9f,  skin = new[] { "jackal_pelt", "scorpion_chitin", "serpent_scale" }, mine = new[] { "gold_ore", "iron_ore", "coral_shard" },       herb = new[] { "dragonfern", "bone_dust", "ancient_relic" } },
        new Haul { dung = "d_molten",  name = "The Molten Depths",     lvl = 48, tier = 2.8f,  skin = new[] { "ember_hide", "salamander_scale", "ember_hide" },  mine = new[] { "obsidian_shard", "magma_core", "gold_ore" },  herb = new[] { "emberthorn", "ashbloom", "emberthorn" } },
        new Haul { dung = "d_hollow",  name = "The Hollow Below",      lvl = 57, tier = 3.3f,  skin = new[] { "grave_pelt", "troll_hide", "grave_pelt" },        mine = new[] { "void_shard", "obsidian_shard", "void_dust" }, herb = new[] { "ashbloom", "void_dust", "ancient_relic" } },
    };
    public static Haul HaulOf(string dung) { return Array.Find(Hauls, h => h.dung == dung); }
    static string[] Of(Haul h, string trade) { return trade == "skin" ? h.skin : trade == "mine" ? h.mine : h.herb; }
    static readonly string[] Gems = { "sapphire", "ruby", "emerald" };

    public static AHLooterState S(AHPlayer p) { if (p.prog.looters == null) p.prog.looters = new AHLooterState(); return p.prog.looters; }
    static long NowS { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

    // ---------- Adventurers: a looter for the next run ----------
    public static long Fee(AHPlayer p) { int L = Mathf.Max(1, p.level); return (long)Mathf.Round(60f + L * 12f) * AHDB.CU; }
    public static Looter Hired(AHPlayer p) { return p == null || p.prog == null ? null : Get(S(p).hired); }

    public static bool Hire(AHGame g, string id)
    {
        var p = g.player; var l = Get(id); if (l == null) return false;
        if (p.path == "artisan") { g.ui.Toast("Artisans don't fight in the dungeons. Sign up as a looter instead."); return false; }
        if (Hired(p) != null) { g.ui.Toast(Hired(p).name + " is already coming with you."); return false; }
        long fee = Fee(p);
        if (p.bag.money < fee) { g.ui.Toast("A looter asks " + AHItems.MoneyText(fee) + "."); return false; }
        p.bag.money -= fee; p.bag.Touch();
        S(p).hired = id;
        g.ui.Toast(l.name + " will come with you on your next dungeon run.", 3.5f);
        g.MarkDirty();
        return true;
    }
    public static void Dismiss(AHGame g) { var p = g.player; var l = Hired(p); if (l == null) return; S(p).hired = ""; g.ui.Toast(l.name + " goes home. (The fee is not given back.)"); g.MarkDirty(); }

    static void Give(AHGame g, AHPlayer p, string id, int n, Dictionary<string, int> got)
    {
        if (n <= 0 || AHItems.Get(id) == null) return;
        if (!p.bag.Add(id, n) && AHAuction.S != null) AHAuction.MailAdd(id, n);
        int c; got.TryGetValue(id, out c); got[id] = c + n;
    }
    static string Said(Dictionary<string, int> got) { var l = new List<string>(); foreach (var kv in got) l.Add(kv.Value + "× " + AHItems.Get(kv.Key).name); return string.Join(", ", l.ToArray()); }

    // from AHDungeon, for each foe that falls in a dungeon (kind: 0 a foe, 1 a mini-boss or elite, 2 the boss)
    public static void OnKill(AHGame g, string dung, int kind)
    {
        var p = g.player; var l = Hired(p); var h = HaulOf(dung); if (l == null || h == null) return;
        var pool = Of(h, l.trade); var got = new Dictionary<string, int>();
        if (kind == 0) { if (UnityEngine.Random.value < 0.3f) Give(g, p, pool[UnityEngine.Random.Range(0, pool.Length)], 1, got); }
        else if (kind == 1) Give(g, p, pool[UnityEngine.Random.Range(0, pool.Length)], 1 + UnityEngine.Random.Range(0, 2), got);
        else
        {
            for (int i = 0; i < 3; i++) Give(g, p, pool[i % pool.Length], 1 + Mathf.FloorToInt(h.tier * 0.6f), got);
            if (UnityEngine.Random.value < 0.3f) Give(g, p, Gems[UnityEngine.Random.Range(0, Gems.Length)], 1, got);
        }
        if (got.Count == 0) return;
        if (kind == 2)
        {
            g.ui.Banner(l.name + "'s haul", Said(got));
            S(p).hired = ""; S(p).runs++;
            g.ui.Toast(l.name + " heads home. Hire a looter again any time (Companions).", 4f);
        }
        else g.ui.Float(p.transform.position + Vector3.up * 2.6f, l.name.Split(' ')[0] + ": +" + Said(got), new Color(0.79f, 0.9f, 0.66f));
        g.MarkDirty();
    }

    // ---------- Artisans: looting contracts ----------
    public static int Mins(Haul h) { return Mathf.RoundToInt(30f * h.tier); }
    public static bool Ready(AHPlayer p, Haul h) { return p.level >= h.lvl - 10 || p.BestMastery() >= Mathf.RoundToInt(h.tier * 8f); }
    public static string Need(Haul h) { return "Level " + (h.lvl - 10) + ", or mastery " + Mathf.RoundToInt(h.tier * 8f) + " in a profession"; }

    public static bool SignUp(AHGame g, string dung)
    {
        var p = g.player; var h = HaulOf(dung); var s = S(p); if (h == null) return false;
        if (p.path != "artisan") { g.ui.Toast("Only Artisans sign up as looters."); return false; }
        if (s.jobs.Count > 0) { g.ui.Toast("You are already looting for a party."); return false; }
        if (!Ready(p, h)) { g.ui.Toast(h.name + " needs " + Need(h) + "."); return false; }
        s.jobs.Add(new AHLootJob { dung = dung, start = NowS, end = NowS + Mins(h) * 60L });
        g.ui.Toast("You join a party bound for " + h.name + ". Back in " + Mins(h) + " minutes.", 3.5f);
        g.MarkDirty();
        return true;
    }
    public static bool Back(AHLootJob j) { return NowS >= j.end; }

    public static void Tick(AHGame g)
    {
        var p = g.player; if (p == null || p.prog == null) return;
        foreach (var j in S(p).jobs)
            if (!j.told && Back(j)) { j.told = true; var h = HaulOf(j.dung); g.ui.Toast("The party is back from " + (h != null ? h.name : "the dungeon") + ". Collect your share in Companions → Dungeon looters.", 4f); g.MarkDirty(); }
    }

    public static void Collect(AHGame g, AHLootJob j)
    {
        var p = g.player; var s = S(p); if (!s.jobs.Contains(j) || !Back(j)) return;
        var h = HaulOf(j.dung); s.jobs.Remove(j); s.contracts++;
        if (h == null) return;
        var got = new Dictionary<string, int>();
        int n = Mathf.RoundToInt(5f * h.tier);
        string[][] all = { h.skin, h.mine, h.herb };
        for (int i = 0; i < n; i++) { var pool = all[UnityEngine.Random.Range(0, 3)]; Give(g, p, pool[UnityEngine.Random.Range(0, pool.Length)], 1, got); }
        if (UnityEngine.Random.value < 0.25f + 0.05f * h.tier) Give(g, p, Gems[UnityEngine.Random.Range(0, Gems.Length)], 1, got);
        long coin = (long)Mathf.Round(90f * h.tier) * AHDB.CU;
        p.AddMoney(coin, p.transform.position);
        g.ui.Banner("Your share from " + h.name, Said(got) + " and " + AHItems.MoneyText(coin));
        p.bag.Touch(); g.MarkDirty();
    }
}

public partial class AHUI
{
    public void OpenLooters() { wkMode = "looters"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderLooters(AHPlayer p)
    {
        AHLooters.Tick(g);
        var s = AHLooters.S(p); var rows = new List<Action<int>>();
        if (p.path == "artisan")
        {
            wkTitle.text = "Looting contracts";
            wkHint.text = "Adventuring parties pay a looter: an Artisan who skins, digs and picks while they fight. Join one; you come back with their leftovers, a cut of their coin, and sometimes a gem. " + s.contracts + " done so far.";
            foreach (var j in s.jobs)
            {
                var jj = j; var h = AHLooters.HaulOf(j.dung); bool back = AHLooters.Back(j);
                rows.Add(sl => Row(sl, "Looting in " + (h != null ? h.name : jj.dung), new Color(1f, 0.62f, 0.24f),
                    back ? "<color=#9be37a>The party is back</color>" : "Back in " + AHExpeditions.LeftText(Math.Max(0, jj.end - DateTimeOffset.UtcNow.ToUnixTimeSeconds())), "",
                    new WkBtn { label = "Collect", on = back, col = Go, act = () => { AHLooters.Collect(g, jj); RenderWork(); } }));
            }
            foreach (var h in AHLooters.Hauls)
            {
                var hh = h; bool ok = AHLooters.Ready(p, h);
                rows.Add(sl => Row(sl, hh.name + " · " + AHLooters.Mins(hh) + " min", ok ? new Color(1f, 0.85f, 0.55f) : new Color(1f, 1f, 1f, 0.5f),
                    ok ? "Pays about " + AHItems.MoneyText((long)Mathf.Round(90f * hh.tier) * AHDB.CU) + " and " + Mathf.RoundToInt(5f * hh.tier) + " materials" : "Needs " + AHLooters.Need(hh), "",
                    new WkBtn { label = "Sign up", on = ok && s.jobs.Count == 0, col = Go, act = () => { AHLooters.SignUp(g, hh.dung); RenderWork(); } }));
            }
        }
        else
        {
            var cur = AHLooters.Hired(p);
            wkTitle.text = "Dungeon looters";
            wkHint.text = "Hire an Artisan of the guild for your next dungeon run. While you fight, the looter gathers what the fallen leave behind, and brings a bundle from the boss. Fee: " + AHItems.MoneyText(AHLooters.Fee(p)) + " a run.";
            if (cur != null) rows.Add(sl => Row(sl, cur.name + " is coming with you", new Color(0.62f, 0.9f, 0.55f), "On your next dungeon run, until the boss falls", "",
                new WkBtn { label = "Send home", on = true, col = new Color(0.45f, 0.22f, 0.17f, 1f), act = () => { AHLooters.Dismiss(g); RenderWork(); } }));
            foreach (var l in AHLooters.Looters)
            {
                var ll = l;
                rows.Add(sl => Row(sl, ll.name, new Color(1f, 0.85f, 0.55f), "An Artisan of the guild, " + ll.blurb, "",
                    new WkBtn { label = "Hire", on = cur == null && p.bag.money >= AHLooters.Fee(p), col = Go, act = () => { AHLooters.Hire(g, ll.id); RenderWork(); } }));
            }
            if (s.runs > 0) rows.Add(sl => Row(sl, "Runs with a looter: " + s.runs, new Color(1f, 1f, 1f, 0.7f), "", ""));
        }
        rows.Add(sl => Row(sl, "Back to companions", new Color(1f, 1f, 1f, 0.8f), "", "", new WkBtn { label = "Back", on = true, col = Plain, act = () => { wkMode = "comp"; wkPageI = 0; RenderWork(); } }));
        int from0 = Paged(rows.Count);
        for (int i = from0; i < Mathf.Min(rows.Count, from0 + RowsPerPage); i++) rows[i](i - from0);
    }
}
