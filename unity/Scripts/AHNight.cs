// Ashen Hollow: the night market, as in the web game (v66, NM_KINDS / NM_GOODS / nmOffers / nmBuild / nmBuy).
// After dark, lantern stalls open in each city's square: a lantern cook (cheap hot food), a night herbalist (potions and
// rare herbs), a curio dealer (stones, gems, sacks) and a travelling trader (goods from the other cities). Each sells four
// things a night, a few of each, below shop prices.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHNight
{
    public class Kind { public string id, name, blurb; public int col; public string[] pool; public float k; public int n; }
    static readonly Kind[] Kinds =
    {
        new Kind { id = "food", name = "Lantern cook", col = 0xe8603a, blurb = "Hot food straight off the grill, cheaper than any inn.", pool = new[] { "stew", "pie", "bread", "trout", "salmon", "tuna", "meat", "spiced_kebab", "hot_mead", "cinder_stew", "swordfish", "coconut" }, k = 0.6f, n = 5 },
        new Kind { id = "herb", name = "Night herbalist", col = 0x6ad87a, blurb = "Potions brewed by moonlight, and herbs that only open after dark.", pool = new[] { "hp_potion", "mana_potion", "big_potion", "elixir_swift", "elixir_iron", "elixir_might", "sunpetal", "mireroot", "frostbloom", "emberthorn", "witch_brew", "tide_tonic" }, k = 0.7f, n = 3 },
        new Kind { id = "curio", name = "Curio dealer", col = 0x9b5cff, blurb = "Odd things from odd places. Don’t ask where.", pool = new[] { "enh_stone", "mystery_sack", "sapphire", "ruby", "emerald", "topaz", "amethyst", "pearl", "magma_core", "gold_ore" }, k = 0.85f, n = 1 },
        new Kind { id = "trader", name = "Travelling trader", col = 0xd9ab3a, blurb = "Goods carried in from the other cities of the realm.", pool = null, k = 0.9f, n = 2 },
    };
    static readonly Dictionary<string, string[]> Goods = new Dictionary<string, string[]>
    {
        { "varrow", new[] { "iron_bar", "bread", "cheese", "wool" } }, { "highcairn", new[] { "frost_pelt", "pine_logs", "hot_mead", "frostbloom" } },
        { "mirewatch", new[] { "mireroot", "bog_slime", "eel", "lurker_moss" } }, { "sunspire", new[] { "spiced_kebab", "gold_ore", "emberthorn", "scorpion_chitin" } },
        { "cinderhold", new[] { "obsidian_shard", "ember_hide", "magma_core", "fire_ward" } }, { "coralport", new[] { "pearl", "coral_shard", "sea_silk", "coconut" } },
    };
    public static readonly Dictionary<string, string> CityOfArea = new Dictionary<string, string> { { "city", "varrow" }, { "hc_city", "highcairn" }, { "mw_city", "mirewatch" }, { "ss_city", "sunspire" }, { "ch_city", "cinderhold" }, { "co_city", "coralport" } };

    public static long NightId(AHGame g) { return (long)Math.Floor((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0 + 100) / Math.Max(60f, g.dayLength)); }
    static string city;
    static readonly List<GameObject> stalls = new List<GameObject>();
    public static List<Vector3> StallPos() { var l = new List<Vector3>(); foreach (var s in stalls) if (s != null) l.Add(s.transform.position); return l; }
    static bool shown, bannerShown;

    public static List<string> Offers(AHGame g, Kind K)
    {
        int seed = (int)(NightId(g) * 977 + city.Length * 31 + K.id.Length * 7); foreach (char ch in city + K.id) seed = seed * 33 + ch;
        var r = new System.Random(seed);
        var pool = new List<string>();
        if (K.pool != null) pool.AddRange(K.pool); else foreach (var kv in Goods) if (kv.Key != city) pool.AddRange(kv.Value);
        pool.RemoveAll(id => AHItems.Get(id) == null || AHComp.Value(id) <= 0);
        var o = new List<string>(); while (o.Count < 4 && pool.Count > 0) { int i = r.Next(pool.Count); o.Add(pool[i]); pool.RemoveAt(i); }
        return o;
    }
    public static long Price(string id, Kind K) { return Math.Max(AHDB.CU, (long)Math.Round(AHShops.PriceBuy(id) * K.k)); }
    static int Bought(AHPlayer p, AHGame g, string key) { var S = p.prog; if (S.nmNight != NightId(g)) { S.nmNight = NightId(g); S.nmBought.Clear(); } return (int)AHProgress.Get(S.nmBought, key); }

    public static void Buy(AHGame g, Kind K, string id)
    {
        var p = g.player; if (!g.IsNight) return; string key = K.id + ":" + id; long pr = Price(id, K);
        if (Bought(p, g, key) >= K.n || p.bag.money < pr) return;
        if (!p.bag.Add(id)) { g.ui.Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
        p.bag.money -= pr; p.bag.Touch(); p.NoteBought(id, 1); AHProgress.Add(p.prog.nmBought, key, 1); AHSound.Play("coin"); g.SaveProgress();
    }
    public static int Left(AHGame g, Kind K, string id) { return K.n - Bought(g.player, g, K.id + ":" + id); }

    public static void Setup(AHGame g)
    {
        stalls.Clear(); shown = false; bannerShown = false; city = null;
        string c; if (!CityOfArea.TryGetValue(AHGame.AreaId, out c)) return; city = c;
        object w = null; if (AHWays.Ways != null) foreach (var q in AHWays.Ways) if (AHJson.S(q, "area") == AHGame.AreaId && (AHJson.S(q, "id") == c || w == null)) w = q;
        if (w == null) return;
        float S = AHDB.S; Vector3 ctr = g.W((float)AHJson.N(w, "x") * S, (float)AHJson.N(w, "y") * S);
        var r = new System.Random(c.GetHashCode());
        var taken = new List<Vector3>();
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var K in Kinds)
        {
            Vector3? spot = null;
            for (int t = 0; t < 160 && !spot.HasValue; t++)
            {
                double a = r.NextDouble() * Math.PI * 2, d = (170 + r.NextDouble() * 260) * S;
                Vector3 p = ctr + new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)) * (float)d;
                if (!g.InArea(p) || g.Blocked(p, 1.4f)) continue;
                bool bad = false; foreach (var s2 in AHGather.Spots) if ((s2.pos - p).magnitude < 4.8f) bad = true; foreach (var tp in taken) if ((tp - p).magnitude < 4.8f) bad = true;
                if (!bad) spot = p;
            }
            if (!spot.HasValue) continue; taken.Add(spot.Value);
            var root = new GameObject("NightStall").transform; root.position = g.Resolve(spot.Value, 1.2f); root.rotation = g.Face(ctr - root.position);
            Func<int, bool, Material> M = (hex, glow) => { var m = new Material(sh); m.SetColor("_BaseColor", AHGame.Hex(hex).linear); if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", AHGame.Hex(hex).linear * 2f); } return m; };
            Action<Vector3, Vector3, Material> box = (pos, size, m) => { var o = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); UnityEngine.Object.Destroy(o.GetComponent<Collider>()); o.transform.SetParent(root, false); o.transform.localPosition = pos; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m; };
            // a KayKit market stall, a different awning for each trader (KayKit Medieval Hexagon, CC0)
            string[] cols = { "red", "blue", "yellow", "green" };
            var spf = Resources.Load<GameObject>("AH/Models/KK/kk_building_market_" + cols[stalls.Count % 4]);
            if (spf != null) { var st = UnityEngine.Object.Instantiate(spf, root, false); foreach (var cl in st.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.Destroy(cl); st.transform.localScale = Vector3.one * 1.7f; }
            else
            {
                var wood = M(0x5a3f28, false);
                box(new Vector3(0, 0.45f, 0.45f), new Vector3(2.1f, 0.9f, 0.5f), M(0x6b4a2c, false));
                box(new Vector3(0, 2.35f, 0), new Vector3(2.5f, 0.15f, 1.9f), M(K.col, false));
            }
            foreach (var sx in new[] { -1, 1 }) box(new Vector3(sx * 1.05f, 2.05f, 0.75f), new Vector3(0.22f, 0.28f, 0.22f), M(0xffb347, true));
            var lg = new GameObject("Glow"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(0, 2f, 0.8f);
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.72f, 0.35f); li.range = 7f; li.intensity = 1.8f; li.shadows = LightShadows.None;
            AHModel.SetShadows(root.gameObject);
            var KK = K; var spotObj = new AHSpot { kind = "use", name = K.name, pos = root.position, r = 0.8f, reach = 3.2f, use = () => g.ui.OpenNight(KK) };
            root.gameObject.AddComponent<AHNightStall>().Setup(g, spotObj);
            root.gameObject.SetActive(false);
            stalls.Add(root.gameObject);
        }
    }

    // open from dusk until dawn (the spots come and go with the stalls)
    public static void Tick(AHGame g)
    {
        if (stalls.Count == 0) return;
        bool open = g.IsNight;
        if (open == shown) return; shown = open;
        foreach (var s in stalls) { s.SetActive(open); var st = s.GetComponent<AHNightStall>(); if (open) st.Show(); else st.Hide(); }
        if (open && !bannerShown) { bannerShown = true; g.ui.Banner("Night market", "Lantern stalls are open in the square until dawn"); }
    }
}

