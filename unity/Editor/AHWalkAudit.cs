// Ashen Hollow (editor test): "Test: Walk-Through Audit" (ctrl+shift+alt+W). Visits each land and lists every standing thing
// (taller than 1.2 m, wider than half a metre, smaller than 30 m: houses, walls, rocks, carts, statues, fences...) whose
// middle you could still walk into: not blocked by the game's walls, round blockers or solid ground map. Writes
// HeroShots/walkaudit/<area>.txt (each thing: name, where, size) and all.txt (a count per land). HeroShots/walkaudit_only.txt
// ("sands,meadow") limits it to those lands; "Test: Walk-Through Audit Here" does just the land you are in.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AHWalkAudit
{
    static readonly string[] Areas = { "meadow", "mill", "silkwood", "mire", "vale", "city", "frost", "hc_city", "sands", "ss_city", "mw_city", "isle", "tide", "co_city", "ember", "ch_city", "fossil", "ashfall", "causeway", "fenwick", "kingsroad", "scorchwind", "whitepine", "homestead" };
    static string Dir { get { var d = Path.Combine(Application.dataPath, "../HeroShots/walkaudit"); Directory.CreateDirectory(d); return d; } }
    static readonly string[] Skip = { "grass", "flower", "leaf", "leaves", "canopy", "crown", "foliage", "water", "ground", "sea", "sky", "range", "horizon", "cloud", "fog", "dust", "spark", "fx", "shadow", "decal", "road", "path", "rug", "carpet", "banner", "flag", "lamp", "torch", "light", "hero", "mount", "pet", "npc", "mob", "ship", "bird", "butterfly", "glow", "beam", "web", "vine", "ivy", "rope", "sign", "gate", "bridge", "door", "arch", "awning", "roof" };

    [MenuItem("Ashen Hollow/Test: Walk-Through Audit %#&w")]
    static void Run()
    {
        var g = AHGame.I; if (!Application.isPlaying || g == null || g.player == null) return;
        string[] list = Areas; string only = Path.Combine(Application.dataPath, "../HeroShots/walkaudit_only.txt");
        if (File.Exists(only)) { var l = File.ReadAllText(only).Split(new[] { ',', '\n', '\r', ' ' }, System.StringSplitOptions.RemoveEmptyEntries); if (l.Length > 0) list = l; }
        var all = new StringBuilder(); int i = -1; double t0 = 0; string want = null;
        EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            var gg = AHGame.I;
            if (!Application.isPlaying || gg == null) { EditorApplication.update -= cb; return; }
            double t = EditorApplication.timeSinceStartup;
            if (want != null && (AHGame.AreaId != want || t - t0 < 10)) { if (t - t0 > 60) want = null; return; }
            if (want != null) all.AppendLine(want + ": " + Audit(gg) + " things you could walk into");
            i++;
            if (i >= list.Length) { EditorApplication.update -= cb; File.WriteAllText(Path.Combine(Dir, "all.txt"), all.ToString()); Debug.Log("Ashen Hollow: walk-through audit done\n" + all); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + list[i]); if (ta == null) { want = null; return; }
            var d = JsonUtility.FromJson<AHWorldData>(ta.text);
            want = list[i]; t0 = t;
            if (AHGame.AreaId != want) gg.Travel(want, d.spawn.x, d.spawn.z, 0f); else t0 -= 10;
        };
        EditorApplication.update += cb;
    }

    [MenuItem("Ashen Hollow/Test: Walk-Through Audit Here")]
    static void Here() { var g = AHGame.I; if (Application.isPlaying && g != null) Debug.Log("Ashen Hollow: " + Audit(g) + " things you could walk into"); }

    static int Audit(AHGame g)
    {
        var sb = new StringBuilder(); int n = 0; var seen = new HashSet<Transform>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (r.gameObject.layer == AHRange.Layer || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
            // the thing a renderer belongs to: its top object below the area's roots (a house of many parts counts once)
            var top = r.transform; while (top.parent != null && top.parent.parent != null && top.parent.GetComponent<Renderer>() == null && top.parent.childCount < 40) top = top.parent;
            if (!seen.Add(top)) continue;
            string path = PathOf(r.transform).ToLowerInvariant(); bool skip = false;
            foreach (var s in Skip) if (path.Contains(s)) { skip = true; break; }
            if (skip) continue;
            var rs = top.GetComponentsInChildren<Renderer>(); Bounds b = r.bounds; foreach (var q in rs) if (!(q is ParticleSystemRenderer)) b.Encapsulate(q.bounds);
            if (b.size.y < 1.2f || Mathf.Min(b.size.x, b.size.z) < 0.5f || Mathf.Max(b.size.x, b.size.z) > 30f) continue;
            Vector3 c = b.center; c.y = g.player.transform.position.y;
            if (!g.InArea(c)) continue;
            // standing on the ground (not a branch, a sign high on a wall)
            if (b.min.y > g.player.transform.position.y + 1.0f) continue;
            if (g.Blocked(c, 0.05f)) continue;
            n++; sb.AppendLine(PathOf(top) + "  at " + c.ToString("F1") + "  size " + b.size.ToString("F1"));
        }
        File.WriteAllText(Path.Combine(Dir, AHGame.AreaId + ".txt"), sb.ToString());
        return n;
    }
    static string PathOf(Transform t) { var s = t.name; for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s; return s; }
}
