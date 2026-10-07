// Ashen Hollow: towns and reputation, and the bounty boards, as in the web game (v66, FACTIONS / repGain / repOnKill /
// writTake / writClaim / repBuy, and newBounty / fillBounties / renderBounties).
// Every town is a faction. Monsters near a town, its bounties, its daily writ and the dungeons beside it raise your
// standing there: Neutral, Friendly, Honored (5% off in its shops, its tabard), Revered (10% off, its signet), Exalted.
// Bounty boards: four hunts on offer, up to three at a time, paid in coin and class XP (+60 reputation each).
// The daily writ: defeat 15 monsters around a town, hand it in at that town's board for +250 reputation.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHBounty { public string type; public int n, prog; public long pay; public int xp; public bool taken, claimed; }
[Serializable] public class AHWrit { public string day, f; public int n, need = 15; public bool done; }

// everything the hero has done that the later systems count (kills by type, reputation, bounties, writs, ...)
[Serializable]
public class AHProgress
{
    public List<AHKV> rep = new List<AHKV>(), kills = new List<AHKV>(), stats = new List<AHKV>();
    public List<AHBounty> bounties = new List<AHBounty>();
    public bool hasWrit; public AHWrit writ = new AHWrit();
    public List<string> found = new List<string>(), ach = new List<string>();
    public string title;
    public AHDailyState daily = new AHDailyState();
    public List<AHKV> evKill = new List<AHKV>(), evSeen = new List<AHKV>(); public long evWarn;
    public List<string> cards = new List<string>(), gems = new List<string>(); public List<AHKV> sp = new List<AHKV>();
    public List<AHClassProg> cps = new List<AHClassProg>();
    public List<AHFestState> fests = new List<AHFestState>();
    public List<int> pbTamers = new List<int>();
    public List<AHMkStall> stalls = new List<AHMkStall>();
    public List<string> coll = new List<string>(); public List<AHKS> cos = new List<AHKS>(); public List<AHOutfit> outfits = new List<AHOutfit>();
    public string chId, chCity; public long chUntil, chSlept;   // the rented city house
    public long nmNight; public List<AHKV> nmBought = new List<AHKV>();
    public long kqDone; public int kqBestSiege, kqBestRush;
    public List<AHFriend> friends = new List<AHFriend>(); public List<AHLetter> letters = new List<AHLetter>();
    public AHArtisanState art = new AHArtisanState();
    public AHWorldState world = new AHWorldState();
    public int bagRows;                                          // extra bag rows bought (9 slots each)
    public List<AHLoadout> loadouts = new List<AHLoadout>();     // saved gear sets
    public string seasonKey = ""; public int seasonXp, seasonClaimed;   // the season track
    public List<AHKV> weaponKills = new List<AHKV>();            // kills by weapon kind (weapon mastery)

    public static long Get(List<AHKV> l, string k) { foreach (var e in l) if (e.k == k) return e.v; return 0; }
    public static void Add(List<AHKV> l, string k, long n) { foreach (var e in l) if (e.k == k) { e.v += n; return; } l.Add(new AHKV { k = k, v = n }); }
    public long Stat(string k) { return Get(stats, k); }
    public void Bump(string k, long n = 1) { Add(stats, k, n); }
}

public static class AHRep
{
    public class Fac { public string id, name; public Color col; }
    public static readonly Fac[] Factions =
    {
        new Fac { id = "ashen", name = "Ashen Hollow", col = AHGame.Hex(0x6a8a3a) }, new Fac { id = "varrow", name = "Varrow", col = AHGame.Hex(0x2a4a9a) },
        new Fac { id = "highcairn", name = "Highcairn", col = AHGame.Hex(0x8ac4ef) }, new Fac { id = "mirewatch", name = "Mirewatch", col = AHGame.Hex(0x4a6a4a) },
        new Fac { id = "sunspire", name = "Sunspire Oasis", col = AHGame.Hex(0xd8a840) }, new Fac { id = "cinderhold", name = "Cinderhold", col = AHGame.Hex(0xc84a1a) },
        new Fac { id = "coralport", name = "Coralport", col = AHGame.Hex(0x2ab0c8) },
    };
    public static Fac Get(string id) { foreach (var f in Factions) if (f.id == id) return f; return null; }
    public static readonly int[] RankAt = { 0, 500, 2500, 7000, 15000 };
    public static readonly string[] RankName = { "Neutral", "Friendly", "Honored", "Revered", "Exalted" };
    public static readonly string[] RankHex = { "#c9c0aa", "#8fd16a", "#5ab4ff", "#c07aff", "#ffb030" };
    static readonly float[] Disc = { 0f, 0f, 0.05f, 0.1f, 0.1f };

