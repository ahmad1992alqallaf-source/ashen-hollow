// Ashen Hollow: rarity for everything you gather. Ore, logs, hides, herbs and the bars smelted from ore now come in
// five grades, each with its own name and colour:
//   Common (the plain one) · Uncommon · Rare · Epic · Legendary
// e.g. Iron ore · Rich iron ore · Pure iron ore · Radiant iron ore · Starborn iron ore.
// A gathering roll decides the grade (more skill above the node's level, a rich vein or an ancient tree, and the
// Artisan path all raise the odds). Graded materials stand in for the plain one in every recipe; the better they are,
// the likelier the piece comes out Masterwork (+30% stats), and anything made with a Legendary material always does.
// Smelting keeps the best grade of the ore into the bar. The graded items are added to the item table when the game
// starts (ids <base>_g1 .. _g4), so shops, the bag, the market and saving treat them like any other item.
using System.Collections.Generic;
using UnityEngine;

public static class AHGrades
{
    public static readonly string[] Names = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
    static readonly Dictionary<string, string[]> Prefix = new Dictionary<string, string[]>
    {
        { "ore", new[] { "Rich", "Pure", "Radiant", "Starborn" } },
        { "bar", new[] { "Fine", "Tempered", "Radiant", "Starforged" } },
        { "log", new[] { "Seasoned", "Heartwood", "Elder", "Ancient" } },
        { "hide", new[] { "Thick", "Supple", "Flawless", "Legendary" } },
        { "herb", new[] { "Fresh", "Vibrant", "Moonlit", "Primordial" } },
    };
    static readonly Dictionary<string, string> kindOf = new Dictionary<string, string>();   // base id -> kind
    static bool injected;

    // the grade of an item id (0: not graded)
    public static int Of(string id)
    {
        if (id == null || id.Length < 4) return 0;
        int n = id.Length; if (id[n - 3] != '_' || id[n - 2] != 'g') return 0;
        int k = id[n - 1] - '0'; return k >= 1 && k <= 4 ? k : 0;
    }
    public static string Base(string id) { return Of(id) > 0 ? id.Substring(0, id.Length - 3) : id; }
    public static string Id(string baseId, int grade) { return grade <= 0 ? baseId : baseId + "_g" + grade; }
    public static bool Gradable(string id) { Inject(); return id != null && kindOf.ContainsKey(Base(id)); }

    // 0..4 by the quality colour band (poor -1)
    public static int Rank(ItemDef d)
    {
        if (d == null) return 0;
        switch (AHItems.QualityName(d)) { case "poor": return -1; case "uncommon": return 1; case "rare": return 2; case "epic": return 3; case "legendary": return 4; }
        return 0;
    }
    public static int RankOf(Color c)
    {
        foreach (var q in new[] { "legendary", "epic", "rare", "uncommon" })
        {
            var row = AHJson.A(AHDB.Table("items", "QUALITY"), q); if (row == null || row.Count < 2) continue;
            var qc = AHDB.Col(row[1], Color.white); if (Mathf.Abs(qc.r - c.r) + Mathf.Abs(qc.g - c.g) + Mathf.Abs(qc.b - c.b) < 0.05f) return q == "legendary" ? 4 : q == "epic" ? 3 : q == "rare" ? 2 : 1;
        }
        return 0;
    }

