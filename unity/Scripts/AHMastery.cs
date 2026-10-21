// Ashen Hollow: gathering mastery, in place of gathering levels. Mining, Woodcutting, Herblore, Fishing and Skinning
// have no levels and no locks any more: anyone can mine any rock, fell any tree, pick any herb, fish any water and
// skin any beast with a plain tool. What you have gathered is counted instead, tier by tier (copper and tin count
// together, then iron, then gold and coral, then obsidian; the herbs, trees, waters and beasts likewise):
//  - at 100, 250, 500 and 1,000 gathered on a tier, a quest opens with the Guild Clerk in Ashen Hollow; take it there
//    and your tool on the belt is marked for that tier (I, II, III, IV). Your one tool carries every mark it earns, so
//    nothing needs merging and nothing takes bag space. The first quest also hands you a Master's tool if your belt
//    has none of that kind;
//  - a tool's mark on a tier raises the chance of better grades from it (Uncommon to Legendary) and brings rare extra
//    finds: gems from rocks, amber from trees, starpetals from herbs, pearls from the water, perfect pelts from beasts;
//  - the fourth mark of any tier gives a title ("the Master Miner"...).
// Artisans count a quarter faster. Heroes who had gathering levels start with the counts their level was worth.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHMastery
{
    public static readonly string[] Profs = { "mining", "woodcutting", "herblore", "fishing", "skinning" };
    public static readonly string[] ProfName = { "Mining", "Woodcutting", "Herblore", "Fishing", "Skinning" };
    public static readonly string[] ToolKind = { "pick", "axe", "sickle", "rod", "knife" };
    public static readonly string[] ToolName = { "pickaxe", "axe", "sickle", "fishing rod", "skinning knife" };
    public static readonly string[] Extra = { null, "amber", "starpetal", "pearl", "perfect_pelt" };   // mining: a gem
    public static readonly int[] Steps = { 100, 250, 500, 1000 };
    public static readonly string[] Roman = { "", "I", "II", "III", "IV" };

    public static bool IsGather(string skill) { return Array.IndexOf(Profs, skill) >= 0; }
    static int PI(string prof) { return Array.IndexOf(Profs, prof); }

    // ---------- the tiers ----------
    public static int Tier(string prof, int req)
    {
        if (prof == "fishing") return req < 8 ? 1 : req < 16 ? 2 : req < 28 ? 3 : req < 36 ? 4 : 5;
        return req < 10 ? 1 : req < 15 ? 2 : req < 18 ? 3 : 4;
    }
    public static int Tiers(string prof) { return prof == "fishing" ? 5 : prof == "woodcutting" ? 2 : 4; }
    public static int FishTier(string land) { return Tier("fishing", AHFish.Of(land)[0].lvl); }
    public static string TierName(string prof, int t)
    {
        var names = new List<string>();
        Func<string, object> rule = k => AHJson.O(AHDB.Rules, k);
        switch (prof)
        {
            case "mining": { var r = rule("ROCKS") as Dictionary<string, object>; if (r != null) foreach (var kv in r) if (Tier(prof, (int)AHJson.N(kv.Value, "req", 1)) == t) names.Add(Cap(kv.Key == "coralrock" ? "coral" : kv.Key)); break; }
            case "woodcutting": { var r = rule("TREES") as Dictionary<string, object>; if (r != null) foreach (var kv in r) if (Tier(prof, (int)AHJson.N(kv.Value, "req", 1)) == t) names.Add(Cap(kv.Key == "tree" ? "oak" : kv.Key)); break; }
            case "herblore": { var r = rule("HERB_T") as Dictionary<string, object>; if (r != null) foreach (var kv in r) if (Tier(prof, (int)AHJson.N(kv.Value, "req", 1)) == t) { var d = AHItems.Get(kv.Key); names.Add(d != null ? d.name : Cap(kv.Key)); } break; }
            case "skinning": return t == 1 ? "Small game" : t == 2 ? "Wild beasts" : t == 3 ? "Great beasts" : "Monsters";
            case "fishing": return t == 1 ? "Meadow and mill waters" : t == 2 ? "Marsh and forest waters" : t == 3 ? "Snow and desert waters" : t == 4 ? "The open sea" : "Fire and fossil waters";
        }
        return names.Count > 0 ? string.Join(" & ", names.ToArray()) : "Tier " + t;
    }
    static string Cap(string s) { return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }

    // ---------- counts and marks ----------
    static string CK(string prof, int t) { return "c:" + prof + ":" + t; }
    static string RK(string prof, int t) { return "r:" + prof + ":" + t; }
    public static long Count(AHPlayer p, string prof, int t) { Init(p); return AHProgress.Get(p.prog.mastery, CK(prof, t)); }
    public static int Marks(AHPlayer p, string prof, int t) { Init(p); return (int)AHProgress.Get(p.prog.mastery, RK(prof, t)); }
    public static int Next(AHPlayer p, string prof, int t) { int m = Marks(p, prof, t); return m < Steps.Length ? Steps[m] : -1; }
    public static bool Ready(AHPlayer p, string prof, int t) { int n = Next(p, prof, t); return n > 0 && Count(p, prof, t) >= n; }
    public static int ReadyCount(AHPlayer p) { int n = 0; foreach (var pr in Profs) for (int t = 1; t <= Tiers(pr); t++) if (Ready(p, pr, t)) n++; return n; }
    public static bool HasTool(AHPlayer p, string prof) { int i = PI(prof); return i >= 0 && AHBags.Tool(p, ToolKind[i]) != null; }
    // the mark that works now: the tool must be on the belt
    public static int Rank(AHPlayer p, string prof, int t) { return HasTool(p, prof) ? Marks(p, prof, t) : 0; }

    static void Init(AHPlayer p)
    {
        if (p == null || p.prog == null || p.prog.masteryInit) return;
        p.prog.masteryInit = true;
        // heroes who had levels: each tier their level had passed counts as gathered (the quests wait at the Guild)
        foreach (var prof in Profs)
        {
            int L = p.Skill(prof);
            for (int t = 1; t <= Tiers(prof); t++)
            {
                int start = prof == "fishing" ? new[] { 1, 8, 16, 28, 36 }[t - 1] : new[] { 1, 10, 15, 18 }[Mathf.Min(t - 1, 3)];
                long c = L >= start + 20 ? 500 : L >= start + 10 ? 250 : L >= start + 3 ? 100 : 0;
                if (c > 0) AHProgress.Add(p.prog.mastery, CK(prof, t), c);
            }
        }
    }

    // something gathered (one ore, log, herb, fish, hide)
    public static void Add(AHGame g, AHPlayer p, string prof, int t, int n = 1)
    {
        if (p == null || !IsGather(prof)) return;
        Init(p);
        if (p.path == "artisan" && UnityEngine.Random.value < 0.25f) n++;   // Artisans count a quarter faster
        int before = Next(p, prof, t); bool wasReady = Ready(p, prof, t);
        AHProgress.Add(p.prog.mastery, CK(prof, t), n);
        if (!wasReady && Ready(p, prof, t) && g != null && g.ui != null)
        {
            g.ui.Banner(ProfName[PI(prof)] + " quest: " + TierName(prof, t), before + " gathered · see the Guild Clerk in Ashen Hollow for your " + ToolName[PI(prof)] + "’s mark " + Roman[Marks(p, prof, t) + 1]);
            AHSound.Play("level");
        }
    }

    // the quest handed in at the Guild: the mark, some coin, and a Master's tool if the belt has none
    public static bool Claim(AHGame g, AHPlayer p, string prof, int t)
    {
        if (!Ready(p, prof, t)) return false;
        int i = PI(prof); int m = Marks(p, prof, t) + 1;
        AHProgress.Add(p.prog.mastery, RK(prof, t), 1);
        long coin = (long)Math.Round((20 + 30 * m * m) * (1 + t * 0.5) * AHDB.CU);
        p.AddMoney(coin, p.transform.position);
        if (!HasTool(p, prof))
        {
            string tid = "master_" + ToolKind[i]; Inject();
            AHBags.SetToolPublic(p, ToolKind[i], tid);
        }
        g.ui.Banner("Your " + ToolName[i] + ": " + TierName(prof, t) + " " + Roman[m], m >= 4 ? "Fully mastered! A new title is yours" : "Better grades and rare finds on this tier");
        AHSound.Play("level"); g.MarkDirty();
        return true;
    }
    public static bool Master(AHPlayer p, string prof) { for (int t = 1; t <= Tiers(prof); t++) if (Marks(p, prof, t) >= 4) return true; return false; }

    // the tool's marks, for the bag and the Guild ("Copper & Tin III · Iron I")
    public static string MarkLine(AHPlayer p, string prof)
    {
        var l = new List<string>();
        for (int t = 1; t <= Tiers(prof); t++) { int m = Marks(p, prof, t); if (m > 0) l.Add(TierName(prof, t) + " " + Roman[m]); }
        return l.Count > 0 ? string.Join(" · ", l.ToArray()) : "no marks yet";
    }

    // ---------- what a mark does ----------
    // the grade roll's multiplier on this tier (a rich vein or ancient tree multiplies on top)
    public static float GradeK(AHPlayer p, string prof, int t) { return 1f + 0.6f * Rank(p, prof, t); }
    // a rare extra find on top of the gathered thing: 2% a mark
    public static string ExtraFind(AHPlayer p, string prof, int t)
    {
        int r = Rank(p, prof, t); if (r <= 0 || UnityEngine.Random.value >= 0.02f * r) return null;
        if (prof == "mining") { float x = UnityEngine.Random.value; return x < 0.45f ? "sapphire" : x < 0.75f ? "ruby" : x < 0.95f ? "emerald" : "diamond"; }
        Inject(); return Extra[PI(prof)];
    }

    // ---------- the new items: the rare finds, the Master's tools, the night finds, the satchel ----------
    static bool injected;
    public static void Inject()
    {
        if (injected) return; var items = AHDB.Items; if (items == null) return; injected = true;
        Action<string, string, string, string, string, double> I = (id, name, icon, c, rar, price) => { if (!items.ContainsKey(id)) items[id] = new Dictionary<string, object> { { "name", name }, { "icon", icon }, { "c", c }, { "rarity", rar }, { "price", price } }; };
        I("amber", "Amber", "gem", "#ffb63a", "rare", 60); I("starpetal", "Starpetal", "herb", "#cfe4ff", "rare", 55);
        I("perfect_pelt", "Perfect pelt", "hide", "#e6d2b0", "rare", 70);
        if (!items.ContainsKey("pearl")) I("pearl", "Pearl", "gem", "#f2f0ea", "rare", 65);
        I("moonpetal", "Moonpetal", "herb", "#b8c8ff", "epic", 140); I("raw_glowfin", "Raw glowfin", "fish", "#8af0ff", "epic", 150);
        if (!items.ContainsKey("glowfin")) items["glowfin"] = new Dictionary<string, object> { { "name", "Grilled glowfin" }, { "icon", "fish" }, { "c", "#8af0ff" }, { "rarity", "epic" }, { "food", new Dictionary<string, object> { { "hp", 90.0 }, { "hunger", 60.0 } } }, { "price", 200.0 } };
        for (int i = 0; i < Profs.Length; i++)
        {
            string id = "master_" + ToolKind[i];
            if (!items.ContainsKey(id)) items[id] = new Dictionary<string, object> { { "name", "Master’s " + ToolName[i] }, { "icon", i == 3 ? "rod" : "bar" }, { "c", "#9fe6ff" }, { "rarity", "rare" }, { "price", 40.0 },
                { "tool", new Dictionary<string, object> { { "kind", ToolKind[i] }, { "k", 0.1 } } }, { "note", "The Guild’s own " + ToolName[i] + ". It carries the marks you earn on every tier." } };
        }
        if (!items.ContainsKey(Satchel)) items[Satchel] = new Dictionary<string, object> { { "name", "Gatherer’s satchel" }, { "icon", "cloth" }, { "c", "#c8a06a" }, { "rarity", "uncommon" }, { "price", 120.0 },
            { "tool", new Dictionary<string, object> { { "kind", "satchel" }, { "k", 0.0 } } }, { "note", "On the tool belt it holds up to " + SatchelSlots + " kinds of ore, bars, logs, herbs, fish, hides and gems, so they don't fill your bag." } };
    }
    public const string Satchel = "satchel_gather"; public const int SatchelSlots = 24;
    // a gathered material (it goes in the satchel)
    public static bool IsMaterial(string id)
    {
        if (id == null) return false;
        if (AHGrades.Gradable(id) || id.StartsWith("raw_") || AHFish.Get(id) != null) return true;
        switch (id) { case "amber": case "starpetal": case "perfect_pelt": case "pearl": case "moonpetal": case "sapphire": case "ruby": case "emerald": case "diamond": case "coral_shard": case "obsidian_shard": case "wolf_fang": return true; }
        return false;
    }
    public static int SatchelRoom(AHPlayer p) { return p != null && AHBags.Tool(p, "satchel") != null ? SatchelSlots : 0; }

    // ---------- the night: some finds only come in the dark ----------
    public static string NightFind(AHGame g, string prof)
    {
        if (g == null || !(g.IsNight || g.Dark)) return null;
        Inject();
        if (prof == "herblore" && UnityEngine.Random.value < 0.06f) return "moonpetal";
        if (prof == "fishing" && UnityEngine.Random.value < 0.05f) return "raw_glowfin";
        return null;
    }
}