    public static long Of(AHPlayer p, string id) { return AHProgress.Get(p.prog.rep, id); }
    public static int Rank(AHPlayer p, string id) { long r = Of(p, id); int k = 0; for (int i = 0; i < RankAt.Length; i++) if (r >= RankAt[i]) k = i; return k; }
    public static float Discount(AHPlayer p, string id) { return id != null && Get(id) != null ? Disc[Rank(p, id)] : 0f; }

    // ---------- where does it count? the town it happened in, otherwise the nearest town (web factionAt) ----------
    class Town { public string id; public Rect r; }
    static List<Town> towns;
    static List<Town> Towns
    {
        get
        {
            if (towns != null) return towns;
            towns = new List<Town>();
            var ts = AHDB.List("world", "TOWNS");
            if (ts != null) foreach (var t in ts) { string id = AHJson.S(t, "id"); if (Get(id) != null) towns.Add(new Town { id = id, r = new Rect((float)AHJson.N(t, "x"), (float)AHJson.N(t, "y"), (float)AHJson.N(t, "w"), (float)AHJson.N(t, "h")) }); }
            return towns;
        }
    }
    public static string At(float x, float y)
    {
        var D = AHDungeon.Def(AHGame.AreaId); var ent = AHJson.O(D, "ent");
        if (ent != null) { x = (float)AHJson.N(ent, "x"); y = (float)AHJson.N(ent, "y"); }
        foreach (var t in Towns) if (t.r.Contains(new Vector2(x, y))) return t.id;
        string best = null; float bd = 9000f;
        foreach (var t in Towns) { float d = Vector2.Distance(t.r.center, new Vector2(x, y)); if (d < bd) { bd = d; best = t.id; } }
        return best;
    }
    public static string At(AHGame g, Vector3 pos) { Vector2 w = g.ToWeb(pos) / AHDB.S; return At(w.x, w.y); }
    // the shop discount where you stand (only inside a town)
    public static float ShopDiscount()
    {
        var g = AHGame.I; if (g == null || g.player == null || !g.InTown(g.player.transform.position)) return 0f;
        return Discount(g.player, At(g, g.player.transform.position));
    }

    // web repGain
    public static void Gain(AHGame g, string id, long n, bool quiet)
    {
        var f = Get(id); if (f == null || n <= 0) return;
        var p = g.player; int before = Rank(p, id);
        AHProgress.Add(p.prog.rep, id, n);
        int after = Rank(p, id);
        if (after > before) g.ui.Banner(RankName[after] + " with " + f.name, after == 1 ? "Their merchants know your face" : after == 2 ? "5% off in their shops · their tabard is for sale" : after == 3 ? "10% off · their quartermaster offers a signet" : "Exalted! A title is yours");
        if (!quiet && n >= 20) g.ui.Float(p.transform.position + Vector3.up * 2.6f, "+" + n + " " + f.name, new Color(0.56f, 0.85f, 1f));
        g.MarkDirty();
    }

    static string Cape(AHPlayer p) { string c; return p.bag.gear.TryGetValue("cape", out c) ? AHJson.S(AHItems.Raw(c), "fac") : null; }