    // the graded items into the item table (once)
    public static void Inject()
    {
        if (injected) return;
        var items = AHDB.Items; if (items == null) return;
        injected = true;
        var rules = AHDB.Rules;
        var rocks = AHJson.O(rules, "ROCKS") as Dictionary<string, object>;
        if (rocks != null) foreach (var kv in rocks) Add(items, AHJson.S(kv.Value, "ore"), "ore");
        foreach (var b in new[] { "bronze_bar", "iron_bar", "gold_bar", "steel_bar", "obsidian_bar" }) Add(items, b, "bar");
        var trees = AHJson.O(rules, "TREES") as Dictionary<string, object>;
        if (trees != null) foreach (var kv in trees) Add(items, AHJson.S(kv.Value, "log"), "log");
        var herbs = AHJson.O(rules, "HERB_T") as Dictionary<string, object>;
        if (herbs != null) foreach (var kv in herbs) Add(items, kv.Key, "herb");
        var skins = AHJson.O(rules, "MOB_SKIN") as Dictionary<string, object>;
        if (skins != null)
            foreach (var kv in skins)
            {
                var l = kv.Value as List<object>; if (l == null) continue;
                foreach (var o in l) { string id = o as string; if (id != null && (id.Contains("hide") || id.Contains("pelt") || id.Contains("leather") || id.Contains("chitin") || id.Contains("scale"))) Add(items, id, "hide"); }
            }
    }
    static void Add(Dictionary<string, object> items, string id, string kind)
    {
        object raw; if (string.IsNullOrEmpty(id) || !items.TryGetValue(id, out raw) || kindOf.ContainsKey(id)) return;
        var src = raw as Dictionary<string, object>; if (src == null) return;
        kindOf[id] = kind;
        string nm = AHJson.S(src, "name", id);
        Color c = AHDB.Col(src.ContainsKey("c") ? src["c"] : null, Color.white);
        for (int k = 1; k <= 4; k++)
        {
            var d = new Dictionary<string, object>(src);
            string pre = Prefix[kind][k - 1];
            d["name"] = pre + " " + (char.ToLowerInvariant(nm[0]) + nm.Substring(1));
            d["rarity"] = "g" + k;
            Color cc = Color.Lerp(c, Color.white, 0.08f * k);
            d["c"] = "#" + ColorUtility.ToHtmlStringRGB(cc).ToLowerInvariant();
            if (src.ContainsKey("price")) { double pr; if (double.TryParse(src["price"].ToString(), out pr)) d["price"] = pr * Worth(k); }
            items[Id(id, k)] = d;
        }
    }
    public static float Worth(int grade) { return grade == 1 ? 2f : grade == 2 ? 5f : grade == 3 ? 14f : grade == 4 ? 50f : 1f; }

    // ---------- the roll ----------
    // chances per gather: skill above the node's level helps (up to +50%), a rich node or ancient tree x3,
    // Artisans x1.5. Returns 0..4
    public static int Roll(AHPlayer p, int skillLv, int req, float bonus = 1f)
    {
        float k = (1f + Mathf.Clamp01((skillLv - req) / 30f) * 0.5f) * bonus * (p != null && p.path == "artisan" ? 1.5f : 1f);
        float r = Random.value;
        if (r < 0.001f * k) return 4;
        if (r < 0.009f * k) return 3;
        if (r < 0.045f * k) return 2;
        if (r < 0.18f * k) return 1;
        return 0;
    }

    // ---------- materials for recipes: the plain one or any grade of it ----------
    public static int CountAll(AHBag b, string id)
    {
        int n = b.Count(id); if (!Gradable(id)) return n;
        for (int k = 1; k <= 4; k++) n += b.Count(Id(id, k));
        return n;
    }
    public static bool UseBest;   // the recipe window's switch: spend the best grades first (else the plainest)
    // takes n of id (any grades); returns the sum of the grades used, and the best one
    public static bool TakeAll(AHBag b, string id, int n, out int gradeSum, out int best)
    {
        gradeSum = 0; best = 0;
        if (CountAll(b, id) < n) return false;
        if (!Gradable(id)) { b.Take(id, n); return true; }
        for (int s = 0; s <= 4 && n > 0; s++)
        {
            int k = UseBest ? 4 - s : s; string gid = Id(id, k); int have = b.Count(gid); if (have <= 0) continue;
            int t = Mathf.Min(have, n); b.Take(gid, t); n -= t; gradeSum += k * t; if (k > best) best = k;
        }
        return true;
    }

    // the better the materials, the likelier a masterwork: extra chance, and a Legendary material makes it certain
    public static float MwBoost(int gradeSum, int units, int best) { if (best >= 4) return 1f; return units <= 0 ? 0f : Mathf.Clamp01(gradeSum / (float)units * 0.18f); }

    public static string Kind(string id) { string k; return kindOf.TryGetValue(Base(id), out k) ? k : null; }
}
