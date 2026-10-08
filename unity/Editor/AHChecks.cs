// Ashen Hollow test tools: quick checks of things that are slow to reach by playing (Play mode only, on the test
// hero). "Run Checks" plays them through in code and writes PASS or FAIL for each to the console and to
// HeroShots/checks.txt: Kael joining, a townsfolk tale part from start to reward, sleeping in your own bed.
// "Next Season" and "Next Deep Twist" switch what the world shows so they can be looked at.
using System;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHChecks
{
    static StringBuilder log;
    static void Check(string name, Func<string> run)
    {
        string res; try { res = run(); } catch (Exception e) { res = "FAIL: " + e.GetType().Name + " " + e.Message; }
        if (res == null) res = "PASS";
        string line = name + ": " + res; log.AppendLine(line); Debug.Log("Ashen Hollow check · " + line);
    }
    static void EndCine() { var c = AHCine.I; if (c == null) return; typeof(AHCine).GetMethod("End", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(c, null); }
    static void SetStat(AHPlayer p, string k, long v) { AHProgress.Add(p.prog.stats, k, v - AHProgress.Get(p.prog.stats, k)); }

    [MenuItem("Ashen Hollow/Test: Run Checks")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: Run Checks needs Play mode"); return; }
        var p = g.player; log = new StringBuilder();
        log.AppendLine("Checks on " + p.heroName + " in " + AHGame.AreaId + " at " + DateTime.Now);

        Check("Kael joins the party", () =>
        {
            if (p.party.Contains("kael")) return null;
            string moved = null; if (p.party.Count >= AHComp.PartyMax) { moved = p.party[p.party.Count - 1]; p.party.RemoveAt(p.party.Count - 1); }   // make room for the test
            typeof(AHStory).GetMethod("JoinKael", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { g });
            bool ok = p.party.Contains("kael"); bool seen = false; foreach (var a in AHComp.Allies) if (a != null && a.name.ToLowerInvariant().Contains("kael")) seen = true;
            if (moved != null) { p.party.Remove("kael"); p.party.Add(moved); AHComp.SpawnAllies(g); }   // and put the party back as it was
            if (!ok) return "FAIL: not in the party";
            return seen ? null : "PASS (joined; his figure was not found by name among the allies)";
        });

        Check("Tale: Bread and Wolves, part 1 start to reward", () =>
        {
            var t = AHTales.Of("Baker Maudie"); var n = AHNpc.Find("Baker Maudie");
            if (n == null) return "FAIL: Baker Maudie is not in this land";
            SetStat(p, "tale:Baker Maudie", 0); SetStat(p, "tales:Baker Maudie", 0); SetStat(p, "talen:Baker Maudie", 0);
            if (!AHTales.HasMark(p, "Baker Maudie")) return "FAIL: no ! over her head";
            if (!AHTales.Talk(g, n)) return "FAIL: talking did not start the tale"; EndCine();
            if (AHProgress.Get(p.prog.stats, "tales:Baker Maudie") != 1) return "FAIL: the part was not taken";
            int had = p.bag.Count("wheat"); p.bag.Add("wheat", 5); long money = p.bag.money;
            if (!AHTales.HasMark(p, "Baker Maudie")) return "FAIL: no ! once the wheat is in the bag";
            if (!AHTales.Talk(g, n)) return "FAIL: handing in did not start"; EndCine();
            if (AHProgress.Get(p.prog.stats, "tale:Baker Maudie") != 1) return "FAIL: part 1 not marked done";
            if (p.bag.Count("wheat") != had) return "FAIL: the wheat was not taken (" + p.bag.Count("wheat") + " left, had " + had + ")";
            if (p.bag.money <= money) return "FAIL: no coin paid";
            return null;
        });

        Check("Sleeping in your own bed", () =>
        {
            var H = p.home; if (H == null) return "FAIL: no house";
            if (H.furn == null) H.furn = new System.Collections.Generic.List<string>();
            if (!H.furn.Contains("bed")) H.furn.Add("bed");
            H.sleepAt = 0; p.rested = 0; p.hp = p.maxHp * 0.5f;
            typeof(AHInterior).GetMethod("Sleep", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { g });
            if (p.rested != AHHome.RestCap(p)) return "FAIL: rested XP " + p.rested + " (want " + AHHome.RestCap(p) + ")";
            if (p.hp < p.maxHp) return "FAIL: HP not restored";
            return null;
        });

        Check("Furniture counts as comfort", () => AHInterior.Pts(p) >= 3 ? null : "FAIL: furniture comfort " + AHInterior.Pts(p));

        g.MarkDirty(); g.SaveProgress();
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Application.dataPath, "../HeroShots"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../HeroShots/checks.txt"), log.ToString());
        Debug.Log("Ashen Hollow: checks done, see HeroShots/checks.txt");
    }

    [MenuItem("Ashen Hollow/Test: Boss Intro Preview %&i")]
    static void BossIntro()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) { Debug.Log("Ashen Hollow: boss intro preview needs Play mode"); return; }
        AHMob best = null; float bd = 1e9f; foreach (var m in g.mobs) { if (m == null || m.dead || !m.gameObject.activeInHierarchy) continue; float d = (m.transform.position - g.player.transform.position).sqrMagnitude; if (d < bd) { bd = d; best = m; } }
        if (best == null) { Debug.Log("Ashen Hollow: no foe near"); return; }
        AHBossIntro.Play(g, best, best.type.name, "A test of the boss entrance");
    }

    [MenuItem("Ashen Hollow/Test: Enter the Deep")]
    static void EnterDeep() { var g = AHGame.I; if (!Application.isPlaying || g == null) return; Debug.Log("Ashen Hollow: entering the Deep, twist " + AHDeep.WeekTwist.name); AHDeep.Start(g); }

    static readonly string[] Seasons = { "autumn", "winter", "spring", "summer" };
    [MenuItem("Ashen Hollow/Test: Describe Ground %&d")]
    static void DescribeGround()
    {
        var sb = new StringBuilder("Ashen Hollow: ground\n");
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.name.StartsWith("AH_GROUND")) continue; var m = r.sharedMaterial;
            sb.AppendLine(r.name + " on=" + r.enabled + " vis=" + !r.forceRenderingOff + " b=" + r.bounds + " shader=" + (m != null ? m.shader.name : "none") + " tex=" + (m != null && m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name + " " + m.GetTexture("_BaseMap").width : "none") + " tint=" + (m != null && m.HasProperty("_Tint") ? m.GetColor("_Tint").ToString() : "-"));
        }
        sb.AppendLine("fog " + RenderSettings.fog + " " + RenderSettings.fogMode + " " + RenderSettings.fogStartDistance + ".." + RenderSettings.fogEndDistance + " col " + RenderSettings.fogColor + " weather " + AHWeather.Name);
        sb.AppendLine("ambient " + RenderSettings.ambientMode + " sky " + RenderSettings.ambientSkyColor + " eq " + RenderSettings.ambientEquatorColor + " gr " + RenderSettings.ambientGroundColor + " int " + RenderSettings.ambientIntensity);
        var sh = RenderSettings.ambientProbe; Vector3[] dirs = { Vector3.up, Vector3.down }; Color[] res = new Color[2]; sh.Evaluate(dirs, res); sb.AppendLine("probe up " + res[0] + " down " + res[1]);
        if (RenderSettings.sun != null) sb.AppendLine("sun " + RenderSettings.sun.intensity + " " + RenderSettings.sun.color);
        if (AHGame.I != null && AHGame.I.cam != null) sb.AppendLine("cam " + AHGame.I.cam.transform.position + " far " + AHGame.I.cam.farClipPlane);
        // the ground map itself, and where the hero stands on it
        var g = AHGame.I;
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.name.StartsWith("AH_GROUND")) continue; var m = r.sharedMaterial; if (m == null || !m.HasProperty("_BaseMap")) continue;
            var tex = m.GetTexture("_BaseMap"); if (tex == null) continue;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0); Graphics.Blit(tex, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt; var t2 = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false); t2.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t2.Apply(); RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots"); System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "ground_map.png"), t2.EncodeToPNG());
            if (g != null && g.player != null)
            {
                RaycastHit hit; var o = g.player.transform.position + Vector3.up * 2f;
                var mc = r.GetComponent<MeshCollider>(); bool made = false;
                if (mc == null) { mc = r.gameObject.AddComponent<MeshCollider>(); made = true; Physics.SyncTransforms(); }
                if (g.grass != null)
                {
                    var sm = typeof(AHGrass).GetMethod("Sample", BindingFlags.Instance | BindingFlags.NonPublic);
                    var wp = g.ToWeb(g.player.transform.position); object[] a = { wp.x, wp.y, null };
                    bool ok = (bool)sm.Invoke(g.grass, a); sb.AppendLine("web " + wp + " grass map ok=" + ok + " colour " + a[2] + " groundMap " + JsonUtility.ToJson(g.data.groundMap));
                }
                sb.AppendLine("hero at " + g.player.transform.position + " mesh uv rect from bounds; tex " + tex.width + "x" + tex.height + " st=" + m.GetVector("_BaseMap_ST"));
                if (mc != null && mc.Raycast(new Ray(o, Vector3.down), out hit, 10f)) { var c = t2.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y); sb.AppendLine("uv under hero " + hit.textureCoord + " colour " + c); }
                else sb.AppendLine("no hit on ground mesh");
                if (made) UnityEngine.Object.Destroy(mc);
            }
            UnityEngine.Object.Destroy(t2); break;
        }
        Debug.Log(sb.ToString());
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../HeroShots/ground.txt"), sb.ToString());
    }

    [MenuItem("Ashen Hollow/Test: Next Weather %&r")]
    static void NextWeather() { if (!Application.isPlaying) return; AHWeather.TestNext(); Debug.Log("Ashen Hollow: weather " + AHWeather.Name); }

    [MenuItem("Ashen Hollow/Test: Next Season %&n")]
    static void NextSeason()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        int i = Array.IndexOf(Seasons, AHSeason.Now); AHSeason.Force = Seasons[(i + 1) % Seasons.Length];
        AHSeason.Rebuild(g); if (g.grass != null) g.grass.Regrow(); g.ui.Toast("Season: " + AHSeason.Name, 2f); Debug.Log("Ashen Hollow: season " + AHSeason.Now + ", snow " + Shader.GetGlobalFloat("_AHSnow") + ", leaves " + Shader.GetGlobalFloat("_AHLeaves"));
    }

    [MenuItem("Ashen Hollow/Test: Next Deep Twist")]
    static void NextTwist()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        var ts = AHDeep.Twists; int i = Array.FindIndex(ts, t => t.id == AHDeep.WeekTwist.id);
        AHDeep.ForceTwist = ts[(i + 1) % ts.Length].id; g.ui.Toast("Deep twist: " + AHDeep.WeekTwist.name, 2f);
        Debug.Log("Ashen Hollow: Deep twist now " + AHDeep.WeekTwist.name);
    }
}
