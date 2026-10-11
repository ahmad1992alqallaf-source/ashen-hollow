// Ashen Hollow (editor test): "Test: World Audit, All Areas" / "Test: World Audit Here" (shift+alt+W). Looks over every
// land for the things that break the picture or the play:
//  - FLOAT: a prop hanging in the air (its lowest point well above the ground)
//  - SUNK: a prop buried (its top barely above the ground, or below it)
//  - INSIDE: two props standing in each other (their boxes overlapping by much of the smaller one)
//  - WALK: a solid prop you can walk straight through (no blocker under it)
// A "prop" is one child of a scene root that holds scenery (the world model's pieces, the rebuilt trees, tents,
// houses...). People, monsters, the hero, lights, effects, grass and the ground itself are left out.
// Writes HeroShots/audit/audit.txt (every land: counts by kind and by root, then each case) and pictures of the
// first cases of each kind, HeroShots/audit/<area>_<kind>.jpg (a frame a case, the case's box drawn as its name).
// Tents are photographed too: HeroShots/audit/<area>_tents.jpg.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHWorldAudit
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead", "forge", "kq", "raid", "abyss", "d_hollow", "d_frost", "d_molten", "d_tomb", "d_warrens" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/audit"); Directory.CreateDirectory(d); return d; } }

    // roots that are not scenery
    static readonly string[] SkipRoots = { "Hero", "Main Camera", "Sun", "HUD", "Weather", "Season", "Pet", "EventSystem", "DontDestroyOnLoad", "Gallery", "ItemStudio", "Mobs", "NPCs", "Allies", "Motes", "Void", "Sky", "Clouds", "Birds", "Grass", "DreamScatter", "Herbs" };
    static readonly string[] SkipNames = { "AH_GROUND", "AH_WATER", "Water", "Grass", "Flower", "Fx", "Glow", "Light", "Spark", "Ember", "Mote", "Smoke", "Seam", "Rift", "Shadow", "Decal", "Cloth", "Banner", "Rug", "Carpet", "Path", "Road", "Fissure", "Tear", "Crack", "Puddle", "Leaf", "Petal" };

    [MenuItem("Ashen Hollow/Test: World Audit, All Areas #&q")]
    static void All()
    {
        if (!Application.isPlaying || AHGame.I == null) return;
        foreach (var f in Directory.GetFiles(Dir)) File.Delete(f);
        File.WriteAllText(Path.Combine(Dir, "audit.txt"), "");
        int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            if (!Application.isPlaying || AHGame.I == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 11)) { if (t - t0 > 45) want = null; return; }
            if (want != null) Here();
            i++;
            if (i >= Areas.Length) { EditorApplication.update -= cb; Debug.Log("Ashen Hollow: world audit done (HeroShots/audit)"); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + Areas[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = Areas[i]; t0 = t;
            if (AHGame.AreaId != want) AHGame.I.Travel(want, d.spawn.x, d.spawn.z, 0f);
        };
        EditorApplication.update += cb;
    }

    class Prop { public Transform t; public string root, path; public Bounds b; public float foot; }
    class Case { public string kind; public Prop a, b; public string note; public float score; }

    static readonly string[] NaturalRoots = { "Mountains", "Forest", "Trees", "Ore rocks", "Ore", "Crystals", "Palms", "Cacti", "Bones", "Coral", "Ferns", "Mushrooms", "Ranges", "Rocks" };
    static readonly string[] Loose = { "Tents", "Rebuilt props", "Rebuilt boxes", "Stalls and wells", "Stations", "Ruins", "Graves", "Treasure" };
    static bool Natural(string r) { foreach (var n in NaturalRoots) if (r == n) return true; return false; }
    static bool Skip(string n) { foreach (var s in SkipNames) if (n.StartsWith(s) || n.Contains(s)) return true; return false; }

    static List<Prop> Props(AHGame g)
    {
        var list = new List<Prop>();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = new List<GameObject>(scene.GetRootGameObjects());
        foreach (var r in roots)
        {
            if (!r.activeInHierarchy) continue;
            bool skip = false; foreach (var s in SkipRoots) if (r.name.StartsWith(s)) skip = true;
            if (skip || r.GetComponentInChildren<AHMob>(true) != null && r.GetComponent<AHMob>() != null) continue;
            if (r.GetComponent<Camera>() != null || r.GetComponent<Canvas>() != null) continue;
            if (r.transform.childCount > 0 && r.GetComponentInChildren<SkinnedMeshRenderer>(true) != null && r.GetComponentsInChildren<Renderer>(true).Length < 40) continue;   // a person or a beast
            // the world model is one more level down (its pieces are the props); a lone built thing is its own prop
            var units = new List<Transform>();
            if (r.transform.childCount == 0) units.Add(r.transform);
            else foreach (Transform c in r.transform) units.Add(c);
            if (units.Count == 1 && units[0].childCount > 3 && units[0] != r.transform && units[0].GetComponent<Renderer>() == null) { var u0 = units[0]; units.Clear(); foreach (Transform c in u0) units.Add(c); }
            foreach (var u in units)
            {
                if (!u.gameObject.activeInHierarchy || Skip(u.name)) continue;
                if (u.GetComponentInParent<AHMob>() != null || u.GetComponentInChildren<AHMob>() != null) continue;
                if (u.GetComponentInChildren<SkinnedMeshRenderer>(true) != null || u.GetComponentInChildren<Animator>(true) != null) continue;   // people and animals
                bool any = false; Bounds b = new Bounds();
                foreach (var rd in u.GetComponentsInChildren<Renderer>())
                {
                    if (!rd.enabled || rd is ParticleSystemRenderer || rd is SkinnedMeshRenderer || rd is LineRenderer || rd is TrailRenderer) continue;
                    if (Skip(rd.name)) continue;
                    if (!any) { b = rd.bounds; any = true; } else b.Encapsulate(rd.bounds);
                }
                if (!any || b.size.magnitude < 0.25f || b.size.x > 60f || b.size.z > 60f) continue;
                var sb = new StringBuilder(); for (var t = u; t != null; t = t.parent) sb.Insert(0, "/" + t.name);
                list.Add(new Prop { t = u, root = r.name, path = sb.ToString(), b = b, foot = Mathf.Max(b.size.x, b.size.z) });
            }
        }
        return list;
    }

    static float GroundY(AHGame g)
    {
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) return r.bounds.max.y;
        return g.W(0, 0).y;
    }

    [MenuItem("Ashen Hollow/Test: World Audit Here #&w")]
    static void Here()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null) return;
        var props = Props(g); float gy = GroundY(g);
        var cases = new List<Case>();
        bool abyss = AHGame.AreaId == "abyss";
        foreach (var p in props)
        {
            // floating: more than 0.35 m clear of the ground (and not a sky thing, a hanging thing or an island's piece)
            float lift = p.b.min.y - gy;
            if (!abyss && lift > 0.35f && lift < 25f && p.b.size.y < 20f && !(p.t.name.Contains("Lamp") || p.t.name.Contains("Sign") || p.t.name.Contains("Lantern")))
            {
                // something under it holding it up (a table under a candle, a roof under a chimney)
                // something holding it up or on: a table under a candle, a roof under a chimney, a wall a torch hangs on
                bool held = false; var pe = p.b; pe.Expand(0.3f);
                foreach (var q in props) if (q != p && q.b.min.y < p.b.min.y - 0.05f && q.b.Intersects(pe)) { held = true; break; }
                if (!held) cases.Add(new Case { kind = "FLOAT", a = p, note = "bottom " + lift.ToString("F2") + " m up", score = lift * p.foot });
            }
            // sunk: its top hardly out of the ground
            if (!abyss && p.b.max.y < gy + 0.12f && p.b.size.y > 0.15f) cases.Add(new Case { kind = "SUNK", a = p, note = "top " + (p.b.max.y - gy).ToString("F2") + " m", score = p.foot });
            // walk-through: a tall, solid-looking thing with nothing to stop you at its middle
            float thin = Mathf.Min(p.b.size.x, p.b.size.z);
            if (p.b.size.y > 1.3f && p.foot > 0.7f && p.foot < 14f && p.foot < thin * 2.6f && p.b.min.y < gy + 0.4f && !p.t.name.StartsWith("qd_wall") && !p.t.name.StartsWith("qd_column"))
            {
                // the middle and four points half way out: none of them stops you
                int hits = 0;
                for (int k = 0; k < 5; k++)
                {
                    Vector3 c = new Vector3(p.b.center.x + (k == 1 ? 0.25f : k == 2 ? -0.25f : 0f) * p.b.size.x, gy, p.b.center.z + (k == 3 ? 0.25f : k == 4 ? -0.25f : 0f) * p.b.size.z);
                    if (g.Blocked(c, 0.05f) || !g.InArea(c)) hits++;
                }
                if (g.Blocked(new Vector3(p.t.position.x, gy, p.t.position.z), 0.05f)) hits++;   // the trunk or post at its pivot
                if (hits == 0) cases.Add(new Case { kind = "WALK", a = p, note = p.b.size.ToString("F1"), score = p.foot * p.b.size.y });
            }
        }
        // inside each other: boxes overlapping by much of the smaller one (both standing on the ground)
        for (int i = 0; i < props.Count; i++)
            for (int j = i + 1; j < props.Count; j++)
            {
                var a = props[i]; var b = props[j];
                if (!a.b.Intersects(b.b) || a.t.IsChildOf(b.t) || b.t.IsChildOf(a.t)) continue;
                if (Natural(a.root) && Natural(b.root)) continue;
                if (a.root == b.root && a.t.parent == b.t.parent && a.t.parent != null && a.t.parent.childCount < 8) continue;   // parts of one thing
                if (a.root == b.root && System.Array.IndexOf(Loose, a.root) < 0) continue;   // a castle's, a ship's, a pier's own pieces   // crags in crags, trees by trees: as nature makes them
                if ((a.root == "Mountains" || b.root == "Mountains") && (a.root == "Dungeon dressing" || b.root == "Dungeon dressing")) continue;   // cave walls set into the rock
                if (a.foot < 0.6f && b.foot < 0.6f) continue;
                float ox = Mathf.Min(a.b.max.x, b.b.max.x) - Mathf.Max(a.b.min.x, b.b.min.x), oz = Mathf.Min(a.b.max.z, b.b.max.z) - Mathf.Max(a.b.min.z, b.b.min.z), oy = Mathf.Min(a.b.max.y, b.b.max.y) - Mathf.Max(a.b.min.y, b.b.min.y);
                if (ox <= 0f || oz <= 0f || oy <= 0f) continue;
                float va = a.b.size.x * a.b.size.y * a.b.size.z, vb = b.b.size.x * b.b.size.y * b.b.size.z, ov = ox * oy * oz;
                float frac = ov / Mathf.Max(1e-4f, Mathf.Min(va, vb));
                // the middles close together too (two boxes can overlap a lot at their corners while the things don't)
                Vector2 d = new Vector2(a.b.center.x - b.b.center.x, a.b.center.z - b.b.center.z);
                float near = d.magnitude / Mathf.Max(0.3f, Mathf.Min(a.foot, b.foot) * 0.5f);
                if (frac > 0.45f && near < 1.0f && Mathf.Max(va, vb) < 4000f)
                    cases.Add(new Case { kind = "INSIDE", a = a, b = b, note = (frac * 100f).ToString("F0") + "% of the smaller", score = Mathf.Min(va, vb) * frac });
            }
        // stuck: something you use (a station, a chest, a waystone) or someone you talk to with no free ground
        // to stand on within reach of it (a new blocker may have closed it in)
        System.Action<Vector3, float, string> stuck = (at, reach, what) =>
        {
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI / 8f;
                foreach (float rr in new[] { reach * 0.55f, reach * 0.85f, reach * 0.98f })
                {
                    var q = at + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr; q.y = gy;
                    if (g.InArea(q) && !g.Blocked(q, 0.3f)) return;
                }
            }
            var pb = new Bounds(new Vector3(at.x, gy + 1f, at.z), new Vector3(1f, 2f, 1f));
            cases.Add(new Case { kind = "STUCK", a = new Prop { t = null, root = what, path = what, b = pb, foot = 1f }, note = "nowhere to stand within " + reach.ToString("F1") + " m", score = 1000f });
        };
        foreach (var sp in AHGather.Spots) if (sp.kind != "chop" && sp.reach > 0.5f) stuck(sp.pos, sp.reach, "spot " + sp.kind + " " + (sp.name ?? sp.type ?? ""));
        foreach (var npc in Object.FindObjectsByType<AHNpc>(FindObjectsSortMode.None)) stuck(npc.transform.position, 2.2f, "npc " + npc.name);
        cases.Sort((x, y) => y.score.CompareTo(x.score));
        var o = new StringBuilder();
        var count = new Dictionary<string, int>(); var byRoot = new Dictionary<string, int>();
        foreach (var c in cases) { int n; count.TryGetValue(c.kind, out n); count[c.kind] = n + 1; string k = c.kind + " " + c.a.root + (c.b != null ? " + " + c.b.root : ""); byRoot.TryGetValue(k, out n); byRoot[k] = n + 1; }
        o.AppendLine("== " + AHGame.AreaId + ": " + props.Count + " props, ground " + gy.ToString("F2"));
        foreach (var kv in count) o.AppendLine("  " + kv.Key + ": " + kv.Value);
        var keys = new List<string>(byRoot.Keys); keys.Sort((x, y) => byRoot[y].CompareTo(byRoot[x]));
        foreach (var k in keys) o.AppendLine("    " + byRoot[k] + " x " + k);
        int shown = 0;
        foreach (var c in cases)
        {
            if (shown++ > 120) break;
            o.AppendLine(string.Format("  {0} {1} [{2}] size {3} at {4} {5}{6}", c.kind, c.a.path, c.a.root, c.a.b.size.ToString("F1"), c.a.b.center.ToString("F0"), c.note, c.b != null ? "  WITH " + c.b.path + " size " + c.b.b.size.ToString("F1") : ""));
        }
        File.AppendAllText(Path.Combine(Dir, "audit.txt"), o.ToString());
        foreach (var kind in new[] { "FLOAT", "SUNK", "INSIDE", "WALK", "STUCK" })
        {
            var sel = new List<Bounds>();
            foreach (var c in cases) if (c.kind == kind && sel.Count < 12) { var b = c.a.b; if (c.b != null) b.Encapsulate(c.b.b); sel.Add(b); }
            if (sel.Count > 0) Sheet(g, sel, AHGame.AreaId + "_" + kind.ToLowerInvariant());
        }
        // every tent, to look at their shapes
        var tents = new List<Bounds>();
        var tr = GameObject.Find("Tents");
        if (tr != null) foreach (Transform t in tr.transform) { if (tents.Count >= 12) break; var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); tents.Add(b); }
        if (tents.Count > 0) Sheet(g, tents, AHGame.AreaId + "_tents");
        Debug.Log("Ashen Hollow: world audit " + AHGame.AreaId + ": " + cases.Count + " cases");
    }

    static void Sheet(AHGame g, List<Bounds> boxes, string name)
    {
        const int W = 360, H = 260, cols = 4;
        int rows = (boxes.Count + cols - 1) / cols;
        var go = new GameObject("AuditCam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 45f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
        if (g.cam != null) { cam.clearFlags = g.cam.clearFlags; cam.backgroundColor = g.cam.backgroundColor; }
        var rt = new RenderTexture(W, H, 24); cam.targetTexture = rt;
        var sheet = new Texture2D(W * cols, H * rows, TextureFormat.RGB24, false);
        bool fog = RenderSettings.fog; RenderSettings.fog = false;
        for (int i = 0; i < boxes.Count; i++)
        {
            var b = boxes[i]; float s = Mathf.Max(2.5f, b.size.magnitude);
            go.transform.position = b.center + new Vector3(0.55f, 0.45f, -0.7f).normalized * (s * 1.3f + 1.5f); go.transform.LookAt(b.center);
            cam.Render(); RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, W, H), (i % cols) * W, (rows - 1 - i / cols) * H); RenderTexture.active = null;
        }
        RenderSettings.fog = fog; sheet.Apply();
        File.WriteAllBytes(Path.Combine(Dir, name + ".jpg"), sheet.EncodeToJPG(80));
        cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(sheet); Object.Destroy(go);
    }
}