    // web repOnKill, plus the kill count and the bounties (web killMob)
    public static void OnKill(AHGame g, AHMob m)
    {
        var p = g.player; var t = m.type;
        AHProgress.Add(p.prog.kills, t.id, 1);
        foreach (var b in p.prog.bounties)
            if (b.taken && !b.claimed && b.type == t.id && b.prog < b.n) { b.prog++; if (b.prog >= b.n) g.ui.Banner("Bounty complete", "Claim it at a bounty board"); }
        if (m.add || AHGame.AreaId == AHRaid.Area) return;
        bool dboss = AHDungeon.Def(AHGame.AreaId) != null && t.id == AHDungeon.BossOf(AHGame.AreaId);
        int n = dboss ? 150 : t.elite ? 10 : 2;
        string f = At(g, m.Home);
        if (f != null) Gain(g, f, n, n < 20);
        string champ = Cape(p); if (champ != null && champ != f) Gain(g, champ, Mathf.CeilToInt(n * 0.75f), true);
        var w = p.prog.writ;
        if (p.prog.hasWrit && !w.done && w.day == Day && f == w.f && !dboss)
        {
            w.n = Mathf.Min(w.need, w.n + 1);
            if (w.n == w.need) g.ui.Banner("Writ complete", "Return to a bounty board in " + Get(w.f).name);
        }
    }

    public static string Day { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }

    // ---------- bounties (web BOUNTY_POOL / newBounty / fillBounties) ----------
    static readonly string[] PoolT = { "boar", "wolf", "bandit", "bogtoad", "lurker", "crab", "jackal", "scorpion", "icewolf", "troll", "raptor", "hornback", "pterra", "deer" };
    static readonly int[] PoolN = { 5, 5, 4, 5, 2, 5, 4, 4, 4, 1, 4, 2, 3, 4 };
    static AHBounty NewBounty()
    {
        int i = UnityEngine.Random.Range(0, PoolT.Length); string type = PoolT[i]; int n = PoolN[i];
        int lvl = (int)AHJson.N(AHJson.O(AHDB.Mobs, type), "lvl", 1);
        return new AHBounty { type = type, n = n, pay = (long)Math.Round(lvl * n * 7 * AHDB.CU + UnityEngine.Random.value * AHDB.CU), xp = lvl * n * 14 };
    }
    public static void Fill(AHPlayer p)
    {
        var l = p.prog.bounties; l.RemoveAll(b => b == null || b.claimed || AHJson.O(AHDB.Mobs, b.type) == null);
        int guard = 0;
        while (l.Count < 4 && guard++ < 100) { var b = NewBounty(); if (!l.Exists(q => q.type == b.type)) l.Add(b); }
    }

    public static void Claim(AHGame g, AHBounty b, string boardFac)
    {
        var p = g.player; if (b.claimed || b.prog < b.n) return;
        b.claimed = true; p.AddMoney(b.pay, p.transform.position); p.GainClassXpDirect(b.xp); p.prog.Bump("bounties");
        Gain(g, boardFac, 60, false);
        g.ui.Banner("Bounty paid", AHItems.MoneyText(b.pay));
        g.SaveProgress();
    }

    // ---------- the daily writ ----------
    public static void WritTake(AHGame g, string f)
    {
        var p = g.player;
        if (p.prog.hasWrit && p.prog.writ.day == Day) { g.ui.Toast("One writ a day. Come back tomorrow."); return; }
        p.prog.hasWrit = true; p.prog.writ = new AHWrit { day = Day, f = f, n = 0, need = 15 };
        g.ui.Toast("Writ of " + Get(f).name + ": defeat 15 monsters in the lands around " + Get(f).name + ".", 4f);
        g.MarkDirty();
    }
    public static void WritClaim(AHGame g, string boardFac)
    {
        var p = g.player; var w = p.prog.writ;
        if (!p.prog.hasWrit || w.done || w.n < w.need) return;
        if (boardFac != w.f) { g.ui.Toast("Hand it in at a bounty board in " + Get(w.f).name + "."); return; }
        w.done = true; Gain(g, w.f, 250, false);
        long c = (long)Mathf.Round(p.level * 40) * AHDB.CU; p.AddMoney(c, p.transform.position); p.GainClassXpDirect(p.level * p.level * 3);
        g.ui.Banner("Writ fulfilled", "+250 " + Get(w.f).name + " · " + AHItems.MoneyText(c));
        g.SaveProgress();
    }

