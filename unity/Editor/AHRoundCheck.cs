// Ashen Hollow (editor): one button that walks through the things the phone testers reported and photographs each
// into HeroShots/rc_*.png, so the fixes can be looked at without a phone:
//  - Old Mill Road by day and by night (the road, the water, the stone blocks, the bridge), from six eyes
//  - the signposts at the roads out (readable?)
//  - the bear, boars and wolves, measured the way a phone measures them (AHModel.PhoneLike)
//  - the bag pictures and the big turning view of several weapons
//  - skinning (the kneel and the knife)
//  - Hollow Meadow's flowers with the phone's materials
// Ashen Hollow > Test: Round Check (play mode).
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHRoundCheck
{
    static readonly List<System.Func<bool>> steps = new List<System.Func<bool>>();
    static int at; static double waitUntil;
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: Round Check")]
    static void Run()
    {
        var g = AHGame.I;
        if (!Application.isPlaying || g == null || g.player == null) { EditorUtility.DisplayDialog("Ashen Hollow", "Press Play first.", "OK"); return; }
        AHModel.PhoneLike = true;
        steps.Clear(); at = 0; logLines.Clear();
        Application.logMessageReceived -= Grab; Application.logMessageReceived += Grab;
        // 1. Old Mill Road
        Add(() => Travel("mill")); Wait(14);
        Add(() => { AHGame.I.SetTimeOfDay(0.3f); return true; }); Wait(2);
        Add(() => { Area("rc_mill_day"); Signs("rc_sign"); Near(AHGame.I.player.transform.position, 30f); return true; });
        Add(() => { Bridge(); return true; }); Wait(1.5);
        Add(() => { Hero("rc_bridge"); var sp = AHGame.I.data.spawn; AHGame.I.player.transform.position = AHGame.I.W(sp.x, sp.z); Debug.Log("Ashen Hollow: round check river blocked at its middle: " + AHGame.I.Blocked(AHGame.I.W(2116.9f, 45f), 0.3f) + " / beside: " + AHGame.I.Blocked(AHGame.I.W(2112f, 45f), 0.3f) + " spread " + AHGame.Spread); return true; });
        Add(() => { AHGame.I.SetTimeOfDay(0.8f); return true; }); Wait(2);
        Add(() => { Area("rc_mill_night"); AHGame.I.SetTimeOfDay(0.3f); return true; }); Wait(1);
        // 2. beasts, measured the phone's way
        Add(() => { Beasts(); return true; }); Wait(2);
        Add(() => { BeastShots(); return true; });
        // 3. skinning: a boar falls at the hero's feet and is skinned
        Add(() => { Skin(); return true; }); Wait(0.9);
        Add(() => { Hero("rc_skinning"); return true; }); Wait(1.5);
        // 3b. every work clip, to pick the kneeling one for skinning
        foreach (var c in Clips) { string cc = c; Add(() => { AHGame.I.player.BeginWork(AHGame.I.player.transform.position + AHGame.I.player.transform.forward, cc, null); return true; }); Wait(1.1); Add(() => { Hero("rc_clip_" + cc); return true; }); }
        // 4. weapons in the bag and the big view
        Add(() => { foreach (var id in Weapons) AHItemStudio.Icon(AHItems.Get(id)); return true; }); Wait(4);
        for (int i = 0; i < Weapons.Length; i++) { int k = i; Add(() => { AHItemStudio.View(AHItems.Get(Weapons[k])); return true; }); Wait(0.6); Add(() => { ViewShot(Weapons[k]); return true; }); }
        Add(() => { AHItemStudio.StopView(); return true; });
        // 5. the meadow's flowers in their phone materials
        Add(() => Travel("meadow")); Wait(14);
        Add(() => { AHGame.I.SetTimeOfDay(0.3f); return true; }); Wait(2);
        Add(() => { Area("rc_meadow_day"); Hero("rc_meadow_hero"); return true; });
        Add(() => { AHModel.PhoneLike = false; Debug.Log("Ashen Hollow: round check done (HeroShots/rc_*.png)"); Application.logMessageReceived -= Grab; File.WriteAllLines(Path.Combine(Dir, "rc_log.txt"), logLines.ToArray()); return true; });
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static readonly string[] Clips = { "Work_Gather", "Work_Gather2", "Work_Farm", "Work_Hammer", "Idle_Work" };
    static readonly string[] Weapons = { "bronze_sword", "bronze_mace", "bronze_staff", "bronze_dagger", "oak_bow", "iron_sword" };

    static readonly List<string> logLines = new List<string>();
    static void Grab(string msg, string stack, LogType t) { if (t == LogType.Error || t == LogType.Exception || msg.StartsWith("Ashen Hollow")) logLines.Add(t + ": " + msg + (t == LogType.Exception ? "\n" + stack : "")); }
    static void Add(System.Func<bool> f) { steps.Add(f); }
    // what the plain blocks near a spot are made of (to find the grey boxes)
    static void Near(Vector3 at, float r)
    {
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!mr.enabled) continue; var b = mr.bounds; Vector3 d = b.center - at; d.y = 0; if (d.magnitude > r) continue;
            var m = mr.sharedMaterial; string tex = ""; if (m != null) foreach (var tn in m.GetTexturePropertyNames()) { var t = m.GetTexture(tn); if (t != null) tex += tn + "=" + t.name + "(" + t.width + ") "; }
            string path = mr.name; for (var p = mr.transform.parent; p != null && path.Length < 160; p = p.parent) path = p.name + "/" + path;
            logLines.Add("near " + path + " size " + b.size.ToString("F1") + " mat " + (m != null ? m.name + " [" + m.shader.name + "] " + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor").ToString() : "") : "none") + " " + tex);
        }
    }
    static void Wait(double s) { steps.Add(() => { if (waitUntil <= 0) { waitUntil = EditorApplication.timeSinceStartup + s; return false; } if (EditorApplication.timeSinceStartup < waitUntil) return false; waitUntil = 0; return true; }); }
    static void Tick()
    {
        if (!Application.isPlaying || AHGame.I == null) { if (!Application.isPlaying) { EditorApplication.update -= Tick; AHModel.PhoneLike = false; } return; }
        if (at >= steps.Count) { EditorApplication.update -= Tick; return; }
        bool done;
        try { done = steps[at](); } catch (System.Exception e) { Debug.LogError("Ashen Hollow: round check step " + at + " failed: " + e); done = true; }
        if (done) at++;
    }

    static bool Travel(string id)
    {
        var g = AHGame.I; var ta = Resources.Load<TextAsset>("AH/Areas/" + id); if (ta == null) return true;
        if (AHGame.AreaId == id) return true;
        var d = JsonUtility.FromJson<AHWorldData>(ta.text); g.Travel(id, d.spawn.x, d.spawn.z, 0f); return true;
    }

    static Texture2D Shot(Camera cam, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24) { antiAliasing = 2 }; cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = null; cam.targetTexture = null; rt.Release(); Object.Destroy(rt); return tex;
    }
    static Camera TempCam() { var go = new GameObject("RoundCheckCam"); var c = go.AddComponent<Camera>(); c.fieldOfView = 50f; c.nearClipPlane = 0.05f; c.farClipPlane = 600f; var g = AHGame.I; if (g.cam != null) { c.clearFlags = g.cam.clearFlags; c.backgroundColor = g.cam.backgroundColor; } return c; }
    static void Save(Texture2D t, string name) { File.WriteAllBytes(Path.Combine(Dir, name + ".png"), t.EncodeToPNG()); }

    // six eyes round the land, low enough to see the ground
    static void Area(string name)
    {
        var g = AHGame.I; var b = g.data.bounds; float cx = (b.x0 + b.x1) / 2f, cz = (b.z0 + b.z1) / 2f, w = b.x1 - b.x0, d = b.z1 - b.z0;
        var cam = TempCam(); const int W = 640, H = 360; var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
        var hero = g.player.transform.position;
        Vector3[] eyes = { g.W(b.x0 + w * 0.2f, cz), g.W(b.x0 + w * 0.4f, cz), g.W(b.x0 + w * 0.6f, cz), g.W(b.x0 + w * 0.8f, cz), g.W(cx, b.z0 + d * 0.3f), hero };
        for (int i = 0; i < 6; i++)
        {
            Vector3 e = eyes[i]; e.y = g.Resolve(e, 0.1f).y;
            Vector3 dir = Quaternion.Euler(0, i * 60f, 0) * Vector3.forward;
            cam.transform.position = e + Vector3.up * 6f - dir * 8f; cam.transform.LookAt(e + dir * 12f);
            var t = Shot(cam, W, H); sheet.SetPixels((i % 3) * W, (1 - i / 3) * H, W, H, t.GetPixels()); Object.Destroy(t);
        }
        sheet.Apply(); Save(sheet, name); Object.Destroy(cam.gameObject);
    }

    static void Signs(string name)
    {
        int n = 0; var cam = TempCam(); cam.fieldOfView = 40f;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name != "Pass signpost") continue;
            Vector3 board = t.position + Vector3.up * 2.35f + t.rotation * new Vector3(-1.05f, 0, 0);
            foreach (int side in new[] { -1, 1 })
            {
                cam.transform.position = board + t.forward * side * 3.2f + Vector3.up * -0.4f; cam.transform.LookAt(board);
                var s = Shot(cam, 480, 270); Save(s, name + "_" + (n++)); Object.Destroy(s);
            }
        }
        Object.Destroy(cam.gameObject);
    }

    static readonly List<AHMob> shown = new List<AHMob>();
    static readonly string[] BeastIds = { "bear", "boar", "boarlord", "wolf", "mistfang", "deer" };
    static void Beasts()
    {
        var g = AHGame.I; foreach (var m in shown) if (m != null) Object.Destroy(m.gameObject); shown.Clear();
        var p = g.player.transform; Vector3 right = p.right; right.y = 0; right.Normalize(); Vector3 fwd = p.forward; fwd.y = 0; fwd.Normalize();
        for (int i = 0; i < BeastIds.Length; i++)
        {
            var o = AHJson.O(AHDB.Mobs, BeastIds[i]); if (o == null) continue;
            var t = new AHMobType { id = BeastIds[i], name = AHJson.S(o, "name", BeastIds[i]), hp = 10, lvl = 1, radius = 0.5f };
            var m = AHMob.Create(g, t, g.Resolve(p.position + fwd * 9f + right * (i - 2.5f) * 3.2f, 0.2f)); m.enabled = false;
            m.transform.rotation = Quaternion.LookRotation(right);
            if (m.anim != null) m.anim.Play("Idle", true);
            shown.Add(m);
        }
    }
    static void BeastShots()
    {
        var cam = TempCam(); cam.fieldOfView = 32f; const int W = 400, H = 300;
        var sheet = new Texture2D(W * 3, H * 2, TextureFormat.RGB24, false);
        for (int i = 0; i < shown.Count && i < 6; i++)
        {
            var m = shown[i]; if (m == null) continue; float h = Mathf.Max(1f, m.height);
            Vector3 c = m.transform.position + Vector3.up * h * 0.5f;
            cam.transform.position = c - m.transform.right * (h * 2.6f + 1.5f) + m.transform.forward * 0.8f + Vector3.up * h * 0.25f; cam.transform.LookAt(c);
            var t = Shot(cam, W, H); sheet.SetPixels((i % 3) * W, (1 - i / 3) * H, W, H, t.GetPixels()); Object.Destroy(t);
            Debug.Log("Ashen Hollow: round check beast " + m.type.id + " height " + m.height.ToString("0.00"));
        }
        sheet.Apply(); Save(sheet, "rc_beasts"); Object.Destroy(cam.gameObject);
        foreach (var m in shown) if (m != null) Object.Destroy(m.gameObject); shown.Clear();
    }

    static void Skin()
    {
        var g = AHGame.I; var p = g.player; Vector3 f = p.transform.forward; f.y = 0; f.Normalize();
        var o = AHJson.O(AHDB.Mobs, "boar"); var t = new AHMobType { id = "boar", name = "Boar", hp = 1, lvl = 1, radius = 0.5f, skin = new[] { "boar_hide" } };
        var m = AHMob.Create(g, t, g.Resolve(p.transform.position + f * 1.4f, 0.2f));
        g.mobs.Add(m);
        m.Hurt(99, p, true);
        p.Interact();
    }
    static void Bridge()
    {
        var g = AHGame.I; if (AHScenery.Decks.Count == 0) { Debug.Log("Ashen Hollow: round check: no footbridge here"); return; }
        var d = AHScenery.Decks[0]; g.player.transform.position = new Vector3(d.c.x, g.player.transform.position.y, d.c.z);
        g.player.transform.rotation = d.rot;
    }
    static void Hero(string name)
    {
        var g = AHGame.I; var cam = TempCam(); cam.fieldOfView = 35f; var p = g.player.transform;
        Vector3 c = p.position + Vector3.up * 0.9f;
        cam.transform.position = c + p.right * 3.2f + p.forward * 1.2f + Vector3.up * 1.4f; cam.transform.LookAt(c);
        var t = Shot(cam, 640, 480); Save(t, name); Object.Destroy(t); Object.Destroy(cam.gameObject);
    }
    static void ViewShot(string id)
    {
        var tex = AHItemStudio.View(AHItems.Get(id)) as RenderTexture; if (tex == null) return;
        RenderTexture.active = tex; var t = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false); t.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t.Apply(); RenderTexture.active = null;
        Save(t, "rc_view_" + id); Object.Destroy(t);
    }
}
