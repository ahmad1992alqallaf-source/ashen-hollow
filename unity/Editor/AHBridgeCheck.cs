// Ashen Hollow (editor test): "Test: Bridge Check, All Areas". Travels to every area in turn and, for each bridge there
// (the wooden footbridges, the lava bridges, the Abyssal Realm's marble bridges), lists whatever else pokes into the
// space you walk through on it (HeroShots/bridges/bridges.txt) and photographs it from the side and from one end
// (HeroShots/bridges/bridges_<area>_<n>.jpg, two bridges a row, eight a sheet). "Test: Bridge Check Here" does just this area.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHBridgeCheck
{
    static readonly string[] Areas = { "abyss", "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "forge", "kq", "raid", "d_hollow", "d_frost", "d_molten", "d_tomb", "d_warrens" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/bridges"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: Bridge Check, All Areas")]
    static void All()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
        File.WriteAllText(Path.Combine(Dir, "bridges.txt"), "");
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying || AHGame.I == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 10)) { if (t - t0 > 45) want = null; return; }
            if (want != null) Here();
            i++;
            if (i >= Areas.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: bridge check done (HeroShots/bridges)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + Areas[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = Areas[i]; t0 = t;
            if (AHGame.AreaId != want) AHGame.I.Travel(want, d.spawn.x, d.spawn.z, 0f);
        };
        EditorApplication.update += cb;
    }

    static bool IsBridge(Transform t) { return t.name == "Bridge" || t.name == "Lava bridge"; }
    static readonly string[] Skip = { "Floor", "Cliff", "Void", "AH_GROUND", "Water", "Sky", "Rift", "Seam" };

    [MenuItem("Ashen Hollow/Test: Bridge Check Here #&b")]
    static void Here()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        var bridges = new List<Transform>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (IsBridge(t) && t.gameObject.activeInHierarchy && (t.parent == null || !IsBridge(t.parent))) bridges.Add(t);
        var o = new StringBuilder(); o.AppendLine("== " + AHGame.AreaId + ": " + bridges.Count + " bridges");
        var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var boxes = new List<Bounds>(); var axes = new List<Vector3>();
        for (int bi = 0; bi < bridges.Count; bi++)
        {
            var br = bridges[bi]; var own = br.GetComponentsInChildren<Renderer>(); if (own.Length == 0) continue;
            Bounds B = own[0].bounds; foreach (var r in own) B.Encapsulate(r.bounds);
            // the deck: the highest flat-ish part near the middle; walk space from just above it to 2 m over it
            float deck = br.position.y; foreach (var r in own) if (r.bounds.size.y < 0.6f && r.bounds.Contains(new Vector3(B.center.x, r.bounds.center.y, B.center.z))) deck = Mathf.Max(deck, r.bounds.max.y);
            bool alongX = B.size.x > B.size.z; Vector3 ax = alongX ? Vector3.right : Vector3.forward;
            // only the inner part, between the rails, and not the ends where the bridge meets the land
            Vector3 sz = new Vector3(alongX ? B.size.x - 3f : B.size.x - 1.2f, 2.0f, alongX ? B.size.z - 1.2f : B.size.z - 3f);
            var walk = new Bounds(new Vector3(B.center.x, deck + 0.25f + 1.0f, B.center.z), new Vector3(Mathf.Max(0.5f, sz.x), sz.y, Mathf.Max(0.5f, sz.z)));
            var hits = new List<string>();
            foreach (var r in all)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is SkinnedMeshRenderer) continue;
                if (r.transform.IsChildOf(br)) continue;
                bool skip = false; foreach (var s in Skip) if (r.name.StartsWith(s)) skip = true; if (skip) continue;
                var rb = r.bounds; if (rb.size.x > 80f || rb.size.z > 80f || rb.size.y < 0.05f) continue;
                if (!rb.Intersects(walk)) continue;
                var sb = new StringBuilder(); for (var t = r.transform; t != null; t = t.parent) { sb.Insert(0, "/" + t.name); if (t.parent != null && t.parent.parent == null) break; }
                hits.Add(string.Format("    {0}  size {1}  at {2}", sb, rb.size.ToString("F1"), rb.center.ToString("F0")));
            }
            o.AppendLine(string.Format("#{0} {1} at {2} size {3} deck {4:F1}: {5}", bi, br.name, B.center.ToString("F0"), B.size.ToString("F1"), deck, hits.Count == 0 ? "clear" : hits.Count + " things in the way"));
            for (int k = 0; k < hits.Count && k < 12; k++) o.AppendLine(hits[k]);
            boxes.Add(B); axes.Add(ax);
        }
        File.AppendAllText(Path.Combine(Dir, "bridges.txt"), o.ToString());
        Shots(g, boxes, axes);
        Debug.Log("Ashen Hollow: bridge check " + AHGame.AreaId + ": " + bridges.Count + " bridges");
    }

    static void Shots(AHGame g, List<Bounds> boxes, List<Vector3> axes)
    {
        const int W = 400, H = 260, perRow = 2, rows = 4;
        var go = new GameObject("BridgeCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 50f; cam.farClipPlane = 260f; cam.nearClipPlane = 0.2f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        int sheets = (boxes.Count + perRow * rows - 1) / (perRow * rows);
        for (int s = 0; s < sheets && s < 4; s++)
        {
            var sheet = new Texture2D(W * 2 * perRow, H * rows, TextureFormat.RGB24, false);
            var px = sheet.GetPixels(); for (int k = 0; k < px.Length; k++) px[k] = Color.black; sheet.SetPixels(px);
            for (int j = 0; j < perRow * rows; j++)
            {
                int bi = s * perRow * rows + j; if (bi >= boxes.Count) break;
                var B = boxes[bi]; Vector3 ax = axes[bi], side = Vector3.Cross(Vector3.up, ax);
                float len = Mathf.Max(B.size.x, B.size.z);
                for (int v = 0; v < 2; v++)
                {
                    Vector3 c = new Vector3(B.center.x, B.max.y - 1f, B.center.z);   // about deck height, not the middle of the arch below
                    if (v == 0) { go.transform.position = c + side * (len * 0.75f + 4f) + Vector3.up * (len * 0.25f + 2f); }
                    else { go.transform.position = c - ax * (len * 0.5f + 5f) + side * 1.5f + Vector3.up * 2.2f; }
                    go.transform.LookAt(c);
                    cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    int col = (j % perRow) * 2 + v, row = rows - 1 - j / perRow;
                    sheet.SetPixels(col * W, row * H, W, H, tex.GetPixels());
                }
            }
            sheet.Apply();
            File.WriteAllBytes(Path.Combine(Dir, "bridges_" + AHGame.AreaId + "_" + s + ".jpg"), sheet.EncodeToJPG(80));
            Object.Destroy(sheet);
        }
        RenderSettings.fog = fog;
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
    }
}
