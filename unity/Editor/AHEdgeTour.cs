// Ashen Hollow (editor test): "Test: Map Edge Tour" (shift+alt+E). Visits every area and photographs how its world ends:
//  - HeroShots/edges/<area>_top.jpg: the whole area from straight above, with <area>_top.txt giving where it lies (so
//    the walkable rectangle, the ponds you cannot cross and the solid blocks can be drawn over it)
//  - HeroShots/edges/<area>_edges.jpg: from just inside each side and each corner, at a hero's height, looking out
// HeroShots/edges_only.txt ("tide,isle") limits it to those areas.
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHEdgeTour
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "abyss" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/edges"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: Map Edge Tour #&e")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] list = Areas; string only = Path.Combine(Application.dataPath, "../HeroShots/edges_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        // driven from the editor (travelling ends the game's own coroutines)
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 8)) { if (t - t0 > 60) want = null; return; }
            if (want != null) Shoot(gg);
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: edge tour done (HeroShots/edges)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) gg.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 8;
        };
        EditorApplication.update += cb;
    }

    [MenuItem("Ashen Hollow/Test: Map Edge Shots Here")]
    static void Here() { var g = AHGame.I; if (Application.isPlaying && g != null) Shoot(g); }

    static void Shoot(AHGame g)
    {
        string id = AHGame.AreaId; var R = g.WalkArea; if (R.width <= 0f) return;
        float gy = g.player.transform.position.y;
        var go = new GameObject("EdgeCam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.2f; cam.farClipPlane = 2000f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        // from above
        const int T = 1400; var rt = new RenderTexture(T, T, 24); cam.targetTexture = rt;
        cam.orthographic = true; float half = Mathf.Max(R.width, R.height) * 0.56f; cam.orthographicSize = half;
        Vector3 c = new Vector3(R.center.x, gy + 400f, R.center.y);
        go.transform.position = c; go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Save(cam, rt, T, T, Path.Combine(Dir, id + "_top.jpg"));
        var sb = new StringBuilder(); var ci = CultureInfo.InvariantCulture;
        sb.AppendLine("view " + R.center.x.ToString("F2", ci) + " " + R.center.y.ToString("F2", ci) + " " + half.ToString("F2", ci));
        sb.AppendLine("area " + R.xMin.ToString("F2", ci) + " " + R.yMin.ToString("F2", ci) + " " + R.xMax.ToString("F2", ci) + " " + R.yMax.ToString("F2", ci));
        foreach (var l in g.Lakes) sb.AppendLine("lake " + l.x.ToString("F2", ci) + " " + l.y.ToString("F2", ci) + " " + l.z.ToString("F2", ci) + " " + l.w.ToString("F2", ci));
        foreach (var f in g.Floors) sb.AppendLine("floor " + f.xMin.ToString("F2", ci) + " " + f.yMin.ToString("F2", ci) + " " + f.xMax.ToString("F2", ci) + " " + f.yMax.ToString("F2", ci));
        foreach (var b in g.Blocks) sb.AppendLine("block " + b.xMin.ToString("F2", ci) + " " + b.yMin.ToString("F2", ci) + " " + b.xMax.ToString("F2", ci) + " " + b.yMax.ToString("F2", ci));
        // what lies where: the ground and water pieces of the world, by name and extent
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string n = r.gameObject.name; if (!(n.StartsWith("AH_GROUND") || n.StartsWith("AH_WATER") || n.Contains("Outskirt") || n.Contains("Range"))) continue;
            var b = r.bounds; sb.AppendLine("mesh " + n + " " + b.min.x.ToString("F1", ci) + " " + b.min.y.ToString("F2", ci) + " " + b.min.z.ToString("F1", ci) + " " + b.max.x.ToString("F1", ci) + " " + b.max.y.ToString("F2", ci) + " " + b.max.z.ToString("F1", ci) + " " + (r.enabled && r.gameObject.activeInHierarchy ? "on" : "off"));
        }
        File.WriteAllText(Path.Combine(Dir, id + "_top.txt"), sb.ToString());
        if (AHSea.On && AHSea.DebugMask != null) File.WriteAllBytes(Path.Combine(Dir, id + "_sea.png"), AHSea.DebugMask.EncodeToPNG());
        if (AHSea.DebugGround != null) File.WriteAllBytes(Path.Combine(Dir, id + "_ground.png"), AHSea.DebugGround.EncodeToPNG());
        Object.Destroy(rt);
        // at the edges, looking out: the four sides' middles and the four corners
        cam.orthographic = false; cam.fieldOfView = 62f;
        const int W = 480, H = 300; rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(W * 4, H * 2, TextureFormat.RGB24, false);
        float m = 5f;
        var spots = new List<KeyValuePair<Vector2, Vector2>>
        {
            new KeyValuePair<Vector2, Vector2>(new Vector2(R.center.x, R.yMax - m), Vector2.up), new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMax - m, R.center.y), Vector2.right),
            new KeyValuePair<Vector2, Vector2>(new Vector2(R.center.x, R.yMin + m), Vector2.down), new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMin + m, R.center.y), Vector2.left),
            new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMax - m, R.yMax - m), new Vector2(1, 1).normalized), new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMax - m, R.yMin + m), new Vector2(1, -1).normalized),
            new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMin + m, R.yMin + m), new Vector2(-1, -1).normalized), new KeyValuePair<Vector2, Vector2>(new Vector2(R.xMin + m, R.yMax - m), new Vector2(-1, 1).normalized),
        };
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var near = new StringBuilder();
        for (int i = 0; i < spots.Count; i++)
        {
            var s = spots[i]; Vector3 at = new Vector3(s.Key.x, gy + 2.4f, s.Key.y), dir = new Vector3(s.Value.x, 0f, s.Value.y);
            // stepped back inside a little, so the edge itself is in view
            go.transform.position = at - dir * 10f + Vector3.up * 3f; go.transform.rotation = Quaternion.LookRotation(dir + Vector3.down * 0.18f);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = null;
            // what stands in the view close by (to name an odd piece seen in the pictures)
            Vector3 look = go.transform.position + go.transform.forward * 12f;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                var bb = r.bounds; if (bb.size.magnitude > 60f || bb.SqrDistance(look) > 64f) continue;
                string path = r.name; for (var tt = r.transform.parent; tt != null && tt.parent != null; tt = tt.parent) path = tt.name + "/" + path;
                near.AppendLine("view " + i + "\t" + path + "\t" + (r.sharedMaterial != null ? r.sharedMaterial.name + " (" + r.sharedMaterial.shader.name + ")" : "-") + "\t" + bb.center.ToString("F1") + " size " + bb.size.ToString("F1"));
            }
            sheet.SetPixels((i % 4) * W, (1 - i / 4) * H, W, H, tex.GetPixels());
        }
        sheet.Apply(); File.WriteAllBytes(Path.Combine(Dir, id + "_edges.jpg"), sheet.EncodeToJPG(84));
        File.WriteAllText(Path.Combine(Dir, id + "_near.txt"), near.ToString());
        RenderSettings.fog = fog;
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet); Object.Destroy(go);
        Debug.Log("Ashen Hollow: edge shots " + id);
    }

    static void Save(Camera cam, RenderTexture rt, int w, int h, string path)
    {
        cam.Render(); RenderTexture.active = rt;
        var t = new Texture2D(w, h, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply(); RenderTexture.active = null;
        File.WriteAllBytes(path, t.EncodeToJPG(84)); Object.Destroy(t);
    }
}
