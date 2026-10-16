// Ashen Hollow test: a play-through of the new systems the way a player meets them, through the game's own paths.
// As an Adventurer: hire a looter, take the weekly challenge, go into the Mire Warrens and defeat every foe there down
// to the boss (the dungeon's own kill handling pays the looter, counts the challenge, fills the Monster book and
// teaches the apprentice). Back outside as an Artisan: gather at a real spot with a real timed knack strike, and let
// the profession pet bring its haul on the game's own timer. Writes HeroShots/playthrough.txt and pictures to
// HeroShots/play/. Changes the hero's progress (test character); path, pet and auto quest are put back.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHPlayThroughTest
{
    static List<Func<bool>> steps; static int si; static double nextAt; static string dir; static List<string> log;
    static string path0, pet0, area0; static bool auto0; static Vector3 pos0;
    static int pages0, wins0, runs0, apprXp0, apprLv0, kills, bag0; static AHSpot spot; static double stepStart;

    [MenuItem("Ashen Hollow/Test: Play-Through Check %#&F9")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: press Play first"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/play"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        log = new List<string>(); var p = g.player;
        path0 = p.path; pet0 = p.pet; area0 = AHGame.AreaId; pos0 = p.transform.position; auto0 = AHAuto.On;
        steps = new List<Func<bool>>(); si = 0; nextAt = EditorApplication.timeSinceStartup + 0.5; stepStart = -1;
        Build(AHGame.I);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Debug.Log("Ashen Hollow: play-through check started");
    }

    static void Ok(bool b, string what) { log.Add((b ? "OK   " : "FAIL ") + what); }
    static void Shot(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png")); }
    static double Now { get { return EditorApplication.timeSinceStartup; } }
    static double InStep { get { if (stepStart < 0) stepStart = Now; return Now - stepStart; } }
    // a step returns true when it is done; false to be called again next frame
    static void Add(Func<bool> f) { steps.Add(f); }
    static void Do(Action a, double wait = 1.2) { steps.Add(() => { a(); nextAt = Now + wait; return true; }); }

    static void Tick()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorApplication.update -= Tick; return; }
        if (Now < nextAt) return;
        if (si >= steps.Count)
        {
            EditorApplication.update -= Tick;
            var p = g.player; p.path = path0; p.pet = pet0; AHComp.SpawnPet(g); if (AHAuto.On != auto0) AHAuto.Toggle(g); g.ui.ShowWork(false); g.MarkDirty();
            File.WriteAllLines(Path.Combine(dir, "../playthrough.txt"), log.ToArray());
            Debug.Log("Ashen Hollow: play-through check done · " + log.FindAll(l => l.StartsWith("FAIL")).Count + " fails · HeroShots/playthrough.txt");
            return;
        }
        bool done;
        try { done = steps[si](); } catch (Exception e) { log.Add("FAIL step " + si + " threw " + e.GetType().Name + ": " + e.Message); Debug.LogException(e); done = true; }
        if (done) { si++; stepStart = -1; }
    }

    static void TravelTo(AHGame g, string id)
    {
        var ta = Resources.Load<TextAsset>("AH/Areas/" + id); if (ta == null) { log.Add("FAIL no area " + id); return; }
        var d = JsonUtility.FromJson<AHWorldData>(ta.text); g.Travel(id, d.spawn.x, d.spawn.z, 0f);
    }
    static void FullHealth(AHPlayer h) { h.hp = h.maxHp; }

    // the hero (a new one each time the game travels to another area)
    static AHPlayer p { get { return AHGame.I.player; } }

    static AHGame g { get { return AHGame.I; } }   // (rebuilt when the game travels)
    static void Build(AHGame g0)
    {
        // ---------- getting ready, as an Adventurer ----------
        Do(() => {
            if (AHAuto.On) AHAuto.Toggle(g);
            p.path = "adventurer"; AHDungeon.ResetAll(); p.bag.money += 20000L * AHDB.CU; p.bag.Touch();
            var ls = AHLooters.S(p); ls.hired = ""; runs0 = ls.runs;
            Ok(AHLooters.Hire(g, "marra"), "hire Marra the miner for the next run");
            var cs = AHChallenge.S(p); cs.taken = false; cs.won = false; wins0 = cs.wins; AHChallenge.Take(g);
            Ok(cs.taken, "take this week's challenge: " + AHChallenge.ThisWeek.name);
            var a = AHApprentice.S(p); if (!a.active) { a.active = true; a.name = AHApprentice.Candidate(p); a.lv = 1; a.xp = 0; }
            if (a.lv >= AHApprentice.MaxLv) { a.lv = 5; a.xp = 0; }
            apprXp0 = (int)a.xp; apprLv0 = a.lv; pages0 = AHMonsterBook.Pages(p); kills = 0;
        });
        // ---------- into the Mire Warrens ----------
        Do(() => TravelTo(g, "d_warrens"), 8);
        Do(() => { Ok(AHGame.AreaId == "d_warrens", "in the Mire Warrens (" + AHGame.AreaId + ")"); Ok(AHChallenge.MobSpeedK() != 1f || AHChallenge.MobTakenK() != 1f || AHChallenge.YouTakenK() != 1f || AHChallenge.ThisWeek.id == "dry", "the twist is on inside the dungeon"); Shot("1_warrens"); }, 1);
        // wait for the dungeon's foes to come in
        Add(() => {
            int live = 0; foreach (var m in g.mobs) if (m != null && !m.dead) live++;
            if (live > 0 || InStep > 20) { log.Add("INFO foes in the Warrens: " + live + " (after " + InStep.ToString("0.0") + "s)"); return true; }
            return false;
        });
        // defeat every foe, nearest first, standing by each (the boss last: the dungeon decides when it wakes)
        Add(() => {
            FullHealth(p);
            AHMob next = null; float bd = float.MaxValue; string boss = AHDungeon.BossOf("d_warrens");
            foreach (var m in g.mobs) { if (m == null || m.dead) continue; float d = (m.transform.position - p.transform.position).sqrMagnitude; if (m.type.id == boss) d += 1e6f; if (d < bd) { bd = d; next = m; } }
            if (next == null || kills > 120 || InStep > 150) { Ok(next == null, "every foe in the Warrens defeated (" + kills + ")"); nextAt = Now + 3; return true; }
            Vector3 dv = p.transform.position - next.transform.position; dv.y = 0; if (dv.sqrMagnitude < 0.01f) dv = Vector3.forward;
            p.transform.position = next.transform.position + dv.normalized * 2f;
            if (next.type.id == boss) Shot("2_boss");
            next.Hurt(999999, p, true); kills++;
            nextAt = Now + (next.type.id == boss ? 1.5 : 0.35);
            return false;
        });
        Do(() => {
            Shot("3_cleared");
            var cs = AHChallenge.S(p); Ok(cs.won && cs.wins == wins0 + 1, "the weekly challenge counts as won");
            Ok(AHLooters.Hired(p) == null && AHLooters.S(p).runs == runs0 + 1, "Marra brought her haul and went home");
            Ok(AHMonsterBook.Pages(p) > pages0 || pages0 >= 20, "new Monster book pages (" + pages0 + " -> " + AHMonsterBook.Pages(p) + ")");
            var a = AHApprentice.S(p); Ok(a.lv > apprLv0 || a.xp > apprXp0, "the squire learned from the fights (level " + apprLv0 + " -> " + a.lv + ")");
        }, 3);
        // ---------- back out, as an Artisan ----------
        Do(() => TravelTo(g, area0 == "d_warrens" ? "meadow" : area0), 8);
        Do(() => {
            p.path = "artisan"; spot = null; float best = 1e9f;
            foreach (var s in AHGather.Spots) if ((s.kind == "chop" || s.kind == "mine") && AHGather.CanDo(p, s, false)) { float d = (s.pos - p.transform.position).sqrMagnitude; if (d < best) { best = d; spot = s; } }
            Ok(spot != null, "a tree or a rock to work");
            if (spot == null) return;
            Vector3 dv = p.transform.position - spot.pos; dv.y = 0; if (dv.sqrMagnitude < 0.01f) dv = Vector3.forward;
            p.transform.position = spot.pos + dv.normalized * Mathf.Max(1.4f, Mathf.Min(spot.reach * 0.9f, spot.r + 1.4f));
            p.transform.rotation = g.Face(spot.pos - p.transform.position);
            bag0 = 0; foreach (var kv in p.bag.inv) bag0 += kv.Value;
            AHGather.Start(p, spot);
        }, 0.3);
        // the knack, timed for real: press when the mark is in the gold
        Add(() => {
            if (!AHGather.KnackOn) { Ok(false, "the work stopped before the gold came round"); return true; }
            if (InStep < 0.05) Shot("4_knack_bar");
            if (Mathf.Abs(AHGather.Frac - AHGather.knackAt) <= AHGather.knackW * 0.35f) { AHGather.Strike(p); Ok(AHGather.knackHit, "a perfect strike, pressed in time"); nextAt = Now + 0.8; return true; }
            if (InStep > 20) { Ok(false, "the mark never reached the gold"); return true; }
            return false;
        });
        Do(() => {
            int n = 0; foreach (var kv in p.bag.inv) n += kv.Value;
            Ok(n >= bag0 + 2, "the perfect strike gave the extra item (" + bag0 + " -> " + n + ")");
            AHGather.Cancel(); Shot("5_after_strike");
        }, 1);
        // ---------- the profession pet's own haul ----------
        Do(() => {
            if (!p.pets.Contains("mole")) p.pets.Add("mole");
            p.pet = "mole"; AHComp.SpawnPet(g); p.prog.profPetPet = "mole"; p.prog.profPetAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1;
            bag0 = 0; foreach (var kv in p.bag.inv) bag0 += kv.Value;
        }, 7);   // the game's own five-second tick brings it
        Do(() => {
            int n = 0; foreach (var kv in p.bag.inv) n += kv.Value;
            Ok(n > bag0, "the mole brought ore on its own timer (" + bag0 + " -> " + n + ")");
            Shot("6_mole_haul");
        }, 1);
        // ---------- the Silver egg and the looks-only rule ----------
        Do(() => {
            var es = AHEggs.S(p); es.silverDay = ""; p.bag.money += AHEggs.SilverPrice; string l = AHEggs.BuySilver(g);
            Ok(l != null, "the Silver egg hatches: " + l);
            string fighter = null; foreach (var k in p.pets) if (!AHProfPets.Is(k) && !AHEggs.StoreOnly(k)) { fighter = k; break; }
            if (fighter != null) { p.pet = fighter; AHComp.SpawnPet(g); p.Recalc(); Ok(!AHPetSkills.Has(p) && !AHPetPassive.Active(p), "for an Artisan the " + fighter + " follows for looks only"); }
            Shot("7_silver_egg");
        }, 1.5);
        // ---------- home again ----------
        Do(() => { p.transform.position = pos0; }, 0.5);
    }
}
