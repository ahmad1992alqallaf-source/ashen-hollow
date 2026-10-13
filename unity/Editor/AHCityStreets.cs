// Ashen Hollow (editor test): "Test: City Street Shots" (shift+alt+Y). Visits every city and photographs its houses from
// the street, and lists anything that should not be there:
//  - HeroShots/streets/<city>_houses.jpg: up to 24 houses, each from a little way off at about head height
//  - HeroShots/streets/<city>.txt: for each house, any other piece (not part of the new city) standing inside it, how
//    deep and thin each house is, and flat round discs lying about the streets
// HeroShots/streets_only.txt ("city,mw_city") limits it.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHCityStreets
{
    static readonly string[] Cities = { "city", "hc_city", "ss_city", "mw_city", "co_city", "ch_city", "meadow" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/streets"); Directory.CreateDirectory(d); return d; } }

    [MenuItem("Ashen Hollow/Test: City Street Shots #&y")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] list = Cities; string only = Path.Combine(Application.dataPath, "../HeroShots/streets_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 9)) { if (t - t0 > 60) want = null; return; }
            if (want != null) Shoot(gg);
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: street shots done (HeroShots/streets)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) gg.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 9;
        };
        EditorApplication.update += cb;
    }

    static Bounds BoundsOf(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(); Bounds b = new Bounds(t.position, Vector3.zero); bool any = false;
        foreach (var r in rs) { if (!r.enabled || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
        return b;
    }

    static void Shoot(AHGame g)
    {
        string id = AHGame.AreaId; var sb = new StringBuilder();
        var rootGo = GameObject.Find("City buildings"); var root = rootGo != null ? rootGo.transform : null;
        var houses = new List<Transform>();
        if (root != null) foreach (Transform h in root) { string n = h.name; if (h.gameObject.activeInHierarchy && (n.StartsWith("Townhouse") || n.StartsWith("Building_") || n.StartsWith("House"))) houses.Add(h); }
        // the village ring in Hollow Meadow
        if (houses.Count == 0) { var v = GameObject.Find("Village"); if (v != null) foreach (Transform h in v.transform) if (h.gameObject.activeInHierarchy) houses.Add(h); }
        sb.AppendLine(id + ": " + houses.Count + " houses");
        // everything else that draws, once
        var others = new List<Renderer>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
            if (root != null && r.transform.IsChildOf(root)) continue;
            string n = r.gameObject.name; if (n.StartsWith("AH_GROUND") || n.StartsWith("AH_SEA") || n == "Outskirts" || n.Contains("Range")) continue;
            if (g.player != null && r.transform.IsChildOf(g.player.transform)) continue;
            if (r.GetComponentInParent<AHMob>() != null) continue;
            others.Add(r);
        }
        int inside = 0, thin = 0;
        foreach (var h in houses)
        {
            var b = BoundsOf(h); if (b.size.sqrMagnitude < 1f) continue;
            float dep = Mathf.Min(b.size.x, b.size.z), wid = Mathf.Max(b.size.x, b.size.z);
            if (dep < 0.45f * wid || dep < 3.2f) { thin++; sb.AppendLine("  thin " + h.name + " at " + b.center.ToString("F1") + " " + b.size.ToString("F1")); }
            var core = new Bounds(b.center, new Vector3(b.size.x * 0.8f, b.size.y * 0.9f, b.size.z * 0.8f));
            foreach (var r in others)
            {
                var rb = r.bounds; if (rb.size.magnitude < 0.8f || rb.size.magnitude > 40f) continue;
                if (!core.Contains(rb.center)) continue;
                // a piece that sticks out of the house's walls or roof
                bool pokes = rb.max.y > b.max.y - 0.2f || rb.min.x < b.min.x || rb.max.x > b.max.x || rb.min.z < b.min.z || rb.max.z > b.max.z;
                string path = r.name; for (var t = r.transform.parent; t != null && t.parent != null; t = t.parent) path = t.name + "/" + path;
                sb.AppendLine("  in " + h.name + (pokes ? " POKES OUT" : "") + ": " + path + " at " + rb.center.ToString("F1") + " size " + rb.size.ToString("F1"));
                inside++;
            }
        }
        // flat round discs on the ground
        int discs = 0;
        foreach (var r in others)
        {
            var rb = r.bounds; if (rb.size.y > 0.25f || rb.min.y > 0.6f) continue;
            float a = rb.size.x, c = rb.size.z; if (a < 1.2f || c < 1.2f || Mathf.Abs(a - c) > 0.25f * Mathf.Max(a, c)) continue;
            string path = r.name; for (var t = r.transform.parent; t != null && t.parent != null; t = t.parent) path = t.name + "/" + path;
            sb.AppendLine("  disc " + path + " (" + (r.sharedMaterial != null ? r.sharedMaterial.name : "-") + ") at " + rb.center.ToString("F1") + " size " + rb.size.ToString("F1"));
            discs++;
        }
        sb.AppendLine(id + ": " + inside + " pieces inside houses, " + thin + " thin houses, " + discs + " flat discs");
        File.WriteAllText(Path.Combine(Dir, id + ".txt"), sb.ToString());
        // the pictures: up to 24 houses, spread through the list
        const int W = 360, H = 240, C = 6; int n0 = Mathf.Min(24, houses.Count); if (n0 == 0) return;
        var go = new GameObject("StreetCam"); var cam = go.AddComponent<Camera>(); cam.nearClipPlane = 0.2f; cam.farClipPlane = 600f; cam.fieldOfView = 55f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; cam.cullingMask = g.cam.cullingMask; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt; var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        int rows = (n0 + C - 1) / C; var sheet = new Texture2D(W * C, H * rows, TextureFormat.RGB24, false);
        var R = g.WalkArea; Vector3 mid = new Vector3(R.center.x, 0f, R.center.y);
        for (int k = 0; k < n0; k++)
        {
            var h = houses[k * houses.Count / n0]; var b = BoundsOf(h);
            // from the street side: towards the middle of the city
            Vector3 dir = mid - b.center; dir.y = 0f; if (dir.sqrMagnitude < 1f) dir = Vector3.forward; dir.Normalize();
            float back = Mathf.Max(b.extents.x, b.extents.z) + 7f;
            go.transform.position = new Vector3(b.center.x, 0f, b.center.z) + dir * back + Vector3.up * 4.5f;
            go.transform.LookAt(new Vector3(b.center.x, b.size.y * 0.45f, b.center.z));
            cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); RenderTexture.active = null;
            sheet.SetPixels((k % C) * W, (rows - 1 - k / C) * H, W, H, tex.GetPixels());
        }
        sheet.Apply(); File.WriteAllBytes(Path.Combine(Dir, id + "_houses.jpg"), sheet.EncodeToJPG(84));
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(sheet); Object.Destroy(go);
        Debug.Log("Ashen Hollow: street shots " + id);
    }
}
