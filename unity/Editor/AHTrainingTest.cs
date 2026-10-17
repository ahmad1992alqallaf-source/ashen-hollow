// Ashen Hollow test: Dawnrest Isle, the training island (ctrl+shift+alt+F6, in Play mode). Visits the island with the
// current hero (as Settings > Dawnrest Isle does) and plays every lesson through the game's own paths: walking to the
// light, turning the camera, talking to Elara, chopping, mining, cooking at her fire, the bag, equipping the training
// weapon, the toads, a spell, dodging the red circles, a potion, the map, and Oskar's boat home. Then checks that
// nothing counted: the same XP, skills and money, the same bag, and back where the hero was.
// Writes HeroShots/training.txt and pictures to HeroShots/training/.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHTrainingTest
{
    static List<Func<bool>> steps; static int si; static double nextAt, stepStart = -1; static string dir; static List<string> log;
    static string area0, path0; static long xp0, money0; static long skills0; static int items0; static string bagSig0;

    static AHGame g { get { return AHGame.I; } }
    static AHPlayer p { get { return AHGame.I.player; } }
    static double Now { get { return EditorApplication.timeSinceStartup; } }
    static double InStep { get { if (stepStart < 0) stepStart = Now; return Now - stepStart; } }

    [MenuItem("Ashen Hollow/Test: Training Island Check %#&F6")]
    static void Run()
    {
        if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: press Play first"); return; }
        if (!AHTraining.Exists) { Debug.LogWarning("Ashen Hollow: Resources/AH/Areas/tutorial.json is missing: Dawnrest Isle is not in the project"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/training"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        log = new List<string>(); steps = new List<Func<bool>>(); si = 0; stepStart = -1; nextAt = Now + 0.5;
        Build();
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Debug.Log("Ashen Hollow: training island check started");
    }

    static void Ok(bool b, string what) { log.Add((b ? "OK   " : "FAIL ") + what); }
    static void Shot(string n) { ScreenCapture.CaptureScreenshot(Path.Combine(dir, n + ".png")); }
    static void Add(Func<bool> f) { steps.Add(f); }
    static void Do(Action a, double wait = 1.0) { steps.Add(() => { a(); nextAt = Now + wait; return true; }); }

    static void Tick()
    {
        if (!Application.isPlaying || g == null || g.player == null) { EditorApplication.update -= Tick; return; }
        if (Now < nextAt) return;
        if (si >= steps.Count)
        {
            EditorApplication.update -= Tick;
            File.WriteAllLines(Path.Combine(dir, "../training.txt"), log.ToArray());
            Debug.Log("Ashen Hollow: training island check done · " + log.FindAll(l => l.StartsWith("FAIL")).Count + " fails · HeroShots/training.txt");
            return;
        }
        bool done;
        try { done = steps[si](); } catch (Exception e) { log.Add("FAIL step " + si + " threw " + e.GetType().Name + ": " + e.Message); Debug.LogException(e); done = true; }
        if (done) { si++; stepStart = -1; }
    }

    static long SkillSum() { long n = 0; foreach (var k in new[] { "attack", "woodcutting", "mining", "cooking", "fishing" }) n += p.SkillXp(k); return n; }
    static string BagSig() { var l = new List<string>(); foreach (var kv in p.bag.inv) l.Add(kv.Key + "*" + kv.Value); foreach (var kv in p.bag.gear) l.Add(kv.Key + "=" + kv.Value); l.Sort(); return string.Join(",", l.ToArray()); }

    static AHSpot Nearest(string kind)
    {
        AHSpot best = null; float bd = 1e9f;
        foreach (var s in AHGather.Spots) if (s.kind == kind && Time.time >= s.until) { float d = (s.pos - p.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = s; } }
        return best;
    }
    static void StandBy(Vector3 at, float gap)
    {
        Vector3 dv = p.transform.position - at; dv.y = 0f; if (dv.sqrMagnitude < 0.01f) dv = Vector3.forward;
        p.transform.position = g.Resolve(at + dv.normalized * gap, 0.4f);
        p.transform.rotation = g.Face(at - p.transform.position);
    }
    // wait until the lesson moves on (or give up after `limit` seconds)
    static Func<bool> Until(string lesson, float limit, Action each = null)
    {
        return () =>
        {
            var c = AHTraining.Cur;
            if (c == null || c.id != lesson) { Ok(true, "lesson done: " + lesson + " (" + InStep.ToString("0.0") + "s)"); nextAt = Now + 0.8; return true; }
            if (InStep > limit) { Ok(false, "lesson stuck: " + lesson); AHTraining.Step = AHTraining.Step + 1; return true; }
            if (each != null) each();
            return false;
        };
    }
    static void Lesson(string id, Action start, float limit = 25f, Action each = null, double startWait = 0.5)
    {
        Do(() => { var c = AHTraining.Cur; if (c == null || c.id != id) { log.Add("INFO lesson " + id + " skipped here (now " + (c != null ? c.id : "none") + ")"); return; } start(); }, startWait);
        Add(Until(id, limit, each));
    }

    static void Build()
    {
        Do(() =>
        {
            area0 = AHGame.AreaId; path0 = p.path; xp0 = p.ClassXp; money0 = p.bag.money; skills0 = SkillSum(); bagSig0 = BagSig();
            items0 = p.bag.inv.Count;
            if (AHAuto.On) AHAuto.Toggle(g);
            if (p.path == "artisan") p.path = "adventurer";   // all the lessons
            AHTraining.Visit(g);
        }, 9);
        Do(() =>
        {
            Ok(AHGame.AreaId == AHTraining.Area, "on Dawnrest Isle (" + AHGame.AreaId + ")");
            Ok(AHTraining.Active && AHTraining.Step == 0, "the first lesson is up: " + (AHTraining.Cur != null ? AHTraining.Cur.id : "none"));
            Ok(AHNpc.Find("Guide Elara") != null && AHNpc.Find("Ferryman Oskar") != null, "Guide Elara and Ferryman Oskar are on the island");
            Ok(Nearest("chop") != null && Nearest("mine") != null && Nearest("fire") != null, "trees, rocks and the campfire are there");
            Shot("01_arrive");
        }, 1);
        Lesson("move", () => { var t = AHTraining.Target(g); if (t.HasValue) p.transform.position = g.Resolve(t.Value, 0.4f); });
        Lesson("look", () => { }, 10f, () => { g.camYaw += 6f; });
        Lesson("talk", () => { var n = AHNpc.Find("Guide Elara"); StandBy(n.transform.position, 1.6f); g.TalkTo(n); Shot("02_elara"); });
        Lesson("chop", () => { var s = Nearest("chop"); StandBy(s.pos, Mathf.Max(1.4f, s.r + 1.2f)); AHGather.Start(p, s); }, 30f, () => { if (!AHGather.KnackOn && InStep > 3) { var s = Nearest("chop"); if (s != null) { StandBy(s.pos, Mathf.Max(1.4f, s.r + 1.2f)); AHGather.Start(p, s); } } });
        Lesson("mine", () => { var s = Nearest("mine"); StandBy(s.pos, Mathf.Max(1.4f, s.r + 1.2f)); AHGather.Start(p, s); Shot("03_mine"); }, 30f, () => { if (!AHGather.KnackOn && InStep > 3) { var s = Nearest("mine"); if (s != null) { StandBy(s.pos, Mathf.Max(1.4f, s.r + 1.2f)); AHGather.Start(p, s); } } });
        Lesson("cook", () => { var s = Nearest("fire"); StandBy(s.pos, 1.6f); AHGather.Start(p, s); Shot("04_cook"); }, 25f, () => { if (InStep > 6 && (int)InStep % 5 == 0) { var s = Nearest("fire"); if (s != null) AHGather.Start(p, s); } });
        Lesson("bag", () => { g.ui.ShowBag(true); Shot("05_bag"); });
        Do(() => g.ui.ShowBag(false), 0.3);
        Lesson("gear", () => { var c = p.bag.inv; foreach (var id in new List<string>(c.Keys)) { var it = AHItems.Get(id); if (it != null && it.slot == "weapon" && AHItems.CanUse(id, p.cls.id)) { p.bag.Equip(id, p.cls.id); p.Recalc(); break; } } });
        Lesson("fight", () => { }, 40f, () =>
        {
            AHMob next = null; float bd = 1e9f;
            foreach (var m in g.mobs) if (m != null && !m.dead && m.type.id == "tutrat") { float d = (m.transform.position - p.transform.position).sqrMagnitude; if (d < bd) { bd = d; next = m; } }
            if (next == null) return;
            StandBy(next.transform.position, 1.6f); next.Hurt(999999, p, true); nextAt = Now + 0.6;
        });
        Do(() => Shot("06_fight"), 0.2);
        Lesson("spell", () => { }, 15f, () =>
        {
            AHMob next = null; foreach (var m in g.mobs) if (m != null && !m.dead) { next = m; break; }
            if (next != null) StandBy(next.transform.position, 1.6f);
            p.mana = p.maxMana; for (int i = 0; i < p.Spells.Length; i++) { p.Cast(i); if (p.lastSpellAt > Time.time - 0.1f) break; }
            nextAt = Now + 0.8;
        });
        Lesson("dodge", () => { var t = AHTraining.Target(g); if (t.HasValue) p.transform.position = g.Resolve(t.Value, 0.4f); }, 30f, () =>
        {
            p.hp = p.maxHp;
            // step well out of any circle a moment after it appears
            if (InStep > 1.5 && (int)(InStep * 2) % 4 == 0) { p.transform.position = g.Resolve(p.transform.position + p.transform.right * 4.5f, 0.4f); Shot("07_dodge"); nextAt = Now + 0.6; }
        });
        Lesson("potion", () => { p.QuickPotion(false); });
        Lesson("map", () => { g.ui.OpenMap(); Shot("08_map"); }, 10f);
        Do(() => g.ui.ShowWork(false), 0.3);
        Do(() =>
        {
            Ok(p.ClassXp == xp0, "no class XP on the island (" + xp0 + " -> " + p.ClassXp + ")");
            Ok(SkillSum() == skills0, "no skill XP on the island (" + skills0 + " -> " + SkillSum() + ")");
            Ok(AHTraining.Cur != null && AHTraining.Cur.id == "ferry", "only Oskar is left: " + (AHTraining.Cur != null ? AHTraining.Cur.id : "none"));
            var n = AHNpc.Find("Ferryman Oskar"); StandBy(n.transform.position, 1.6f); Shot("09_oskar"); g.TalkTo(n);
        }, 9);
        Do(() =>
        {
            Ok(AHGame.AreaId == area0, "back home in " + area0 + " (" + AHGame.AreaId + ")");
            Ok(!AHTraining.Active && AHTraining.Step == AHTraining.Done, "the training is finished");
            Ok(p.ClassXp == xp0 && SkillSum() == skills0, "still no XP gained");
            Ok(p.bag.money == money0, "the same money as before (" + money0 + " -> " + p.bag.money + ")");
            Ok(BagSig() == bagSig0, "the same bag and gear as before (nothing gathered on the island came back)");
            if (BagSig() != bagSig0) log.Add("INFO before " + bagSig0 + "\nINFO after  " + BagSig());
            p.path = path0; g.MarkDirty();
            Shot("10_home");
        }, 1);
    }
}
