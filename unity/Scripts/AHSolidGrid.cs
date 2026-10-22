// Ashen Hollow: nothing you can walk into. Once a land is built, everything standing on it (crags, walls, houses,
// towers, ships, statues, rocks) is read from its own mesh: every face that passes between knee and head height is laid
// onto a grid (half a metre a cell), and whatever those faces close in (the inside of a crag, a house, a tower) is filled
// solid too. The hero, mounts and monsters are kept out of it (Resolve), and the camera will not go into it either: each
// cell knows how tall the thing standing on it is.
//  - left open: things you walk through or under (gates, doors, arches, bridges, piers, stairs, rugs, banners, lamps),
//    plants and trees (they have their own trunks), figures, water and the ground
//  - a place closed in all round that still holds something you go to (the spawn, a way out, a trader, a station, a
//    work spot) is not filled: it is meant to be reached
// One pass when the land loads; nothing is checked per frame but a grid lookup.
using System.Collections.Generic;
using UnityEngine;

public static class AHSolidGrid
{
    public const float Cell = 0.5f;
    const float BandLo = 0.6f, BandHi = 1.7f;    // knee to head, above the ground
    static int nx, nz; static float x0, z0, gy;
    static bool[] solid;          // the hero cannot stand here
    static float[] top;           // how tall the thing on this cell is (for the camera)
    static int[] nearest;         // for a solid cell: the open cell nearest it
    public static bool On { get; private set; }
    public static int Cells, Things, Unread;

    static readonly HashSet<string> SkipRoots = new HashSet<string> { "Forest", "Tree", "Pine", "Grass", "GrassPatch", "Flowers", "DreamScatter", "Herbs", "Plant", "Season", "Weather", "WeatherFall", "Ranges", "Range", "Bridges", "Bridge", "Piers", "pier", "Lava bridge", "Hero", "Pet", "Mount", "Phoenix", "HUD", "Streams", "Stream", "Pool", "Lava pool", "Treasure X", "Telegraph", "Motes", "Embers", "Cutscene", "Boss intro", "Sky", "Stars", "Moonlight", "Sun", "Ferns", "Leaf", "Fruit", "Bubbles", "Lightning", "Fill", "Horizon ships" };
    static readonly string[] Open = { "Bridge", "bridge", "Rug", "Carpet", "Arch", "arch", "Awning", "awning", "Gate", "gate", "Portal", "Door", "door", "Path", "Road", "Plank", "Dock", "Pier", "pier", "Stairs", "stairs", "Step", "Banner", "banner", "Flag", "flag", "Lamp", "lamp", "Lantern", "lantern", "Sign", "Torch", "torch", "Rope", "Web", "web", "Pennant", "AH_GROUND", "AH_SEA", "Water", "water", "Grass", "grass", "Flower", "flower", "Fern", "fern", "Bush", "bush", "Shadow", "Glow", "Light", "Spark", "Flame", "Smoke", "Cloud", "Decal", "Puddle", "Leaves" };

    public static void Clear() { On = false; solid = null; top = null; nearest = null; }

    public static bool Skip(Transform t)
    {
        for (var a = t; a != null; a = a.parent)
        {
            string n = a.name;
            if (SkipRoots.Contains(n)) return true;
            if (n.Contains("post") || n.Contains("Post")) continue;   // a gate post is solid
            foreach (var o in Open) if (n.Contains(o)) return true;
        }
        return t.GetComponentInParent<AHMob>() != null || t.GetComponentInParent<AHNpc>() != null || t.GetComponentInParent<AHPlayer>() != null;
    }