// the Guild's mastery page: every craft and tier, how far along, and the quests to hand in (only at the Guild)
public partial class AHUI
{
    bool masteryAtGuild;
    public void OpenMastery(bool atGuild) { masteryAtGuild = atGuild; wkMode = "mastery"; wkPageI = 0; ShowWork(true); RenderWork(); }

    void RenderMastery(AHPlayer p)
    {
        wkTitle.text = "Gathering mastery";
        wkHint.text = masteryAtGuild ? "Hand in a finished quest to have your tool marked for that tier: better grades and rare finds from it."
                                     : "Quests are handed in to the Guild Clerk in Ashen Hollow. A mark works while that tool is on your belt.";
        var rows = new List<Action<int>>();
        for (int i = 0; i < AHMastery.Profs.Length; i++)
        {
            string prof = AHMastery.Profs[i]; int ii = i;
            rows.Add(s => Row(s, AHMastery.ProfName[ii] + " · your " + AHMastery.ToolName[ii] + (AHMastery.HasTool(p, prof) ? "" : "  <color=#ff8a7a>(none on your belt)</color>"), CGGold, AHMastery.MarkLine(p, prof), ""));
            for (int t = 1; t <= AHMastery.Tiers(prof); t++)
            {
                int tt = t; long c = AHMastery.Count(p, prof, t); int m = AHMastery.Marks(p, prof, t), next = AHMastery.Next(p, prof, t);
                bool ready = AHMastery.Ready(p, prof, t);
                string l2 = next < 0 ? "Mastered · " + c.ToString("#,0") + " gathered" : c.ToString("#,0") + " / " + next.ToString("#,0") + " gathered · next: mark " + AHMastery.Roman[m + 1];
                string l3 = m > 0 ? "Mark " + AHMastery.Roman[m] + ": grades ×" + (1f + 0.6f * m).ToString("0.0") + " · rare finds " + (2 * m) + "%" : "";
                rows.Add(s => Row(s, "   " + AHMastery.TierName(prof, tt) + (m > 0 ? "  " + AHMastery.Roman[m] : ""), ready ? new Color(0.6f, 1f, 0.6f) : Color.white, l2, l3,
                    ready ? new WkBtn { label = masteryAtGuild ? "Hand in" : "At the Guild", on = masteryAtGuild, col = Go, act = () => { AHMastery.Claim(g, p, prof, tt); RenderWork(); } } : null));
            }
        }
        int from = Paged(rows.Count);
        for (int k = from; k < Mathf.Min(rows.Count, from + RowsPerPage); k++) rows[k](k - from);
    }
}
