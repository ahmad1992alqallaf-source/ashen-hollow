// Ashen Hollow: your homestead, as in the web game (v67, HOME / HPLOTS / farmTick / renderFarm / travelHome).
// Buy the deed from a Land Agent (Hilda in Varrow, Farmer Tobin in Ashen Hollow; level 10). Your own land:
//  - a house that grows from a Tent to a Cottage (bed: fills rested XP; storage chest; mirror) to a Manor;
//  - six fields: plant seeds, water them at the well (twice as fast), spread bone meal or compost, harvest;
//    crops grow in real time, even while you are away;
//  - pens: chickens, ducks, bees, pigs, cows, sheep and goats that give eggs, milk, wool, honey... while fed;
//  - workshops: kitchen (dishes with a Well Fed buff), mill, dairy, spinning wheel and compost bin;
//  - a market stall that sells your goods while you are away, and home comforts that raise your rested XP.
// Rested XP: resting in a town fills it slowly; sleeping in your bed fills it; kills give double XP until it runs out.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHField { public string id, crop; public float prog; public long last; public bool water, bm, comp; }
[Serializable] public class AHPen { public string id; public int n, ready; public long fed, acc, last; }
[Serializable]
public class AHHomeState
{
    public int tier = 1; public List<string> built = new List<string>(), comf = new List<string>();
    public List<AHField> fields = new List<AHField>(); public List<AHPen> pens = new List<AHPen>();
    public long sleepAt; public List<AHStack> stall = new List<AHStack>(); public long stallCoins, stallLast; public int stallSold;
    public string fromArea; public float fromX, fromZ;
}

public static class AHHome
{
    public const string Area = "homestead";
    const double FarmMin = 60000;
    static long NowMs { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); } }
    public static object HOME { get { return AHDB.Table("home", "HOME"); } }
    public static List<object> Plots { get { return AHDB.List("home", "HPLOTS"); } }
    public static object Crop(string id) { return AHJson.O(AHDB.Table("home", "CROPS"), id); }
    public static object Animal(string id) { return AHJson.O(AHDB.Table("home", "ANIMALS"), id); }
    public static object Tier(int t) { var l = AHJson.A(HOME, "tiers"); return l != null && t >= 0 && t < l.Count ? l[t] : null; }
    public static object Plot(string id) { foreach (var p in Plots) if (AHJson.S(p, "id") == id) return p; return null; }
    public static AHField Field(AHHomeState H, string id) { return H.fields.Find(f => f.id == id); }
    public static AHPen Pen(AHHomeState H, string id) { var p = H.pens.Find(f => f.id == id); if (p == null) { p = new AHPen { id = id, last = NowMs }; H.pens.Add(p); } return p; }
    public static bool Built(AHHomeState H, string id) { return H != null && H.built.Contains(id); }
    public static int Cap(AHPlayer p) { return p.home == null ? 0 : (int)AHJson.N(Tier(p.home.tier), "cap", 3) + (p.Spec("farmer", "ranch") ? 2 : 0); }
    static float GrowK(AHPlayer p) { return (p.Perk("farmer", 5) ? 0.85f : 1f) * (p.Spec("farmer", "crops") ? 0.75f : 1f); }
    static float AnimalK(AHPlayer p) { return (p.Perk("farmer", 40) ? 0.75f : 1f) * (p.Spec("farmer", "ranch") ? 0.75f : 1f); }
    static float FeedK(AHPlayer p) { return p.Perk("farmer", 20) ? 2f : 1f; }
    public static double CropDur(AHPlayer p, AHField f) { return AHJson.N(Crop(f.crop), "min", 6) * FarmMin * (f.water ? 1 : 2) * (f.bm ? 0.7 : 1) * GrowK(p); }

    // ---------- comforts (economy.json COMFORTS) ----------
    public static List<object> Comforts { get { return AHDB.List("economy", "COMFORTS"); } }
    public static int ComfortPts(AHPlayer p)
    {
        if (p.home == null) return 0; int n = 0;
        var cs = Comforts; if (cs != null) foreach (var c in cs) if (p.home.comf.Contains(AHJson.S(c, "id"))) n += (int)AHJson.N(c, "pts", 1);
        return n;
    }
    public static long SleepCd(AHPlayer p) { return (60 - Math.Min(30, ComfortPts(p) * 2)) * 60000L; }

    // ---------- rested XP (web restCap / addRested / restUpdate) ----------
    public static long LevelStep(AHPlayer p) { var t = AHDB.ClAt; int L = p.level; return L + 1 < t.Length ? t[L + 1] - t[L] : 1000; }
    public static long RestCap(AHPlayer p) { return (long)Math.Round(LevelStep(p) * 1.5 * (1 + ComfortPts(p) * 0.04)); }
    static float restT;
    public static void RestTick(AHGame g, float dt)
    {
        restT += dt; if (restT < 10f) return; restT = 0f;
        var p = g.player; if (p == null || p.dead || AHDungeon.IsDungeon(AHGame.AreaId)) return;
        bool home = AHGame.AreaId == Area;
        if ((g.InTown(p.transform.position) || home) && Time.time - p.LastHurt > 20f)
            p.rested = Math.Min(RestCap(p), p.rested + (long)Math.Round(10.0 / 3600 * LevelStep(p) * 0.05 * (home ? 1 + ComfortPts(p) * 0.15 : 1)));
    }

    // ---------- time passes on the farm, even while you are away (web farmTick, stallTick) ----------
    public static void Tick(AHGame g)
    {
        var p = g.player; var H = p != null ? p.home : null; if (H == null) return;
        long t = NowMs;
        foreach (var f in H.fields)
        {
            if (string.IsNullOrEmpty(f.crop)) continue;
            if (f.prog < 1f)
            {
                f.prog = Mathf.Min(1f, f.prog + (float)(Math.Max(0, t - f.last) / CropDur(p, f)));
                if (f.prog >= 1f && AHGame.AreaId == Area) g.ui.Toast(AHJson.S(Crop(f.crop), "name") + " in " + AHJson.S(Plot(f.id), "name") + " is ready to harvest!", 3f);
            }
            f.last = t;
        }
        foreach (var pen in H.pens)
        {
            var pl = Plot(pen.id); var A = Animal(AHJson.S(pl, "animal"));
            if (pen.n <= 0) { pen.last = t; pen.acc = 0; continue; }
            bool feeds = AHJson.A(A, "feed") != null;
            long end = feeds ? Math.Min(t, pen.fed) : t;
            pen.acc += Math.Max(0, end - pen.last); pen.last = t;
            double every = AHJson.N(A, "every", 10) * FarmMin * AnimalK(p);
            while (pen.acc >= every) { pen.acc -= (long)every; pen.ready = Math.Min(pen.n * 3, pen.ready + pen.n); }
            if (pen.ready >= pen.n * 3) pen.acc = 0;
        }
        // the market stall: one sale about every 3 minutes
        if (H.stall.Count == 0) H.stallLast = t;
        while (t - H.stallLast >= 180000 && H.stall.Count > 0)
        {
            H.stallLast += 180000; int i = UnityEngine.Random.Range(0, H.stall.Count); var s = H.stall[i];
            H.stallCoins += StallPrice(s.id); H.stallSold++; if (--s.n <= 0) H.stall.RemoveAt(i);
        }
        if (AHGame.AreaId == Area && AHHomeView.I != null) AHHomeView.I.Refresh();
    }
    public static long StallPrice(string id)
    {
        var it = AHItems.Get(id); bool sold = AHAuction.ShopSold(id);
        float k = sold ? AHShops.Markup * AHShops.Resale : (it != null && it.rarity == "crafted" ? 0.22f : 0.4f) * 1.5f;
        return Math.Max(1, (long)Math.Round(AHComp.Value(id) * k * AHDB.CU));
    }

    // no grass on the plots of your land (fields, pens, workshops, the house)
    static List<Rect> plotRects;
    public static bool NoGrass(float mx, float mz)
    {
        if (AHGame.AreaId != Area) return false;
        if (plotRects == null) { plotRects = new List<Rect>(); foreach (var p in Plots) plotRects.Add(new Rect((float)AHJson.N(p, "x") - 20, (float)AHJson.N(p, "y") - 20, (float)AHJson.N(p, "w") + 40, (float)AHJson.N(p, "h") + 40)); }
        Vector2 w = new Vector2(mx / AHDB.S, mz / AHDB.S);
        foreach (var r in plotRects) if (r.Contains(w)) return true;
        return false;
    }

    public static bool CanPay(AHPlayer p, object cost)
    {
        if (p.bag.money < (long)AHJson.N(cost, "money", 0) * AHDB.CU) return false;
        var m = AHJson.O(cost, "mats") as Dictionary<string, object>;
        if (m != null) foreach (var kv in m) if (p.bag.Count(kv.Key) < (int)(double)kv.Value) return false;
        return true;
    }
    public static bool Pay(AHPlayer p, object cost)
    {
        if (!CanPay(p, cost)) return false;
        p.bag.money -= (long)AHJson.N(cost, "money", 0) * AHDB.CU;
        var m = AHJson.O(cost, "mats") as Dictionary<string, object>;
        if (m != null) foreach (var kv in m) p.bag.Take(kv.Key, (int)(double)kv.Value);
        p.bag.Touch(); return true;
    }
    public static string CostText(AHPlayer p, object cost)
    {
        var l = new List<string>(); long mo = (long)AHJson.N(cost, "money", 0);
        if (mo > 0) l.Add((p.bag.money >= mo * AHDB.CU ? "" : "<color=#ff8a7a>") + AHItems.MoneyText(mo * AHDB.CU) + (p.bag.money >= mo * AHDB.CU ? "" : "</color>"));
        var m = AHJson.O(cost, "mats") as Dictionary<string, object>;
        if (m != null) foreach (var kv in m) { int need = (int)(double)kv.Value, have = p.bag.Count(kv.Key); var it = AHItems.Get(kv.Key); l.Add((have >= need ? "" : "<color=#ff8a7a>") + have + "/" + need + " " + (it != null ? it.name : kv.Key) + (have >= need ? "" : "</color>")); }
        return l.Count > 0 ? string.Join(" · ", l.ToArray()) : "Free";
    }

    // ---------- going home and back (web travelHome; the sign by the gate takes you back) ----------
    public static void TravelHome(AHGame g)
    {
        var p = g.player;
        if (p.home == null) { g.ui.Toast("You need a homestead deed first. Land Agents in Varrow and Ashen Hollow sell them."); return; }
        if (AHGame.AreaId == Area) return;
        Vector2 w = g.ToWeb(p.transform.position);
        p.home.fromArea = AHGame.AreaId; p.home.fromX = w.x; p.home.fromZ = w.y;
        var a = AHJson.O(HOME, "arrive");
        g.Travel(Area, (float)AHJson.N(a, "x") * AHDB.S, (float)AHJson.N(a, "y") * AHDB.S, 0f);
    }
    public static void Leave(AHGame g)
    {
        var p = g.player; var H = p.home;
        if (H != null && !string.IsNullOrEmpty(H.fromArea) && H.fromArea != Area) g.Travel(H.fromArea, H.fromX, H.fromZ, 0f);
        else { var sp = AHDB.Table("world", "SPAWN"); g.Travel("meadow", (float)AHJson.N(sp, "x", 1000) * AHDB.S, (float)AHJson.N(sp, "y", 1000) * AHDB.S, 0f); }
    }

    public static void Harvest(AHGame g, AHField f)
    {
        var p = g.player; var c = Crop(f.crop); string id = AHJson.S(c, "id");
        int n = (int)AHJson.N(c, "yld", 4) + (f.comp ? 2 : 0) + p.Skill("farming") / 5 + (UnityEngine.Random.value < 0.5f ? 1 : 0) + (p.Perk("farmer", 10) ? 1 : 0);
        if (p.Perk("farmer", 30) && UnityEngine.Random.value < 0.2f) { n *= 2; g.ui.Toast("Bumper crop! Double harvest."); }
        var need = new List<string> { id, id + "_seed" }; if (id == "wheat") need.Add("straw");
        if (!p.bag.Fits(need)) { g.ui.Toast("Your bag is full. Make room for the harvest."); return; }
        p.bag.Add(id, n);
        if (id == "wheat") p.bag.Add("straw", Mathf.CeilToInt(n / 2f));
        if (p.Spec("farmer", "crops") || UnityEngine.Random.value < 0.4f + (p.Skill("farming") >= 10 ? 0.2f : 0f)) p.bag.Add(id + "_seed", 1);
        p.GainXp("farming", Mathf.RoundToInt((float)AHJson.N(c, "xp", 10) * n * 1.5f), false);
        g.quests.Event("gather", id, n, g);
        g.ui.Toast("You harvest " + n + " " + AHJson.S(c, "name").ToLowerInvariant() + (id == "wheat" ? " and some straw" : "") + ".", 3f);
        p.home.fields.Remove(f);
    }
}

