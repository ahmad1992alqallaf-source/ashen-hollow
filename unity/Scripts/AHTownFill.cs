// Ashen Hollow: a walled town seen from out in the wild (Sunspire Oasis from the Sunscar Wastes, Highcairn from the
// Frostfang Reach...) was a ring of wall round an empty square: its streets and houses are in the city area you enter
// by the gate. Now the inside is built up: rows of houses in the town's own style, their roofs showing over the wall,
// with a street in from every gate to a square in the middle, and room left round everything you go there for (the
// stalls, the traders, the gate buttons, the way in to the city).
using System.Collections.Generic;
using UnityEngine;

public static class AHTownFill
{
    public class Town { public Rect web; public List<Vector2> gates; public string col; }
    public static readonly List<Town> Towns = new List<Town>();
    static readonly Dictionary<string, string> Style = new Dictionary<string, string>
    {
        { "vale", "varrow" }, { "kingsroad", "varrow" }, { "meadow", "varrow" }, { "frost", "hc" }, { "mire", "mw" }, { "ember", "ch" }, { "tide", "co" }, { "sands", "ss" }, { "isle", "ss" }, { "scorchwind", "ss" },
    };
    public static int Last;

    public static void Run(AHGame g)
    {
        Last = 0;
        if (g == null || Towns.Count == 0 || AHHollow.Active) return;
        string st; Style.TryGetValue(AHGame.AreaId, out st);
        var root = new GameObject("Town houses").transform;
        float gy = g.W(0f, 0f).y;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        foreach (var t in Towns)
        {
            Vector3 a = g.W(t.web.xMin, t.web.yMin), b = g.W(t.web.xMax, t.web.yMax);
            var R = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
            if (R.width < 16f || R.height < 16f) continue;
            Vector2 C = R.center;
            // what must stay open: the gates (and a street from each in to the square), the square, and everything you use
            var keep = new List<Vector2>(); var streets = new List<Vector2>();
            foreach (var gp in t.gates) { var w = g.W(gp.x, gp.y); streets.Add(new Vector2(w.x, w.z)); }
            foreach (var s in AHGather.Spots) if (R.Contains(new Vector2(s.pos.x, s.pos.z))) keep.Add(new Vector2(s.pos.x, s.pos.z));
            foreach (var n in AHNpc.All) if (n != null && R.Contains(new Vector2(n.transform.position.x, n.transform.position.z))) keep.Add(new Vector2(n.transform.position.x, n.transform.position.z));
            if (g.data != null)
            {
                var sp = g.W(g.data.spawn.x, g.data.spawn.z); keep.Add(new Vector2(sp.x, sp.z));
                if (g.data.exits != null) foreach (var e in g.data.exits) { var w = e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f); keep.Add(new Vector2(w.x, w.z)); }
            }
            // the square: round the middle of what is kept inside (the stalls), or the town's middle
            Vector2 sq = C; int kn = 0; Vector2 ks = Vector2.zero;
            foreach (var k in keep) if (R.Contains(k)) { ks += k; kn++; }
            if (kn > 0) sq = Vector2.Lerp(C, ks / kn, 0.6f);
            float sqR = Mathf.Clamp(Mathf.Min(R.width, R.height) * 0.16f, 7f, 14f);
            const float lot = 9f, inset = 5f;
            int nx = Mathf.FloorToInt((R.width - inset * 2f) / lot), nz = Mathf.FloorToInt((R.height - inset * 2f) / lot);
            float ox = R.xMin + (R.width - nx * lot) / 2f, oz = R.yMin + (R.height - nz * lot) / 2f;
            var rnd = new System.Random(AHGame.AreaId.GetHashCode() ^ (int)(C.x * 13 + C.y * 7));
            int built = 0;
            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                {
                    Vector2 c = new Vector2(ox + (ix + 0.5f) * lot, oz + (iz + 0.5f) * lot);
                    if ((c - sq).magnitude < sqR + lot * 0.45f) continue;
                    bool clear = true;
                    foreach (var k in keep) if ((k - c).magnitude < lot * 0.5f + 3.2f) { clear = false; break; }
                    if (!clear) continue;
                    // the streets: a lane four metres either side of the line from each gate to the square
                    foreach (var s in streets) if (SegDist(c, s, sq) < lot * 0.5f + 3.5f) { clear = false; break; }
                    if (!clear) continue;
                    Vector3 p = new Vector3(c.x, gy, c.y);
                    if (g.Blocked(p, 3.4f)) continue;
                    // face the nearest open way: the square or a street (square to the grid)
                    Vector2 to = sq - c; float best = to.magnitude;
                    foreach (var s in streets) { Vector2 q = Closest(c, s, sq); if ((q - c).magnitude < best) { best = (q - c).magnitude; to = q - c; } }
                    Vector3 face = Mathf.Abs(to.x) > Mathf.Abs(to.y) ? new Vector3(Mathf.Sign(to.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(to.y));
                    GameObject pf = null;
                    if (st != null)
                    {
                        int wi = rnd.Next(3) == 0 ? 2 : 1, fl = rnd.Next(10) < 2 ? 3 : rnd.Next(10) < 6 ? 2 : 1, v = rnd.Next(2);
                        pf = Resources.Load<GameObject>("AH/Models/Town/" + st + "_" + wi + "_" + fl + "_" + v);
                    }
                    if (pf == null) pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_" + (rnd.Next(2) == 0 ? "home_A_" : "home_B_") + t.col);
                    if (pf == null) continue;
                    var go = Object.Instantiate(pf, root, false); go.name = "Town house";
                    foreach (var cl in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
                    go.transform.rotation = Quaternion.LookRotation(face, Vector3.up); go.transform.localScale = Vector3.one; go.transform.position = Vector3.zero;
                    var ms = Local(go);
                    float tx = (lot - 1.3f) / Mathf.Max(0.1f, ms.x), tz = (lot - 1.6f) / Mathf.Max(0.1f, ms.z);
                    go.transform.localScale = new Vector3(tx, Mathf.Clamp((tx + tz) * 0.5f, 0.85f, 1.15f), tz);
                    var mb = Measure(go);
                    go.transform.position = new Vector3(p.x - (mb.center.x - go.transform.position.x), gy - (mb.min.y - go.transform.position.y) - 0.02f, p.z - (mb.center.z - go.transform.position.z));
                    AHModel.SetShadows(go);
                    g.AddBlockBox(Measure(go), 0.3f);
                    built++;
                }
            Last += built;
        }
        if (Last == 0) Object.Destroy(root.gameObject);
        else Debug.Log("Ashen Hollow: " + Last + " houses built inside the town walls");
    }

    static Vector2 Closest(Vector2 p, Vector2 a, Vector2 b) { Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude)); return a + ab * t; }
    static float SegDist(Vector2 p, Vector2 a, Vector2 b) { return (Closest(p, a, b) - p).magnitude; }
    static Bounds Measure(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(true); Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b; }
    // the size in the building's own axes (x across its front, z front to back)
    static Vector3 Local(GameObject go)
    {
        var q = go.transform.rotation; go.transform.rotation = Quaternion.identity;
        var b = Measure(go); go.transform.rotation = q; return b.size;
    }
}
