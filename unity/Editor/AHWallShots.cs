// Ashen Hollow (editor test): "Test: City Wall Shots" (shift+alt+J). Goes to each walled city in turn and photographs
// its walls: a high look over the whole city, then each wall from outside and from inside (both ends and the middle),
// each corner, and each gate. HeroShots/walls/<city>.jpg, a sheet per city. HeroShots/walls_only.txt ("hc_city,co_city")
// limits it to those cities.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHWallShots
{
    static readonly string[] Cities = { "city", "hc_city", "mw_city", "ss_city", "ch_city", "co_city" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/walls"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: City Wall Shots #&j")]
    static void All()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        string[] list = Cities; string only = Path.Combine(Application.dataPath, "../HeroShots/walls_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying || AHGame.I == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 10)) { if (t - t0 > 45) want = null; return; }
            if (want != null) Shoot(AHGame.I);
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: wall shots done (HeroShots/walls)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) AHGame.I.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 10;
        };
        EditorApplication.update += cb;
    }

    [MenuItem("Ashen Hollow/Test: City Wall Shots Here")]
    static void Here() { if (Application.isPlaying && AHGame.I != null) Shoot(AHGame.I); }

    static void Shoot(AHGame g)
    {
        var d = g.data; if (d == null || d.boxes == null || d.bounds == null) return;
        var walls = new List<AHBox>(); foreach (var b in d.boxes) if (Mathf.Min(b.w, b.h) < 2f && Mathf.Max(b.w, b.h) > 8f) walls.Add(b);
        Vector3 ground = g.W(d.spawn.x, d.spawn.z); float gy = ground.y;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        float cx = (d.bounds.x0 + d.bounds.x1) / 2f, cz = (d.bounds.z0 + d.bounds.z1) / 2f;
        Vector3 C = g.W(cx, cz); C.y = gy;
        var views = new List<KeyValuePair<Vector3, Vector3>>();   // eye, target
        // over the whole city, from two sides
        float span = Mathf.Max(d.bounds.x1 - d.bounds.x0, d.bounds.z1 - d.bounds.z0);
        views.Add(new KeyValuePair<Vector3, Vector3>(C + new Vector3(-0.55f, 0.75f, -0.55f).normalized * span * 0.95f, C));
        views.Add(new KeyValuePair<Vector3, Vector3>(C + new Vector3(0.55f, 0.75f, 0.55f).normalized * span * 0.95f, C));
        foreach (var w in walls)
        {
            bool alongX = w.w >= w.h; float L = Mathf.Max(w.w, w.h);
            float line = alongX ? w.z + w.h / 2f : w.x + w.w / 2f, a0 = alongX ? w.x : w.z;
            System.Func<float, Vector3> at = u => { var p = alongX ? g.W(u, line) : g.W(line, u); p.y = gy; return p; };
            Vector3 mid = at(a0 + L / 2f), inward = C - mid; inward.y = 0f; inward.Normalize();
            Vector3 dir = at(a0 + L) - at(a0); dir.y = 0f; dir.Normalize();
            // from outside, looking at the wall's middle, and along it from each end (outside and inside)
            views.Add(new KeyValuePair<Vector3, Vector3>(mid - inward * 22f + Vector3.up * 9f, mid + Vector3.up * 2f));
            views.Add(new KeyValuePair<Vector3, Vector3>(at(a0) - inward * 9f - dir * 6f + Vector3.up * 5f, at(a0 + L * 0.5f) + Vector3.up * 2f));
            views.Add(new KeyValuePair<Vector3, Vector3>(at(a0 + L) + inward * 9f + dir * 6f + Vector3.up * 5f, at(a0 + L * 0.5f) + Vector3.up * 2f));
        }
        if (d.gates != null) foreach (var gt in d.gates)
            {
                Vector3 p = g.W(gt.x, gt.z); p.y = gy; Vector3 inw = C - p; inw.y = 0f; inw.Normalize();
                views.Add(new KeyValuePair<Vector3, Vector3>(p - inw * 16f + Vector3.up * 5f, p + Vector3.up * 3f));
                views.Add(new KeyValuePair<Vector3, Vector3>(p + inw * 14f + Vector3.up * 5f, p + Vector3.up * 3f));
            }
        const int W = 480, H = 300, cols = 4;
        int rows = (views.Count + cols - 1) / cols;
        var go = new GameObject("WallCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 55f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 800f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(W * cols, H * rows, TextureFormat.RGB24, false);
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        for (int i = 0; i < views.Count; i++)
        {
            go.transform.position = views[i].Key; go.transform.LookAt(views[i].Value);
            cam.Render(); RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, W, H), (i % cols) * W, (rows - 1 - i / cols) * H); RenderTexture.active = null;
        }
        RenderSettings.fog = fog; sheet.Apply();
        File.WriteAllBytes(Path.Combine(Dir, AHGame.AreaId + ".jpg"), sheet.EncodeToJPG(82));
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(sheet); Object.Destroy(go);
        // what stands on the wall lines (to find what is left of the old walls, or anything else in the way)
        var sb = new System.Text.StringBuilder(); var seen = new HashSet<string>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is SkinnedMeshRenderer) continue;
            var b = r.bounds; Vector2 p0 = g.ToWeb(b.min), p1 = g.ToWeb(b.max);
            float x0 = Mathf.Min(p0.x, p1.x), x1 = Mathf.Max(p0.x, p1.x), z0 = Mathf.Min(p0.y, p1.y), z1 = Mathf.Max(p0.y, p1.y);
            bool on = false; foreach (var w in walls) if (x1 > w.x - 1.5f && x0 < w.x + w.w + 1.5f && z1 > w.z - 1.5f && z0 < w.z + w.h + 1.5f) { on = true; break; }
            if (!on) continue;
            string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            string key = path.Length > 60 ? path.Substring(0, 60) : path;
            string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "-";
            string line = path + " | " + mat + " | size " + b.size.ToString("F1") + " | web " + x0.ToString("F0") + "," + z0.ToString("F0") + " .. " + x1.ToString("F0") + "," + z1.ToString("F0");
            if (seen.Add(line)) sb.AppendLine(line);
        }
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (r.transform.parent != null && r.transform.parent.name == "Wall" || r.name == "Wall")
            {
                var m = r.sharedMaterial; if (m == null) continue;
                sb.Insert(0, "WALL MATERIAL " + m.name + " shader " + m.shader.name + " props " + string.Join(",", m.GetTexturePropertyNames()) + " baseColor " + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-") + " color " + (m.HasProperty("_Color") ? m.GetColor("_Color").ToString() : "-") + "\n");
                break;
            }
        File.WriteAllText(Path.Combine(Dir, AHGame.AreaId + ".txt"), sb.ToString());
        Debug.Log("Ashen Hollow: wall shots " + AHGame.AreaId + " (" + views.Count + " views)");
    }
}
