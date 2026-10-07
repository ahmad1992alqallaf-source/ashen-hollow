// Ashen Hollow: rare recipes. Set gear used to come only from drops; now a Rare recipe scroll teaches you to make one
// piece yourself. Scrolls turn up from treasure goblins, secret caches, world events and the first time you beat a
// boss. Read one in your bag to learn a recipe you don't know yet; it then shows at its station (anvil or loom) with a
// "Rare recipe" tag. Twelve to learn: the Forgebound set at the anvil, Cinderweave and Emberdawn pieces at the loom.
using System.Collections.Generic;
using UnityEngine;

public static class AHRareRecipes
{
    public const string ScrollId = "recipe_scroll";
    class Def { public string outId, station, skill; public int lvl, xp; public string[] mats; public int[] n; }
    static readonly Def[] Defs =
    {
        new Def { outId = "forge_helm",      station = "anvil", skill = "smithing", lvl = 16, xp = 420, mats = new[] { "obsidian_shard", "magma_core", "gold_ore" }, n = new[] { 2, 1, 1 } },
        new Def { outId = "forge_boots",     station = "anvil", skill = "smithing", lvl = 16, xp = 420, mats = new[] { "obsidian_shard", "magma_core", "iron_bar" }, n = new[] { 2, 1, 2 } },
        new Def { outId = "forge_gauntlets", station = "anvil", skill = "smithing", lvl = 17, xp = 440, mats = new[] { "obsidian_shard", "magma_core", "gold_ore" }, n = new[] { 2, 1, 1 } },
        new Def { outId = "forge_pauldrons", station = "anvil", skill = "smithing", lvl = 18, xp = 480, mats = new[] { "obsidian_shard", "magma_core", "gold_ore" }, n = new[] { 3, 1, 2 } },
        new Def { outId = "forge_greaves",   station = "anvil", skill = "smithing", lvl = 18, xp = 480, mats = new[] { "obsidian_shard", "magma_core", "iron_bar" }, n = new[] { 3, 1, 2 } },
        new Def { outId = "forge_plate",     station = "anvil", skill = "smithing", lvl = 20, xp = 650, mats = new[] { "obsidian_shard", "magma_core", "gold_ore", "ruby" }, n = new[] { 4, 2, 2, 1 } },
        new Def { outId = "cinder_hood",     station = "loom",  skill = "tailoring", lvl = 16, xp = 420, mats = new[] { "ember_hide", "salamander_scale", "linen" }, n = new[] { 2, 1, 1 } },
        new Def { outId = "cinder_gloves",   station = "loom",  skill = "tailoring", lvl = 16, xp = 420, mats = new[] { "ember_hide", "salamander_scale" }, n = new[] { 2, 1 } },
        new Def { outId = "cinder_boots",    station = "loom",  skill = "tailoring", lvl = 17, xp = 440, mats = new[] { "ember_hide", "salamander_scale", "linen" }, n = new[] { 2, 1, 1 } },
        new Def { outId = "cinder_robe",     station = "loom",  skill = "tailoring", lvl = 19, xp = 600, mats = new[] { "ember_hide", "drake_wing", "salamander_scale", "linen" }, n = new[] { 3, 1, 2, 2 } },
        new Def { outId = "dawn_mantle",     station = "loom",  skill = "tailoring", lvl = 18, xp = 480, mats = new[] { "sea_silk", "linen", "sunpetal" }, n = new[] { 2, 2, 3 } },
        new Def { outId = "dawn_robe",       station = "loom",  skill = "tailoring", lvl = 20, xp = 650, mats = new[] { "sea_silk", "linen", "sunpetal", "ruby" }, n = new[] { 3, 3, 4, 1 } },
    };

    public static bool Known(AHPlayer p, string outId) { return p != null && p.prog != null && p.prog.recipes.Contains(outId); }
    public static int KnownCount(AHPlayer p) { int n = 0; foreach (var d in Defs) if (Known(p, d.outId)) n++; return n; }
    public static int Total { get { return Defs.Length; } }

    // AHGather.Recipes: the rare ones join their station's list (shown only once learned)
    public static void AddTo(string station, List<AHRecipe> l)
    {
        foreach (var d in Defs)
        {
            if (d.station != station || AHItems.Get(d.outId) == null) continue;
            var r = new AHRecipe { station = station, outId = d.outId, skill = d.skill, lvl = d.lvl, xp = d.xp, rare = true };
            for (int i = 0; i < d.mats.Length; i++) if (AHItems.Get(d.mats[i]) != null) r.mats.Add(new KeyValuePair<string, int>(d.mats[i], d.n[i]));
            l.Add(r);
        }
    }

    // reading a scroll in the bag
    public static void Read(AHGame g)
    {
        var p = g.player; if (p == null) return;
        var unknown = new List<Def>(); foreach (var d in Defs) if (!Known(p, d.outId) && AHItems.Get(d.outId) != null) unknown.Add(d);
        if (unknown.Count == 0) { g.ui.Toast("You already know every rare recipe. Sell the scroll to another crafter."); return; }
        if (!p.bag.Take(ScrollId)) return;
        var pick = unknown[Random.Range(0, unknown.Count)];
        p.prog.recipes.Add(pick.outId);
        var it = AHItems.Get(pick.outId);
        g.ui.Banner("Rare recipe: " + it.name, (pick.station == "anvil" ? "Anvil" : "Loom") + " · " + (pick.skill == "smithing" ? "Smithing " : "Tailoring ") + pick.lvl + " · " + KnownCount(p) + " / " + Total + " rare recipes");
        AHSound.Play("level"); p.bag.Touch(); g.MarkDirty();
    }

    // a scroll as a reward
    public static void Give(AHGame g, AHPlayer p, string from)
    {
        if (p == null || !p.bag.Add(ScrollId)) return;
        if (g.ui != null) g.ui.Toast("A Rare recipe scroll from " + from + "! Read it in your bag.", 3f);
    }
}
