// Ashen Hollow: the raid, the Ember Throne, as in the web game (v67, raidEnter / raidThink / raidBossDown / raidTick).
// Level 50 and up. Vaelor, the Ember King, grows with your level (60-90) and your party: you and your sellswords.
// His fight: a circle of flame under each of you every 6 s, a great ring of fire around him every 13 s, ember imps at
// 70% and 35%, embers raining from the roof at 50%, and after 7 minutes he is enraged. His Kingsflame set (shoulders,
// legs, feet, cape: the slots dungeons don't drop) is yours once a week (Monday reset); you can help as often as you like.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHRaid
{
    public const string Area = "raid", Boss = "r_vaelor";
    public const int MinLevel = 50;
    const float RCx = 241200, RCy = 1000, RCr = 820;   // the arena circle (web RC)
    const string FromKey = "ah_raid_from", WeekKey = "ah_raid_week";

    static string Week()
    {
        var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; d = d.AddDays(-back);
        return d.ToString("yyyy-MM-dd");
    }
    public static bool Looted { get { return AHPrefs.GetString(WeekKey, "") == Week(); } }

    public static void Enter(AHGame g)
    {
        var p = g.player;
        if (p.level < MinLevel) g.ui.Toast("The Ember Throne is for level " + MinLevel + " and up. You are level " + p.level + ": this will be very hard.", 4f);
        Vector2 w = g.ToWeb(p.transform.position);
        AHPrefs.SetString(FromKey, AHGame.AreaId + "|" + w.x + "|" + w.y);
        g.Travel(Area, RCx * AHDB.S, (RCy + RCr - 160) * AHDB.S, -Mathf.PI / 2f);
    }

    public static void Leave(AHGame g)
    {
        var s = AHPrefs.GetString(FromKey, "").Split('|'); float x, z;
        if (s.Length == 3 && float.TryParse(s[1], out x) && float.TryParse(s[2], out z) && s[0] != Area) g.Travel(s[0], x, z, Mathf.PI / 2f);
        else g.Travel("city", 41900 * AHDB.S, 1250 * AHDB.S, Mathf.PI / 2f);
    }

    // web raidBossDef: Vaelor is the magma titan, 2.3 times bigger and burning red
    public static void EnsureDef()
    {
        var all = AHDB.Mobs as Dictionary<string, object>;
        if (all == null || all.ContainsKey(Boss)) return;
        var d = new Dictionary<string, object>();
        var mt = AHJson.O(all, "magmatitan") as Dictionary<string, object>;
        if (mt != null) foreach (var kv in mt) d[kv.Key] = kv.Value;
        d["name"] = "Vaelor, the Ember King"; d["scale"] = 2.3; d["tint"] = (double)0xff4a1a; d["tintK"] = 0.55; d["glow"] = (double)0xff6a1a;
        d["noSkin"] = true; d["elite"] = true; d["slam"] = false; d["drops"] = new List<object>();
        all[Boss] = d;
    }

    // web raidBossDef: Vaelor grows with your level and your party
    public static void Scale(AHGame g, AHMobType t)
    {
        int L = Mathf.Clamp(g.player.level, 60, 90); double k = Math.Pow(L / 60.0, 1.5), heads = 0.6 + 0.55 * 1 + 0.3 * g.player.party.Count;
        t.lvl = L; t.hp = (int)Math.Round(78000 * k * heads); t.dmg = (int)Math.Round(95.0 * L / 60); t.xp = (int)Math.Round(30000 * k); t.gold = L * 120;
        t.elite = true; t.respawn = 1e9f; t.noSkin = true; t.aggro = 1400 * AHDB.S;
    }

    class Brain { public bool engaged, add1, add2, add3, enr; public float aT, bT, t; }
    static readonly Dictionary<AHMob, Brain> brains = new Dictionary<AHMob, Brain>();

    // web raidThink
    public static bool Think(AHGame g, AHMob m, float dt)
    {
        Brain b; if (!brains.TryGetValue(m, out b)) brains[m] = b = new Brain();
        if (!b.engaged) { b.engaged = true; b.aT = 4f; b.bT = 9f; b.t = 0f; g.ui.Banner("Vaelor wakes", "Spread out, watch the ground, kill the imps"); }
        b.t += dt; float f = m.hp / m.type.hp; bool enr = b.t > 420f; float fast = enr ? 0.5f : f < 0.35f ? 0.75f : 1f, hit = Mathf.Round(m.type.dmg * (enr ? 3f : 1.5f));
        if (enr && !b.enr) { b.enr = true; m.enraged = true; g.ui.Banner("Vaelor is enraged", "Seven minutes have passed. Finish him!"); }
        if (!b.add1 && f < 0.7f) { b.add1 = true; Imps(g, m, 3); }
        if (!b.add3 && f < 0.5f)
        {
            b.add3 = true; g.ui.Toast("Embers rain from the roof! Keep moving!", 3f);
            for (int k = 0; k < 14; k++) { float a = UnityEngine.Random.value * 6.28f, r = UnityEngine.Random.value * (RCr - 80); AHDungeon.Telegraph(g.W((RCx + Mathf.Cos(a) * r) * AHDB.S, (RCy + Mathf.Sin(a) * r) * AHDB.S), 80 * AHDB.S, 1.6f + UnityEngine.Random.value, hit, m); }
        }
        if (!b.add2 && f < 0.35f) { b.add2 = true; Imps(g, m, 5); }
        b.aT -= dt; b.bT -= dt;
        if (b.aT <= 0f)
        {
            b.aT = 6f * fast;
            AHDungeon.Telegraph(g.player.transform.position, 85 * AHDB.S, 1.5f, hit, m);
            foreach (var a in AHComp.Allies) if (a != null && !a.dead) AHDungeon.Telegraph(a.transform.position, 85 * AHDB.S, 1.5f, hit, m);
            return false;
        }
        if (b.bT <= 0f)
        {
            b.bT = 13f * fast;
            AHDungeon.Telegraph(m.transform.position, m.type.radius + 210 * AHDB.S, 1.9f, Mathf.Round(hit * 1.3f), m);
            m.Windup(1.9f); g.ui.Toast("Vaelor gathers flame! Get away from him!", 2.5f);
            return true;
        }
        return false;
    }

    static AHMobType impType;
    static void Imps(AHGame g, AHMob boss, int n)
    {
        if (impType == null)
        {
            var d = AHJson.O(AHDB.Mobs, "magmaimp");
            impType = new AHMobType { id = "magmaimp", name = "Ember imp", model = "Mobs/magmaimp", speed = (float)AHJson.N(d, "speed", 120) * AHDB.S, radius = (float)AHJson.N(d, "r", 16) * AHDB.S, atkCd = 1.3f, respawn = 1e9f, noSkin = true };
        }
        impType.lvl = boss.type.lvl; impType.hp = Mathf.RoundToInt(boss.type.dmg * 18f); impType.dmg = Mathf.RoundToInt(boss.type.dmg * 0.35f); impType.xp = 400; impType.gold = 20; impType.aggro = 1400 * AHDB.S;
        for (int k = 0; k < n; k++)
        {
            float a = UnityEngine.Random.value * 6.28f;
            Vector3 at = g.Resolve(boss.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (boss.type.radius + 90 * AHDB.S), impType.radius);
            var m = AHMob.Create(g, impType, at); m.add = true; m.Engage(); g.mobs.Add(m);
            AHFx.Pop(at + Vector3.up * 0.6f, 1.6f, new Color(1f, 0.5f, 0.2f));
        }
        g.ui.Toast("Vaelor calls his ember imps!", 2.5f);
    }

    // web raidBossDown: the set piece once a week, money every time
    public static void BossDown(AHGame g, AHMob m)
    {
        var p = g.player; bool first = !Looted; int L = m.type.lvl;
        g.ui.Banner("Vaelor falls!", first ? "Kingsflame loot is yours for this week" : "You already looted him this week");
        for (int k = 0; k < 5; k++) AHFx.Ring(m.transform.position, 1f, 3f + k, new Color(1f, 0.5f, 0.2f), 1.2f + k * 0.3f);
        foreach (var q in new List<AHMob>(g.mobs)) if (q.add && !q.dead) q.Hurt(999999, null, true);
        if (first)
        {
            AHPrefs.SetString(WeekKey, Week());
            var set = new List<string>(); foreach (var s in new[] { "shoulders", "legs", "feet", "cape" }) { string id = "kf_" + p.cls.id + "_" + s; if (AHItems.Get(id) != null) set.Add(id); }
            var pool = set.FindAll(id => p.bag.Count(id) == 0 && !p.bag.gear.ContainsValue(id)); if (pool.Count == 0) pool = set;
            if (pool.Count > 0) { string id = pool[UnityEngine.Random.Range(0, pool.Count)]; p.bag.Add(id); g.ui.Banner(AHItems.Get(id).name, "Kingsflame set piece"); }
            if (!p.mounts.Contains(AHPhoenix.Id) && UnityEngine.Random.value < 0.06f) { p.mounts.Add(AHPhoenix.Id); g.ui.Banner("Ashen Phoenix", "Rare mount! It rises from Vaelor’s ashes · tap RIDE"); g.ui.RefreshRide(); }
            p.bag.Add("void_shard", 2); p.bag.Add("enh_stone", 3); if (UnityEngine.Random.value < 0.25f) p.bag.Add("lucky_charm");
            p.AddMoney((long)Math.Round(L * 400.0) * AHDB.CU, m.transform.position);
        }
        else p.AddMoney((long)Math.Round(L * 60.0) * AHDB.CU, m.transform.position);
        g.SaveProgress();
    }
}

