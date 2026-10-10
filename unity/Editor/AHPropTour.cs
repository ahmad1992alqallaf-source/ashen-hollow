// Ashen Hollow (editor test): "Test: Prop Tour, All Areas". Every area in turn: one close picture of each different
// thing standing in it (one per mesh, the biggest first, up to 20) and of a few of its monsters, on a sheet per area
// (HeroShots/props_<area>.png), with the list of what is in each picture (props_<area>.txt). For checking by eye that
// nothing odd, boxy or broken is left anywhere.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHPropTour
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "forge", "d_hollow", "d_frost", "d_molten", "d_tomb", "d_warrens" };
    static string[] list; static int i; static double t0; static string want;

    [MenuItem("Ashen Hollow/Test: Prop Tour, All Areas")]
    static void All() { Start(Areas); }
    [MenuItem("Ashen Hollow/Test: Prop Tour, This Area")]
    static void Here() { if (Application.isPlaying) Shoot(AHGame.AreaId); }

    static void Start(string[] l)
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        list = l; i = -1; want = null;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
    }
    static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        double t = EditorApplication.timeSinceStartup;
        if (want != null && (AHGame.AreaId != want || t - t0 < 12)) { if (t - t0 > 45) want = null; return; }
        if (want != null) Shoot(want);
        i++;
        if (i >= list.Length) { EditorApplication.update -= Tick; Debug.Log("Ashen Hollow: prop tour done"); return; }
        var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
        var d = JsonUtility.FromJson<AHWorldData>(ta.text);
        want = list[i]; t0 = t;
        if (AHGame.AreaId != want) AHGame.I.Travel(want, d.spawn.x, d.spawn.z, 0f);
    }

    static void Shoot(string area)
    {
        var g = AHGame.I; if (g == null) return;
        // one renderer per mesh (the biggest of each), person-to-house sized, then a few monsters
        var best = new Dictionary<string, Renderer>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var b = r.bounds; float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (big < 0.5f || big > 14f) continue;
            if (g.player != null && r.transform.IsChildOf(g.player.transform)) continue;
            string key = mf.sharedMesh.name;
            Renderer o; if (!best.TryGetValue(key, out o) || o.bounds.size.magnitude < b.size.magnitude) best[key] = r;
        }
        var items = new List<Renderer>(best.Values);
        items.Sort((a, b) => b.bounds.size.magnitude.CompareTo(a.bounds.size.magnitude));
        var shots = new List<KeyValuePair<string, Bounds>>();
        // the 12 biggest things, then up to 20 of the smaller ones (spread through the list)
        var pick = new List<Renderer>();
        for (int k = 0; k < items.Count && k < 12; k++) pick.Add(items[k]);
        int rest = items.Count - pick.Count, step = Mathf.Max(1, rest / 20);
        for (int k = pick.Count; k < items.Count && pick.Count < 32; k += step) pick.Add(items[k]);
        foreach (var r in pick) shots.Add(new KeyValuePair<string, Bounds>(Path(r.transform) + "  [" + r.GetComponent<MeshFilter>().sharedMesh.name + ", " + Tris(r) + " tris]", r.bounds));
        int nm = 0;
        foreach (var m in g.mobs)
        {
            if (m == null || nm >= 4) continue;
            var rs = m.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            shots.Add(new KeyValuePair<string, Bounds>("mob " + m.name, b)); nm++;
        }
        const int W = 384, H = 288, C = 6; int rows = Mathf.Max(1, (shots.Count + C - 1) / C);
        var sheet = new Texture2D(W * C, H * rows, TextureFormat.RGB24, false);
        var cg = new GameObject("PropCam"); var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 40f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var txt = new StringBuilder();
        for (int k = 0; k < shots.Count; k++)
        {
            var b = shots[k].Value; float d = Mathf.Max(2.5f, b.size.magnitude * 1.4f);
            cam.transform.position = b.center + new Vector3(0.62f, 0.42f, -0.66f).normalized * d; cam.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
            sheet.SetPixels((k % C) * W, (rows - 1 - k / C) * H, W, H, tex.GetPixels());
            txt.AppendLine(k + "\t" + shots[k].Key + "\tat " + b.center.ToString("F0"));
        }
        // and anything big drawn as a plain untextured block (a wall of colour where a building or ground should be)
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var b = r.bounds; if (Mathf.Max(b.size.x, b.size.y, b.size.z) < 8f || b.size.y < 2f) continue;
            if (Tris(r) > 300) continue;
            var m = r.sharedMaterial; if (m != null && m.mainTexture != null) continue;
            Color c = m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m != null && m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : Color.white;
            txt.AppendLine("BIG PLAIN\t" + Path(r.transform) + "  [" + mf.sharedMesh.name + ", " + Tris(r) + " tris, #" + ColorUtility.ToHtmlStringRGB(c) + ", " + (m != null ? m.shader.name : "-") + "]\tsize " + b.size.ToString("F0") + " at " + b.center.ToString("F0"));
        }
        sheet.Apply();
        string dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots/props"); Directory.CreateDirectory(dir);
        File.WriteAllBytes(System.IO.Path.Combine(dir, "props_" + area + ".png"), sheet.EncodeToJPG(80));
        File.WriteAllText(System.IO.Path.Combine(dir, "props_" + area + ".txt"), txt.ToString());
        cam.targetTexture = null; Object.Destroy(cg); Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet);
        Debug.Log("Ashen Hollow: prop tour " + area + ": " + shots.Count + " pictures");
    }
    static string Path(Transform t) { var sb = new StringBuilder(t.name); int n = 0; for (var p = t.parent; p != null && n < 3; p = p.parent, n++) sb.Insert(0, p.name + "/"); return sb.ToString(); }
    static long Tris(Renderer r) { var m = r.GetComponent<MeshFilter>().sharedMesh; long n = 0; for (int k = 0; k < m.subMeshCount; k++) n += m.GetIndexCount(k) / 3; return n; }
}