    // ---------- the quartermaster (web QM / repBuy) ----------
    public static long Price(string k) { return (k == "tabard" ? 200L : 2000L) * AHDB.CU * 100; }
    public static int Need(string k) { return k == "tabard" ? 2 : 3; }
    public static bool Owns(AHPlayer p, string item) { return p.bag.Count(item) > 0 || p.bag.gear.ContainsValue(item); }
    public static void Buy(AHGame g, string f, string k)
    {
        var p = g.player; string item = k + "_" + f;
        if (Rank(p, f) < Need(k) || Owns(p, item)) return;
        long pr = Price(k);
        if (p.bag.money < pr) { g.ui.Toast("That costs " + AHItems.MoneyText(pr) + "."); return; }
        if (!p.bag.Add(item)) { g.ui.Toast(p.bag.lastWarn ?? "Your bag is full."); return; }
        p.bag.money -= pr; p.bag.Touch();
        g.ui.Toast(k == "tabard" ? "The " + AHItems.Get(item).name + " is in your bag. Wear it as your cape to champion " + Get(f).name + "." : "The " + AHItems.Get(item).name + " is yours.", 4f);
        g.SaveProgress();
    }

    // ---------- the boards in this area ----------
    public static void Setup(AHGame g)
    {
        float S = AHDB.S;
        var l = AHDB.List("world", "BOARDS");
        if (l == null) return;
        foreach (var b in l)
        {
            Vector3 p = g.W((float)AHJson.N(b, "x") * S, (float)AHJson.N(b, "y") * S);
            if (!g.InArea(p)) continue;
            string fac = At((float)AHJson.N(b, "x"), (float)AHJson.N(b, "y"));
            var spot = new AHSpot { kind = "use", name = "Bounty board", pos = p, r = 0.6f, reach = 2.6f };
            spot.use = () => g.ui.OpenBoard(fac);
            AHGather.Spots.Add(spot);
            Vector3 face; HideOld(g, p, out face);
            Board(g, p, face);
        }
    }