    public static void Build(AHGame g)
    {
        Clear(); Cells = 0; Things = 0; Unread = 0;
        if (g == null || AHGame.AreaId == "abyss" || AHGame.AreaId == AHDeep.Area) return;
        var t0 = Time.realtimeSinceStartup;
        AHBevel.HideGiants();   // (the web game's giant light volumes and plaza slabs: put away first)
        gy = 0f; foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        var R = g.WalkArea; if (R.width <= 1f || R.height <= 1f) return;
        x0 = R.xMin - 4f; z0 = R.yMin - 4f; nx = Mathf.CeilToInt((R.width + 8f) / Cell); nz = Mathf.CeilToInt((R.height + 8f) / Cell);
        if (nx * nz > 4000000) return;
        var wall = new bool[nx * nz]; top = new float[nx * nz];
        float lo = gy + BandLo, hi = gy + BandHi;
        var area = new Bounds(new Vector3(R.center.x, gy, R.center.y), new Vector3(R.width + 8f, 400f, R.height + 8f));
        var verts = new List<Vector3>(); var tris = new List<int>(); var big = new List<KeyValuePair<int, string>>();
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!mr.enabled || !mr.gameObject.activeInHierarchy || mr.gameObject.layer == 30) continue;
            var b = mr.bounds;
            if (b.max.y < lo + 0.2f || b.min.y > hi || !b.Intersects(area)) continue;
            if (Mathf.Max(b.size.x, b.size.z) < 0.4f) continue;
            if (Skip(mr.transform)) continue;
            var mf = mr.GetComponent<MeshFilter>(); var m = mf != null ? mf.sharedMesh : null; if (m == null) continue;
            if (!m.isReadable)
            {
                // a mesh the game cannot read (some imported models): its outline, an oval a little inside its box
                Unread++;
                if (b.size.y < 0.8f || Mathf.Max(b.size.x, b.size.z) > 30f) continue;
                markN = 0; Oval(b, wall); if (markN > 0) Things++;
                continue;
            }
            var M = mr.transform.localToWorldMatrix;
            m.GetVertices(verts);
            var wv = new Vector3[verts.Count]; for (int i = 0; i < verts.Count; i++) wv[i] = M.MultiplyPoint3x4(verts[i]);
            bool hit = false; markN = 0;
            for (int s = 0; s < m.subMeshCount; s++)
            {
                if (m.GetTopology(s) != MeshTopology.Triangles) continue;
                m.GetTriangles(tris, s);
                for (int i = 0; i + 2 < tris.Count; i += 3) if (Tri(wv[tris[i]], wv[tris[i + 1]], wv[tris[i + 2]], lo, hi, wall)) hit = true;
            }
            if (hit) { Things++; if (markN > 400) big.Add(new KeyValuePair<int, string>(markN, Path(mr.transform) + " " + b.size.ToString("F1") + " y" + b.min.y.ToString("F1") + "-" + b.max.y.ToString("F1"))); }
        }
        big.Sort((u, v) => v.Key.CompareTo(u.Key));
        if (Application.isEditor)
        {
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < Mathf.Min(25, big.Count); k++) sb.AppendLine(big[k].Key + " cells " + big[k].Value);
            try { var dir = System.IO.Path.Combine(Application.dataPath, "../HeroShots/solid"); System.IO.Directory.CreateDirectory(dir); System.IO.File.WriteAllText(System.IO.Path.Combine(dir, AHGame.AreaId + "_big.txt"), sb.ToString()); } catch { }
        }
        // the inside of what those faces close in: not reached from anywhere you are meant to go
        var seen = new bool[nx * nz]; var q = new Queue<int>();
        System.Action<Vector3> seed = p =>
        {
            int x = Mathf.FloorToInt((p.x - x0) / Cell), z = Mathf.FloorToInt((p.z - z0) / Cell);
            // the nearest cell that is not a face (a trader stands right at his counter)
            for (int rr = 0; rr <= 6; rr++)
                for (int dz = -rr; dz <= rr; dz++)
                    for (int dx = -rr; dx <= rr; dx++)
                    {
                        int X = x + dx, Z = z + dz; if (X < 0 || Z < 0 || X >= nx || Z >= nz) continue;
                        int i = Z * nx + X; if (wall[i]) continue;
                        if (!seen[i]) { seen[i] = true; q.Enqueue(i); }
                        return;
                    }
        };
        // the whole rim of the grid lies outside the land: the open country carries on round everything
        for (int x = 0; x < nx; x++) { seed(new Vector3(x0 + (x + 0.5f) * Cell, 0, z0 + 0.25f)); seed(new Vector3(x0 + (x + 0.5f) * Cell, 0, z0 + (nz - 0.5f) * Cell)); }
        for (int z = 0; z < nz; z++) { seed(new Vector3(x0 + 0.25f, 0, z0 + (z + 0.5f) * Cell)); seed(new Vector3(x0 + (nx - 0.5f) * Cell, 0, z0 + (z + 0.5f) * Cell)); }
        if (g.player != null) seed(g.player.transform.position);
        if (g.data != null)
        {
            seed(g.W(g.data.spawn.x, g.data.spawn.z));
            if (g.data.exits != null) foreach (var e in g.data.exits) seed(e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f));
        }
        foreach (var s in AHGather.Spots) seed(s.pos);
        foreach (var n in AHNpc.All) if (n != null) seed(n.transform.position);
        while (q.Count > 0)
        {
            int i = q.Dequeue(); int x = i % nx, z = i / nx;
            if (x > 0) Step(i - 1, wall, seen, q);
            if (x < nx - 1) Step(i + 1, wall, seen, q);
            if (z > 0) Step(i - nx, wall, seen, q);
            if (z < nz - 1) Step(i + nx, wall, seen, q);
        }
        solid = new bool[nx * nz];
        for (int i = 0; i < solid.Length; i++)
        {
            solid[i] = wall[i] || !seen[i];
            if (solid[i]) Cells++;
        }
        // an enclosed inside takes the height of the walls round it (for the camera)
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < solid.Length; i++)
            {
                if (!solid[i] || wall[i]) continue;
                int x = i % nx, z = i / nx; float h = top[i];
                if (x > 0) h = Mathf.Max(h, top[i - 1]); if (x < nx - 1) h = Mathf.Max(h, top[i + 1]);
                if (z > 0) h = Mathf.Max(h, top[i - nx]); if (z < nz - 1) h = Mathf.Max(h, top[i + nx]);
                top[i] = h;
            }
        // for each solid cell, the open cell nearest it (to be put back out onto)
        nearest = new int[nx * nz]; for (int i = 0; i < nearest.Length; i++) nearest[i] = solid[i] ? -1 : i;
        var bq = new Queue<int>(); for (int i = 0; i < nearest.Length; i++) if (!solid[i]) bq.Enqueue(i);
        while (bq.Count > 0)
        {
            int i = bq.Dequeue(); int x = i % nx, z = i / nx;
            if (x > 0 && nearest[i - 1] < 0) { nearest[i - 1] = nearest[i]; bq.Enqueue(i - 1); }
            if (x < nx - 1 && nearest[i + 1] < 0) { nearest[i + 1] = nearest[i]; bq.Enqueue(i + 1); }
            if (z > 0 && nearest[i - nx] < 0) { nearest[i - nx] = nearest[i]; bq.Enqueue(i - nx); }
            if (z < nz - 1 && nearest[i + nx] < 0) { nearest[i + nx] = nearest[i]; bq.Enqueue(i + nx); }
        }
        On = true;
        Debug.Log("Ashen Hollow: solid ground map " + nx + "x" + nz + ": " + Things + " things, " + (Cells * Cell * Cell).ToString("F0") + " m² you cannot walk into" + (Unread > 0 ? ", " + Unread + " meshes unreadable" : "") + " (" + ((Time.realtimeSinceStartup - t0) * 1000f).ToString("F0") + " ms)");
    }

    static int markN;
    static string Path(Transform t) { string s = t.name; for (var a = t.parent; a != null && s.Length < 140; a = a.parent) s = a.name + "/" + s; return s; }
    static void Oval(Bounds b, bool[] wall)
    {
        float rx = b.extents.x * 0.82f, rz = b.extents.z * 0.82f;
        int cx0 = Mathf.Max(0, Mathf.FloorToInt((b.min.x - x0) / Cell)), cx1 = Mathf.Min(nx - 1, Mathf.FloorToInt((b.max.x - x0) / Cell));
        int cz0 = Mathf.Max(0, Mathf.FloorToInt((b.min.z - z0) / Cell)), cz1 = Mathf.Min(nz - 1, Mathf.FloorToInt((b.max.z - z0) / Cell));
        for (int z = cz0; z <= cz1; z++)
            for (int x = cx0; x <= cx1; x++)
            {
                float u = (x0 + (x + 0.5f) * Cell - b.center.x) / Mathf.Max(0.1f, rx), v = (z0 + (z + 0.5f) * Cell - b.center.z) / Mathf.Max(0.1f, rz);
                if (u * u + v * v > 1f) continue;
                int i = z * nx + x; if (!wall[i]) markN++; wall[i] = true; top[i] = Mathf.Max(top[i], b.max.y);
            }
    }
    static void Step(int j, bool[] wall, bool[] seen, Queue<int> q) { if (!seen[j] && !wall[j]) { seen[j] = true; q.Enqueue(j); } }

    // the part of a face between knee and head height, laid onto the grid (its outline and what it covers)
    static readonly List<Vector3> polyA = new List<Vector3>(8), polyB = new List<Vector3>(8);
    static bool Tri(Vector3 a, Vector3 b, Vector3 c, float lo, float hi, bool[] wall)
    {
        float ymax = Mathf.Max(a.y, Mathf.Max(b.y, c.y)), ymin = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        if (ymax < lo || ymin > hi) return false;
        polyA.Clear(); polyA.Add(a); polyA.Add(b); polyA.Add(c);
        ClipY(polyA, polyB, lo, true); ClipY(polyB, polyA, hi, false);
        if (polyA.Count == 0) return false;
        bool any = false;
        // the outline (a wall seen edge-on from above is only a line)
        for (int i = 0; i < polyA.Count; i++) any |= Line(polyA[i], polyA[(i + 1) % polyA.Count], ymax, wall);
        // what it covers (a sloping face: the side of a crag)
        float mx0 = float.MaxValue, mx1 = float.MinValue, mz0 = float.MaxValue, mz1 = float.MinValue;
        foreach (var p in polyA) { mx0 = Mathf.Min(mx0, p.x); mx1 = Mathf.Max(mx1, p.x); mz0 = Mathf.Min(mz0, p.z); mz1 = Mathf.Max(mz1, p.z); }
        int cx0 = Mathf.Max(0, Mathf.FloorToInt((mx0 - x0) / Cell)), cx1 = Mathf.Min(nx - 1, Mathf.FloorToInt((mx1 - x0) / Cell));
        int cz0 = Mathf.Max(0, Mathf.FloorToInt((mz0 - z0) / Cell)), cz1 = Mathf.Min(nz - 1, Mathf.FloorToInt((mz1 - z0) / Cell));
        if (cx1 - cx0 >= 1 && cz1 - cz0 >= 1)
            for (int z = cz0; z <= cz1; z++)
                for (int x = cx0; x <= cx1; x++)
                {
                    float px = x0 + (x + 0.5f) * Cell, pz = z0 + (z + 0.5f) * Cell;
                    if (!Inside(polyA, px, pz)) continue;
                    int i = z * nx + x; if (!wall[i]) markN++; wall[i] = true; top[i] = Mathf.Max(top[i], ymax); any = true;
                }
        return any;
    }
    static void ClipY(List<Vector3> src, List<Vector3> dst, float y, bool keepAbove)
    {
        dst.Clear(); int n = src.Count; if (n == 0) return;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = src[i], q = src[(i + 1) % n];
            bool pin = keepAbove ? p.y >= y : p.y <= y, qin = keepAbove ? q.y >= y : q.y <= y;
            if (pin) dst.Add(p);
            if (pin != qin) { float t = (y - p.y) / (q.y - p.y); dst.Add(Vector3.Lerp(p, q, t)); }
        }
    }
    static bool Inside(List<Vector3> poly, float px, float pz)
    {
        bool c = false; int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector3 a = poly[i], b = poly[j];
            if (((a.z > pz) != (b.z > pz)) && (px < (b.x - a.x) * (pz - a.z) / (b.z - a.z) + a.x)) c = !c;
        }
        return c;
    }
    static bool Line(Vector3 a, Vector3 b, float h, bool[] wall)
    {
        float len = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z));
        int steps = Mathf.Max(1, Mathf.CeilToInt(len / (Cell * 0.5f)));
        bool any = false;
        for (int k = 0; k <= steps; k++)
        {
            float t = k / (float)steps; float px = a.x + (b.x - a.x) * t, pz = a.z + (b.z - a.z) * t;
            int x = Mathf.FloorToInt((px - x0) / Cell), z = Mathf.FloorToInt((pz - z0) / Cell);
            if (x < 0 || z < 0 || x >= nx || z >= nz) continue;
            int i = z * nx + x; if (!wall[i]) markN++; wall[i] = true; top[i] = Mathf.Max(top[i], h); any = true;
        }
        return any;
    }

    static int Idx(float px, float pz)
    {
        int x = Mathf.FloorToInt((px - x0) / Cell), z = Mathf.FloorToInt((pz - z0) / Cell);
        return x < 0 || z < 0 || x >= nx || z >= nz ? -1 : z * nx + x;
    }

    // something solid within r of here
    public static bool Blocked(Vector3 p, float r)
    {
        if (!On) return false;
        int i = Idx(p.x, p.z); if (i >= 0 && solid[i]) return true;
        if (r <= 0f) return false;
        for (int k = 0; k < 8; k++)
        {
            float a = k * Mathf.PI / 4f; int j = Idx(p.x + Mathf.Cos(a) * r, p.z + Mathf.Sin(a) * r);
            if (j >= 0 && solid[j]) return true;
        }
        return false;
    }

    // out of anything solid: the hero's middle onto open ground, then their edge clear of it
    public static Vector3 Keep(Vector3 p, float r)
    {
        if (!On) return p;
        int i = Idx(p.x, p.z);
        if (i >= 0 && solid[i])
        {
            int j = nearest[i]; if (j < 0) return p;
            // back along the line to the first open cell (not its middle: no jump)
            float sx = x0 + (j % nx + 0.5f) * Cell, sz = z0 + (j / nx + 0.5f) * Cell;
            Vector3 to = new Vector3(sx - p.x, 0f, sz - p.z); float len = to.magnitude;
            if (len > 1e-4f)
            {
                Vector3 st = to / len * (Cell * 0.25f); Vector3 a = p; bool done = false;
                for (int k = 0; k < 800 && !done; k++) { a += st; int m = Idx(a.x, a.z); if (m < 0 || !solid[m]) { p = new Vector3(a.x, p.y, a.z); done = true; } }
                if (!done) p = new Vector3(sx, p.y, sz);
            }
        }
        // the edge: pushed away from solid cells it overlaps
        Vector3 push = Vector3.zero; int hits = 0;
        for (int k = 0; k < 12; k++)
        {
            float a = k * Mathf.PI / 6f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            int j = Idx(p.x + d.x * r, p.z + d.z * r);
            if (j >= 0 && solid[j])
            {
                // how far in: back off until clear
                float inside = 0f; for (float s = r; s > 0f; s -= 0.05f) { int m = Idx(p.x + d.x * s, p.z + d.z * s); if (m < 0 || !solid[m]) break; inside = r - s + 0.05f; }
                push -= d * Mathf.Max(0.05f, Mathf.Min(r, inside + 0.02f)); hits++;
            }
        }
        if (hits > 0 && hits < 10) { push /= Mathf.Max(1, hits / 2); p += push; }
        return p;
    }

    // how far the camera can go from 'from' along dir before it would be inside something
    public static float Clear(Vector3 from, Vector3 dir, float dist)
    {
        if (!On) return dist;
        for (float s = 0.6f; s <= dist; s += 0.25f)
        {
            Vector3 q = from + dir * s; int i = Idx(q.x, q.z);
            if (i >= 0 && solid[i] && q.y < top[i] + 0.3f)
            {
                // the building the hero stands in has its upper walls lifted away: the camera looks in over the rest
                var inn = AHHallFade.In; if (inn != null && inn.Contains(q, 1.2f) && q.y > 1.5f) continue;
                return Mathf.Max(0f, s - 0.4f);
            }
        }
        return dist;
    }

    // for the editor's walk-into test: solid spots with open ground 'away' metres off in some direction (from, to)
    public static List<KeyValuePair<Vector3, Vector3>> Targets(int n, float away, int seed)
    {
        var l = new List<KeyValuePair<Vector3, Vector3>>(); if (!On) return l;
        var rnd = new System.Random(seed);
        for (int tries = 0; tries < 4000 && l.Count < n; tries++)
        {
            int i = rnd.Next(solid.Length); if (!solid[i] || top[i] < 1.8f) continue;
            int x = i % nx, z = i / nx; if (x < 4 || z < 4 || x >= nx - 4 || z >= nz - 4) continue;
            Vector3 c = new Vector3(x0 + (x + 0.5f) * Cell, gy, z0 + (z + 0.5f) * Cell);
            float a = (float)(rnd.NextDouble() * Mathf.PI * 2); Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 from = c + d * away; int j = Idx(from.x, from.z); if (j < 0 || solid[j]) continue;
            // open all the way to within two metres of it
            bool clear = true; for (float s = 2f; s < away; s += 0.5f) { int k = Idx(c.x + d.x * s, c.z + d.z * s); if (k < 0 || solid[k]) { clear = false; break; } }
            if (!clear) continue;
            bool near = false; foreach (var o in l) if ((o.Value - c).magnitude < 15f) near = true; if (near) continue;
            l.Add(new KeyValuePair<Vector3, Vector3>(from, c));
        }
        return l;
    }

    // for the editor's checks: the map as a picture (red: solid)
    public static Texture2D Picture()
    {
        if (!On) return null;
        var t = new Texture2D(nx, nz, TextureFormat.RGB24, false);
        var px = new Color32[nx * nz];
        for (int i = 0; i < px.Length; i++) px[i] = solid[i] ? new Color32(220, 40, 40, 255) : new Color32(30, 30, 30, 255);
        t.SetPixels32(px); t.Apply(); return t;
    }
    public static Rect Extent { get { return new Rect(x0, z0, nx * Cell, nz * Cell); } }
}
