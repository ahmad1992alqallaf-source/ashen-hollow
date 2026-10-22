// Ashen Hollow: every land has its own fish, five of them from Common to Legendary. A trout bites anywhere in Hollow
// Meadow; a Sunscale koi rises once in a long while. Higher rarities need more Fishing skill, bite less often and
// fight on the line (AHFishing's reel). Each fish cooks into its own dish, better the rarer it is. The new fish are
// added to the item table when the game starts; the old ones (trout, salmon, tuna, eel, swordfish, lava eel) keep
// their own dishes.
using System.Collections.Generic;
using UnityEngine;

public static class AHFish
{
    public class Fish { public string id, name, cooked, cookedName; public int tier, lvl, xp, heal, hunger; public Color col; }
    static readonly Dictionary<string, Fish[]> byLand = new Dictionary<string, Fish[]>();
    static readonly Dictionary<string, Fish> byId = new Dictionary<string, Fish>();
    static bool built, injected;
    public static readonly string[] TierName = { "common", "uncommon", "rare", "epic", "legendary" };

    // land -> its five fish (id without raw_, name, colour); levels grow from the land's own
    static readonly string[][] Table =
    {
        new[] { "meadow", "1", "trout|Trout|8fa8b6", "perch|Meadow perch|9ab26a", "goldfin|Goldfin carp|e0b040", "golden_carp|Golden carp|ffcc33", "sunscale|Sunscale koi|ff8a2a" },
        new[] { "mill", "4", "trout|Trout|8fa8b6", "pike|Millrace pike|7f9a62", "salmon|Salmon|e69a86", "silvershad|Silver shad|d8e0ea", "millking|Old Mill king carp|c98b3a" },
        new[] { "fenwick", "6", "eel|Eel|6a6a4a", "bogcarp|Bog carp|6f7a40", "mudskipper|Mire mudskipper|8a7a52", "witchgar|Witch gar|5aa070", "bogwyrm|Bogwyrm eel|3fd28a" },
        new[] { "silkwood", "8", "trout|Trout|8fa8b6", "silkperch|Silkwood perch|a4b07a", "salmon|Salmon|e69a86", "shadowcat|Shadow catfish|4a4a6a", "moonfin|Moonfin|b8d6ff" },
        new[] { "mire", "12", "eel|Eel|6a6a4a", "bogcarp|Bog carp|6f7a40", "mudskipper|Mire mudskipper|8a7a52", "witchgar|Witch gar|5aa070", "bogwyrm|Bogwyrm eel|3fd28a" },
        new[] { "kingsroad", "10", "salmon|Salmon|e69a86", "kingtrout|King's trout|9fb0c8", "bluegill|Vale bluegill|5a8ad0", "crownbass|Crown bass|d4b04a", "riftfin|Riftfin|b36cff" },
        new[] { "vale", "14", "salmon|Salmon|e69a86", "kingtrout|King's trout|9fb0c8", "bluegill|Vale bluegill|5a8ad0", "crownbass|Crown bass|d4b04a", "riftfin|Riftfin|b36cff" },
        new[] { "whitepine", "16", "icecod|Ice cod|c8d8e8", "frosttrout|Frost trout|9ad0e8", "glacierpike|Glacier pike|7ab8e0", "aurora|Aurora char|7affd0", "rimeking|Rimeking sturgeon|e0f4ff" },
        new[] { "frost", "20", "icecod|Ice cod|c8d8e8", "frosttrout|Frost trout|9ad0e8", "glacierpike|Glacier pike|7ab8e0", "aurora|Aurora char|7affd0", "rimeking|Rimeking sturgeon|e0f4ff" },
        new[] { "sands", "24", "oasiscarp|Oasis carp|d8b070", "sandloach|Sand loach|c8a868", "sunperch|Sunperch|f0c040", "mirage|Mirage fish|8ae0f0", "sunking|Sun-king catfish|ffb020" },
        new[] { "tide", "28", "tuna|Tuna|5a7a9a", "mackerel|Mackerel|6a9ab0", "swordfish|Swordfish|4a6a8a", "coralray|Coral ray|ff8aa0", "krakensnapper|Kraken snapper|a040ff" },
        new[] { "isle", "30", "tuna|Tuna|5a7a9a", "mackerel|Mackerel|6a9ab0", "swordfish|Swordfish|4a6a8a", "coralray|Coral ray|ff8aa0", "krakensnapper|Kraken snapper|a040ff" },
        new[] { "ember", "34", "lavaeel|Lava eel|e0602a", "cinderfin|Cinderfin|ff7a3a", "magmaray|Magma ray|ff4a1a", "emberkoi|Ember koi|ffb040", "phoenixfin|Phoenix fin|ffd060" },
        new[] { "fossil", "40", "bonefish|Bonefish|d8d0c0", "fossilgar|Fossil gar|a89878", "amberfin|Amberfin|ffb030", "coelacanth|Old coelacanth|6a8aa0", "primordial|Primordial angler|50ffd0" },
    };
    // lands without their own list use a neighbour's
    static readonly Dictionary<string, string> Like = new Dictionary<string, string>
    {
        { "causeway", "mire" }, { "scorchwind", "sands" }, { "ashfall", "sands" }, { "co_city", "tide" }, { "city", "vale" }, { "hc_city", "frost" },
        { "mw_city", "mire" }, { "ss_city", "sands" }, { "ch_city", "ember" }, { "homestead", "meadow" }, { "tutorial", "meadow" },
    };
    // the old fish keep their dish (raw_trout -> trout ...)
    static readonly HashSet<string> Old = new HashSet<string> { "trout", "salmon", "tuna", "eel", "swordfish", "lavaeel" };

