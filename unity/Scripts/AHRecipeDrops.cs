// Ashen Hollow: recipes for everything, World of Warcraft style. The first recipes of each craft (up to skill 10) are
// known from the start; every one above that has to be learned:
//  - from the station itself (the trainer's book): once your skill reaches the recipe's level, pay to learn it, or
//  - from a recipe scroll ("Recipe: Iron longsword"): bosses drop one often, elites and rare beasts now and then,
//    other beasts very seldom. Read it in the bag to learn it at once, even before your skill gets there (you still
//    need the skill to make it). Scrolls you don't need sell well or go on the market.
// Heroes who were already crafting keep everything their skill had reached when this came in.
using System.Collections.Generic;
using UnityEngine;

public static class AHRecipeDrops
{
    public const int FreeUpTo = 10;
    public const string Pre = "rcp_";
    static readonly HashSet<string> Stations = new HashSet<string> { "anvil", "loom", "brew", "jewel" };
    static bool injected;
    // outId -> (station, skill, lvl) read straight from the rules, so the scrolls exist before the recipe lists do
    class Info { public string station, skill; public int lvl; }
    static Dictionary<string, Info> info;

    static void Load()
    {
        if (info != null) return;
        info = new Dictionary<string, Info>();
        var all = AHJson.O(AHDB.Rules, "RECIPES") as Dictionary<string, object>; if (all == null) return;
        foreach (var kv in all)
        {
            if (!Stations.Contains(kv.Key)) continue;
            var arr = kv.Value as List<object>; if (arr == null) continue;
            foreach (var o in arr)
            {
                string outId = AHJson.S(o, "out"); int lvl = (int)AHJson.N(o, "lvl", 1);
                if (outId == null || lvl <= FreeUpTo || info.ContainsKey(outId)) continue;
                info[outId] = new Info { station = kv.Key, skill = AHJson.S(o, "skill"), lvl = lvl };
            }
        }
    }

    // the scroll items into the item table (from AHItems.Get's first call)
    public static void Inject()
    {
        if (injected) return;
        var items = AHDB.Items; if (items == null) return;
        injected = true; Load();
        foreach (var kv in info)
        {
            object raw; if (!items.TryGetValue(kv.Key, out raw)) continue;
            string nm = AHJson.S(raw, "name", kv.Key);
            var i = kv.Value;
            string rar = i.lvl >= 30 ? "epic" : i.lvl >= 18 ? "rare" : "uncommon";
            items[Pre + kv.Key] = new Dictionary<string, object>
            {
                { "name", "Recipe: " + nm }, { "icon", "cloth" }, { "c", "#e8d9a8" }, { "rarity", rar },
                { "price", (double)(4 + i.lvl * i.lvl * 0.3) },
                { "note", "Teaches you to make " + nm + " at the " + AHGather.StationName(i.station).ToLowerInvariant() + " (" + AHDB.SkillName(i.skill) + " " + i.lvl + ")." },
            };
        }
    }

    public static bool IsScroll(string id) { return id != null && id.StartsWith(Pre) && AHItems.Get(id) != null; }
    public static bool NeedsLearning(AHRecipe r) { return r != null && !r.rare && Stations.Contains(r.station) && r.lvl > FreeUpTo; }

    static void Init(AHPlayer p)
    {
        if (p.prog.learnedInit) return;
        p.prog.learnedInit = true;
        // the grandfather rule: what your skill already reached is yours
        Load();
        foreach (var kv in info) if (p.Skill(kv.Value.skill) >= kv.Value.lvl && !p.prog.learned.Contains(kv.Key)) p.prog.learned.Add(kv.Key);
    }
    public static bool Known(AHPlayer p, AHRecipe r)
    {
        if (!NeedsLearning(r)) return true;
        if (p == null || p.prog == null) return false;
        Init(p);
        return p.prog.learned.Contains(r.outId);
    }
    public static long TrainCost(AHRecipe r) { return (long)System.Math.Round((5 + r.lvl * r.lvl * 0.6) * AHDB.CU); }
    public static bool CanTrain(AHPlayer p, AHRecipe r) { return !Known(p, r) && p.Skill(r.skill) >= r.lvl; }

    public static bool Train(AHGame g, AHPlayer p, AHRecipe r)
    {
        if (!CanTrain(p, r)) return false;
        long c = TrainCost(r);
        if (p.bag.money < c) { g.ui.Toast("You need " + AHItems.MoneyText(c) + " to learn this."); return false; }
        p.bag.money -= c; p.bag.Touch();
        Learn(g, p, r.outId, false);
        return true;
    }

    static void Learn(AHGame g, AHPlayer p, string outId, bool fromScroll)
    {
        Init(p);
        if (!p.prog.learned.Contains(outId)) p.prog.learned.Add(outId);
        var it = AHItems.Get(outId);
        if (g != null && g.ui != null) g.ui.Banner("Recipe learned: " + (it != null ? it.name : outId), fromScroll ? "From a recipe scroll" : "From the trainer’s book");
        AHSound.Play("level");
        if (g != null) g.MarkDirty();
    }

    // reading a scroll in the bag
    public static void Read(AHGame g, string id)
    {
        var p = g.player; if (p == null || !IsScroll(id)) return;
        string outId = id.Substring(Pre.Length);
        Init(p);
        if (p.prog.learned.Contains(outId)) { g.ui.Toast("You already know this recipe. Sell the scroll, or put it on the market."); return; }
        if (!p.bag.Take(id)) return;
        Learn(g, p, outId, true);
    }

    // a kill may leave a recipe scroll on the body: bosses 35%, elites 8%, rare beasts 12%, others 0.4%,
    // for a recipe near the beast's level (within its level +5)
    public static string Roll(AHGame g, AHMob m, AHPlayer p)
    {
        if (m == null || m.type == null) return null;
        Load(); Inject();
        var t = m.type;
        bool boss = AHJson.B(AHJson.O(AHDB.Mobs, t.id), "boss") || AHDeep.IsBoss(m) || AHStory.IsBoss(m);
        float ch = boss ? 0.35f : t.elite ? 0.08f : t.rare ? 0.12f : 0.004f;
        if (Random.value >= ch * AHComp.Lucky(p)) return null;
        var pool = new List<string>();
        foreach (var kv in info) if (kv.Value.lvl <= t.lvl + 5 && kv.Value.lvl >= t.lvl - 12 && AHItems.Get(Pre + kv.Key) != null) pool.Add(Pre + kv.Key);
        if (pool.Count == 0) foreach (var kv in info) if (kv.Value.lvl <= t.lvl + 5 && AHItems.Get(Pre + kv.Key) != null) pool.Add(Pre + kv.Key);
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    public static int KnownCount(AHPlayer p) { Load(); Init(p); int n = 0; foreach (var k in info.Keys) if (p.prog.learned.Contains(k)) n++; return n; }
    public static int Total { get { Load(); return info.Count; } }
}
