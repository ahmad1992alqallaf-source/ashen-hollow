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
        int ri = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (ri >= 8 || (t.name != "Portal arch" && t.name != "Hay bale" && t.name != "Old chest")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float d = Mathf.Max(4f, b.size.magnitude * 1.5f);
            cam.transform.position = b.center + new Vector3(0.6f, 0.45f, -0.75f).normalized * d; cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 360), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../HeroShots/rebuilt_" + ri + "_" + t.name.Replace(" ", "") + ".png"), tex.EncodeToPNG()); ri++;
        }
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(rt); Object.Destroy(tex);
        Debug.Log("Ashen Hollow: " + total + " plain block props listed in " + p);
    }
}