    // the web game's own flat board stands on the same spot: hide it (it showed through the new one) and turn the new
    // board's notices the way the old one faced, towards the road
    static void HideOld(AHGame g, Vector3 at, out Vector3 face)
    {
        face = Vector3.zero;
        var w = g.World; if (w == null) return;
        Vector3 all = Vector3.zero, papers = Vector3.zero; int na = 0, np = 0;
        foreach (var r in w.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue;
            var b = r.bounds; Vector3 d = b.center - at; d.y = 0;
            if (d.magnitude > 1.5f || Mathf.Max(b.size.x, b.size.z) > 2.6f || b.size.y > 2.8f) continue;
            r.enabled = false; all += b.center; na++;
            var m = r.sharedMaterial; Color c = Color.black;
            if (m != null) { if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); }
            if (c.r + c.g + c.b > 1.5f) { papers += b.center; np++; }
        }
        if (na > 0 && np > 0) { face = papers / np - all / na; face.y = 0; if (face.sqrMagnitude > 1e-6f) face.Normalize(); }
    }

    static Material wood, paper;
    static Material wood2, roofM, pinM, paper2, glowM;
    static void Board(AHGame g, Vector3 at, Vector3 face)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (wood == null) { wood = new Material(sh); wood.SetColor("_BaseColor", AHGame.Hex(0x6a4a2a).linear); paper = new Material(sh); paper.SetColor("_BaseColor", AHGame.Hex(0xe8dcb8).linear); }
        var root = new GameObject("BountyBoard"); root.transform.position = g.Resolve(at, 0.6f);
        // the notices are on the board's -Z side
        root.transform.rotation = face.sqrMagnitude > 0.5f ? Quaternion.LookRotation(-face) : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        Action<Vector3, Vector3, Material> box = (pos, size, m) =>
        {
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(root.transform, false); o.transform.localPosition = pos; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m;
        };
        // two round log posts, a board of planks under a little shingled roof, notices pinned at all angles, a lantern
        Action<PrimitiveType, Vector3, Vector3, Vector3, Material> part = (pt, pos, size, rot, m) =>
        {
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(root.transform, false); o.transform.localPosition = pos; o.transform.localScale = size; o.transform.localRotation = Quaternion.Euler(rot); o.GetComponent<Renderer>().sharedMaterial = m;
        };
        if (wood2 == null)
        {
            wood2 = new Material(sh); wood2.SetColor("_BaseColor", AHGame.Hex(0x80603a).linear);
            roofM = new Material(sh); roofM.SetColor("_BaseColor", AHGame.Hex(0x7a3a26).linear);
            pinM = new Material(sh); pinM.SetColor("_BaseColor", AHGame.Hex(0xc8202a).linear); pinM.SetFloat("_Metallic", 0.5f);
            paper2 = new Material(sh); paper2.SetColor("_BaseColor", AHGame.Hex(0xd8c49a).linear);
            glowM = new Material(sh); glowM.SetColor("_BaseColor", AHGame.Hex(0xffc060).linear); glowM.EnableKeyword("_EMISSION"); glowM.SetColor("_EmissionColor", AHGame.Hex(0xffb040).linear * 2f);
        }
        foreach (var sx in new[] { -1f, 1f }) part(PrimitiveType.Cylinder, new Vector3(sx * 0.85f, 1.1f, 0), new Vector3(0.16f, 1.1f, 0.16f), Vector3.zero, wood);
        for (int i = 0; i < 5; i++) part(PrimitiveType.Cube, new Vector3(0, 0.95f + i * 0.21f, 0), new Vector3(1.6f, 0.2f, 0.07f), new Vector3(0, 0, (i % 2 == 0 ? 0.6f : -0.5f)), i % 2 == 0 ? wood : wood2);
        part(PrimitiveType.Cube, new Vector3(0, 1.98f, 0), new Vector3(1.9f, 0.08f, 0.16f), Vector3.zero, wood);
        foreach (var sz in new[] { -1f, 1f }) part(PrimitiveType.Cube, new Vector3(0, 2.18f, sz * 0.2f), new Vector3(2.05f, 0.05f, 0.48f), new Vector3(sz * 32f, 0, 0), roofM);
        var rr = new System.Random((int)(at.x * 17 + at.z * 3));
        for (int i = 0; i < 6; i++)
        {
            float x = -0.6f + (i % 3) * 0.6f + (float)rr.NextDouble() * 0.12f, y = 1.55f - (i / 3) * 0.48f + (float)rr.NextDouble() * 0.08f, tilt = (float)rr.NextDouble() * 16f - 8f;
            part(PrimitiveType.Cube, new Vector3(x, y, -0.05f), new Vector3(0.32f, 0.4f, 0.01f), new Vector3(0, 0, tilt), i % 2 == 0 ? paper : paper2);
            part(PrimitiveType.Cube, new Vector3(x, y + 0.03f, -0.054f), new Vector3(0.2f, 0.02f, 0.004f), new Vector3(0, 0, tilt), wood);   // a line of writing
            part(PrimitiveType.Cube, new Vector3(x, y - 0.06f, -0.054f), new Vector3(0.16f, 0.02f, 0.004f), new Vector3(0, 0, tilt), wood);
            part(PrimitiveType.Sphere, new Vector3(x, y + 0.17f, -0.07f), Vector3.one * 0.035f, Vector3.zero, pinM);
        }
        part(PrimitiveType.Cube, new Vector3(0.95f, 1.75f, -0.15f), new Vector3(0.16f, 0.22f, 0.16f), Vector3.zero, glowM);
        var lg = new GameObject("Lantern"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = new Vector3(0.95f, 1.75f, -0.4f);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.75f, 0.4f); li.range = 4f; li.intensity = 0.9f; li.shadows = LightShadows.None;
        // a gold "!" floats over the board when a bounty is ready to hand in
        var mark = new GameObject("Ready"); mark.transform.SetParent(root.transform, false); mark.transform.localPosition = new Vector3(0, 2.75f, 0);
        var gold = new Material(sh); gold.SetColor("_BaseColor", AHGame.Hex(0xffc83a).linear); gold.EnableKeyword("_EMISSION"); gold.SetColor("_EmissionColor", AHGame.Hex(0xffb020).linear * 1.6f);
        var bar = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); UnityEngine.Object.Destroy(bar.GetComponent<Collider>()); bar.transform.SetParent(mark.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.12f, 0); bar.transform.localScale = new Vector3(0.11f, 0.32f, 0.11f); bar.GetComponent<Renderer>().sharedMaterial = gold;
        var dot = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); UnityEngine.Object.Destroy(dot.GetComponent<Collider>()); dot.transform.SetParent(mark.transform, false);
        dot.transform.localPosition = new Vector3(0, -0.15f, 0); dot.transform.localScale = Vector3.one * 0.13f; dot.GetComponent<Renderer>().sharedMaterial = gold;
        root.AddComponent<AHBoardMark>().mark = mark.transform;
        AHModel.SetShadows(root);
        g.AddBlocker(root.transform.position, 0.6f);
    }
}