// the homestead you walk around in: plots, houses, crops, pens, animals, workshops and comforts, built from your state
public class AHHomeView : MonoBehaviour
{
    public static AHHomeView I;
    AHGame g;
    readonly Dictionary<string, GameObject> plotGo = new Dictionary<string, GameObject>();
    readonly Dictionary<string, AHSpot> spots = new Dictionary<string, AHSpot>();
    readonly Dictionary<string, string> sig = new Dictionary<string, string>();
    readonly List<Rect> blocks = new List<Rect>();
    readonly List<Transform> animals = new List<Transform>();
    readonly List<Vector4> animalTo = new List<Vector4>();   // target x, z, wait, pen index
    readonly List<object> animalPen = new List<object>();
    readonly Dictionary<Transform, AHAnim> animalAnim = new Dictionary<Transform, AHAnim>();   // the real farm beasts (cow, pig, sheep, goat)
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    float tick;

    public static void Setup(AHGame g)
    {
        I = null;
        if (AHGame.AreaId != AHHome.Area) return;
        var v = new GameObject("Homestead").AddComponent<AHHomeView>();
        v.g = g; I = v;
        v.Build();
    }

    static Material M(int hex)
    {
        string k = hex.ToString("x6"); Material m;
        if (mats.TryGetValue(k, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", AHGame.Hex(hex).linear); m.SetFloat("_Smoothness", 0.15f);
        mats[k] = m; return m;
    }
    static GameObject Prim(PrimitiveType t, Transform par, Vector3 pos, Vector3 size, int col, Vector3 rot = default(Vector3))
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(t), t); Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(par, false); o.transform.localPosition = pos; o.transform.localScale = size; o.transform.localRotation = Quaternion.Euler(rot);
        o.GetComponent<Renderer>().sharedMaterial = M(col); return o;
    }
    const int Wood = 0x8a6238, Dark = 0x5a3f28, Stone = 0x9a948a, Red = 0xa8342a, Straw = 0xe8d08a, White = 0xf2efe6, Pink = 0xf0a8a0, Black = 0x2a2522, Green = 0x4a9a3a, Leaf = 0x3f7a2e, Soil = 0x5a3f24, Yellow = 0xf0c030, Canvas = 0xe8dcc0, RoofB = 0x3a5a8a;

