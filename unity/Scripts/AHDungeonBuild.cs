// Ashen Hollow: the dungeons built of stone. Under the hill they were bare caves: rough rock walls and a sandy floor.
// Now each one is a dug-out keep: dressed stone walls follow the cave's edge all the way round, with columns where
// the wall turns, iron torches fixed to the walls, banners, swords hung on the stone and cobwebs in the corners; the
// floor is laid with flagstones, and old bones, skulls, rocks, candles and potions lie about. Each dungeon has its
// own stone (warm sandstone in the Sand King's tomb, pale ice-washed grey in Frostpeak, scorched dark rock in the
// Molten Depths, mossy grey in the Mire Warrens). The rock still rises behind the walls.
// Pieces: Quaternius Modular Dungeon packs (2018 and 2019), CC0 (Resources/AH/Models/QD, kept on the PC).
// Nothing here blocks your way: the walls stand just inside the rock, the props along the walls.
using System.Collections.Generic;
using UnityEngine;

public static class AHDungeonBuild
{
    const string Dir = "AH/Models/QD/";
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    public static GameObject Q(string n) { GameObject g; if (!cache.TryGetValue(n, out g)) { g = Resources.Load<GameObject>(Dir + n); cache[n] = g; } return g; }
    public static int Walls { get; private set; }

    // the stone of each dungeon (multiplies the pieces' own colours)
    static Color StoneOf(string area)
    {
        if (area.Contains("tomb")) return new Color(1.12f, 1.0f, 0.82f);
        if (area.Contains("frost")) return new Color(0.86f, 0.95f, 1.12f);
        if (area.Contains("molten") || area == "forge") return new Color(0.62f, 0.55f, 0.52f);
        if (area.Contains("warren")) return new Color(0.78f, 0.9f, 0.72f);
        return new Color(0.8f, 0.8f, 0.86f);
    }

    public class Seg { public Vector3 a, b, inward; }

    // builds the walls along the cave's edge; returns the wall pieces (for torches and props on them)
    public static List<Seg> Build(AHGame g, Transform root, Color flame)
    {
        var segs = new List<Seg>(); Walls = 0;
        if (Q("qd_wall_modular") == null) return segs;
        var area = g.AreaRect; float y = g.W(0, 0).y;
        Color stone = StoneOf(AHGame.AreaId);
        var exits = new List<Vector3>(); foreach (var s in AHGather.Spots) if (s.kind == "gate" || s.kind == "use" || s.kind == "door") exits.Add(s.pos);

        // where you can walk, sampled every 0.75 m
        const float res = 0.75f;
        int nx = Mathf.CeilToInt(area.width / res) + 1, nz = Mathf.CeilToInt(area.height / res) + 1;
        if (nx * nz > 400000) return segs;
        var walk = new bool[nx, nz];
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) walk[i, j] = i > 0 && j > 0 && i < nx - 1 && j < nz - 1 && !g.Blocked(new Vector3(area.xMin + i * res, y, area.yMin + j * res), 0.2f);
        System.Func<float, float, Vector3> P = (fi, fj) => new Vector3(area.xMin + fi * res, y, area.yMin + fj * res);

        // marching squares: the edge between walkable and rock, as little segments
        var raw = new List<KeyValuePair<Vector2, Vector2>>();
        for (int i = 0; i < nx - 1; i++)
            for (int j = 0; j < nz - 1; j++)
            {
                int c = (walk[i, j] ? 1 : 0) | (walk[i + 1, j] ? 2 : 0) | (walk[i + 1, j + 1] ? 4 : 0) | (walk[i, j + 1] ? 8 : 0);
                if (c == 0 || c == 15) continue;
                Vector2 bo = new Vector2(i + 0.5f, j), ri = new Vector2(i + 1, j + 0.5f), to = new Vector2(i + 0.5f, j + 1), le = new Vector2(i, j + 0.5f);
                switch (c)
                {
                    case 1: case 14: raw.Add(new KeyValuePair<Vector2, Vector2>(le, bo)); break;
                    case 2: case 13: raw.Add(new KeyValuePair<Vector2, Vector2>(bo, ri)); break;
                    case 3: case 12: raw.Add(new KeyValuePair<Vector2, Vector2>(le, ri)); break;
                    case 4: case 11: raw.Add(new KeyValuePair<Vector2, Vector2>(ri, to)); break;
                    case 6: case 9: raw.Add(new KeyValuePair<Vector2, Vector2>(bo, to)); break;
                    case 7: case 8: raw.Add(new KeyValuePair<Vector2, Vector2>(le, to)); break;
                    case 5: raw.Add(new KeyValuePair<Vector2, Vector2>(le, bo)); raw.Add(new KeyValuePair<Vector2, Vector2>(ri, to)); break;
                    case 10: raw.Add(new KeyValuePair<Vector2, Vector2>(bo, ri)); raw.Add(new KeyValuePair<Vector2, Vector2>(le, to)); break;
                }
            }
        // chain them into lines
        var ends = new Dictionary<long, List<int>>();
        System.Func<Vector2, long> K = v => (long)Mathf.RoundToInt(v.x * 2) * 100000L + Mathf.RoundToInt(v.y * 2);
        for (int s = 0; s < raw.Count; s++) foreach (var v in new[] { raw[s].Key, raw[s].Value }) { long k = K(v); List<int> l; if (!ends.TryGetValue(k, out l)) ends[k] = l = new List<int>(); l.Add(s); }
        var used = new bool[raw.Count]; var lines = new List<List<Vector2>>();
        for (int s0 = 0; s0 < raw.Count; s0++)
        {
            if (used[s0]) continue; used[s0] = true;
            var line = new List<Vector2> { raw[s0].Key, raw[s0].Value };
            for (int dir = 0; dir < 2; dir++)
            {
                while (true)
                {
                    Vector2 tip = dir == 0 ? line[line.Count - 1] : line[0]; int next = -1;
                    foreach (int s in ends[K(tip)]) if (!used[s]) { next = s; break; }
                    if (next < 0) break; used[next] = true;
                    Vector2 o = K(raw[next].Key) == K(tip) ? raw[next].Value : raw[next].Key;
                    if (dir == 0) line.Add(o); else line.Insert(0, o);
                }
            }
            if (line.Count >= 4) lines.Add(line);
        }