public class AHNightStall : MonoBehaviour
{
    AHGame g; AHSpot spot;
    public void Setup(AHGame game, AHSpot s) { g = game; spot = s; }
    public void Show() { if (!AHGather.Spots.Contains(spot)) AHGather.Spots.Add(spot); }
    public void Hide() { AHGather.Spots.Remove(spot); }
}

public partial class AHUI
{
    AHNight.Kind nightSel;
    public void OpenNight(AHNight.Kind K) { nightSel = K; wkMode = "night"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderNight(AHPlayer p)
    {
        var K = nightSel; var rows = new List<Action<int>>();
        wkTitle.text = K.name + " · night market";
        wkHint.text = K.blurb + " New goods every night. " + AHItems.MoneyText(p.bag.money) + ".";
        foreach (var id in AHNight.Offers(g, K))
        {
            string ii = id; var it = AHItems.Get(id); long pr = AHNight.Price(id, K); int left = AHNight.Left(g, K, id);
            rows.Add(s => Row(s, it.name, AHItems.Quality(it), AHItems.MoneyText(pr) + " · " + (left > 0 ? left + " left tonight" : "sold out for you tonight"), it.note ?? "",
                new WkBtn { label = "Buy", on = left > 0 && p.bag.money >= pr && g.IsNight, col = Go, act = () => { AHNight.Buy(g, K, ii); RenderWork(); } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
