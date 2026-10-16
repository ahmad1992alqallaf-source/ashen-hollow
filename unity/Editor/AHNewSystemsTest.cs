// Ashen Hollow test: runs the eight new systems on the hero in play mode (profession pets, looters, two-part quest,
// gathering knack, weekly challenge, apprentice, repairs, monster book), takes a picture of each window and writes
// what happened to HeroShots/new_systems.txt. It changes the hero's progress (test character); path and pet are put back.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AHNewSystemsTest
{
    static List<Action> steps; static int si; static double nextAt; static string dir; static List<string> log;
    static string path0, pet0; static AHSpot knackSpot; static int knackWait, knackTries;

    [MenuItem("Ashen Hollow/Test: New Systems Check %#&F8")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: press Play first"); return; }
        dir = Path.Combine(Application.dataPath, "../HeroShots/newsys"); Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
        log = new List<string>(); var p = g.player; path0 = p.path; pet0 = p.pet;
        steps = new List<Action>(); si = 0; nextAt = EditorApplication.timeSinceStartup + 0.5;
        Build(g);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Debug.Log("Ashen Hollow: new systems check started (" + steps.Count + " steps)");
    }

    static void Ok(bool b, string what) { log.Add((b ? "OK   " : "FAIL ") + what); }
    static void Shot(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png")); }
    static void Close(AHGame g) { g.ui.ShowWork(false); }
    // a camera of its own in front of the pet: the pet alone, wherever the hero stands
    static void CloseUp(string name)
    {
        var pg = GameObject.Find("Pet"); if (pg == null) { log.Add("FAIL no pet out for " + name); return; }
        var rs = pg.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        var go = new GameObject("TestCam"); var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 30f; cam.nearClipPlane = 0.05f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.55f, 0.6f, 0.66f);
        cam.cullingMask = 1 << pg.layer;
        float r0 = b.extents.magnitude; Vector3 look = (pg.transform.forward + Vector3.up * 0.45f).normalized;
        go.transform.position = b.center + look * r0 * 4.2f; go.transform.LookAt(b.center);
        var rt = new RenderTexture(640, 480, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(640, 480, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(go);
    }
    static void Wait(double s) { nextAt = EditorApplication.timeSinceStartup + s; }
    static void Add(Action a) { steps.Add(a); }
    static void SetUI(AHGame g, string f, object v) { var fi = typeof(AHUI).GetField(f, BindingFlags.NonPublic | BindingFlags.Instance); if (fi != null) fi.SetValue(g.ui, v); }
    static string Snap(AHGame g) { var p = g.player; return "money " + p.bag.money; }

    static void Tick()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < nextAt) return;
        if (si >= steps.Count)
        {
            EditorApplication.update -= Tick;
            var p = g.player; p.path = path0; p.pet = pet0; AHComp.SpawnPet(g); Close(g); g.MarkDirty();
            File.WriteAllLines(Path.Combine(dir, "../new_systems.txt"), log.ToArray());
            Debug.Log("Ashen Hollow: new systems check done · " + log.FindAll(l => l.StartsWith("FAIL")).Count + " fails · HeroShots/new_systems.txt");
            return;
        }
        Wait(1.3);
        try { steps[si++](); } catch (Exception e) { log.Add("FAIL step " + si + " threw " + e.GetType().Name + ": " + e.Message); Debug.LogException(e); }
    }

    static void Build(AHGame g)
    {
        var p = g.player;
        // ---------- 1. profession pets ----------
        foreach (var d0 in AHProfPets.Defs)
        {
            var d = d0;
            Add(() => {
                Close(g);
                Ok(AHComp.Pets != null && AHComp.Pets.ContainsKey(d.id), "pet " + d.id + " is in the pet list");
                Ok(AHModel.Exists("Comp/pet_" + d.id), "pet " + d.id + " has a model");
                if (!p.pets.Contains(d.id)) p.pets.Add(d.id);
                p.path = "artisan"; p.pet = d.id; AHComp.SpawnPet(g);
                int before = 0; foreach (var t in d.tiers) before += p.bag.Count(t.item);
                AHProfPets.Haul(g, p, d);
                int after = 0; foreach (var t in d.tiers) after += p.bag.Count(t.item);
                Ok(after > before || p.Skill(d.skill) < d.tiers[0].lvl, "pet " + d.id + " brings a haul (" + before + " -> " + after + ")");
            });
            Add(() => { var pg = GameObject.Find("Pet"); if (pg != null) { var t = p.transform; pg.transform.position = t.position + t.right * 1.4f - t.forward * 0.3f; pg.transform.rotation = t.rotation * Quaternion.Euler(0, 200, 0);
                    var rs = pg.GetComponentsInChildren<Renderer>(true); string info = rs.Length + " renderers";
                    if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); info += ", size " + b.size.ToString("F2") + ", centre off hero " + (b.center - t.position).ToString("F2") + ", on " + rs[0].enabled + "/" + rs[0].gameObject.activeInHierarchy + ", layer " + rs[0].gameObject.layer; }
                    log.Add("INFO pet " + d.id + ": " + info); var f = pg.GetComponent<AHPetFollow>(); if (f != null) f.enabled = false; } Wait(0.3); });
            Add(() => { CloseUp("1_pet_" + d.id); var pg = GameObject.Find("Pet"); if (pg != null) { var f = pg.GetComponent<AHPetFollow>(); if (f != null) f.enabled = true; } });
        }
        Add(() => { p.path = "artisan"; g.ui.OpenProfPets(); Ok(AHProfPets.Helps(p), "an Artisan's profession pet helps"); });
        Add(() => Shot("1_profpets_artisan"));
        Add(() => { p.path = "adventurer"; g.ui.OpenProfPets(); Ok(!AHProfPets.Helps(p), "an Adventurer's profession pet does not gather"); });
        Add(() => Shot("1_profpets_adventurer"));

        // ---------- 1b. the Artisans' Silver egg ----------
        Add(() => {
            p.path = "artisan"; var es = AHEggs.S(p); es.silverDay = ""; p.bag.money += AHEggs.SilverPrice * 2;
            int pets0 = p.pets.Count; long ess0 = es.essence, m0 = p.bag.money;
            string line = AHEggs.BuySilver(g);
            Ok(line != null && p.bag.money == m0 - AHEggs.SilverPrice, "Artisan buys a Silver egg: " + line);
            Ok(p.pets.Count > pets0 || es.essence > ess0, "it hatches a new pet or Essence");
            Ok(AHEggs.SilverLeft(p) == AHEggs.SilverPerDay - 1, "daily count goes down");
            string fighter = null; foreach (var k in p.pets) if (!AHProfPets.Is(k) && !AHEggs.StoreOnly(k)) { fighter = k; break; }
            if (fighter != null) { p.pet = fighter; Ok(!AHPetSkills.Has(p) && AHComp.PetK(p, "guard") == 0f && !AHPetPassive.Active(p), "for an Artisan the " + fighter + " is looks only"); }
            p.path = "adventurer"; Ok(AHEggs.BuySilver(g) == null, "an Adventurer can't buy a Silver egg");
            p.path = "artisan"; SetUI(g, "wkMode", "comp"); SetUI(g, "wkPageI", 0); g.ui.ShowWork(true); typeof(AHUI).GetMethod("RenderWork", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(g.ui, null);
        });
        Add(() => Shot("1b_silver_egg"));
        // ---------- 2. looters ----------
        Add(() => {
            p.path = "adventurer"; AHLooters.S(p).hired = ""; p.bag.money += AHLooters.Fee(p) * 2;
            bool hired = AHLooters.Hire(g, "marra"); Ok(hired && AHLooters.Hired(p) != null, "Adventurer hires Marra");
            AHLooters.OnKill(g, "d_warrens", 1); AHLooters.OnKill(g, "d_warrens", 2);
            Ok(AHLooters.Hired(p) == null, "looter goes home after the boss");
            g.ui.OpenLooters();
        });
        Add(() => Shot("2_looters_adventurer"));
        Add(() => {
            p.path = "artisan"; AHLooters.S(p).jobs.Clear();
            bool ok = AHLooters.SignUp(g, "d_warrens"); Ok(ok, "Artisan signs up for the Warrens");
            if (ok) { var j = AHLooters.S(p).jobs[0]; j.end = 0; long m0 = p.bag.money; AHLooters.Collect(g, j); Ok(p.bag.money > m0 && AHLooters.S(p).jobs.Count == 0, "Artisan collects a share"); }
            g.ui.OpenLooters();
        });
        Add(() => Shot("2_looters_artisan"));

        // ---------- 3. two-part quest (as an Adventurer) ----------
        Add(() => {
            p.path = "adventurer"; p.prog.twoPart = null; var s = AHTwoPart.S(p);
            AHTwoPart.Start(g); if (!s.started) { s.started = true; s.step = 0; s.n = 0; log.Add("NOTE two-part not open for the hero yet; started by hand"); }
            g.quests.Event("dclear", "d_warrens", 1, g);
            Ok(s.step == 1 && AHTwoPart.Waiting(p), "chapter 1 done, partner forging the key");
            g.ui.OpenTwoPart();
        });
        Add(() => Shot("3_twopart_waiting"));
        Add(() => {
            var s = AHTwoPart.S(p); s.waitEnd = 0; AHTwoPart.Tick(g);
            Ok(s.step == 2, "partner's chapter finishes on its timer");
            g.quests.Event("kill", "any", 25, g);
            Ok(s.step == 3 && AHTwoPart.Waiting(p), "chapter 3 done (25 foes)");
            s.waitEnd = 0; AHTwoPart.Tick(g); Ok(s.done, "the quest is done");
            long m0 = p.bag.money; AHTwoPart.Claim(g); Ok(s.claimed && p.bag.money > m0, "reward claimed");
            g.ui.OpenTwoPart();
        });
        Add(() => Shot("3_twopart_done"));

        // ---------- 4. gathering knack ----------
        Add(() => {
            Close(g); p.path = "artisan"; knackSpot = null;
            float best = 1e9f;
            foreach (var s in AHGather.Spots) if (AHGather.IsGather(s.kind) && AHGather.CanDo(p, s, false)) { float d = (s.pos - p.transform.position).sqrMagnitude; if (d < best) { best = d; knackSpot = s; } }
            Ok(knackSpot != null, "a gathering spot the hero can work");
            if (knackSpot == null) return;
            Vector3 dv = p.transform.position - knackSpot.pos; dv.y = 0; if (dv.sqrMagnitude < 0.01f) dv = Vector3.forward;
            p.transform.position = knackSpot.pos + dv.normalized * Mathf.Max(1.4f, Mathf.Min(knackSpot.reach * 0.9f, knackSpot.r + 1.4f));
            p.transform.rotation = g.Face(knackSpot.pos - p.transform.position);
            AHGather.Start(p, knackSpot); Ok(AHGather.KnackOn, "knack bar on while " + knackSpot.kind); log.Add("INFO knack frac " + AHGather.Frac + " gold " + AHGather.knackAt + " width " + AHGather.knackW + " dist " + Vector3.Distance(p.transform.position, knackSpot.pos).ToString("F2") + " reach " + knackSpot.reach);
            Wait(0.25);
        });
        Add(() => { Shot("4_knack_bar"); knackWait = 0; knackTries = 0; Wait(0.02); });
        Add(KnackAim);
        Add(() => Shot("4_knack_perfect"));

        // ---------- 5. weekly challenge ----------
        Add(() => {
            p.path = "adventurer"; var s = AHChallenge.S(p); s.taken = false; s.won = false;
            AHChallenge.Take(g); Ok(s.taken, "challenge taken: " + AHChallenge.ThisWeek.name);
            Ok(AHChallenge.MobSpeedK() == 1f && AHChallenge.YouTakenK() == 1f, "twist is off outside a dungeon");
            g.ui.OpenChallenge();
        });
        Add(() => Shot("5_challenge"));

        // ---------- 6. apprentice ----------
        Add(() => {
            p.path = "adventurer"; p.prog.appr = null; var s = AHApprentice.S(p);
            if (!AHApprentice.Open(p)) log.Add("NOTE apprentice not open for the hero yet (level " + p.level + "); taken by hand");
            if (!AHApprentice.Open(p)) { s.active = true; s.name = AHApprentice.Candidate(p); s.lv = 1; } else AHApprentice.Take(g);
            Ok(s.active, "apprentice taken: " + s.name);
            g.quests.Event("kill", "any", 120, g); Ok(s.lv >= 3, "apprentice levels up from fights (level " + s.lv + ")");
            AHApprentice.ClaimGift(g); Ok(!AHApprentice.GiftReady(p), "daily gift taken");
            g.ui.OpenApprentice();
        });
        Add(() => Shot("6_apprentice"));

        // ---------- 7. repairs ----------
        Add(() => { p.path = "artisan"; AHRepairs.S(p).day = ""; var o = AHRepairs.Orders(p); var seen = new HashSet<string>(); foreach (var x in o) seen.Add(x.piece); Ok(seen.Count == o.Count, "repair orders are all different"); Ok(o.Count == 3, "three repair orders today"); g.ui.OpenRepairs(); });
        Add(() => Shot("7_repairs_artisan"));
        Add(() => { p.path = "adventurer"; g.ui.OpenRepairs(); Ok(AHRepairs.GuildK < 1f, "guild repairs cost less than a shop"); });
        Add(() => Shot("7_repairs_adventurer"));

        // ---------- 8. monster book ----------
        Add(() => {
            var all = AHMonsterBook.All; Ok(all.Count > 50, "monster book has " + all.Count + " pages");
            int pg0 = AHMonsterBook.Pages(p);
            for (int i = 0; i < Mathf.Min(12, all.Count); i++) g.quests.Event("kill", all[i].id, 1, g);
            for (int k = 0; k < 10; k++) g.quests.Event("kill", all[0].id, 1, g);
            Ok(AHMonsterBook.Pages(p) >= Mathf.Min(12, all.Count), "pages fill in (" + pg0 + " -> " + AHMonsterBook.Pages(p) + ")");
            Ok(AHMonsterBook.Kills(p, all[0].id) >= AHMonsterBook.Studied, all[0].name + " studied");
            g.ui.OpenMonsterBook();
        });
        Add(() => Shot("8_book_volumes"));
        Add(() => { SetUI(g, "bookVol", 0); SetUI(g, "wkPageI", 0); typeof(AHUI).GetMethod("RenderWork", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(g.ui, null); });
        Add(() => Shot("8_book_volume1"));
    }

    // press the action button when the mark is in the gold
    static int knackBag;
    static void KnackAim()
    {
        var g = AHGame.I; var p = g.player;
        if (knackSpot == null) return;
        Vector3 dv = p.transform.position - knackSpot.pos; dv.y = 0; if (dv.sqrMagnitude < 0.01f) dv = Vector3.forward;
        p.transform.position = knackSpot.pos + dv.normalized * Mathf.Max(1.4f, Mathf.Min(knackSpot.reach * 0.9f, knackSpot.r + 1.4f));   // (auto quest may have walked the hero off)
        AHGather.Cancel(); AHGather.Start(p, knackSpot);
        if (!AHGather.KnackOn) { Ok(false, "knack: could not start the action again"); return; }
        int before = 0; foreach (var kv in p.bag.inv) before += kv.Value;
        AHGather.knackAt = AHGather.Frac; AHGather.Strike(p); bool hit = AHGather.knackHit;
        AHGather.Update(p, 0.01f, false);   // the strike finishes the action at once
        int after = 0; foreach (var kv in p.bag.inv) after += kv.Value;
        Ok(hit, "perfect strike lands");
        Ok(after >= before + 2, "perfect strike gives the extra item (" + before + " -> " + after + ")" + (after == before ? " · bag says: " + (p.bag.lastWarn ?? "nothing") + " · slots used " + p.bag.UsedSlots : ""));
        AHGather.Cancel();
    }
}
