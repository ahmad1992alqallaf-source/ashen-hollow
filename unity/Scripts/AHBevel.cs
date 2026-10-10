// Ashen Hollow: no bare boxes. Every plain cube the game builds props from (planks, posts, slabs, crates, beds,
// hedges...) gets chamfered edges, so it catches the light along its edges like something made, not a grey block.
// Box(size) is a chamfered box of that size in metres (the chamfer stays the same width however the box is stretched);
// the scenery and station builders use it in place of the plain cube, and SoftenScene() swaps any plain cube still in
// the scene (a primitive made and scaled elsewhere) for one shaped to its own size.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AHBevel
{
    static readonly Dictionary<long, Mesh> boxes = new Dictionary<long, Mesh>();

    // a chamfered box, centred, size in metres (rounded to a centimetre so similar boxes share one mesh)
    public static Mesh Box(Vector3 size, float edge = 0.035f)
    {
        int x = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.x) * 100f)), y = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.y) * 100f)), z = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(size.z) * 100f));
        long key = ((long)x << 40) | ((long)y << 20) | (long)z; Mesh m;
        if (boxes.TryGetValue(key, out m) && m != null) return m;
        Vector3 h = new Vector3(x, y, z) * 0.005f; float thin = Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 2f;
        float e = Mathf.Min(edge, thin * 0.28f);
        m = Make(Vector3.zero, h, Vector3.one * e); boxes[key] = m; return m;
    }

    // c centre, h half size, b how much is cut off each edge (per axis, so a stretched mesh can be cut to match)
    public static Mesh Make(Vector3 c, Vector3 h, Vector3 b)
    {
        var v = new List<Vector3>(); var nr = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
        System.Action<Vector3[], Vector3> poly = (ps, n) =>
        {
            if (Vector3.Dot(Vector3.Cross(ps[1] - ps[0], ps[2] - ps[0]), n) < 0f) System.Array.Reverse(ps);
            int o = v.Count; n = n.normalized;
            foreach (var p in ps) { v.Add(c + p); nr.Add(n); uv.Add(Mathf.Abs(n.y) > 0.6f ? new Vector2(p.x, p.z) : Mathf.Abs(n.x) > 0.6f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)); }
            for (int i = 1; i + 1 < ps.Length; i++) { tr.Add(o); tr.Add(o + i); tr.Add(o + i + 1); }
        };
        System.Func<int, float, Vector3> E = (a, f) => { var ev = Vector3.zero; ev[a] = f; return ev; };
        for (int a = 0; a < 3; a++)
        {
            int u = (a + 1) % 3, w = (a + 2) % 3;
            foreach (float s in new[] { -1f, 1f })
            {
                Vector3 f = E(a, s * h[a]); float hu = h[u] - b[u], hw = h[w] - b[w];
                poly(new[] { f + E(u, -hu) + E(w, -hw), f + E(u, hu) + E(w, -hw), f + E(u, hu) + E(w, hw), f + E(u, -hu) + E(w, hw) }, E(a, s));
                foreach (float t in new[] { -1f, 1f })
                {
                    Vector3 p1 = E(a, s * h[a]) + E(u, t * (h[u] - b[u])), p2 = E(a, s * (h[a] - b[a])) + E(u, t * h[u]); Vector3 dw = E(w, h[w] - b[w]);
                    poly(new[] { p1 - dw, p1 + dw, p2 + dw, p2 - dw }, E(a, s) + E(u, t));
                }
            }
        }
        foreach (float sx in new[] { -1f, 1f }) foreach (float sy in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
                    poly(new[] { new Vector3(sx * h.x, sy * (h.y - b.y), sz * (h.z - b.z)), new Vector3(sx * (h.x - b.x), sy * h.y, sz * (h.z - b.z)), new Vector3(sx * (h.x - b.x), sy * (h.y - b.y), sz * h.z) }, new Vector3(sx, sy, sz));
        var m = new Mesh { name = "Bevelled box" }; m.SetVertices(v); m.SetNormals(nr); m.SetUVs(0, uv); m.SetTriangles(tr, 0); m.RecalculateBounds(); m.RecalculateTangents();
        return m;
    }

    // a plain cube primitive (Unity's own, or any 24-corner box mesh named Cube)
    static bool IsCube(Mesh m) { return m != null && m.vertexCount == 24 && m.name.StartsWith("Cube"); }

    static readonly Dictionary<string, Mesh> fitted = new Dictionary<string, Mesh>();
    // every plain cube left in the scene gets chamfered edges of the same width in metres, whatever its scale
    public static int SoftenScene()
    {
        int n = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            var m = mf.sharedMesh; if (!IsCube(m)) continue;
            var ls = mf.transform.lossyScale; Vector3 sc = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            Vector3 w = Vector3.Scale(m.bounds.size, sc); float thin = Mathf.Min(w.x, Mathf.Min(w.y, w.z));
            if (thin < 0.012f || sc.x < 1e-4f || sc.y < 1e-4f || sc.z < 1e-4f) continue;   // a flat sheet or a decal: no edges to round
            float e = Mathf.Clamp(thin * 0.25f, 0.006f, 0.04f);
            Vector3 b = new Vector3(e / sc.x, e / sc.y, e / sc.z);
            string key = m.bounds.center.ToString("F3") + m.bounds.extents.ToString("F3") + b.ToString("F4"); Mesh nm;
            if (!fitted.TryGetValue(key, out nm) || nm == null) { nm = Make(m.bounds.center, m.bounds.extents, b); fitted[key] = nm; }
            mf.sharedMesh = nm; n++;
        }
        return n;
    }

    // once the area is built, and again a little later for what is built as it runs (stalls, camps, furniture)
    public static void Schedule(MonoBehaviour host) { if (host != null) host.StartCoroutine(Later()); }
    static IEnumerator Later()
    {
        foreach (float t in new[] { 0.5f, 4f, 15f })
        {
            yield return new WaitForSeconds(t);
            int n = SoftenScene(); if (n > 0) Debug.Log("Ashen Hollow: " + n + " plain cubes given chamfered edges");
        }
    }
}