// bobs and turns the "!" over a bounty board, shown only while one of your bounties is ready to claim
public class AHBoardMark : MonoBehaviour
{
    public Transform mark; float t, check;
    void Update()
    {
        if (mark == null) return;
        check -= Time.deltaTime;
        if (check <= 0f)
        {
            check = 0.5f; bool ready = false; var g = AHGame.I;
            if (g != null && g.player != null && g.player.prog != null && g.player.prog.bounties != null)
                foreach (var b in g.player.prog.bounties) if (b.taken && !b.claimed && b.prog >= b.n) { ready = true; break; }
            if (mark.gameObject.activeSelf != ready) mark.gameObject.SetActive(ready);
        }
        if (!mark.gameObject.activeSelf) return;
        t += Time.deltaTime;
        mark.localPosition = new Vector3(0, 2.75f + Mathf.Sin(t * 2.2f) * 0.08f, 0);
        mark.localRotation = Quaternion.Euler(0, t * 90f, 0);
    }
}

public partial class AHUI
{
    string boardFac;
    public void OpenBoard(string fac)
    {
        if (g.player.mounted) AHComp.Dismount(g, true);
        boardFac = fac; wkMode = "board"; wkPageI = 0; ShowWork(true); RenderWork();
    }
    public void OpenRep() { wkMode = "rep"; wkPageI = 0; ShowWork(true); RenderWork(); }

    static string RepLine(AHPlayer p, string id)
    {
        long r = AHRep.Of(p, id); int k = AHRep.Rank(p, id); long lo = AHRep.RankAt[k]; long hi = k + 1 < AHRep.RankAt.Length ? AHRep.RankAt[k + 1] : -1;
        float d = AHRep.Discount(p, id);
        return "<color=" + AHRep.RankHex[k] + ">" + AHRep.RankName[k] + "</color>" + (hi > 0 ? " · " + (r - lo).ToString("#,0") + " / " + (hi - lo).ToString("#,0") : "") + (d > 0 ? " · " + Mathf.RoundToInt(d * 100) + "% off in shops" : "");
    }