    // a KayKit building stood on a plot: its widest side fitted to 'span' metres, centred, its floor on the ground
    static GameObject Model(Transform par, string file, float span)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/" + file); if (pf == null) return null;
        var go = Instantiate(pf); go.name = file;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Destroy(go); return null; }
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        float k = span / Mathf.Max(0.1f, Mathf.Max(b.size.x, b.size.z));
        go.transform.SetParent(par, false);
        go.transform.localScale = Vector3.one * k;
        go.transform.localPosition = new Vector3(-b.center.x * k, -b.min.y * k, -b.center.z * k);
        AHModel.SetShadows(go);
        return go;
    }

    float S { get { return AHDB.S; } }
    Vector3 P(float wx, float wy) { return g.W(wx * S, wy * S); }
    // a group placed at a web point, its local axes the web's (x east, z south)
    Transform Group(string name, float wx, float wy)
    {
        var t = new GameObject(name).transform; t.SetParent(transform, false); t.position = P(wx, wy);
        Vector3 ex = g.W(1f, 0f) - g.W(0f, 0f), ez = g.W(0f, 1f) - g.W(0f, 0f);
        t.rotation = Quaternion.LookRotation(ez.normalized, Vector3.up);
        if (Vector3.Dot(Vector3.Cross(Vector3.up, ez), ex) < 0f) t.localScale = new Vector3(-1f, 1f, 1f);
        return t;
    }

    void Build()
    {
        var p = g.player;
        foreach (var pl in AHHome.Plots) BuildPlot(pl);
        BuildComforts();
        // the sign by the gate: back to where you came from
        var sgn = AHJson.O(AHHome.HOME, "sign");
        var s = new AHSpot { kind = "use", type = "home", name = "Leave homestead", pos = P((float)AHJson.N(sgn, "x"), (float)AHJson.N(sgn, "y")), r = 0.5f, reach = 95f * S };
        s.use = () => AHHome.Leave(g); AHGather.Spots.Add(s);
        // the well: water a field you stand next to from here
        Refresh();
    }

    string Sig(object pl)
    {
        var p = g.player; var H = p.home; string id = AHJson.S(pl, "id"), kind = AHJson.S(pl, "kind");
        if (H == null) return "none";
        if (kind == "house") return "h" + H.tier;
        if (!AHHome.Built(H, id)) return "stake" + H.tier;
        if (kind == "field") { var f = AHHome.Field(H, id); return f == null ? "soil" : "crop:" + f.crop + ":" + f.water; }
        if (kind == "pen") return "pen" + AHHome.Pen(H, id).n;
        return "station";
    }

    // (re)build what stands on a plot when it changes
    void BuildPlot(object pl)
    {
        var p = g.player; var H = p.home;
        string id = AHJson.S(pl, "id"), kind = AHJson.S(pl, "kind"), sg = Sig(pl);
        string old; if (sig.TryGetValue(id, out old) && old == sg) return;
        sig[id] = sg;
        GameObject go; if (plotGo.TryGetValue(id, out go) && go != null) Destroy(go);
        float x = (float)AHJson.N(pl, "x"), y = (float)AHJson.N(pl, "y"), w = (float)AHJson.N(pl, "w"), h = (float)AHJson.N(pl, "h"), cx = (float)AHJson.N(pl, "cx"), cy = (float)AHJson.N(pl, "cy");
        var t = Group(id, cx, cy); plotGo[id] = t.gameObject;
        float W = w * S, D = h * S;
        bool built = AHHome.Built(H, id);
        RemoveAnimals(id);
        if (kind == "house" && H != null)
        {
            int tier = H.tier;
            if (tier == 1)
            {
                // a proper canvas ridge tent with a log seat and a ring of stones for the fire
                var tent = AHTents.Ridge(t, id.GetHashCode()); tent.transform.localScale = Vector3.one * 1.45f;
                Prim(PrimitiveType.Cylinder, t, new Vector3(2.6f, 0.22f, 2.2f), new Vector3(0.44f, 0.75f, 0.44f), Dark, new Vector3(0, 30, 90));
                for (int i = 0; i < 7; i++) { float a = i * Mathf.PI * 2 / 7; Prim(PrimitiveType.Sphere, t, new Vector3(-0.2f + Mathf.Sin(a) * 0.55f, 0.08f, 3.6f + Mathf.Cos(a) * 0.55f), new Vector3(0.28f, 0.18f, 0.26f), Stone); }
                Prim(PrimitiveType.Cylinder, t, new Vector3(-0.2f, 0.03f, 3.6f), new Vector3(0.7f, 0.02f, 0.7f), Black);
            }
            else if (tier == 2 && Model(t, "kk_building_home_A_red", 9.5f) != null) { }
            else if (tier >= 3 && Model(t, "kk_building_home_B_red", 13f) != null)
            {
                // a manor: the big house, a round tower at one corner and a cottage wing at the other
                var tw = Model(t, "kk_building_tower_A_red", 4.6f); if (tw != null) tw.transform.localPosition += new Vector3(7.6f, 0, -1.5f);
                var wg = Model(t, "kk_building_home_A_red", 7f); if (wg != null) wg.transform.localPosition += new Vector3(-9f, 0, 0.5f);
            }
            else if (tier == 2)
            {
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.6f, 0), new Vector3(10, 3.2f, 7), Wood);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.2f, 0), new Vector3(10.2f, 0.4f, 7.2f), Dark);
                Roof(t, new Vector3(0, 3.2f, 0), 11f, 7.8f, 3.2f, Red);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.15f, 3.52f), new Vector3(1.4f, 2.3f, 0.1f), Dark);
                Prim(PrimitiveType.Cube, t, new Vector3(3.4f, 4.8f, -1.4f), new Vector3(0.9f, 3f, 0.9f), Stone);
                foreach (float wx in new[] { -3f, 3f }) Glow(t, new Vector3(wx, 1.9f, 3.52f), new Vector3(1.3f, 1f, 0.08f));
            }
            else
            {
                Prim(PrimitiveType.Cube, t, new Vector3(0, 2.2f, 0), new Vector3(18, 4.4f, 11), Stone);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 4.5f, 0), new Vector3(18.3f, 0.35f, 11.3f), Dark);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 6.3f, -0.5f), new Vector3(12, 3.4f, 9), Stone);
                Roof(t, new Vector3(0, 8f, -0.5f), 13f, 10f, 4f, RoofB);
                Roof(t, new Vector3(6.2f, 4.7f, 0), 5.5f, 5.5f, 2.4f, RoofB); Roof(t, new Vector3(-6.2f, 4.7f, 0), 5.5f, 5.5f, 2.4f, RoofB);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.5f, 5.56f), new Vector3(2.2f, 3f, 0.12f), Dark);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 3.6f, 6.6f), new Vector3(5, 0.3f, 2.6f), Stone);
                foreach (float wx in new[] { -2.2f, 2.2f }) Prim(PrimitiveType.Cylinder, t, new Vector3(wx, 1.8f, 7.6f), new Vector3(0.48f, 1.8f, 0.48f), White);
                foreach (float wx in new[] { -6.5f, -4f, 4f, 6.5f }) Glow(t, new Vector3(wx, 2.4f, 5.56f), new Vector3(1.4f, 1.4f, 0.08f));
                foreach (float wx in new[] { -3.5f, 0f, 3.5f }) Glow(t, new Vector3(wx, 6.6f, 4.02f), new Vector3(1.2f, 1.2f, 0.08f));
            }
        }
        else if (!built || H == null)
        {
            // a stake and a sign: build here
            // a signpost: two posts, a plank with a darker frame, and the plot pegged out with rope
            foreach (float sx in new[] { -0.5f, 0.5f }) Prim(PrimitiveType.Cylinder, t, new Vector3(sx, 0.7f, 0), new Vector3(0.1f, 0.7f, 0.1f), Dark);
            Prim(PrimitiveType.Cube, t, new Vector3(0, 1.15f, 0.07f), new Vector3(1.35f, 0.62f, 0.06f), Dark);
            Prim(PrimitiveType.Cube, t, new Vector3(0, 1.15f, 0.1f), new Vector3(1.2f, 0.48f, 0.04f), Straw);
            Prim(PrimitiveType.Cube, t, new Vector3(0, 1.5f, 0.06f), new Vector3(1.5f, 0.08f, 0.16f), Dark);
            var cs = new[] { new Vector2(-W / 2, -D / 2), new Vector2(W / 2, -D / 2), new Vector2(W / 2, D / 2), new Vector2(-W / 2, D / 2) };
            for (int i = 0; i < 4; i++)
            {
                Vector2 c = cs[i], c2 = cs[(i + 1) % 4];
                Prim(PrimitiveType.Cylinder, t, new Vector3(c.x, 0.25f, c.y), new Vector3(0.09f, 0.25f, 0.09f), Wood);
                Vector2 mid = (c + c2) * 0.5f, d = c2 - c;
                Prim(PrimitiveType.Cube, t, new Vector3(mid.x, 0.42f, mid.y), new Vector3(0.025f, 0.025f, d.magnitude), Straw, new Vector3(0, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, 0));
            }
        }
        else if (kind == "field")
        {
            var f = AHHome.Field(H, id);
            int soil = f != null && f.water ? 0x4a321e : 0x6a4a2a;
            Prim(PrimitiveType.Cube, t, new Vector3(0, 0.03f, 0), new Vector3(W, 0.06f, D), soil);
            // ridged furrows: long mounds of earth half sunk in the bed, and a plank edging round it
            for (float v = -D / 2 + 1.2f; v < D / 2 - 0.4f; v += 1.6f)
            {
                var ridge = new GameObject("Ridge"); ridge.transform.SetParent(t, false); ridge.transform.localPosition = new Vector3(0, 0.06f, v);
                ridge.AddComponent<MeshFilter>().sharedMesh = RidgeMesh(W - 1.0f); ridge.AddComponent<MeshRenderer>().sharedMaterial = M(Shade(soil, 0.82f));
            }
            foreach (float sz in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, t, new Vector3(0, 0.1f, sz * (D / 2 - 0.06f)), new Vector3(W, 0.2f, 0.12f), Wood);
            foreach (float sx in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, t, new Vector3(sx * (W / 2 - 0.06f), 0.1f, 0), new Vector3(0.12f, 0.2f, D), Wood);
            foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, t, new Vector3(sx * (W / 2 - 0.06f), 0.15f, sz * (D / 2 - 0.06f)), new Vector3(0.18f, 0.3f, 0.18f), Dark);
            if (f != null && !string.IsNullOrEmpty(f.crop))
            {
                var c = AHHome.Crop(f.crop); var rng = new System.Random(id.GetHashCode());
                for (int r = 0; r < 3; r++) for (int k = 0; k < 4; k++)
                    {
                        var plant = new GameObject("Plant").transform; plant.SetParent(t, false);
                        plant.localPosition = new Vector3(-W / 2 + 2f + k * (W - 4f) / 3f + (float)(rng.NextDouble() - 0.5) * 0.6f, 0.06f, -D / 2 + 2.2f + r * (D - 4.4f) / 2f + (float)(rng.NextDouble() - 0.5) * 0.5f);
                        plant.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                        PlantModel(plant, c);
                    }
            }
        }
        else if (kind == "pen")
        {
            Fence(t, W, D, id == "apiary");
            if (id == "coop") Hut(t, new Vector3(-W / 2 + 1.8f, 0, -D / 2 + 1.5f), 2.6f, 2.0f, 1.6f, Wood, Red);
            if (id == "pond") { Hut(t, new Vector3(-W / 2 + 1.2f, 0, -D / 2 + 1.1f), 1.6f, 1.4f, 1.0f, Wood, Dark); }
            if (id == "sty") { Prim(PrimitiveType.Cylinder, t, new Vector3(0.8f, 0.02f, 0.6f), new Vector3(4.4f, 0.02f, 4.4f), 0x5a3a22); Hut(t, new Vector3(-W / 2 + 1.8f, 0, -D / 2 + 1.4f), 2.6f, 2f, 1.5f, Wood, Dark); }
            if (id == "barn") { Hut(t, new Vector3(-W / 2 + 3.2f, 0, -D / 2 + 2.4f), 5.2f, 3.8f, 3.2f, Red, Dark); for (int i = 0; i < 2; i++) Prim(PrimitiveType.Cylinder, t, new Vector3(W / 2 - 1.2f - i * 1.4f, 0.5f, D / 2 - 1f), new Vector3(1.2f, 0.5f, 1.2f), Straw, new Vector3(0, 0, 90)); }
            if (id == "pasture" || id == "goatpen") Hut(t, new Vector3(-W / 2 + 1.9f, 0, -D / 2 + 1.3f), 2.8f, 1.8f, 1.6f, Wood, Leaf);
            if (id == "apiary") for (int i = 0; i < 3; i++) { float hx = -W / 2 + 2 + i * 3; Prim(PrimitiveType.Cube, t, new Vector3(hx, 0.3f, 0), new Vector3(1f, 0.25f, 0.9f), Dark); Prim(PrimitiveType.Cube, t, new Vector3(hx, 0.9f, 0), new Vector3(0.9f, 0.9f, 0.8f), White); Prim(PrimitiveType.Cube, t, new Vector3(hx, 1.4f, 0), new Vector3(1.05f, 0.12f, 0.95f), RoofB); }
            var pen = AHHome.Pen(H, id); string an = AHJson.S(pl, "animal");
            for (int i = 0; i < (an == "bee" ? Mathf.Min(12, pen.n * 3) : pen.n); i++) SpawnAnimal(pl, an, x, y, w, h);
        }
        else if (kind == "station")
        {
            StationModel(t, id);
        }
        foreach (var r in t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        // collisions: the house and the pen fences
        RebuildBlocks();
        // what the big button does here
        AHSpot s; spots.TryGetValue(id, out s);
        if (s != null) AHGather.Spots.Remove(s);
        if (kind == "station" && built && id != "stall") s = new AHSpot { kind = id, type = id, name = AHJson.S(pl, "name"), pos = P(cx, cy), r = 0.5f, reach = 70f * S };
        else
        {
            string pid = id; object ppl = pl;
            s = new AHSpot { kind = "use", type = "plot", name = Label(pl), pos = P(cx, cy), r = 0.5f, reach = Mathf.Max(W, D) * 0.5f + 1.2f, use = () => g.ui.OpenFarmPlot(pid) };
            if (kind == "house" && H != null) s.reach = (H.tier == 1 ? 3.5f : H.tier == 2 ? 7f : 11f);
        }
        spots[id] = s; AHGather.Spots.Add(s);
    }

    string Label(object pl)
    {
        var H = g.player.home; string id = AHJson.S(pl, "id"), kind = AHJson.S(pl, "kind");
        if (kind == "house") return H != null ? AHJson.S(AHHome.Tier(H.tier), "name", "House") : "Build a house";
        if (!AHHome.Built(H, id)) return "Build: " + AHJson.S(pl, "name");
        if (kind == "field") { var f = AHHome.Field(H, id); return f == null ? "Farm" : f.prog >= 1f ? "Harvest" : "Farm"; }
        if (kind == "pen") return AHHome.Pen(H, id).ready > 0 ? "Collect" : "Animals";
        if (id == "stall") return "Market stall";
        return AHJson.S(pl, "name");
    }

    void RebuildBlocks()
    {
        foreach (var b in blocks) g.doors.Remove(b);
        blocks.Clear();
        var H = g.player.home; if (H == null) return;
        float S = AHDB.S;
        foreach (var pl in AHHome.Plots)
        {
            string id = AHJson.S(pl, "id"), kind = AHJson.S(pl, "kind");
            float x = (float)AHJson.N(pl, "x"), y = (float)AHJson.N(pl, "y"), w = (float)AHJson.N(pl, "w"), h = (float)AHJson.N(pl, "h"), cx = (float)AHJson.N(pl, "cx"), cy = (float)AHJson.N(pl, "cy");
            if (kind == "house") { float fw = H.tier == 1 ? 150 : H.tier == 2 ? 420 : 740, fh = H.tier == 1 ? 150 : H.tier == 2 ? 300 : 470; blocks.Add(g.MapRect((cx - fw / 2) * S, (cy - fh / 2) * S, fw * S, fh * S)); }
            else if (kind == "pen" && id != "apiary" && AHHome.Built(H, id))
            {
                const float tk = 14; // the fence, with a gate gap on the south side
                blocks.Add(g.MapRect(x * S, (y - tk / 2) * S, w * S, tk * S));
                blocks.Add(g.MapRect((x - tk / 2) * S, y * S, tk * S, h * S));
                blocks.Add(g.MapRect((x + w - tk / 2) * S, y * S, tk * S, h * S));
            }
        }
        foreach (var b in blocks) g.doors.Add(b);
    }

    // ---------- little models (web houseModel, penModel, stationModel, plantModel, animalModel) ----------
    void Cone(Transform t, Vector3 at, float r, float h, int col)
    {
        // a four-sided cone (tent / roof) from a scaled, turned cube pyramid stand-in
        var o = new GameObject("Cone"); o.transform.SetParent(t, false); o.transform.localPosition = at;
        var mf = o.AddComponent<MeshFilter>(); var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = M(col);
        mf.sharedMesh = Pyramid(r * 1.414f, r * 1.414f, h);
    }
    void Roof(Transform t, Vector3 at, float w, float d, float h, int col)
    {
        var o = new GameObject("Roof"); o.transform.SetParent(t, false); o.transform.localPosition = at;
        var mf = o.AddComponent<MeshFilter>(); var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = M(col);
        mf.sharedMesh = Pyramid(w, d, h);
    }
    static Mesh Pyramid(float w, float d, float h)
    {
        var m = new Mesh(); float a = w / 2, b = d / 2;
        Vector3 top = new Vector3(0, h, 0), p0 = new Vector3(-a, 0, -b), p1 = new Vector3(a, 0, -b), p2 = new Vector3(a, 0, b), p3 = new Vector3(-a, 0, b);
        var v = new List<Vector3>(); var tri = new List<int>();
        Action<Vector3, Vector3, Vector3> f = (x, y, z) => { int i = v.Count; v.Add(x); v.Add(y); v.Add(z); tri.Add(i); tri.Add(i + 1); tri.Add(i + 2); };
        f(p0, top, p1); f(p1, top, p2); f(p2, top, p3); f(p3, top, p0); f(p0, p1, p2); f(p0, p2, p3);
        m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
    void Glow(Transform t, Vector3 at, Vector3 size)
    {
        var o = Prim(PrimitiveType.Cube, t, at, size, 0xffe6a0);
        var m = new Material(M(0xffe6a0)); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", AHGame.Hex(0xffc860).linear * 1.6f); o.GetComponent<Renderer>().sharedMaterial = m;
    }
    // a little farm building: plank walls on a stone footing, corner posts, a gabled roof with overhanging eaves and a
    // ridge beam, a framed plank door and a shuttered window (coop, sty, barn, shelter, kennel)
    void Hut(Transform t, Vector3 at, float w, float d, float h, int wall, int roof)
    {
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, 0.1f, 0), new Vector3(w + 0.12f, 0.2f, d + 0.12f), Stone);
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, 0.2f + (h - 0.2f) / 2, 0), new Vector3(w, h - 0.2f, d), wall);
        int seam = Shade(wall, 0.78f);
        for (float y = 0.5f; y < h - 0.1f; y += 0.32f) { Prim(PrimitiveType.Cube, t, at + new Vector3(0, y, d / 2 + 0.005f), new Vector3(w, 0.025f, 0.02f), seam); Prim(PrimitiveType.Cube, t, at + new Vector3(0, y, -d / 2 - 0.005f), new Vector3(w, 0.025f, 0.02f), seam); }
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, t, at + new Vector3(sx * w / 2, h / 2, sz * d / 2), new Vector3(0.16f, h, 0.16f), Dark);
        // the gable: end triangles in the wall colour, two roof slabs and a ridge beam
        float rise = Mathf.Min(w, d) * 0.42f + 0.2f, ov = 0.25f;
        var gt = new GameObject("Gable"); gt.transform.SetParent(t, false); gt.transform.localPosition = at + new Vector3(0, h, 0);
        gt.AddComponent<MeshFilter>().sharedMesh = GableEnds(w, d, rise); gt.AddComponent<MeshRenderer>().sharedMaterial = M(wall);
        float slope = Mathf.Atan2(rise, w / 2) * Mathf.Rad2Deg;
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 ridge = new Vector3(0, h + rise + 0.05f, 0), eave = new Vector3(sx * (w / 2 + ov), h + 0.05f - ov * rise / (w / 2), 0);
            Prim(PrimitiveType.Cube, t, at + (ridge + eave) * 0.5f, new Vector3((eave - ridge).magnitude + 0.06f, 0.1f, d + ov * 2), roof, new Vector3(0, 0, -sx * slope));
        }
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, h + rise + 0.02f, 0), new Vector3(0.16f, 0.14f, d + ov * 2 + 0.05f), Dark);
        // a door with a frame and a cross brace, and a shuttered window beside it
        float dw = Mathf.Min(0.95f, w * 0.32f), dh = Mathf.Min(1.9f, h * 0.78f);
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, 0.2f + dh / 2, d / 2 + 0.03f), new Vector3(dw + 0.16f, dh + 0.1f, 0.05f), Dark);
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, 0.2f + dh / 2, d / 2 + 0.05f), new Vector3(dw, dh, 0.04f), Shade(Wood, 0.9f));
        Prim(PrimitiveType.Cube, t, at + new Vector3(0, 0.2f + dh / 2, d / 2 + 0.075f), new Vector3(0.07f, Mathf.Sqrt(dw * dw + dh * dh) * 0.95f, 0.02f), Dark, new Vector3(0, 0, Mathf.Atan2(dw, dh) * Mathf.Rad2Deg));
        if (w > 2.2f && h > 1.4f)
        {
            float wx = w / 2 - Mathf.Max(0.45f, w * 0.18f);
            Prim(PrimitiveType.Cube, t, at + new Vector3(wx, h * 0.6f, d / 2 + 0.03f), new Vector3(0.62f, 0.52f, 0.05f), Dark);
            Prim(PrimitiveType.Cube, t, at + new Vector3(wx, h * 0.6f, d / 2 + 0.045f), new Vector3(0.48f, 0.38f, 0.03f), 0x2a2420);
            foreach (float sx in new[] { -1f, 1f }) Prim(PrimitiveType.Cube, t, at + new Vector3(wx + sx * 0.4f, h * 0.6f, d / 2 + 0.05f), new Vector3(0.22f, 0.5f, 0.03f), roof);
        }
    }
    static int Shade(int c, float k) { return ((int)(((c >> 16) & 255) * k) << 16) | ((int)(((c >> 8) & 255) * k) << 8) | (int)((c & 255) * k); }
    // the two triangular gable ends of a roof running along z, w wide, rise high, on top of walls d deep
    static Mesh GableEnds(float w, float d, float rise)
    {
        var m = new Mesh(); float a = w / 2, b = d / 2;
        var v = new List<Vector3> { new Vector3(-a, 0, b), new Vector3(a, 0, b), new Vector3(0, rise, b), new Vector3(a, 0, -b), new Vector3(-a, 0, -b), new Vector3(0, rise, -b) };
        m.SetVertices(v); m.SetTriangles(new[] { 0, 1, 2, 3, 4, 5 }, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    void Fence(Transform t, float W, float D, bool none)
    {
        if (none) return;
        Action<float, float, float, float> side = (x0, z0, x1, z1) =>
        {
            float L = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)); int n = Mathf.Max(2, Mathf.RoundToInt(L / 2.2f));
            for (int i = 0; i <= n; i++) Prim(PrimitiveType.Cube, t, new Vector3(x0 + (x1 - x0) * i / n, 0.5f, z0 + (z1 - z0) * i / n), new Vector3(0.14f, 1f, 0.14f), Wood);
            float ang = -Mathf.Atan2(z1 - z0, x1 - x0) * Mathf.Rad2Deg;
            foreach (float hh in new[] { 0.45f, 0.85f }) Prim(PrimitiveType.Cube, t, new Vector3((x0 + x1) / 2, hh, (z0 + z1) / 2), new Vector3(L, 0.08f, 0.06f), Wood, new Vector3(0, ang, 0));
        };
        side(-W / 2, -D / 2, W / 2, -D / 2); side(W / 2, -D / 2, W / 2, D / 2); side(-W / 2, D / 2, -W / 2, -D / 2);
        side(W / 2, D / 2, 0.9f, D / 2); side(-0.9f, D / 2, -W / 2, D / 2);
    }
    void StationModel(Transform t, string kind)
    {
        if (kind == "oven") { Prim(PrimitiveType.Cube, t, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1f, 1.4f), Stone); Prim(PrimitiveType.Sphere, t, new Vector3(0, 1f, 0), new Vector3(1.5f, 1.1f, 1.5f), Stone); Prim(PrimitiveType.Cube, t, new Vector3(0.4f, 1.6f, -0.3f), new Vector3(0.35f, 1.2f, 0.35f), Stone); Glow(t, new Vector3(0, 0.55f, 0.72f), new Vector3(0.6f, 0.45f, 0.06f)); }
        else if (kind == "mill") { foreach (float yy in new[] { 0.35f, 0.75f }) Prim(PrimitiveType.Cylinder, t, new Vector3(0, yy, 0), new Vector3(1.6f, 0.17f, 1.6f), Stone); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0), new Vector3(0.1f, 1.2f, 0.1f), Dark); Prim(PrimitiveType.Cube, t, new Vector3(0.45f, 1.75f, 0), new Vector3(0.9f, 0.08f, 0.08f), Dark); Prim(PrimitiveType.Cylinder, t, new Vector3(1.2f, 0.35f, 0.5f), new Vector3(0.8f, 0.35f, 0.8f), Straw); }
        else if (kind == "dairy") { Prim(PrimitiveType.Cube, t, new Vector3(0, 0.9f, 0), new Vector3(1.8f, 0.1f, 0.9f), Wood); foreach (float xx in new[] { -0.8f, 0.8f }) Prim(PrimitiveType.Cube, t, new Vector3(xx, 0.45f, 0), new Vector3(0.1f, 0.9f, 0.8f), Dark); Prim(PrimitiveType.Cylinder, t, new Vector3(-1.4f, 0.55f, 0.3f), new Vector3(0.8f, 0.55f, 0.8f), Wood); for (int i = 0; i < 3; i++) Prim(PrimitiveType.Cylinder, t, new Vector3(-0.4f + i * 0.4f, 1.05f, 0), new Vector3(0.56f, 0.1f, 0.56f), Yellow); }
        else if (kind == "spin") { Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1f, 0), new Vector3(1.2f, 0.04f, 1.2f), Wood, new Vector3(90, 0, 0)); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.5f, -0.1f), new Vector3(0.08f, 1f, 0.08f), Dark); Prim(PrimitiveType.Cube, t, new Vector3(0.2f, 0.3f, 0), new Vector3(1.2f, 0.08f, 0.3f), Dark); Prim(PrimitiveType.Sphere, t, new Vector3(0.7f, 1f, 0), new Vector3(0.4f, 0.4f, 0.4f), White); }
        else if (kind == "compost") { Prim(PrimitiveType.Cube, t, new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.8f, 1.2f), Wood); Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.8f, 0), new Vector3(1.4f, 0.5f, 1.0f), 0x4a3a24); }
        else if (kind == "stall")
        {
            // a proper market stall with a striped awning and produce on the counter, crates and a sack beside it
            var st = AHStations.Stall(t, 2.6f, 1.4f, 5); st.transform.localPosition = Vector3.zero;
            var cr = Model(t, "kk_crate_A_big", 0.8f); if (cr != null) cr.transform.localPosition += new Vector3(1.8f, 0, 0.3f);
            var sk = Model(t, "kk_sack", 0.6f); if (sk != null) sk.transform.localPosition += new Vector3(-1.75f, 0, 0.4f);
        }
    }
    void PlantModel(Transform t, object c)
    {
        string look = AHJson.S(c, "look", "stalk");
        Color cc = ColorFromHex(AHJson.S(c, "c", "#6aa84a")); int ci = ((int)(cc.r * 255) << 16) | ((int)(cc.g * 255) << 8) | (int)(cc.b * 255);
        var fruit = new GameObject("Fruit").transform; fruit.SetParent(t, false);
        var rnd = new System.Random(t.GetSiblingIndex() * 31 + look.Length);
        System.Func<float, float, float> R = (a, b) => a + (float)rnd.NextDouble() * (b - a);
        if (look == "stalk")
        {
            // a clump of grain: tall arching blades and stems with heavy seed heads
            for (int i = 0; i < 7; i++) Blade(t, Vector3.zero, i * 51f + R(0, 20), R(10, 30), R(0.7f, 1.15f), 0.07f, 0.35f, Green);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.6f + R(0, 0.5f), lean = R(4, 12); var top = new Vector3(Mathf.Cos(a) * 0.12f, 1.3f, Mathf.Sin(a) * 0.12f);
                Blade(t, new Vector3(top.x * 0.3f, 0, top.z * 0.3f), a * Mathf.Rad2Deg, lean, 1.3f, 0.03f, 0.05f, 0x6a9a3a);
                Prim(PrimitiveType.Capsule, fruit, top + new Vector3(0, 0.05f, 0), new Vector3(0.09f, 0.16f, 0.09f), ci, new Vector3(lean, a * Mathf.Rad2Deg, 0));
            }
        }
        else if (look == "root")
        {
            // a root crop: a feathery green tuft over a fat shoulder showing at the soil
            for (int i = 0; i < 8; i++) Blade(t, Vector3.zero, i * 45f + R(0, 15), R(20, 45), R(0.35f, 0.55f), 0.06f, 0.5f, Leaf);
            Prim(PrimitiveType.Sphere, fruit, new Vector3(0, 0.05f, 0), new Vector3(0.3f, 0.26f, 0.3f), ci);
        }
        else if (look == "leafy")
        {
            // a cabbage: a rosette of broad cupped leaves round a tight head
            for (int i = 0; i < 9; i++) Blade(t, Vector3.zero, i * 40f + R(0, 10), R(45, 65), R(0.32f, 0.42f), 0.24f, 0.6f, i % 2 == 0 ? 0x6aa84a : 0x5a9a42);
            Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.18f, 0), new Vector3(0.4f, 0.34f, 0.4f), 0x7ab85a);
            Prim(PrimitiveType.Sphere, fruit, new Vector3(0, 0.22f, 0), new Vector3(0.46f, 0.4f, 0.46f), ci);
        }
        else if (look == "bush")
        {
            // a berry bush: a mound of leaves on short stems, berries in clusters
            for (int i = 0; i < 12; i++) Blade(t, new Vector3(R(-0.1f, 0.1f), R(0.05f, 0.25f), R(-0.1f, 0.1f)), i * 30f + R(0, 20), R(30, 70), R(0.3f, 0.45f), 0.14f, 0.45f, i % 3 == 0 ? Green : Leaf);
            for (int i = 0; i < 6; i++) { float a = i * 1.05f + R(0, 0.4f), r = R(0.18f, 0.32f); for (int k = 0; k < 3; k++) Prim(PrimitiveType.Sphere, fruit, new Vector3(Mathf.Cos(a) * r + k * 0.04f, R(0.3f, 0.55f), Mathf.Sin(a) * r), Vector3.one * 0.09f, ci); }
        }
        else if (look == "gourd")
        {
            // a pumpkin patch: big flat leaves on the ground, a curling vine and a ribbed gourd
            for (int i = 0; i < 4; i++) Blade(t, Vector3.zero, i * 90f + R(0, 30), R(70, 82), R(0.45f, 0.6f), 0.36f, 0.2f, Leaf);
            var gm = new GameObject("Gourd"); gm.transform.SetParent(fruit, false); gm.transform.localPosition = new Vector3(0.1f, 0.2f, 0.05f);
            gm.AddComponent<MeshFilter>().sharedMesh = Gourd(); gm.AddComponent<MeshRenderer>().sharedMaterial = M(ci); gm.transform.localScale = new Vector3(0.62f, 0.44f, 0.62f);
            Prim(PrimitiveType.Cylinder, fruit, new Vector3(0.1f, 0.44f, 0.05f), new Vector3(0.05f, 0.06f, 0.05f), Dark, new Vector3(10, 0, 8));
        }
        else
        {
            // a flower: a stem with leaves and a ring of petals round the heart
            Blade(t, Vector3.zero, R(0, 360), 4f, 0.8f, 0.03f, 0.05f, Green);
            for (int i = 0; i < 4; i++) Blade(t, new Vector3(0, 0.15f + i * 0.12f, 0), i * 90f, 55f, 0.25f, 0.08f, 0.4f, Leaf);
            for (int i = 0; i < 8; i++) Blade(fruit, new Vector3(0, 0.8f, 0), i * 45f, 70f, 0.16f, 0.09f, 0.2f, ci);
            Prim(PrimitiveType.Sphere, fruit, new Vector3(0, 0.82f, 0), Vector3.one * 0.08f, Yellow);
        }
    }
    // a leaf blade from 'at', turned 'yaw' and leaning 'lean' degrees out, len long, w wide, arching by 'bend'
    void Blade(Transform par, Vector3 at, float yaw, float lean, float len, float w, float bend, int col)
    {
        var o = new GameObject("Leaf"); o.transform.SetParent(par, false); o.transform.localPosition = at; o.transform.localRotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(lean, 0, 0);
        o.transform.localScale = new Vector3(w, len, len);
        o.AddComponent<MeshFilter>().sharedMesh = BladeMesh(Mathf.Round(bend * 10f) / 10f); o.AddComponent<MeshRenderer>().sharedMaterial = M(col);
    }
    static readonly Dictionary<float, Mesh> blades = new Dictionary<float, Mesh>();
    // a unit blade: up +y, 1 long and 1 wide at its widest, curling over toward +z by 'bend'; both faces
    static Mesh BladeMesh(float bend)
    {
        Mesh m; if (blades.TryGetValue(bend, out m) && m != null) return m;
        var v = new List<Vector3>(); var tri = new List<int>(); int n = 6;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n, wid = 0.5f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 0.9f + 0.1f)) * (1f - u * 0.6f), a = u * bend * 1.6f;
            Vector3 c = new Vector3(0, Mathf.Sin(a) / Mathf.Max(0.01f, bend * 1.6f) * (bend > 0.01f ? 1f : 0f) + (bend > 0.01f ? 0f : u), (1f - Mathf.Cos(a)) / Mathf.Max(0.01f, bend * 1.6f) * (bend > 0.01f ? 1f : 0f));
            if (bend <= 0.01f) c = new Vector3(0, u, 0);
            v.Add(c + new Vector3(-wid, 0, 0)); v.Add(c + new Vector3(0, 0, -0.04f * (1f - u))); v.Add(c + new Vector3(wid, 0, 0));
        }
        int f = v.Count; for (int i = 0; i < f; i++) v.Add(v[i]);
        for (int i = 0; i < n; i++)
            for (int k = 0; k < 2; k++)
            {
                int a = i * 3 + k, b = a + 3;
                tri.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 });
                tri.AddRange(new[] { f + a, f + b + 1, f + b, f + a, f + a + 1, f + b + 1 });
            }
        m = new Mesh { name = "blade" }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); blades[bend] = m; return m;
    }
    static Mesh egg;
    // an egg shape, unit size, the fat end toward +z... rounder at the front, drawn to a point at the back
    static Mesh Egg()
    {
        if (egg != null) return egg;
        var v = new List<Vector3>(); var tri = new List<int>(); int seg = 14, rings = 10;
        for (int i = 0; i <= rings; i++)
        {
            float la = Mathf.PI * (i / (float)rings - 0.5f);
            for (int k = 0; k <= seg; k++) { float lo = k * Mathf.PI * 2f / seg; var p = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo)) * 0.5f; float back = Mathf.Clamp01(-p.z * 2f); p.x *= 1f - 0.35f * back; p.y *= 1f - 0.25f * back; p.y += 0.12f * back * back; v.Add(p); }
        }
        for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int p = i * (seg + 1) + k, q = p + seg + 1; tri.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
        egg = new Mesh { name = "egg" }; egg.SetVertices(v); egg.SetTriangles(tri, 0); egg.RecalculateNormals(); egg.RecalculateBounds(); return egg;
    }
    static Mesh gourd;
    // a ribbed pumpkin, unit size, sitting on y = -0.5
    static Mesh Gourd()
    {
        if (gourd != null) return gourd;
        var v = new List<Vector3>(); var tri = new List<int>(); int seg = 20, rings = 10;
        for (int i = 0; i <= rings; i++)
        {
            float la = Mathf.PI * (i / (float)rings - 0.5f);
            for (int k = 0; k <= seg; k++)
            {
                float lo = k * Mathf.PI * 2f / seg, rib = 1f - 0.12f * Mathf.Pow(Mathf.Abs(Mathf.Cos(lo * 4f)), 4f);
                float r = Mathf.Cos(la) * 0.5f * rib, y = Mathf.Sin(la) * 0.5f * (1f - 0.25f * Mathf.Pow(Mathf.Cos(la), 8f));
                v.Add(new Vector3(Mathf.Cos(lo) * r, y, Mathf.Sin(lo) * r));
            }
        }
        for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int p = i * (seg + 1) + k, q = p + seg + 1; tri.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
        gourd = new Mesh { name = "gourd" }; gourd.SetVertices(v); gourd.SetTriangles(tri, 0); gourd.RecalculateNormals(); gourd.RecalculateBounds(); return gourd;
    }
    static readonly Dictionary<float, Mesh> ridges = new Dictionary<float, Mesh>();
    // a long mound of earth along x, L long, rounded at the ends
    static Mesh RidgeMesh(float L)
    {
        L = Mathf.Round(L * 2f) / 2f; Mesh m; if (ridges.TryGetValue(L, out m) && m != null) return m;
        var v = new List<Vector3>(); var tri = new List<int>(); int n = Mathf.Max(4, Mathf.RoundToInt(L * 2)), seg = 6;
        for (int i = 0; i <= n; i++)
        {
            float u = i / (float)n, x = (u - 0.5f) * L, end = Mathf.Sqrt(Mathf.Clamp01(Mathf.Min(u, 1f - u) * L / 0.4f)), wob = 1f + 0.08f * Mathf.Sin(i * 2.3f);
            for (int k = 0; k <= seg; k++) { float a = Mathf.PI * k / seg; v.Add(new Vector3(x, Mathf.Sin(a) * 0.16f * end * wob, Mathf.Cos(a) * 0.32f * Mathf.Max(0.3f, end))); }
        }
        for (int i = 0; i < n; i++) for (int k = 0; k < seg; k++) { int p = i * (seg + 1) + k, q = p + seg + 1; tri.AddRange(new[] { p, q + 1, p + 1, p, q, q + 1 }); }
        m = new Mesh { name = "ridge" }; m.SetVertices(v); m.SetTriangles(tri, 0); m.RecalculateNormals(); m.RecalculateBounds(); ridges[L] = m; return m;
    }
    // test: one of everything the farm builds, in a row from 'at' (for the snapshot sheet)
    public static List<GameObject> Showcase(AHGame g, Vector3 at)
    {
        var v = new GameObject("FarmShowcase").AddComponent<AHHomeView>(); v.enabled = false; v.g = g; var outl = new List<GameObject>(); int n = 0;
        System.Func<string, Transform> Slot = name => { var t = new GameObject(name).transform; t.position = at + Vector3.right * 14f * n++; outl.Add(t.gameObject); return t; };
        v.Hut(Slot("coop"), Vector3.zero, 2.6f, 2.0f, 1.6f, Wood, Red);
        v.Hut(Slot("barn"), Vector3.zero, 5.2f, 3.8f, 3.2f, Red, Dark);
        var crops = AHDB.Table("home", "CROPS") as Dictionary<string, object>;
        var looks = new HashSet<string>();
        if (crops != null) foreach (var kv in crops)
            {
                string lk = AHJson.S(kv.Value, "look", "stalk"); if (!looks.Add(lk)) continue;
                var t = Slot("crop " + lk);
                for (int i = 0; i < 3; i++) { var pl = new GameObject("Plant").transform; pl.SetParent(t, false); pl.localPosition = new Vector3((i - 1) * 1.1f, 0, 0); pl.localScale = Vector3.one * 1.25f; v.PlantModel(pl, kv.Value); }
            }
        foreach (var k in new[] { "cow", "pig", "sheep", "goat", "chicken", "duck" })
        {
            var t = Slot(k); var body = new GameObject("Body").transform; body.SetParent(t, false);
            float len = k == "cow" ? 2.4f : k == "pig" ? 1.35f : k == "sheep" ? 1.3f : k == "goat" ? 1.15f : 0f; AHAnim an;
            if (len > 0f) { AHModel.Spawn(body, "Comp/farm_" + k, len, true, 0f, out an); if (an != null) { an.Play("Idle", true); an.Tick(1f); } } else v.AnimalModel(body, k);
        }
        var st = Slot("stall"); v.StationModel(st, "stall");
        var ct = Slot("cat"); v.ComfortModel(ct, "cat"); var dg = Slot("dog"); v.ComfortModel(dg, "dog");
        foreach (var pa in v.petAnims) pa.Tick(1f);
        return outl;
    }
    readonly List<AHAnim> petAnims = new List<AHAnim>();
    // a real animal model resting on the spot (the cat on its cushion, the dog by its kennel), breathing in its idle
    bool Pet(Transform t, string file, float len, Vector3 at)
    {
        if (Resources.Load<GameObject>("AH/Models/" + file) == null) return false;
        var h = new GameObject("Pet").transform; h.SetParent(t, false); h.localPosition = at; h.localRotation = Quaternion.Euler(0, 160f, 0);
        AHAnim an; AHModel.Spawn(h, file, len, true, 0f, out an);
        if (an != null && an.HasClips) { an.Play(an.Has("Idle_2_HeadLow") ? "Idle_2_HeadLow" : "Idle", true, 0.7f, true); petAnims.Add(an); }
        return true;
    }
    static Color ColorFromHex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.green; }

    void AnimalModel(Transform t, string kind)
    {
        Action<float, int, Vector3, Vector3> sp = (r, col, pos, sc) => Prim(PrimitiveType.Sphere, t, pos, new Vector3(r * 2 * sc.x, r * 2 * sc.y, r * 2 * sc.z), col);
        Action<int, float, float, float, float> legs = (col, w, d, h, th) => { foreach (float x in new[] { -w, w }) foreach (float z in new[] { -d, d }) Prim(PrimitiveType.Cube, t, new Vector3(x, h / 2, z), new Vector3(th, h, th), col); };
        switch (kind)
        {
            case "chicken":
            case "duck":
                {
                    bool duck = kind == "duck"; int body = duck ? 0x8a6a4a : White, head = duck ? 0x2a7a4a : White;
                    // a plump egg-shaped body tipped up at the tail, folded wings, a fan of tail feathers
                    var bo = new GameObject("Body"); bo.transform.SetParent(t, false); bo.transform.localPosition = new Vector3(0, duck ? 0.26f : 0.34f, 0);
                    bo.transform.localRotation = Quaternion.Euler(duck ? -4f : -16f, 0, 0); bo.transform.localScale = duck ? new Vector3(0.42f, 0.32f, 0.62f) : new Vector3(0.38f, 0.36f, 0.48f);
                    bo.AddComponent<MeshFilter>().sharedMesh = Egg(); bo.AddComponent<MeshRenderer>().sharedMaterial = M(body);
                    foreach (int sx in new[] { -1, 1 }) Blade(t, new Vector3(sx * (duck ? 0.19f : 0.17f), duck ? 0.32f : 0.4f, 0.1f), 180f + sx * 8f, 100f, duck ? 0.34f : 0.26f, 0.2f, 0.15f, Shade(body, 0.85f));
                    for (int k = 0; k < (duck ? 3 : 5); k++) Blade(t, new Vector3(0, duck ? 0.3f : 0.42f, -0.2f), 180f + (k - (duck ? 1 : 2)) * 14f, duck ? 60f : 20f, duck ? 0.14f : 0.3f, 0.09f, 0.6f, duck ? body : (k % 2 == 0 ? 0x2a2522 : 0x7a4a22));
                    // the head on a short neck, the beak or bill, the comb and wattle, little eyes
                    var hp = duck ? new Vector3(0, 0.5f, 0.27f) : new Vector3(0, 0.6f, 0.17f);
                    Prim(PrimitiveType.Sphere, t, hp - new Vector3(0, 0.1f, 0.03f), new Vector3(0.13f, 0.2f, 0.13f), duck ? head : body);
                    Prim(PrimitiveType.Sphere, t, hp, Vector3.one * (duck ? 0.17f : 0.16f), head);
                    if (duck) Prim(PrimitiveType.Cube, t, hp + new Vector3(0, -0.03f, 0.11f), new Vector3(0.09f, 0.03f, 0.13f), Yellow);
                    else
                    {
                        Prim(PrimitiveType.Capsule, t, hp + new Vector3(0, -0.01f, 0.09f), new Vector3(0.035f, 0.04f, 0.035f), Yellow, new Vector3(90, 0, 0));
                        for (int k = 0; k < 3; k++) Prim(PrimitiveType.Sphere, t, hp + new Vector3(0, 0.08f + (k == 1 ? 0.02f : 0f), -0.03f + k * 0.035f), new Vector3(0.03f, 0.06f, 0.04f), Red);
                        Prim(PrimitiveType.Sphere, t, hp + new Vector3(0, -0.07f, 0.07f), new Vector3(0.03f, 0.06f, 0.03f), Red);
                    }
                    foreach (int sx in new[] { -1, 1 }) Prim(PrimitiveType.Sphere, t, hp + new Vector3(sx * 0.07f, 0.02f, 0.04f), Vector3.one * 0.025f, Black);
                    foreach (int sx in new[] { -1, 1 }) { Prim(PrimitiveType.Capsule, t, new Vector3(sx * 0.07f, 0.09f, 0.02f), new Vector3(0.025f, 0.09f, 0.025f), duck ? 0xf09a2a : Yellow); Prim(PrimitiveType.Cube, t, new Vector3(sx * 0.07f, 0.01f, 0.06f), new Vector3(duck ? 0.1f : 0.06f, 0.015f, duck ? 0.12f : 0.1f), duck ? 0xf09a2a : Yellow); }
                    break;
                }
            case "pig": sp(0.4f, Pink, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 0.8f, 1.3f)); sp(0.25f, Pink, new Vector3(0, 0.58f, 0.5f), Vector3.one); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.55f, 0.74f), new Vector3(0.2f, 0.04f, 0.2f), 0xe08a88, new Vector3(90, 0, 0)); legs(Pink, 0.22f, 0.3f, 0.3f, 0.12f); break;
            case "cow": Prim(PrimitiveType.Cube, t, new Vector3(0, 0.95f, 0), new Vector3(0.7f, 0.6f, 1.4f), White); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.05f, -0.2f), new Vector3(0.72f, 0.3f, 0.4f), Black); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0.85f), new Vector3(0.4f, 0.42f, 0.5f), White); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.08f, 1.12f), new Vector3(0.32f, 0.18f, 0.1f), Pink); legs(White, 0.25f, 0.5f, 0.65f, 0.14f); break;
            case "sheep": sp(0.42f, White, new Vector3(0, 0.72f, 0), new Vector3(1, 0.85f, 1.25f)); sp(0.28f, White, new Vector3(0, 0.9f, -0.2f), Vector3.one); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.85f, 0.55f), new Vector3(0.24f, 0.3f, 0.3f), Black); legs(Black, 0.18f, 0.28f, 0.45f, 0.09f); break;
            case "goat": Prim(PrimitiveType.Cube, t, new Vector3(0, 0.72f, 0), new Vector3(0.45f, 0.45f, 0.9f), 0xa87a4a); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.02f, 0.55f), new Vector3(0.28f, 0.32f, 0.36f), 0xa87a4a); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.8f, 0.72f), new Vector3(0.08f, 0.15f, 0.06f), White); legs(0x6a4a2a, 0.16f, 0.3f, 0.5f, 0.08f); break;
            case "bee": Prim(PrimitiveType.Sphere, t, new Vector3(0, 0, 0), Vector3.one * 0.12f, 0xffd020); break;
        }
    }

    void SpawnAnimal(object pl, string kind, float x, float y, float w, float h)
    {
        var a = new GameObject("Animal_" + kind).transform; a.SetParent(transform, false);
        float ax = x + 60 + UnityEngine.Random.value * Mathf.Max(1, w - 120), ay = y + 60 + UnityEngine.Random.value * Mathf.Max(1, h - 120);
        a.position = P(ax, ay) + (kind == "bee" ? Vector3.up * 0.9f : Vector3.zero);
        a.rotation = Quaternion.Euler(0, UnityEngine.Random.value * 360f, 0);
        var body = new GameObject("Body").transform; body.SetParent(a, false); body.localRotation = Quaternion.Euler(0, g.ModelYaw, 0);
        float len = kind == "cow" ? 2.4f : kind == "pig" ? 1.35f : kind == "sheep" ? 1.3f : kind == "goat" ? 1.15f : 0f;
        if (len > 0f && Resources.Load<GameObject>("AH/Models/Comp/farm_" + kind) != null)
        {
            // a real animal: rigged, walking and grazing (made for Ashen Hollow from Quaternius animals, CC0)
            body.localRotation = Quaternion.identity; AHAnim an;
            AHModel.Spawn(body, "Comp/farm_" + kind, len, true, 0f, out an);
            if (an != null && an.HasClips) { an.Play("Idle", true, 0.8f + UnityEngine.Random.value * 0.4f, true); animalAnim[a] = an; }
        }
        else AnimalModel(body, kind);
        foreach (var r in a.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        animals.Add(a); animalTo.Add(new Vector4(ax, ay, UnityEngine.Random.value * 3f, 0)); animalPen.Add(pl);
    }
    void RemoveAnimals(string pen)
    {
        for (int i = animals.Count - 1; i >= 0; i--)
            if (AHJson.S(animalPen[i], "id") == pen) { AHAnim an; if (animals[i] != null && animalAnim.TryGetValue(animals[i], out an)) { an.Dispose(); animalAnim.Remove(animals[i]); } if (animals[i] != null) Destroy(animals[i].gameObject); animals.RemoveAt(i); animalTo.RemoveAt(i); animalPen.RemoveAt(i); }
    }

    // ---------- comforts on your land ----------
    readonly Dictionary<string, GameObject> comfGo = new Dictionary<string, GameObject>();
    public void BuildComforts()
    {
        var H = g.player.home; var cs = AHHome.Comforts; if (cs == null) return;
        var hz = AHDB.List("zones", "ALLZ"); float zx = 185000, zy = 0;
        if (hz != null) foreach (var z in hz) if (AHJson.S(z, "id") == AHHome.Area) { zx = (float)AHJson.N(z, "x"); zy = (float)AHJson.N(z, "y"); }
        foreach (var c in cs)
        {
            string id = AHJson.S(c, "id");
            bool want = H != null && H.comf.Contains(id);
            GameObject go; comfGo.TryGetValue(id, out go);
            if (want && go == null)
            {
                var t = Group("Comfort_" + id, zx + (float)AHJson.N(c, "u"), zy + (float)AHJson.N(c, "v"));
                ComfortModel(t, id); comfGo[id] = t.gameObject;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
    }
    void ComfortModel(Transform t, string id)
    {
        switch (id)
        {
            case "flowers": foreach (float s in new[] { -1.6f, 1.6f }) { Prim(PrimitiveType.Cube, t, new Vector3(s, 0.15f, 0), new Vector3(1.6f, 0.3f, 0.8f), Wood); for (int i = 0; i < 6; i++) Prim(PrimitiveType.Sphere, t, new Vector3(s - 0.6f + i * 0.24f, 0.42f, (i % 2) * 0.2f - 0.1f), Vector3.one * 0.18f, new[] { 0xffd23a, 0xff8ac0, 0xffffff }[i % 3]); } break;
            case "bench": Prim(PrimitiveType.Cube, t, new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.08f, 0.5f), Wood); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.75f, -0.22f), new Vector3(1.8f, 0.4f, 0.06f), Wood); foreach (float s in new[] { -0.8f, 0.8f }) Prim(PrimitiveType.Cube, t, new Vector3(s, 0.22f, 0), new Vector3(0.1f, 0.45f, 0.45f), Dark); break;
            case "hearth": for (int i = 0; i < 8; i++) { float a = i / 8f * Mathf.PI * 2; Prim(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * 0.8f, 0.15f, Mathf.Sin(a) * 0.8f), new Vector3(0.4f, 0.3f, 0.4f), Stone); } Glow(t, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.5f, 0.6f)); Light(t, new Vector3(0, 0.8f, 0), new Color(1f, 0.6f, 0.3f), 8f); break;
            case "lanterns": for (int i = 0; i < 4; i++) { float z = -3f + i * 2f; foreach (float s in new[] { -1.2f }) { Prim(PrimitiveType.Cube, t, new Vector3(s, 0.8f, z), new Vector3(0.1f, 1.6f, 0.1f), Dark); Glow(t, new Vector3(s, 1.7f, z), Vector3.one * 0.25f); } } Light(t, new Vector3(-1.2f, 1.8f, 0), new Color(1f, 0.8f, 0.5f), 7f); break;
            case "scarecrow": Prim(PrimitiveType.Cube, t, new Vector3(0, 1f, 0), new Vector3(0.12f, 2f, 0.12f), Dark); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.5f, 0), new Vector3(1.6f, 0.1f, 0.1f), Dark); Prim(PrimitiveType.Cube, t, new Vector3(0, 1.3f, 0), new Vector3(0.6f, 0.7f, 0.3f), 0x8a6a44); Prim(PrimitiveType.Sphere, t, new Vector3(0, 2f, 0), Vector3.one * 0.45f, 0xe0b060); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.3f, 0), new Vector3(0.7f, 0.05f, 0.7f), Straw); break;
            case "chimes": Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0), new Vector3(0.1f, 2.4f, 0.1f), Dark); for (int i = 0; i < 5; i++) Prim(PrimitiveType.Cylinder, t, new Vector3(-0.3f + i * 0.15f, 1.9f - i * 0.05f, 0.2f), new Vector3(0.05f, 0.25f, 0.05f), 0xc89a4a); break;
            case "cat": Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.05f, 0), new Vector3(0.85f, 0.14f, 0.85f), 0xa8443a); if (!Pet(t, "Beasts/b_cat", 0.75f, new Vector3(0, 0.1f, 0))) { Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.3f, 0), new Vector3(0.45f, 0.3f, 0.6f), 0xe08a3a); Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.42f, 0.28f), Vector3.one * 0.24f, 0xe08a3a); } break;
            case "dog": Hut(t, new Vector3(0, 0, -0.8f), 1.4f, 1.2f, 1.0f, Wood, Red); if (!Pet(t, "Beasts/b_jackal", 1.0f, new Vector3(0, 0, 0.7f))) { Prim(PrimitiveType.Cube, t, new Vector3(0, 0.4f, 0.6f), new Vector3(0.35f, 0.35f, 0.8f), 0xb08050); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 1.05f), new Vector3(0.28f, 0.28f, 0.3f), 0xb08050); } break;
            case "swing": foreach (float s in new[] { -0.6f, 0.6f }) Prim(PrimitiveType.Cube, t, new Vector3(s, 1.2f, 0), new Vector3(0.03f, 2f, 0.03f), Straw); Prim(PrimitiveType.Cube, t, new Vector3(0, 0.25f, 0), new Vector3(1.4f, 0.08f, 0.4f), Wood); Prim(PrimitiveType.Cube, t, new Vector3(0, 2.25f, 0), new Vector3(2.4f, 0.2f, 0.2f), Dark); break;
            case "fountain": Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.3f, 2.6f), Stone); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.62f, 0), new Vector3(2.3f, 0.02f, 2.3f), 0x2f6f95); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.9f, 0), new Vector3(0.3f, 0.6f, 0.3f), Stone); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.5f, 0), new Vector3(1f, 0.08f, 1f), Stone); break;
            case "gazebo": Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.1f, 0), new Vector3(4.4f, 0.1f, 4.4f), Stone); for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2; Prim(PrimitiveType.Cylinder, t, new Vector3(Mathf.Cos(a) * 1.9f, 1.3f, Mathf.Sin(a) * 1.9f), new Vector3(0.18f, 1.2f, 0.18f), White); } Roof(t, new Vector3(0, 2.5f, 0), 4.6f, 4.6f, 1.4f, Red); break;
            case "trophies": Prim(PrimitiveType.Cube, t, new Vector3(0, 0.5f, 0), new Vector3(2f, 1f, 0.6f), Wood); Prim(PrimitiveType.Sphere, t, new Vector3(-0.5f, 1.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), 0x6a8a5a); Prim(PrimitiveType.Capsule, t, new Vector3(0.5f, 1.3f, 0), new Vector3(0.2f, 0.35f, 0.2f), White); break;
            case "telescope": foreach (float s in new[] { -0.35f, 0.35f }) Prim(PrimitiveType.Cube, t, new Vector3(s, 0.6f, 0), new Vector3(0.06f, 1.2f, 0.06f), Dark); Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.4f, 0.2f), new Vector3(0.25f, 0.8f, 0.25f), 0xc89a4a, new Vector3(-55, 0, 0)); break;
        }
    }
    void Light(Transform t, Vector3 at, Color c, float range)
    {
        var lg = new GameObject("Light"); lg.transform.SetParent(t, false); lg.transform.localPosition = at;
        var li = lg.AddComponent<UnityEngine.Light>(); li.type = LightType.Point; li.color = c; li.range = range; li.intensity = 1.6f; li.shadows = LightShadows.None;
        lg.AddComponent<AHFlicker>();
    }

    // ---------- every frame: crops grow, animals wander ----------
    public void Refresh()
    {
        var H = g.player.home;
        foreach (var pl in AHHome.Plots)
        {
            BuildPlot(pl);
            string id = AHJson.S(pl, "id"); AHSpot s;
            if (spots.TryGetValue(id, out s) && s.kind == "use") s.name = Label(pl);
            // crops: grow with progress, fruit shows when ripe
            if (H != null && AHJson.S(pl, "kind") == "field")
            {
                var f = AHHome.Field(H, id); GameObject go; plotGo.TryGetValue(id, out go);
                if (f != null && go != null)
                {
                    float sc = (0.3f + 0.7f * Mathf.Min(1f, f.prog)) * 1.25f;
                    foreach (Transform c in go.transform) if (c.name == "Plant") { c.localScale = Vector3.one * sc; var fr = c.Find("Fruit"); if (fr != null) fr.gameObject.SetActive(f.prog >= 1f); }
                }
            }
        }
        BuildComforts();
    }

    void Update()
    {
        if (g == null || g.player == null) return;
        tick += Time.deltaTime;
        if (tick > 1f) { tick = 0f; AHHome.Tick(g); }
        float dt = Time.deltaTime;
        foreach (var pa in petAnims) pa.Tick(dt);
        for (int i = 0; i < animals.Count; i++)
        {
            var a = animals[i]; if (a == null) continue; var to = animalTo[i]; var pl = animalPen[i];
            string kind = AHJson.S(pl, "animal");
            if (kind == "bee") { to.w += dt * 2.5f; a.position = P(to.x + Mathf.Cos(to.w) * 30f, to.y + Mathf.Sin(to.w * 1.3f) * 20f) + Vector3.up * (0.9f + Mathf.Sin(to.w * 2f) * 0.3f); animalTo[i] = to; continue; }
            to.z -= dt;
            if (to.z <= 0f)
            {
                to.z = 2f + UnityEngine.Random.value * 5f;
                if (UnityEngine.Random.value < 0.7f) { float x = (float)AHJson.N(pl, "x"), y = (float)AHJson.N(pl, "y"), w = (float)AHJson.N(pl, "w"), h = (float)AHJson.N(pl, "h"); to.x = x + 50 + UnityEngine.Random.value * (w - 100); to.y = y + 50 + UnityEngine.Random.value * (h - 100); }
            }
            Vector3 goal = P(to.x, to.y), d = goal - a.position; d.y = 0;
            float spd = (kind == "chicken" || kind == "duck" ? 40f : 28f) * AHDB.S;
            AHAnim an; animalAnim.TryGetValue(a, out an);
            bool moving = d.magnitude > 0.16f;
            if (moving)
            {
                a.position += d.normalized * Mathf.Min(d.magnitude, spd * dt);
                a.rotation = Quaternion.Slerp(a.rotation, g.Face(d), 1f - Mathf.Exp(-dt * 5f));
                if (an == null) { var b = a.GetChild(0); b.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 10f + i)) * 0.05f, 0); }
            }
            if (an != null)
            {
                // walk while going somewhere; at rest mostly graze, now and then look about
                if (moving) an.Play("Walk", true, 0.9f);
                else an.Play((((int)to.x + i) % 3 == 0) ? "Idle" : (an.Has("Eating") ? "Eating" : "Idle"), true, 1f);
                an.Tick(dt);
            }
            animalTo[i] = to;
        }
    }

    void OnDestroy() { if (I == this) I = null; foreach (var b in blocks) if (g != null) g.doors.Remove(b); foreach (var an in animalAnim.Values) an.Dispose(); animalAnim.Clear(); foreach (var pa in petAnims) pa.Dispose(); petAnims.Clear(); }
}
