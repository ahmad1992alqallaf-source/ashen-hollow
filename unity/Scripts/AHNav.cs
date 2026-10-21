// Ashen Hollow: finding the way, for AUTO (and anything else that walks by itself). The land is laid out as a grid of
// 1.2 m cells, made once per land the first time a way is asked for:
//  - roads, town paving and bridges cost 1 to cross; open grass and fields cost 4; trees, rocks, houses, walls, water
//    and the edge of the map can't be crossed at all;
//  - the shortest-cost way (A*) therefore runs along the roads whenever there is one, steps off them only for the last
//    stretch to the goal, and goes round anything solid instead of walking into it;
//  - the way is then straightened (corners cut where the straight line stays as cheap and clear), so the hero walks
//    it smoothly.
using System.Collections.Generic;
using UnityEngine;

public static class AHNav
{
    const float Cell = 1.2f;
    const byte Blocked = 0, Road = 1, Open = 4;
    static string area; static float x0, z0; static int nx, nz; static byte[] cost;
    static Color32[] px; static int pw, ph; static AHGroundMap gm;

    public static void Forget() { area = null; cost = null; px = null; groundArea = null; }

    static void Build(AHGame g)
    {
        area = AHGame.AreaId; cost = null;
        if (g.data == null || g.data.bounds == null) return;
        Vector3 a = g.W(g.data.bounds.x0, g.data.bounds.z0), b = g.W(g.data.bounds.x1, g.data.bounds.z1);
        x0 = Mathf.Min(a.x, b.x); z0 = Mathf.Min(a.z, b.z);
        nx = Mathf.CeilToInt(Mathf.Abs(b.x - a.x) / Cell) + 1; nz = Mathf.CeilToInt(Mathf.Abs(b.z - a.z) / Cell) + 1;
        if (nx * nz > 400000) { float k = Mathf.Sqrt(nx * nz / 400000f); nx = Mathf.CeilToInt(nx / k); nz = Mathf.CeilToInt(nz / k); }
        LoadGround(g);
        cost = new byte[nx * nz];
        float cx = Mathf.Abs(b.x - a.x) / Mathf.Max(1, nx - 1), cz = Mathf.Abs(b.z - a.z) / Mathf.Max(1, nz - 1);
        sx = cx; sz = cz;
        Vector3 c = Vector3.zero;
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                c.x = x0 + x * cx; c.z = z0 + z * cz;
                cost[z * nx + x] = g.Blocked(c, 0.4f) ? Blocked : IsRoad(g, c) ? Road : Open;
            }
    }
    static float sx = Cell, sz = Cell;
    static string groundArea;
    // the ground's picture: its colours say where the roads are
    static void LoadGround(AHGame g)
    {
        groundArea = AHGame.AreaId; px = null; gm = g.data != null ? g.data.groundMap : null;
        var png = Resources.Load<TextAsset>(AHGame.AreaPath + AHGame.AreaId + "_ground");
        if (png == null || gm == null || gm.w <= 0f) return;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (tex.LoadImage(png.bytes)) { px = tex.GetPixels32(); pw = tex.width; ph = tex.height; }
        Object.Destroy(tex);
    }

    // a road, town paving or a bridge under this point (by the ground's colour)
    public static bool IsRoad(AHGame g, Vector3 p)
    {
        if (AHScenery.DeckLift(p) > 0.05f) return true;   // on a bridge
        if (groundArea != AHGame.AreaId) LoadGround(g);
        if (px == null) return false;
        Vector2 w = g.ToWeb(p);
        float u = (w.x - gm.x0) / gm.w, v = (w.y - gm.z0) / gm.h;
        if (u < 0f || v < 0f || u >= 1f || v >= 1f) return false;
        var col = px[(ph - 1 - Mathf.Min(ph - 1, (int)(v * ph))) * pw + Mathf.Min(pw - 1, (int)(u * pw))];
        int r = col.r, gg = col.g, bb = col.b;
        bool dirt = r > gg + 8 && gg > bb + 8 && r >= 100 && r <= 200 && r - bb >= 35;        // brown road
        bool stone = Mathf.Abs(r - gg) < 16 && Mathf.Abs(gg - bb) < 22 && r >= 105 && r <= 190; // grey paving
        return dirt || stone;
    }

    static int Idx(Vector3 p) { int x = Mathf.RoundToInt((p.x - x0) / sx), z = Mathf.RoundToInt((p.z - z0) / sz); return x < 0 || z < 0 || x >= nx || z >= nz ? -1 : z * nx + x; }
    static Vector3 Pos(int i, float y) { return new Vector3(x0 + (i % nx) * sx, y, z0 + (i / nx) * sz); }

    // the nearest cell that can be walked, within r cells
    static int Open_(int i, int r)
    {
        if (i < 0) return -1; if (cost[i] != Blocked) return i;
        int ix = i % nx, iz = i / nx, best = -1; float bd = float.MaxValue;
        for (int dz = -r; dz <= r; dz++) for (int dx = -r; dx <= r; dx++)
            {
                int x = ix + dx, z = iz + dz; if (x < 0 || z < 0 || x >= nx || z >= nz) continue;
                int j = z * nx + x; if (cost[j] == Blocked) continue;
                float d = dx * dx + dz * dz; if (d < bd) { bd = d; best = j; }
            }
        return best;
    }

    // the way from 'from' to 'to' as points to walk through (the last one is 'to'), or null when there is none
    static float[] gScore; static int[] came; static bool[] closed; static readonly List<int> heap = new List<int>(); static float[] fScore;
    public static List<Vector3> Path(AHGame g, Vector3 from, Vector3 to)
    {
        if (area != AHGame.AreaId || cost == null) Build(g);
        if (cost == null) return null;
        int s = Open_(Idx(from), 3), t = Open_(Idx(to), 6);
        if (s < 0 || t < 0) return null;
        int n = nx * nz;
        if (gScore == null || gScore.Length < n) { gScore = new float[n]; came = new int[n]; closed = new bool[n]; fScore = new float[n]; }
        for (int i = 0; i < n; i++) { gScore[i] = float.MaxValue; closed[i] = false; came[i] = -1; }
        heap.Clear(); gScore[s] = 0f; fScore[s] = H(s, t); Push(s);
        int tx = t % nx, tz = t / nx, steps = 0;
        while (heap.Count > 0)
        {
            int cur = Pop(); if (closed[cur]) continue; closed[cur] = true;
            if (cur == t) break;
            if (++steps > 200000) return null;
            int cx = cur % nx, cz = cur / nx;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int x = cx + dx, z = cz + dz; if (x < 0 || z < 0 || x >= nx || z >= nz) continue;
                    int j = z * nx + x; byte c = cost[j]; if (c == Blocked || closed[j]) continue;
                    if (dx != 0 && dz != 0 && (cost[cz * nx + x] == Blocked || cost[z * nx + cx] == Blocked)) continue;   // no squeezing past corners
                    float step = (dx != 0 && dz != 0 ? 1.414f : 1f) * (c + cost[cur]) * 0.5f;
                    float ng = gScore[cur] + step;
                    if (ng < gScore[j]) { gScore[j] = ng; came[j] = cur; fScore[j] = ng + H(j, t); Push(j); }
                }
        }
        if (came[t] < 0 && t != s) return null;
        // back from the goal, then straighten
        var cells = new List<int>(); for (int c = t; c >= 0; c = came[c]) { cells.Add(c); if (c == s) break; }
        cells.Reverse();
        var pts = new List<Vector3>();
        int a = 0;
        while (a < cells.Count - 1)
        {
            int b = cells.Count - 1;
            while (b > a + 1 && !Straight(cells[a], cells[b])) b--;
            pts.Add(Pos(cells[b], from.y)); a = b;
        }
        if (pts.Count == 0 || (pts[pts.Count - 1] - to).sqrMagnitude > 0.01f) pts.Add(to);
        else pts[pts.Count - 1] = to;
        return pts;
    }
    static float H(int a, int b) { float dx = (a % nx - b % nx) * sx, dz = (a / nx - b / nx) * sz; return Mathf.Sqrt(dx * dx + dz * dz) / Cell; }
    // a straight line from cell a to cell b stays clear and no dearer than its ends (so it doesn't cut across grass)
    static bool Straight(int a, int b)
    {
        int ax = a % nx, az = a / nx, bx = b % nx, bz = b / nx; byte lim = (byte)Mathf.Max(cost[a], cost[b]);
        int steps = Mathf.Max(Mathf.Abs(bx - ax), Mathf.Abs(bz - az)) * 2;
        for (int i = 1; i < steps; i++)
        {
            float f = i / (float)steps; int x = Mathf.RoundToInt(Mathf.Lerp(ax, bx, f)), z = Mathf.RoundToInt(Mathf.Lerp(az, bz, f));
            byte c = cost[z * nx + x]; if (c == Blocked || c > lim) return false;
        }
        return true;
    }
    static void Push(int i)
    {
        heap.Add(i); int k = heap.Count - 1;
        while (k > 0) { int p = (k - 1) / 2; if (fScore[heap[p]] <= fScore[heap[k]]) break; int tmp = heap[p]; heap[p] = heap[k]; heap[k] = tmp; k = p; }
    }
    static int Pop()
    {
        int top = heap[0], last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
        if (heap.Count > 0)
        {
            heap[0] = last; int k = 0;
            while (true)
            {
                int l = k * 2 + 1, r = l + 1, m = k;
                if (l < heap.Count && fScore[heap[l]] < fScore[heap[m]]) m = l;
                if (r < heap.Count && fScore[heap[r]] < fScore[heap[m]]) m = r;
                if (m == k) break; int tmp = heap[m]; heap[m] = heap[k]; heap[k] = tmp; k = m;
            }
        }
        return top;
    }
}
