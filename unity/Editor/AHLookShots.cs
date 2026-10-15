// Ashen Hollow (editor test): "Test: Player View Shots" (shift+alt+V). Photographs lands the way a player sees them:
// the game's own camera (its fog, view distance and sky), behind and above a hero standing at a spot, looking a given way.
// HeroShots/look.txt, one line per shot:
//   sands 12.5 -40 0          land, x, z, compass heading (0 north, 90 east), [pitch] [distance] [hour 0-24]
//   sands edges               from 12 m inside the middle of each side and each corner, looking out
//   sands info                just the land's walking area, written to HeroShots/look/<land>_info.txt
//   sands passes [hour]       from 22 m inside each road out of the land (AHPass), looking out along it
// Pictures: HeroShots/look/<land>_<n>.jpg (960x540) and a sheet of them all, HeroShots/look/sheet_<land>.jpg.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AHLookShots
{
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/look"); Directory.CreateDirectory(d); return d; } }
    const int W = 960, H = 540;

    class Shot { public string area; public float x, z, yaw, pitch = 16f, dist = 7f, hour = -1f; public bool edges, info, passes; }

    [MenuItem("Ashen Hollow/Test: Player View Shots #&v")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string f = Path.Combine(Application.dataPath, "../HeroShots/look.txt"); if (!File.Exists(f)) { Debug.Log("Ashen Hollow: no HeroShots/look.txt"); return; }
        var shots = new List<Shot>(); var ci = CultureInfo.InvariantCulture;
        foreach (var raw in File.ReadAllLines(f))
        {
            var w = raw.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries); if (w.Length < 2 || w[0].StartsWith("#")) continue;
            var s = new Shot { area = w[0] };
            if (w[1] == "edges") { s.edges = true; if (w.Length > 2) s.hour = float.Parse(w[2], ci); }
            else if (w[1] == "info") s.info = true;
            else if (w[1] == "passes") { s.passes = true; if (w.Length > 2) s.hour = float.Parse(w[2], ci); }
            else
            {
                if (w.Length < 4) continue;
                s.x = float.Parse(w[1], ci); s.z = float.Parse(w[2], ci); s.yaw = float.Parse(w[3], ci);
                if (w.Length > 4) s.pitch = float.Parse(w[4], ci); if (w.Length > 5) s.dist = float.Parse(w[5], ci); if (w.Length > 6) s.hour = float.Parse(w[6], ci);
            }
            shots.Add(s);
        }
        int i = 0; double t0 = 0; string want = null; var done = new Dictionary<string, List<Texture2D>>(); int n = 0;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 9)) { if (t - t0 > 60) { want = null; i++; } return; }
            want = null;
            if (i >= shots.Count) { EditorApplication.update -= cb; Sheets(done); Debug.Log("Ashen Hollow: player view shots done (HeroShots/look)"); return; }
            var s = shots[i];
            if (AHGame.AreaId != s.area)
            {
                var ta = Resources.Load<TextAsset>("AH/Areas/" + s.area); if (ta == null) { i++; return; }
                var d = JsonUtility.FromJson<AHWorldData>(ta.text);
                want = s.area; t0 = t; gg.Travel(s.area, d.spawn.x, d.spawn.z, 0f); return;
            }
            i++;
            var R = gg.WalkArea;
            if (s.info || s.edges)
                File.WriteAllText(Path.Combine(Dir, s.area + "_info.txt"), string.Format(ci, "walk {0:F1} {1:F1} {2:F1} {3:F1}\nsolid {4:F1} {5:F1} {6:F1} {7:F1}\n", R.xMin, R.yMin, R.xMax, R.yMax, AHSolidGrid.Extent.xMin, AHSolidGrid.Extent.yMin, AHSolidGrid.Extent.xMax, AHSolidGrid.Extent.yMax));
            if (s.info)
            {
                // every broad flat thing (water sheets, plains): what lies under the sea's edge
                var sb = new System.Text.StringBuilder(File.ReadAllText(Path.Combine(Dir, s.area + "_info.txt")));
                sb.AppendLine(string.Format(ci, "seaLand {0} seaY {1:F2}", AHSea.Land, AHSea.SeaY));
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    var b = r.bounds; if (Mathf.Max(b.size.x, b.size.z) < 150f || b.size.y > 6f) continue;
                    var m = r.sharedMaterial; string mi = m == null ? "none" : m.shader.name + (m.HasProperty("_Mode") ? string.Format(ci, " mode {0} out {1} sides {2}", m.GetFloat("_Mode"), m.HasProperty("_SeaOut") ? m.GetFloat("_SeaOut") : -1f, m.HasProperty("_SeaSides") ? m.GetVector("_SeaSides").ToString() : "-") : "");
                    sb.AppendLine(string.Format(ci, "{0} active {1} enabled {2} min {3} max {4} q {5} | {6}", PathOf(r.transform), r.gameObject.activeInHierarchy, r.enabled, b.min.ToString("F1"), b.max.ToString("F1"), m == null ? 0 : m.renderQueue, mi));
                }
                File.WriteAllText(Path.Combine(Dir, s.area + "_info.txt"), sb.ToString());
                // the see-through things around each corner's camera (a mist or veil over the view)
                foreach (var c in new[] { new Vector2(R.xMax - 12f, R.yMin + 12f), new Vector2(R.xMin + 12f, R.yMax - 12f), new Vector2(R.xMin + 12f, R.yMin + 12f) })
                {
                    var box = new Bounds(new Vector3(c.x, 5f, c.y), new Vector3(40f, 30f, 40f)); sb.AppendLine("near " + c);
                    foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    {
                        if (!r.enabled || !r.gameObject.activeInHierarchy || !r.bounds.Intersects(box)) continue;
                        var m = r.sharedMaterial; if (m == null || m.renderQueue < 2500) continue;
                        sb.AppendLine(string.Format(ci, "   {0} q {1} {2} size {3}", PathOf(r.transform), m.renderQueue, m.shader.name, r.bounds.size.ToString("F0")));
                    }
                }
                File.WriteAllText(Path.Combine(Dir, s.area + "_info.txt"), sb.ToString());
                if (AHSea.DebugMask != null) File.WriteAllBytes(Path.Combine(Dir, s.area + "_seamask.png"), AHSea.DebugMask.EncodeToPNG());
                return;
            }
            var list = new List<Shot>();
            if (s.edges)
            {
                float m = 12f; var cx = R.center.x; var cz = R.center.y;
                list.Add(new Shot { x = cx, z = R.yMax - m, yaw = 0f }); list.Add(new Shot { x = R.xMax - m, z = R.yMax - m, yaw = 45f });
                list.Add(new Shot { x = R.xMax - m, z = cz, yaw = 90f }); list.Add(new Shot { x = R.xMax - m, z = R.yMin + m, yaw = 135f });
                list.Add(new Shot { x = cx, z = R.yMin + m, yaw = 180f }); list.Add(new Shot { x = R.xMin + m, z = R.yMin + m, yaw = 225f });
                list.Add(new Shot { x = R.xMin + m, z = cz, yaw = 270f }); list.Add(new Shot { x = R.xMin + m, z = R.yMax - m, yaw = 315f });
            }
            else if (s.passes)
                foreach (var pw in AHPass.Ways) { Vector3 p0 = pw.at - pw.outw * 22f; list.Add(new Shot { x = p0.x, z = p0.z, yaw = Mathf.Atan2(pw.outw.x, pw.outw.z) * Mathf.Rad2Deg, pitch = 10f, dist = 8f }); }
            else list.Add(s);
            if (!done.ContainsKey(s.area)) done[s.area] = new List<Texture2D>();
            foreach (var q in list) { var tex = Shoot(gg, q.x, q.z, q.yaw, q.pitch, q.dist, s.hour); if (tex != null) { File.WriteAllBytes(Path.Combine(Dir, s.area + "_" + (n++) + ".jpg"), tex.EncodeToJPG(85)); done[s.area].Add(tex); } }
        };
        EditorApplication.update += cb;
    }

    static Texture2D Shoot(AHGame g, float x, float z, float yaw, float pitch, float dist, float hour)
    {
        if (hour >= 0f) g.SetTimeOfDay(hour / 24f);
        Vector3 at = new Vector3(x, g.player.transform.position.y, z);
        g.player.transform.position = at;
        var go = new GameObject("LookCam"); var cam = go.AddComponent<Camera>(); cam.CopyFrom(g.cam); cam.enabled = false;
        cam.aspect = (float)W / H;
        Quaternion q = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 look = at + Vector3.up * 1.55f;
        go.transform.position = look - q * Vector3.forward * dist; go.transform.rotation = q;
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
        cam.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
        return tex;
    }

    static void Sheets(Dictionary<string, List<Texture2D>> done)
    {
        foreach (var kv in done)
        {
            var l = kv.Value; if (l.Count == 0) continue;
            int cols = Mathf.Min(4, l.Count), rows = (l.Count + cols - 1) / cols, w = W / 2, h = H / 2;
            var sheet = new Texture2D(cols * w, rows * h, TextureFormat.RGB24, false);
            for (int k = 0; k < l.Count; k++)
            {
                var small = new Color[w * h]; var src = l[k];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) small[y * w + x] = src.GetPixel(x * 2, y * 2);
                sheet.SetPixels((k % cols) * w, (rows - 1 - k / cols) * h, w, h, small);
            }
            sheet.Apply(); File.WriteAllBytes(Path.Combine(Dir, "sheet_" + kv.Key + ".jpg"), sheet.EncodeToJPG(85));
        }
    }
    static string PathOf(Transform t) { var q = t.name; for (var p = t.parent; p != null; p = p.parent) q = p.name + "/" + q; return q; }
}