    void RenderBoard(AHPlayer p)
    {
        AHRep.Fill(p);
        var F = AHRep.Get(boardFac);
        wkTitle.text = "Bounty board" + (F != null ? " · " + F.name : "");
        int taken = p.prog.bounties.FindAll(b => b.taken && !b.claimed).Count;
        wkHint.text = "Take up to 3 bounties at a time. Bounties completed: " + p.prog.Stat("bounties") + "." + (F != null ? " " + F.name + ": " + RepLine(p, F.id) : "");
        var rows = new List<Action<int>>();
        foreach (var b in p.prog.bounties)
        {
            var bb = b; var d = AHJson.O(AHDB.Mobs, b.type); string nm = AHJson.S(d, "name", b.type).ToLowerInvariant();
            bool done = b.taken && b.prog >= b.n;
            rows.Add(s => Row(s, "Hunt " + bb.n + " " + nm + (bb.n > 1 && !nm.EndsWith("deer") ? "s" : ""), new Color(1f, 0.8f, 0.45f),
                "Level " + (int)AHJson.N(d, "lvl", 1) + " · reward " + AHItems.MoneyText(bb.pay) + " and " + bb.xp + " class XP", bb.taken ? bb.prog + " / " + bb.n : "",
                done ? new WkBtn { label = "Claim", on = true, col = Go, act = () => { AHRep.Claim(g, bb, boardFac); RenderWork(); } }
                    : bb.taken ? null : new WkBtn { label = "Accept", on = taken < 3, col = Plain, act = () => { bb.taken = true; Toast("Bounty accepted. Happy hunting."); g.MarkDirty(); RenderWork(); } }));
        }
        if (F != null)
        {
            var w = p.prog.writ; bool today = p.prog.hasWrit && w.day == AHRep.Day;
            rows.Add(s => Row(s, "Daily writ", new Color(0.56f, 0.85f, 1f),
                today ? (w.done ? "Done for today. A new writ tomorrow." : "Writ of " + AHRep.Get(w.f).name + ": " + w.n + " / " + w.need + " monsters" + (w.f != F.id ? " · hand in at " + AHRep.Get(w.f).name : "")) : "Defeat 15 monsters around " + F.name + " for +250 reputation, coins and XP.", "",
                today ? (!w.done && w.n >= w.need ? new WkBtn { label = "Hand in", on = w.f == F.id, col = Go, act = () => { AHRep.WritClaim(g, boardFac); RenderWork(); } } : null)
                      : new WkBtn { label = "Take writ", on = true, col = Go, act = () => { AHRep.WritTake(g, F.id); RenderWork(); } }));
            foreach (var k in new[] { "tabard", "signet" })
            {
                string kk = k, item = k + "_" + F.id; var it = AHItems.Get(item); if (it == null) continue;
                bool ok = AHRep.Rank(p, F.id) >= AHRep.Need(k), own = AHRep.Owns(p, item); long pr = AHRep.Price(k);
                rows.Add(s => Row(s, it.name + "  <color=" + AHRep.RankHex[AHRep.Need(kk)] + ">" + AHRep.RankName[AHRep.Need(kk)] + "</color>", AHItems.Quality(it),
                    kk == "tabard" ? "Wear it: every monster you defeat anywhere counts for this town." : "+16 attack, +12 armor, +70 HP. One per town.", "",
                    own ? null : new WkBtn { label = AHItems.MoneyText(pr), on = ok && p.bag.money >= pr, col = ok ? Go : Plain, act = () => { AHRep.Buy(g, F.id, kk); RenderWork(); } }));
            }
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }

    void RenderRepList(AHPlayer p)
    {
        wkTitle.text = "Reputation";
        string ch; string champ = p.bag.gear.TryGetValue("cape", out ch) ? AHJson.S(AHItems.Raw(ch), "fac") : null;
        wkHint.text = "Every town remembers what you do for it: monsters near the town, its bounties, its daily writ and the dungeons nearby. " +
            (champ != null && AHRep.Get(champ) != null ? "You are championing " + AHRep.Get(champ).name + ": every kill also counts for them." : "Wear a town's tabard as your cape to champion it everywhere you fight.");
        var rows = new List<Action<int>>();
        foreach (var f in AHRep.Factions) { var ff = f; rows.Add(s => Row(s, ff.name, ff.col * 1.4f, RepLine(p, ff.id), "")); }
        rows.Add(s => Row(s, "Back", new Color(1f, 1f, 1f, 0.7f), "", "", new WkBtn { label = "Menu", on = true, col = Plain, act = OpenHub }));
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}
