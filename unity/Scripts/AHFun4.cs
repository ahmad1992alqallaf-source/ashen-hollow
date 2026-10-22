// Ashen Hollow: the fourth batch of things to do.
//  - Mounts learn tricks as they level: Gallop (a burst of speed, from mount level 3; it recovers faster from level 6).
//  - Crafting mastery stars: make the same piece of gear often and you earn stars for it (8, 20, 50 made); each star
//    gives a chance that a new one comes out a grade better (+1, +2, +3).
//  - The daily wheel: one free spin a day for a small reward (coins, potions, an elixir, stones, a sack, marks).
//  - The weekly legend: one very hard named beast a week, in one of your lands; your fastest kill against the realm's.
//  - The sky dragon: now and then a dragon circles over Hollow Meadow's fields, then lands to fight.
//  - Visitors: friends you have made (two hearts or more) drop by your homestead now and then with a gift.
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AHFun4
{
    // ===================================== mount tricks =====================================
    static float gallopUntil, gallopReady;
    public static bool CanGallop(AHPlayer p) { return p != null && p.mounted && p.mountSel != null && AHComp.MountLv(p, p.mountSel) >= 3; }
    public static float GallopCd(AHPlayer p) { return p != null && p.mountSel != null && AHComp.MountLv(p, p.mountSel) >= 6 ? 12f : 20f; }
    public static float GallopK { get { return Time.time < gallopUntil ? 1.45f : 1f; } }
    public static float GallopLeft01(AHPlayer p) { return Time.time >= gallopReady ? 0f : Mathf.Clamp01((gallopReady - Time.time) / GallopCd(p)); }
    public static void Gallop(AHGame g)
    {
        var p = g.player; if (!CanGallop(p)) { g.ui.Toast(AHLoc.T("Your mount learns to gallop at mount level 3.", "يتعلم ركوبك العدو عند المستوى ٣."), 2.5f); return; }
        if (Time.time < gallopReady) return;
        gallopUntil = Time.time + 4f; gallopReady = Time.time + GallopCd(p);
        AHSound.Play("whoosh"); AHFx.Pop(p.transform.position + Vector3.up * 0.4f, 1.6f, new Color(1f, 0.9f, 0.6f));
    }

    // ===================================== crafting mastery stars =====================================
    public static int Made(string id) { return AHPrefs.GetInt("ah_craftn_" + id, 0); }
    public static int Stars(string id) { int n = Made(id); return n >= 50 ? 3 : n >= 20 ? 2 : n >= 8 ? 1 : 0; }
    public static string StarText(string id) { int s = Stars(id); return s <= 0 ? "" : new string('★', s); }
    // from CraftOut, after a piece of gear went into the bag: count it, and maybe make it better
    public static void OnCrafted(AHGame g, AHPlayer p, string outId)
    {
        var d = AHItems.Get(outId); if (d == null || !d.IsGear || d.cosmetic || d.enh > 0) return;
        int before = Stars(outId);
        AHPrefs.SetInt("ah_craftn_" + outId, Made(outId) + 1);
        int s = Stars(outId);
        if (s > before) g.ui.Banner(AHLoc.T("Practised hands: ", "أيدٍ متمرّسة: ") + new string('★', s), AHLoc.T(d.name + " now comes out better more often", d.name + " يخرج أفضل أكثر"));
        if (s <= 0 || UnityEngine.Random.value >= s * 0.12f) return;
        string up = outId + "+" + s;
        if (AHItems.Get(up) == null || !p.bag.Take(outId)) return;
        if (!p.bag.Add(up)) { p.bag.Add(outId); return; }
        g.ui.Float(p.transform.position + Vector3.up * 2.6f, "★ " + AHItems.Get(up).name, new Color(1f, 0.85f, 0.4f));
        AHSound.Play("loot");
    }

    // ===================================== the daily wheel =====================================
    public class Slice { public string en, ar; public long money; public int marks; public string item; public int n; public float w; public Color col; }
    public static readonly Slice[] Wheel =
    {
        new Slice { en = "A few coins", ar = "بعض العملات", money = 40, w = 22, col = new Color(0.85f, 0.7f, 0.3f) },
        new Slice { en = "Health potions", ar = "جرعات صحة", item = "big_potion", n = 2, w = 16, col = new Color(0.8f, 0.3f, 0.3f) },
        new Slice { en = "A purse", ar = "صُرّة", money = 150, w = 12, col = new Color(0.95f, 0.8f, 0.35f) },
        new Slice { en = "Enhancement stones", ar = "أحجار التحسين", item = "enh_stone", n = 2, w = 12, col = new Color(0.5f, 0.6f, 0.85f) },
        new Slice { en = "An elixir", ar = "إكسير", item = "elixir_might", n = 1, w = 12, col = new Color(0.7f, 0.45f, 0.9f) },
        new Slice { en = "20 marks", ar = "٢٠ علامة", marks = 20, w = 12, col = new Color(0.45f, 0.8f, 0.55f) },
        new Slice { en = "Mystery sack", ar = "كيس الغموض", item = "mystery_sack", n = 1, w = 9, col = new Color(0.9f, 0.55f, 0.3f) },
        new Slice { en = "Lucky charm", ar = "تعويذة الحظ", item = "lucky_charm", n = 1, w = 5, col = new Color(0.4f, 0.85f, 0.85f) },
    };
    static string Today() { return DateTime.Now.ToString("yyyy-MM-dd"); }
    public static bool WheelReady { get { return AHPrefs.GetString("ah_wheel_day", "") != Today(); } }
    public static int PickSlice() { float sum = 0f; foreach (var s in Wheel) sum += s.w; float r = UnityEngine.Random.value * sum; for (int i = 0; i < Wheel.Length; i++) { r -= Wheel[i].w; if (r <= 0f) return i; } return 0; }
    public static void GiveSlice(AHGame g, int i)
    {
        AHPrefs.SetString("ah_wheel_day", Today());
        var s = Wheel[i]; var p = g.player; int L = Mathf.Max(1, p.level);
        var r = new AHDaily.Reward { name = AHLoc.T("Daily wheel · ", "عجلة اليوم · ") + AHLoc.T(s.en, s.ar), money = s.money > 0 ? s.money * (1 + L / 10) : 0, marks = s.marks };
        if (s.item != null && AHItems.Get(s.item) != null) r.items.Add(new KeyValuePair<string, int>(s.item, s.n));
        AHDaily.Grant(g, r);
    }

    // ===================================== the weekly legend =====================================
    static readonly string[] LegendNames = { "Thornmaw the Undying", "Old Gravelhide", "The Ash Widow", "Kingtusk", "Moonfang", "The Hollow Stag", "Gorewing", "Mother of Storms" };
    static readonly string[] Rivals = { "Varn the Swift", "Ilsa Ashborn", "Brother Odo", "Mira Quickstep", "Teodric the Old", "Sable Wren", "Corin Dusk" };
    public static int WeekNo { get { var d = DateTime.Now.Date; int back = ((int)d.DayOfWeek + 6) % 7; return (int)(d.AddDays(-back) - new DateTime(2026, 1, 5)).TotalDays / 7; } }
    public static string LegendName { get { return LegendNames[((WeekNo % LegendNames.Length) + LegendNames.Length) % LegendNames.Length]; } }
    public static string LegendArea(AHPlayer p)
    {
        string a = AHPrefs.GetString("ah_legend_area_" + WeekNo, "");
        if (a != "" && p.prog.found.Contains(a)) return a;
        var lands = new List<string>();
        foreach (var o in AHDB.List("world", "REGIONS") ?? new List<object>())
        {
            string id = AHJson.S(o, "id"), lv = AHJson.S(o, "lv", "");
            if (id == null || lv == "safe" || !p.prog.found.Contains(id) || AHDungeon.IsDungeon(id) || id == AHDeep.Area || id == AHAbyss.Area) continue;
            lands.Add(id);
        }
        if (lands.Count == 0) lands.Add("meadow");
        lands.Sort();
        a = lands[((WeekNo * 7 + 3) % lands.Count + lands.Count) % lands.Count];
        AHPrefs.SetString("ah_legend_area_" + WeekNo, a); return a;
    }
    public static int LegendBest { get { return AHPrefs.GetInt("ah_legend_best_" + WeekNo, 0); } }
    public static bool LegendDown { get { return LegendBest > 0; } }
    // the realm's best this week (rivals, fixed for the week)
    public static List<KeyValuePair<string, int>> Board(AHPlayer p)
    {
        var rnd = new System.Random(WeekNo * 31 + 7); var l = new List<KeyValuePair<string, int>>();
        foreach (var r in Rivals) l.Add(new KeyValuePair<string, int>(r, 95 + rnd.Next(0, 330)));
        if (LegendBest > 0) l.Add(new KeyValuePair<string, int>((p.heroName ?? "You") + AHLoc.T(" (you)", " (أنت)"), LegendBest));
        l.Sort((a, b) => a.Value.CompareTo(b.Value));
        return l;
    }
    static AHMob legend; static string legendArea; static float legendSpawnT = 8f, legendStart;
    public static AHMob Legend { get { return legend != null && !legend.dead ? legend : null; } }
    static void LegendTick(AHGame g, float dt)
    {
        var p = g.player;
        if (legendArea != AHGame.AreaId) { legend = null; legendArea = AHGame.AreaId; legendSpawnT = 10f; legendStart = 0f; }
        if (legend != null)
        {
            if (legendStart == 0f && legend.hp < legend.type.hp) legendStart = Time.time;   // the clock starts with the first blow
            if (legend.dead)
            {
                int secs = Mathf.Max(1, Mathf.RoundToInt(Time.time - (legendStart > 0f ? legendStart : Time.time)));
                bool first = LegendBest == 0; bool better = first || secs < LegendBest;
                if (better) AHPrefs.SetInt("ah_legend_best_" + WeekNo, secs);
                int place = Board(p).FindIndex(kv => kv.Value == LegendBest) + 1;
                var r = new AHDaily.Reward { name = AHLoc.T("The legend falls: ", "سقطت الأسطورة: ") + LegendName, money = first ? 300 + p.level * 60 : 0, xp = first ? (long)(p.level * p.level * 20 + 500) : 0, marks = first ? 60 : 0 };
                if (first) { r.items.Add(new KeyValuePair<string, int>("mystery_sack", 2)); r.items.Add(new KeyValuePair<string, int>("enh_stone", 4)); AHDaily.Grant(g, r); }
                g.ui.Banner(LegendName + AHLoc.T(" · " + AHDeep.Clock(secs), " · " + AHDeep.Clock(secs)), better ? AHLoc.T("Your best this week · place " + place + " on the legend board", "أفضل وقت لك هذا الأسبوع · المركز " + place) : AHLoc.T("Your best stays " + AHDeep.Clock(LegendBest), "أفضل وقت لك يبقى " + AHDeep.Clock(LegendBest)));
                legend = null; legendSpawnT = 600f;   // back for another try later
            }
            return;
        }
        if ((legendSpawnT -= dt) > 0f || g.mobs.Count == 0) return;
        legendSpawnT = 30f;
        if (LegendArea(p) != AHGame.AreaId) return;
        legend = AHFun3.SpawnNamed(g, LegendName, 30, 6, 1.7f, 2.2f, 25, 10); legendStart = 0f;
        if (legend != null) g.ui.Banner(AHLoc.T("The weekly legend", "أسطورة الأسبوع"), LegendName + AHLoc.T(" walks this land · the purple mark on the minimap", " تجول هذه الأرض · العلامة البنفسجية على الخريطة"));
    }
    public static void TestLegendHere(AHPlayer p) { AHPrefs.SetString("ah_legend_area_" + WeekNo, AHGame.AreaId); legendSpawnT = 1f; legend = null; }

    // ===================================== the sky dragon over Hollow Meadow =====================================
    static AHMob dragon; static float dragonT; static Vector3 dragonGround, dragonCentre; static int dragonSlot = -1; static bool dragonLanded;
    public static AHMob Dragon { get { return dragon != null && !dragon.dead ? dragon : null; } }
    static int Slot { get { var n = DateTime.Now; return (n.Hour * 60 + n.Minute) / 40; } }   // a dragon every 40 minutes
    public static void SummonDragon(AHGame g)
    {
        try { SummonDragon0(g); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: sky dragon could not come (" + e.Message + ")\n" + e.StackTrace); dragon = null; }
    }
    static void SummonDragon0(AHGame g)
    {
        var p = g.player; if (p == null || dragon != null) return;
        var d = AHJson.O(AHDB.Mobs, "emberdrake"); string model = AHJson.S(d, "model");   // (its look comes from the bestiary by its id)
        // it lands out in the fields: a beast's place outside the walls, a fair way from you
        Vector3 at = p.transform.position; float best = 0f;
        foreach (var m in g.mobs) { if (m == null || m.dead) continue; float dd = (m.transform.position - p.transform.position).magnitude; if (dd > 30f && dd < 110f && !g.InTown(m.transform.position) && (best == 0f || Mathf.Abs(dd - 55f) < Mathf.Abs(best - 55f))) { best = dd; at = m.transform.position; } }
        if (best == 0f) { Debug.Log("Ashen Hollow: the sky dragon found nowhere to land"); return; }
        int L = Mathf.Max(3, p.level + 2);
        var t = new AHMobType { id = "emberdrake", name = AHLoc.T("Sky dragon", "تنين السماء"), model = model, lvl = L, hp = Mathf.RoundToInt(Mathf.Max(400f, p.Power * 55f)), dmg = Mathf.RoundToInt(Mathf.Max(8f, p.maxHp * 0.07f)), xp = L * L * 10 + 200, speed = 4f, radius = 1.6f, aggro = 18f, atkCd = 1.6f, respawn = 1e9f, elite = true, gold = 40 + L * 10 };
        dragon = AHMob.Create(g, t, at); g.mobs.Add(dragon); dragon.enabled = false;
        dragonGround = dragon.transform.position; dragonCentre = AHHollow.Active ? AHHollow.C : at; dragonT = 0f; dragonLanded = false;
        Debug.Log("Ashen Hollow: sky dragon out, landing at " + dragonGround);
        g.ui.Banner(AHLoc.T("A dragon over Hollow Meadow!", "تنين فوق هولو!"), AHLoc.T("It circles the town, then lands in the fields", "يحوم فوق البلدة ثم يهبط في الحقول"));
        AHSound.Play("roar");
    }
    static void DragonTick(AHGame g, float dt)
    {
        if (AHGame.AreaId != "meadow") { dragon = null; return; }
        if (dragon == null || dragon.dead)
        {
            if (dragon != null && dragon.dead) { dragon = null; g.ui.Banner(AHLoc.T("The sky dragon falls!", "سقط تنين السماء!"), AHLoc.T("Hollow Meadow cheers", "هولو تهتف")); }
            int s = Slot; var n = DateTime.Now;
            if (s != dragonSlot && (n.Hour * 60 + n.Minute) % 40 == 0 && g.player.level >= 3) { dragonSlot = s; SummonDragon(g); }
            return;
        }
        if (dragonLanded) return;
        dragonT += dt;
        var tr = dragon.transform;
        if (dragonT < 22f)
        {
            // round the town, high up
            float a = dragonT * 0.32f; Vector3 c = dragonCentre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 26f + Vector3.up * 24f;
            Vector3 tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            tr.position = c; tr.rotation = Quaternion.LookRotation(tan);
            return;
        }
        // down to the fields
        float k = Mathf.Clamp01((dragonT - 22f) / 4f);
        Vector3 from = dragonCentre + Vector3.up * 24f + (dragonGround - dragonCentre).normalized * 26f;
        tr.position = Vector3.Lerp(from, dragonGround, Mathf.SmoothStep(0f, 1f, k));
        Vector3 fl = dragonGround - from; fl.y = 0f; if (fl.sqrMagnitude > 0.01f) tr.rotation = Quaternion.LookRotation(fl);
        if (k >= 1f) { dragonLanded = true; dragon.enabled = true; AHSound.Play("roar"); AHFx.Ring(dragonGround + Vector3.up * 0.2f, 0.5f, 6f, new Color(1f, 0.6f, 0.3f), 0.6f); }
    }

    // ===================================== visitors at the homestead =====================================
    static AHNpc visitor; static string visitArea; static float visitT;
    static void VisitTick(AHGame g, float dt)
    {
        var p = g.player;
        if (visitArea != AHGame.AreaId) { visitArea = AHGame.AreaId; visitor = null; visitT = 6f; }
        if (AHGame.AreaId != AHHome.Area || visitor != null || p.home == null) return;
        if ((visitT -= dt) > 0f) return; visitT = 1e9f;
        string day = Today(); if (AHPrefs.GetString("ah_visit_day", "") == day) return;
        // a friend with two hearts or more
        var who = new List<string>();
        if (AHFriends.All != null) foreach (var kv in AHFriends.All) if (AHFriends.Hearts(p, kv.Key) >= 2) who.Add(kv.Key);
        if (who.Count == 0) return;
        string n = who[UnityEngine.Random.Range(0, who.Count)];
        Vector3 at = p.transform.position + p.transform.forward * 4f + p.transform.right * 2f;
        visitor = AHNpc.SpawnAt(g, n, at, p.transform.position - at, new[] { "I was passing by and thought of you!" });
        visitor.label = AHLoc.T("Visitor", "زائر");
        var F = AHJson.O(AHFriends.All, n); string gift = AHJson.S(F, "gift");
        var vv = visitor;
        visitor.onTalk = () =>
        {
            if (AHPrefs.GetString("ah_visit_day", "") == day) { g.ui.TalkLine(vv, AHLoc.T("Lovely place you have here.", "مكان جميل لديك هنا.")); return true; }
            AHPrefs.SetString("ah_visit_day", day);
            g.ui.TalkLine(vv, AHLoc.T("I brought you something. Don't tell the others!", "أحضرت لك شيئًا. لا تخبر الآخرين!"));
            var r = new AHDaily.Reward { name = AHLoc.T("A gift from ", "هدية من ") + n, money = 30 + p.level * 4 };
            if (AHItems.Get(gift) != null) r.items.Add(new KeyValuePair<string, int>(gift, 2));
            AHDaily.Grant(g, r);
            return true;
        };
        g.ui.Toast(AHLoc.T(n + " has come to visit your homestead!", n + " جاء لزيارة مزرعتك!"), 4f);
    }

    // ===================================== every frame (from the UI) =====================================
    public static void Tick(AHGame g, float dt)
    {
        if (g == null || g.player == null || g.ui == null || AHLoading.Showing) return;
        try { LegendTick(g, dt); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: legend (" + e.Message + ")"); legendSpawnT = 120f; }
        try { DragonTick(g, dt); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: sky dragon (" + e.Message + ")"); dragon = null; }
        try { VisitTick(g, dt); } catch (Exception e) { Debug.LogWarning("Ashen Hollow: visitor (" + e.Message + ")"); }
    }
}
