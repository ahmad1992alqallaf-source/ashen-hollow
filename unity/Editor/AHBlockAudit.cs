// Ashen Hollow (editor test): "Test: Plain Block Audit". Lists every prop of the old web game's that is still drawn
// as plain low-poly blocks (each piece 36 triangles or fewer, no texture) to HeroShots/blocks.txt: where it stands, its
// pieces' sizes and colours, grouped by look, the commonest first, so they can be rebuilt as proper models.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHBlockAudit
{
    // every area in turn: audit here, take the next way out, wait for the next area to settle, audit again
    [MenuItem("Ashen Hollow/Test: Plain Block Audit, All Areas %&s")]
    static void Sweep()
    {
        if (!Application.isPlaying) return;
        File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/blocks_all.txt"), "");
        var seen = new HashSet<string>(); string area = null; double t0 = EditorApplication.timeSinceStartup; int hops = 0;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying || hops > 24) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: block sweep done (" + seen.Count + " areas)"); return; }
            double t = EditorApplication.timeSinceStartup;
            if (AHGame.AreaId != area) { area = AHGame.AreaId; t0 = t; return; }
            if (t - t0 < 9) return;
            if (!seen.Contains(area)) { seen.Add(area); Run(); }
            AHMenu.GoToExit(); hops++; t0 = t + 6;
        };
        EditorApplication.update += cb;
    }

    [MenuItem("Ashen Hollow/Test: Plain Block Audit %&x")]
    static void Run()
    {
        if (!Application.isPlaying) return;
        var groups = new Dictionary<string, List<string>>();
        int total = 0; var shots = new List<Bounds>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("obj")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(false); if (rs.Length == 0) continue;
            bool plain = true; int on = 0; var parts = new List<string>(); Bounds all = new Bounds(); bool first = true;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; on++;
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) { plain = false; break; }
                long tri = 0; for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tri += mf.sharedMesh.GetIndexCount(k) / 3;
                var m = r.sharedMaterial; bool hasTex = m != null && m.mainTexture != null;
                if (tri > 200 || hasTex) { plain = false; break; }
                Color c = m != null && m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                var b = r.bounds; if (first) { all = b; first = false; } else all.Encapsulate(b);
                parts.Add(string.Format("{0:F1}x{1:F1}x{2:F1}#{3}({4})", b.size.x, b.size.y, b.size.z, ColorUtility.ToHtmlStringRGB(c), tri));
            }
            if (!plain || on == 0) continue;
            parts.Sort(); string key = on + " parts, " + string.Format("{0:F0}x{1:F0}x{2:F0} m", all.size.x, all.size.y, all.size.z) + ": " + string.Join(" ", parts.ToArray());
            shots.Add(all);
            List<string> l; if (!groups.TryGetValue(key, out l)) groups[key] = l = new List<string>();
            l.Add(all.center.ToString("F0")); total++;
        }
        var keys = new List<string>(groups.Keys); keys.Sort((a, b) => groups[b].Count.CompareTo(groups[a].Count));
        var o = new StringBuilder(); o.AppendLine(total + " plain block props, " + keys.Count + " looks");
        foreach (var k in keys) o.AppendLine(groups[k].Count + " x  " + k + "   at " + string.Join(" ", groups[k].GetRange(0, Mathf.Min(4, groups[k].Count)).ToArray()));
        string p = Path.Combine(Application.dataPath, "../HeroShots/blocks.txt"); File.WriteAllText(p, o.ToString());
        File.AppendAllText(Path.Combine(Application.dataPath, "../HeroShots/blocks_all.txt"), "== " + AHGame.AreaId + "\n" + o.ToString());
        // a picture of each (the first 12)
        var cg = new GameObject("BlockCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 40f; cam.farClipPlane = 120f;
        var rt = new RenderTexture(480, 360, 24); cam.targetTexture = rt; var tex = new Texture2D(480, 360, TextureFormat.RGB24, false);
        for (int i = 0; i < shots.Count && i < 12; i++)
        {
            var b = shots[i]; float d = Mathf.Max(5f, b.size.magnitude * 1.6f);
            cam.transform.position = b.center + new Vector3(0.6f, 0.45f, -0.75f).normalized * d; cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/block_" + AHGame.AreaId + "_" + i + ".png"), tex.EncodeToPNG());
        }
        // and the rebuilt ones (bales, stalls, arches), to check them
        int ri = 0, bevShots = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            bool bev = false; var tmf = t.GetComponent<MeshFilter>(); if (tmf != null && tmf.sharedMesh != null && tmf.sharedMesh.name == "Bevelled box" && bevShots < 2) { bev = true; bevShots++; }
            if (tour && (t.name == "Hay bale" || t.name == "Old chest")) continue;
            if (ri >= 10 || (!bev && t.name != "Portal arch" && t.name != "Hay bale" && t.name != "Old chest" && t.name != "Lava bridge" && t.name != "Signpost" && t.name != "Plinth" && t.name != "Statue")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float d = Mathf.Max(4f, b.size.magnitude * 1.5f);
            cam.transform.position = b.center + (t.name == "Plinth" || t.name == "Signpost" || t.name == "Statue" ? new Vector3(0.35f, 0.5f, -0.6f) : new Vector3(0.6f, 0.45f, -0.75f)).normalized * d; cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/rebuilt_" + ri + "_" + t.name.Replace(" ", "") + ".png"), tex.EncodeToPNG()); ri++;
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(rt); Object.Destroy(tex);
        Debug.Log("Ashen Hollow: " + total + " plain block props listed in " + p);
    }

    // ---- every area by travel (not just the roads from here), and every lone plain box, even inside a richer prop ----
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "forge", "d_hollow", "d_frost", "d_molten", "d_tomb", "d_warrens" };
    // a short tour of the areas with rebuilt loose boxes, photographing them (rebuilt_*.png)
    static string[] list = Areas; static bool tour;
    [MenuItem("Ashen Hollow/Test: Rebuilt Props Tour %&y")]
    static void Tour() { list = new[] { "ember", "homestead", "city", "tide" }; tour = true; Go(); }
    [MenuItem("Ashen Hollow/Test: Box Hunt, All Areas %&e")]
    static void Hunt() { list = Areas; tour = false; Go(); }
    static void Go()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        if (!tour) File.WriteAllText(Path.Combine(Application.dataPath, "../HeroShots/boxes_all.txt"), "");
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 10)) { if (t - t0 > 40) want = null; return; }
            if (want != null) { if (tour) { foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "../HeroShots"), "rebuilt_*.png")) File.Delete(f); Run(); foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "../HeroShots"), "rebuilt_*.png")) { string nf = f.Replace("rebuilt_", "rb_" + want + "_"); File.Copy(f, nf, true); } } else Loose(); }
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: box hunt done"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) return;
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) AHGame.I.Travel(want, d.spawn.x, d.spawn.z, 0f);
        };
        EditorApplication.update += cb;
    }

    static void Loose()
    {
        var o = new StringBuilder(); var shots = new List<Bounds>(); int n = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled) continue;
            bool web = false; for (var t = r.transform; t != null; t = t.parent) if (t.name.StartsWith("obj")) { web = true; break; }
            if (!web) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            long tri = 0; for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tri += mf.sharedMesh.GetIndexCount(k) / 3;
            var m = r.sharedMaterial; if (tri > 12 || (m != null && m.mainTexture != null)) continue;
            var b = r.bounds; float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (big < 0.3f || big > 30f || Mathf.Min(b.size.x, Mathf.Min(b.size.y, b.size.z)) < 0.02f) continue;   // flat decals are not boxes
            Color c = m != null && m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            var sb = new StringBuilder(); for (var t = r.transform; t != null && t.parent != null; t = t.parent) sb.Insert(0, "/" + t.name);
            o.AppendLine(string.Format("{0:F1}x{1:F1}x{2:F1} #{3} at {4}  {5}", b.size.x, b.size.y, b.size.z, ColorUtility.ToHtmlStringRGB(c), b.center.ToString("F0"), sb));
            if (shots.Count < 10) shots.Add(b); n++;
        }
        File.AppendAllText(Path.Combine(Application.dataPath, "../HeroShots/boxes_all.txt"), "== " + AHGame.AreaId + " (" + n + " boxes)\n" + o);
        var cg = new GameObject("BoxCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 40f; cam.farClipPlane = 150f;
        var rt = new RenderTexture(400, 300, 24); cam.targetTexture = rt; var tex = new Texture2D(400, 300, TextureFormat.RGB24, false);
        for (int i = 0; i < shots.Count; i++)
        {
            var b = shots[i]; float d = Mathf.Max(5f, b.size.magnitude * 2f);
            cam.transform.position = b.center + new Vector3(0.6f, 0.5f, -0.7f).normalized * d; cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 400, 300), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/box_" + AHGame.AreaId + "_" + i + ".png"), tex.EncodeToPNG());
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(rt); Object.Destroy(tex);
    }
}