    static void Build()
    {
        if (built) return; built = true;
        int[] heal = { 12, 20, 32, 50, 80 }, hunger = { 25, 32, 40, 50, 60 }, xpK = { 20, 32, 55, 95, 200 }, lvlUp = { 0, 2, 5, 8, 12 };
        foreach (var row in Table)
        {
            string land = row[0]; int L = int.Parse(row[1]); var list = new Fish[5];
            for (int t = 0; t < 5; t++)
            {
                var p = row[2 + t].Split('|');
                Fish f;
                if (!byId.TryGetValue("raw_" + p[0], out f))
                {
                    f = new Fish { id = "raw_" + p[0], name = p[1], tier = t, lvl = Mathf.Clamp(L + lvlUp[t] - 1, 1, 60), xp = xpK[t] + L * 3, heal = heal[t] + L, hunger = hunger[t], col = AHGame.Hex(System.Convert.ToInt32(p[2], 16)) };
                    f.cooked = p[0]; f.cookedName = "Grilled " + char.ToLowerInvariant(p[1][0]) + p[1].Substring(1);
                    byId[f.id] = f;
                }
                list[t] = f;
            }
            byLand[land] = list;
        }
    }

    public static IEnumerable<string> Lands { get { Build(); return byLand.Keys; } }   // (the fishing journal)
    public static Fish[] Of(string land)
    {
        Build();
        Fish[] l; string like;
        if (byLand.TryGetValue(land ?? "", out l)) return l;
        if (Like.TryGetValue(land ?? "", out like) && byLand.TryGetValue(like, out l)) return l;
        return byLand["meadow"];
    }
    public static Fish Get(string rawId) { Build(); Fish f; return rawId != null && byId.TryGetValue(rawId, out f) ? f : null; }
    public static IEnumerable<Fish> All { get { Build(); return byId.Values; } }

    // the new fish (and their dishes) into the item table, once
    public static void Inject()
    {
        if (injected) return;
        var items = AHDB.Items; if (items == null) return;
        injected = true; Build();
        foreach (var f in byId.Values)
        {
            string hex = "#" + ColorUtility.ToHtmlStringRGB(f.col).ToLowerInvariant();
            if (!items.ContainsKey(f.id)) items[f.id] = new Dictionary<string, object> { { "name", "Raw " + char.ToLowerInvariant(f.name[0]) + f.name.Substring(1) }, { "icon", "fish" }, { "c", hex }, { "price", (double)(4 + f.lvl * 2) * AHGrades.Worth(f.tier) } };
            if (f.tier > 0 && !Old.Contains(f.cooked)) ((Dictionary<string, object>)items[f.id])["rarity"] = TierName[f.tier];
            if (!Old.Contains(f.cooked) && !items.ContainsKey(f.cooked))
                items[f.cooked] = new Dictionary<string, object> { { "name", f.cookedName }, { "icon", "fish" }, { "c", hex }, { "rarity", f.tier > 0 ? TierName[f.tier] : "common" }, { "food", new Dictionary<string, object> { { "hp", (double)f.heal }, { "hunger", (double)f.hunger } } }, { "price", (double)(8 + f.lvl * 3) * AHGrades.Worth(f.tier) } };
        }
        // a fishing lure (sold by the fishmongers and the general stores): rarer fish bite for ten minutes
        if (!items.ContainsKey("fishing_lure")) items["fishing_lure"] = new Dictionary<string, object> { { "name", "Glimmer lure" }, { "icon", "fish" }, { "c", "#7ae0ff" }, { "rarity", "uncommon" }, { "price", 60.0 } };
    }

    // ---------- cooking ----------
    public static string Cooked(string raw) { var f = Get(raw); if (f != null) return f.cooked; return null; }
    public static int CookReq(string raw) { var f = Get(raw); return f != null ? f.lvl : 1; }
    // the best raw fish in the bag this cook can manage (null: none)
    public static string BestRaw(AHPlayer p)
    {
        Build(); string best = null; int bl = -1, c = p.Skill("cooking");
        foreach (var kv in p.bag.inv) { var f = Get(kv.Key); if (f == null || kv.Value <= 0 || f.lvl > c + 2) continue; if (f.lvl > bl) { bl = f.lvl; best = kv.Key; } }
        return best;
    }
}