        var mats = new Dictionary<Material, Material>(); var built = new List<GameObject>();
        var wallPf = Q("qd_wall_modular"); var colPf = Q("qd_column");
        const float H = 3.4f;
        foreach (var line in lines)
        {
            var pts = Simplify(line, 0.9f / res);
            for (int k = 0; k < pts.Count - 1; k++)
            {
                Vector3 a = P(pts[k].x, pts[k].y), b = P(pts[k + 1].x, pts[k + 1].y);
                Vector3 d = b - a; float L = d.magnitude; if (L < 0.8f) continue; d /= L;
                Vector3 n = new Vector3(-d.z, 0, d.x); Vector3 m = (a + b) * 0.5f;
                bool inA = !g.Blocked(m + n * 0.9f, 0.1f), inB = !g.Blocked(m - n * 0.9f, 0.1f);
                if (inA == inB) continue;
                Vector3 inward = inA ? n : -n;
                int cnt = Mathf.Max(1, Mathf.RoundToInt(L / 2.6f)); float l = L / cnt;
                for (int q = 0; q < cnt; q++)
                {
                    Vector3 c = a + d * (l * (q + 0.5f));
                    bool near = false; foreach (var x in exits) if ((x - c).sqrMagnitude < 12f) near = true;
                    if (near) continue;
                    var w = Object.Instantiate(wallPf, root, false); Strip(w); Tint(w, stone, mats);
                    w.transform.position = c - inward * 0.35f + Vector3.up * (H * 0.5f);
                    w.transform.rotation = Quaternion.LookRotation(inward);
                    w.transform.localScale = new Vector3(l / 2f + 0.04f, H / 2f, 1f);
                    segs.Add(new Seg { a = c - d * l * 0.5f, b = c + d * l * 0.5f, inward = inward }); Walls++; built.Add(w);
                }
                // a column where the wall turns
                if (k > 0 && colPf != null)
                {
                    bool near = false; foreach (var x in exits) if ((x - a).sqrMagnitude < 12f) near = true;
                    if (!near)
                    {
                        var col = Object.Instantiate(colPf, root, false); Strip(col); Tint(col, stone * 0.92f, mats);
                        col.transform.position = a - inward * 0.3f; col.transform.rotation = Quaternion.LookRotation(inward);
                        col.transform.localScale = Vector3.one * 0.82f; built.Add(col);
                    }
                }
            }
        }
        // the old cave rocks that stood out in the open floor, in front of the new walls: hidden (the walls are the cave's
        // edge now; rocks mostly behind them stay, their tops showing over the wall as the cave roof going up)
        int rocks = 0;
        var mt = GameObject.Find("Mountains");
        if (mt != null)
            foreach (var r in mt.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue;
                var b = r.bounds; int all = 0, open = 0;
                for (float x = b.min.x; x <= b.max.x; x += res)
                    for (float z = b.min.z; z <= b.max.z; z += res)
                    {
                        int i = Mathf.RoundToInt((x - area.xMin) / res), j = Mathf.RoundToInt((z - area.yMin) / res);
                        if (i < 0 || j < 0 || i >= nx || j >= nz) continue;
                        all++; if (walk[i, j]) open++;
                    }
                if (all > 0 && open > all * 0.25f) { r.enabled = false; rocks++; }
            }
        // the old web-game blocks and cobweb strands the new walls now stand in (the Sunken Forge's pillars showed
        // half in and half out of the dressed stone, a plain dark box against the bricks): hidden behind the wall
        var wb = new List<Bounds>();
        foreach (var w in built) { var rs = w.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); wb.Add(b); }
        int inWall = 0;
        if (wb.Count > 0)
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.transform.IsChildOf(root)) continue;
                if (!r.name.StartsWith("obj") && (r.transform.parent == null || !r.transform.parent.name.StartsWith("obj"))) continue;
                var b = r.bounds; float fx = b.size.x, fz = b.size.z; if (Mathf.Max(fx, fz) > 3.5f || b.size.y > 6f) continue;
                float own = Mathf.Max(0.01f, fx * fz), over = 0f; bool swallow = false;
                foreach (var w in wb)
                {
                    float ox = Mathf.Min(b.max.x, w.max.x) - Mathf.Max(b.min.x, w.min.x), oz = Mathf.Min(b.max.z, w.max.z) - Mathf.Max(b.min.z, w.min.z);
                    if (ox > 0f && oz > 0f && b.max.y > w.min.y && b.min.y < w.max.y)
                    {
                        over += ox * oz;
                        if (ox * oz > 0.5f * w.size.x * w.size.z) swallow = true;   // the block swallows half a wall piece
                    }
                }
                if (swallow) over = own;
                // a thin strand (a cobweb line) counts by length: any real cross with a wall
                bool thin = Mathf.Min(fx, fz) < 0.25f;
                if ((thin && over > 0.02f) || over > own * 0.3f) { r.enabled = false; inWall++; }
            }
        if (inWall > 0) Debug.Log("Ashen Hollow: " + inWall + " old blocks hidden inside the new dungeon walls");
        // the floor laid with flagstones (the ground's own colour, a stone pattern over it)
        var tex = Resources.Load<Texture2D>("AH/Textures/dungeon_floor");
        if (tex != null)
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.name.StartsWith("AH_GROUND") && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_PaveMap"))
                {
                    var m = r.sharedMaterial; m.SetTexture("_PaveMap", tex); m.SetFloat("_PaveScale", 0.42f); m.SetFloat("_PaveAmt", 1f); m.SetFloat("_PaveGrass", 1f); m.SetFloat("_PaveWater", 1f);
                    m.SetVector("_PaveRect", new Vector4(-1e6f, -1e6f, 1e6f, 1e6f));
                }
        Debug.Log("Ashen Hollow: dungeon built of stone: " + lines.Count + " wall lines, " + Walls + " wall pieces, " + rocks + " rocks off the floor");
        return segs;
    }

    // Douglas-Peucker: the cave's ragged edge as a few straight runs of wall
    static List<Vector2> Simplify(List<Vector2> p, float tol)
    {
        var keep = new bool[p.Count]; keep[0] = keep[p.Count - 1] = true;
        var st = new Stack<Vector2Int>(); st.Push(new Vector2Int(0, p.Count - 1));
        while (st.Count > 0)
        {
            var r = st.Pop(); float best = 0; int bi = -1;
            Vector2 a = p[r.x], b = p[r.y], ab = b - a; float len = ab.magnitude;
            for (int i = r.x + 1; i < r.y; i++)
            {
                float dd = len < 1e-4f ? (p[i] - a).magnitude : Mathf.Abs(ab.x * (p[i].y - a.y) - ab.y * (p[i].x - a.x)) / len;
                if (dd > best) { best = dd; bi = i; }
            }
            if (bi >= 0 && (best > tol || (r.x == 0 && r.y == p.Count - 1 && len < 1e-3f))) { keep[bi] = true; st.Push(new Vector2Int(r.x, bi)); st.Push(new Vector2Int(bi, r.y)); }
        }
        var o = new List<Vector2>(); for (int i = 0; i < p.Count; i++) if (keep[i]) o.Add(p[i]);
        return o;
    }

    public static void Strip(GameObject go) { foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c); }
    public static void Tint(GameObject go, Color k, Dictionary<Material, Material> mats)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var sm = r.sharedMaterials;
            for (int i = 0; i < sm.Length; i++)
            {
                if (sm[i] == null) continue; Material t;
                if (!mats.TryGetValue(sm[i], out t))
                {
                    t = new Material(sm[i]);
                    foreach (var p in new[] { "_BaseColor", "_Color" }) if (t.HasProperty(p)) { var c = t.GetColor(p); t.SetColor(p, new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a)); }
                    t.enableInstancing = true; mats[sm[i]] = t;
                }
                sm[i] = t;
            }
            r.sharedMaterials = sm;
        }
        AHModel.SetShadows(go);
    }

    // a piece hung on a wall: turned so the side it reaches out to faces into the room (whatever way the model was
    // made), its back against the stone
    public static GameObject Mount(GameObject pf, Transform root, Vector3 at, Vector3 inward, float scale)
    {
        if (pf == null) return null;
        var go = Object.Instantiate(pf, root, false); Strip(go);
        go.transform.position = at; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one * scale;
        var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return go;
        Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        Vector3 reach = b.center - at; reach.y = 0;
        if (reach.sqrMagnitude < 1e-4f) { go.transform.rotation = Quaternion.LookRotation(inward); return go; }
        go.transform.rotation = Quaternion.AngleAxis(Vector3.SignedAngle(reach, inward, Vector3.up), Vector3.up);
        AHModel.SetShadows(go);
        return go;
    }
}