// the throne hall: decor (throne, pillars, lava pools, fires, the way out), and the arena's edge (web raidDecor / raidTick)
public class AHRaidView : MonoBehaviour
{
    AHGame g;
    const float RCx = 241200, RCy = 1000, RCr = 820;

    public static void Setup(AHGame g)
    {
        if (AHGame.AreaId != AHRaid.Area) return;
        var v = new GameObject("Raid").AddComponent<AHRaidView>(); v.g = g; v.Build();
        foreach (var m in g.mobs) if (m.type.id == AHRaid.Boss) { AHRaid.Scale(g, m.type); m.hp = m.type.hp; }
        foreach (var s in AHGather.Spots) if (s.kind == "gate" && s.gate != null && s.gate.label == "Leave the raid") s.use = () => AHRaid.Leave(g);
        g.ui.Banner("The Ember Throne", "Vaelor, the Ember King · " + (AHRaid.Looted ? "you already looted him this week" : "his Kingsflame set drops once a week"));
    }

    Vector3 P(float x, float y) { return g.W(x * AHDB.S, y * AHDB.S); }
    static Material Mat(int hex, bool glow = false)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", AHGame.Hex(hex).linear);
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", AHGame.Hex(hex).linear * 2f); }
        return m;
    }
    void Box(Vector3 at, Vector3 size, Material m)
    {
        var o = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); Destroy(o.GetComponent<Collider>());
        o.transform.SetParent(transform, false); o.transform.position = at + Vector3.up * size.y / 2; o.transform.localScale = size; o.GetComponent<Renderer>().sharedMaterial = m;
    }

    void Build()
    {
        g.SetDark(AHGame.Hex(0x1a0c08));
        Material st = Mat(0x4a3a34), dk = Mat(0x2a1c18), gold = Mat(0xd9ab3a), lava = Mat(0xff5a14, true);
        // the throne
        Vector3 th = P(RCx, RCy - 640);
        // the throne on its dais, facing the arena
        var thr = AHStations.Throne(transform); thr.transform.position = th;
        Vector3 toC = P(RCx, RCy) - th; toC.y = 0f; if (toC.sqrMagnitude > 0.01f) thr.transform.rotation = Quaternion.LookRotation(toC.normalized);
        // pillars around the arena
        for (int i = 0; i < 12; i++) { float a = i / 12f * Mathf.PI * 2; Vector3 at = P(RCx + Mathf.Cos(a) * (RCr + 40), RCy + Mathf.Sin(a) * (RCr + 40)); var col = AHStations.Column(transform, 6.2f, 40 + i); col.transform.position = at; g.AddBlocker(at, 0.95f); }
        // lava pools
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2 + 0.26f; Vector3 at = P(RCx + Mathf.Cos(a) * 520, RCy + Mathf.Sin(a) * 420);
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(transform, false); o.transform.position = at + Vector3.up * 0.03f; o.transform.localScale = new Vector3(3.2f, 0.03f, 3.2f); o.GetComponent<Renderer>().sharedMaterial = lava;
            var rim = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); Destroy(rim.GetComponent<Collider>());
            rim.transform.SetParent(transform, false); rim.transform.position = at + Vector3.up * 0.02f; rim.transform.localScale = new Vector3(3.8f, 0.04f, 3.8f); rim.GetComponent<Renderer>().sharedMaterial = dk;
            AHStations.LavaDress(transform, at, 1.6f, 90 + i);   // rocky rim, dark crust and bubbles
            var lg = new GameObject("LavaGlow"); lg.transform.SetParent(transform, false); lg.transform.position = at + Vector3.up * 0.8f;
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.4f, 0.1f); li.range = 7f; li.intensity = 2.5f; li.shadows = LightShadows.None;
        }
        // fires around the edge
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2; Vector3 at = P(RCx + Mathf.Cos(a) * (RCr - 60), RCy + Mathf.Sin(a) * (RCr - 60));
            var brz = AHStations.Brazier(transform, 1.1f, 70 + i); brz.transform.position = at; g.AddBlocker(at, 0.4f);
        }
        // the way out
        AHDungeon.Portal(g, P(RCx, RCy + RCr - 20), 0f, AHGame.Hex(0xff6a1a));
    }

    void Update()
    {
        var p = g.player; if (p == null) return;
        // the arena's edge (web raidTick)
        Vector2 w = g.ToWeb(p.transform.position) / AHDB.S; Vector2 d = w - new Vector2(RCx, RCy);
        if (d.magnitude > RCr + 30) { d = d.normalized * (RCr + 20); p.transform.position = g.W((RCx + d.x) * AHDB.S, (RCy + d.y) * AHDB.S); }
    }
}
