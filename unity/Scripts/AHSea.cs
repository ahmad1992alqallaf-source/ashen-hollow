// Ashen Hollow: the sea painted on the ground. Several maps (Tidewake Isles, Dragonscale Isle, Coralport, the Duskmire
// coast) have their sea drawn into the ground picture rather than made as water, so you could walk across it. When an
// area loads, the ground picture is read into a grid (half a metre a cell): the painted sea is found by its colour, and
//  - you can wade a little way in from the shore, but no further (bridges and piers stay walkable): Resolve keeps
//    everything, the hero and the monsters, out of the deep water
//  - real water is laid over it: one sheet with the water shader's "painted sea" mode, its shape and the depth from
//    the shore given by a mask, so the coast stays smooth and foams where it meets the land
// Round ponds are left alone (they are water already and have their own banks).
using System.Collections.Generic;
using UnityEngine;

public static class AHSea
{
    public static bool On { get; private set; }
    const float Cell = 0.5f;          // metres a cell
    public static float WadeM = 1.3f;   // how far in from the shore you can still walk
    static int nx, nz; static float x0, z0;
    static bool[] deep;                // too deep to walk
    static bool[] wet;                 // sea at all (shallows too)
    static int[] nearest;              // for a deep cell: the walkable cell nearest it (index)
    static GameObject sheet;
    public static Texture2D DebugMask, DebugGround;   // (for the editor's edge tour)

    public static void Clear() { On = false; deep = null; wet = null; nearest = null; if (sheet != null) Object.Destroy(sheet); sheet = null; }

    public static void Build(AHGame g, Transform world, Material waterMat, List<Vector4> lakes)
    {
        Clear(); if (DebugGround != null) { Object.Destroy(DebugGround); DebugGround = null; }
        if (world == null || waterMat == null) return;
        // the ground pieces and their pictures
        var grounds = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.gameObject.name.StartsWith("AH_GROUND") && r.sharedMaterial != null) grounds.Add(r);
        if (grounds.Count == 0) return;
        Bounds all = grounds[0].bounds; foreach (var r in grounds) all.Encapsulate(r.bounds);
        x0 = all.min.x; z0 = all.min.z; nx = Mathf.CeilToInt(all.size.x / Cell); nz = Mathf.CeilToInt(all.size.z / Cell);
        if (nx < 4 || nz < 4 || nx * nz > 3000000) return;
        var sea = new float[nx * nz]; int seaN = 0;
        foreach (var r in grounds)
        {
            var m = r.sharedMaterial; Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture; if (tex == null) continue;
            var b = r.bounds;
            int cx0 = Mathf.Clamp(Mathf.FloorToInt((b.min.x - x0) / Cell), 0, nx), cx1 = Mathf.Clamp(Mathf.CeilToInt((b.max.x - x0) / Cell), 0, nx);
            int cz0 = Mathf.Clamp(Mathf.FloorToInt((b.min.z - z0) / Cell), 0, nz), cz1 = Mathf.Clamp(Mathf.CeilToInt((b.max.z - z0) / Cell), 0, nz);
            int w = Mathf.Max(1, cx1 - cx0), h = Mathf.Max(1, cz1 - cz0);
            // how the picture lies on the ground: from the mesh's own corners and their texture coordinates (some lands
            // have it mirrored or turned), with the material's tiling; if that cannot be read, straight over the bounds
            Vector2 U0 = Vector2.zero, Ux = Vector2.right, Uz = Vector2.up; bool mapped = false;
            var gmf = r.GetComponent<MeshFilter>(); var gm = gmf != null ? gmf.sharedMesh : null;
            if (gm != null && gm.isReadable && gm.uv != null && gm.uv.Length == gm.vertexCount && gm.vertexCount >= 3)
            {
                var vv = gm.vertices; var uv = gm.uv; Vector4 st = m.HasProperty("_BaseMap_ST") ? m.GetVector("_BaseMap_ST") : new Vector4(1, 1, 0, 0);
                // three far-apart corners: least x+z, most x, most z
                int ia = 0, ib = 0, ic = 0; var wp = new Vector3[vv.Length];
                for (int i = 0; i < vv.Length; i++) wp[i] = r.transform.TransformPoint(vv[i]);
                for (int i = 1; i < vv.Length; i++) { if (wp[i].x + wp[i].z < wp[ia].x + wp[ia].z) ia = i; if (wp[i].x - wp[i].z > wp[ib].x - wp[ib].z) ib = i; if (wp[i].z - wp[i].x > wp[ic].z - wp[ic].x) ic = i; }
                Vector2 pa = new Vector2(wp[ia].x, wp[ia].z), pb = new Vector2(wp[ib].x, wp[ib].z), pc = new Vector2(wp[ic].x, wp[ic].z);
                Vector2 ta = Vector2.Scale(uv[ia], new Vector2(st.x, st.y)) + new Vector2(st.z, st.w), tb = Vector2.Scale(uv[ib], new Vector2(st.x, st.y)) + new Vector2(st.z, st.w), tc = Vector2.Scale(uv[ic], new Vector2(st.x, st.y)) + new Vector2(st.z, st.w);
                // uv = U0 + Ux * x + Uz * z through the three
                Vector2 e1 = pb - pa, e2 = pc - pa; float det = e1.x * e2.y - e1.y * e2.x;
                if (Mathf.Abs(det) > 1f)
                {
                    Vector2 d1 = tb - ta, d2 = tc - ta;
                    Ux = (d1 * e2.y - d2 * e1.y) / det; Uz = (d2 * e1.x - d1 * e2.x) / det;
                    U0 = ta - Ux * pa.x - Uz * pa.y; mapped = true;
                }
            }
            // (every land's world model so far lies mirrored east to west: the picture's left edge on the ground's east side)
            if (!mapped) { Ux = new Vector2(-1f / Mathf.Max(0.01f, b.size.x), 0f); Uz = new Vector2(0f, 1f / Mathf.Max(0.01f, b.size.z)); U0 = new Vector2(b.max.x / Mathf.Max(0.01f, b.size.x), -b.min.z * Uz.y); }
            // the picture read back (whatever its import settings), then looked up cell by cell
            int tw = Mathf.Clamp(tex.width, 16, 1024), th = Mathf.Clamp(tex.height, 16, 1024);
            var rt = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;   // (before the Blit, which leaves the target active)
            Graphics.Blit(tex, rt); RenderTexture.active = rt;
            var px = new Texture2D(tw, th, TextureFormat.RGBA32, false); px.ReadPixels(new Rect(0, 0, tw, th), 0, 0); px.Apply(false);
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var src = px.GetPixels32(); Object.Destroy(px);
            var cols = new Color32[w * h];
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    float wx = x0 + (cx0 + x + 0.5f) * Cell, wz = z0 + (cz0 + z + 0.5f) * Cell;
                    Vector2 t = U0 + Ux * wx + Uz * wz; t.x -= Mathf.Floor(t.x); t.y -= Mathf.Floor(t.y);
                    cols[z * w + x] = src[Mathf.Min(th - 1, (int)(t.y * th)) * tw + Mathf.Min(tw - 1, (int)(t.x * tw))];
                }
            if (Application.isEditor) { if (DebugGround != null) Object.Destroy(DebugGround); DebugGround = new Texture2D(w, h, TextureFormat.RGBA32, false); DebugGround.SetPixels32(cols); DebugGround.Apply(); }
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                {
                    var c = cols[z * w + x]; float R = c.r / 255f, G = c.g / 255f, B = c.b / 255f;
                    // painted sea: a clear, rich blue (not the pale blue-white of ice and snow, not a bog's green)
                    bool s = R < 0.45f && B > 0.27f && B - R > 0.15f && B >= G * 0.85f;
                    if (!s) continue;
                    int gx = cx0 + x, gz = cz0 + z; if (gx >= nx || gz >= nz) continue;
                    sea[gz * nx + gx] = 1f; seaN++;
                }
            if (mapped && (Ux.x < 0f || Uz.y < 0f || Mathf.Abs(Ux.y) > 1e-4f)) Debug.Log("Ashen Hollow: ground picture lies turned or mirrored (" + Ux.ToString("F4") + ", " + Uz.ToString("F4") + ")");
        }
        // where the ground has no floor at all along its edge (Emberreach's west shore: the web game's ocean plane showed
        // through a gap in the ground and you could walk across it): sea too, reached in from the edge
        if (!AHDungeon.IsDungeon(AHGame.AreaId)) seaN += Gaps(grounds, sea);
        if (seaN < 400) return;   // no sea here (a stray blue patch is not one)
        // the picture's own rim (a white frame of surf painted round its edge) is sea when the sea runs up to it
        const int Rim = 24;
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int ex = Mathf.Min(x, nx - 1 - x), ez = Mathf.Min(z, nz - 1 - z); if (Mathf.Min(ex, ez) >= Rim) continue;
                int ix = ex < Rim ? (x < nx / 2 ? Rim + 2 : nx - Rim - 3) : x, iz = ez < Rim ? (z < nz / 2 ? Rim + 2 : nz - Rim - 3) : z;
                if (sea[iz * nx + ix] > 0f) sea[z * nx + x] = 1f;
            }
        // round ponds are their own water
        if (lakes != null)
            for (int i = 0; i < sea.Length; i++)
            {
                if (sea[i] <= 0f) continue;
                float wx = x0 + (i % nx + 0.5f) * Cell, wz = z0 + (i / nx + 0.5f) * Cell;
                foreach (var l in lakes) { float u = (wx - l.x) / (l.z * 1.6f), v = (wz - l.y) / (l.w * 1.6f); if (u * u + v * v < 1f) { sea[i] = 0f; break; } }
            }
        // specks of blue on land (a flower bed, a banner) are not sea: a cell needs sea all round it
        var clean = new float[sea.Length];
        for (int z = 1; z < nz - 1; z++)
            for (int x = 1; x < nx - 1; x++)
            {
                int i = z * nx + x; if (sea[i] <= 0f) continue;
                int n = 0; for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) if (sea[i + dz * nx + dx] > 0f) n++;
                if (n >= 6) clean[i] = 1f;
            }
        // (the outermost cells keep what they were: the cleaning cannot look past the edge)
        for (int x = 0; x < nx; x++) { clean[x] = sea[x]; clean[(nz - 1) * nx + x] = sea[(nz - 1) * nx + x]; }
        for (int z = 0; z < nz; z++) { clean[z * nx] = sea[z * nx]; clean[z * nx + nx - 1] = sea[z * nx + nx - 1]; }
        sea = clean;
        // distance from the land, cell by cell (8-way)
        var dist = new float[sea.Length]; var q = new Queue<int>();
        for (int i = 0; i < sea.Length; i++) { if (sea[i] > 0f) dist[i] = float.MaxValue; else { dist[i] = 0f; q.Enqueue(i); } }
        nearest = new int[sea.Length]; for (int i = 0; i < nearest.Length; i++) nearest[i] = i;
        // (the walkable cell nearest each sea cell, carried along the search)
        while (q.Count > 0)
        {
            int i = q.Dequeue(); int x = i % nx, z = i / nx;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue; int X = x + dx, Z = z + dz; if (X < 0 || Z < 0 || X >= nx || Z >= nz) continue;
                    int j = Z * nx + X; float nd = dist[i] + ((dx != 0 && dz != 0) ? 1.414f : 1f);
                    if (nd < dist[j]) { dist[j] = nd; nearest[j] = nearest[i]; q.Enqueue(j); }
                }
        }
        // too deep: past the wading band, unless a bridge or pier stands there
        deep = new bool[sea.Length]; wet = new bool[sea.Length];
        for (int i = 0; i < sea.Length; i++) wet[i] = sea[i] > 0f;
        var bridges = AHScenery.BridgeBoxes;
        for (int i = 0; i < sea.Length; i++)
        {
            if (sea[i] <= 0f || dist[i] * Cell <= WadeM) continue;
            float wx = x0 + (i % nx + 0.5f) * Cell, wz = z0 + (i / nx + 0.5f) * Cell; bool onBridge = false;
            if (g.TileLand(new Vector3(wx, 0f, wz))) continue;   // the area's own tile map has land here (a pier, a square)
            foreach (var B in bridges) if (wx > B.min.x - 0.8f && wx < B.max.x + 0.8f && wz > B.min.z - 0.8f && wz < B.max.z + 0.8f) { onBridge = true; break; }
            deep[i] = !onBridge;
        }
        // the walkable cell a deep cell gives onto must not itself be deep (bridges): take the shore cell
        for (int i = 0; i < deep.Length; i++) if (deep[i] && deep[nearest[i]]) nearest[i] = i;
        // the water sheet: its shape (red, softened a cell) and the depth from the shore (green, 6 m to full depth)
        var mask = new Texture2D(nx, nz, TextureFormat.RGBA32, false); mask.wrapMode = TextureWrapMode.Clamp; mask.filterMode = FilterMode.Bilinear;
        var mc = new Color32[sea.Length];
        for (int i = 0; i < sea.Length; i++)
        {
            float s = sea[i]; int x = i % nx, z = i / nx;
            // a half-cell fringe so the coast blends
            if (s <= 0f) { int n = 0; for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) { int X = x + dx, Z = z + dz; if (X >= 0 && Z >= 0 && X < nx && Z < nz && sea[Z * nx + X] > 0f) n++; } s = n >= 3 ? 0.45f : 0f; }
            float d = s > 0f ? Mathf.Clamp01(dist[i] * Cell / 6f) : 0f;
            mc[i] = new Color32((byte)(s * 255), (byte)(d * 255), 0, 255);
        }
        mask.SetPixels32(mc); mask.Apply(false, false);
        DebugMask = mask;
        // the sheet, just over the painted floor
        float floor = all.max.y + 0.04f;
        sheet = new GameObject("AH_SEA"); sheet.transform.SetParent(world, true);
        var mf = sheet.AddComponent<MeshFilter>(); var mr = sheet.AddComponent<MeshRenderer>();
        // reaching far past the ground (the mask's edge carries on), so the sea runs on unbroken to the horizon over the
        // old ocean
        var mesh = new Mesh(); float w2 = nx * Cell, h2 = nz * Cell, M = 600f;
        mesh.vertices = new[] { new Vector3(x0 - M, floor, z0 - M), new Vector3(x0 + w2 + M, floor, z0 - M), new Vector3(x0 - M, floor, z0 + h2 + M), new Vector3(x0 + w2 + M, floor, z0 + h2 + M) };
        float ux = M / w2, uz = M / h2;
        mesh.uv = new[] { new Vector2(-ux, -uz), new Vector2(1 + ux, -uz), new Vector2(-ux, 1 + uz), new Vector2(1 + ux, 1 + uz) };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        var mat = new Material(waterMat); mat.SetFloat("_Mode", 2f); mat.SetTexture("_SeaMask", mask); mat.SetFloat("_Refract", 1f); mat.SetFloat("_Foam", 1.1f);
        mat.SetColor("_Color", new Color32(0x2c, 0x78, 0xa4, 255));
        // an island map (sea round most of its edge): open sea beyond it, not the edge pixels stretched to the horizon
        int edgeN = 0, edgeS = 0;
        for (int x = 0; x < nx; x++) { edgeN += 2; if (sea[x] > 0f) edgeS++; if (sea[(nz - 1) * nx + x] > 0f) edgeS++; }
        for (int z = 0; z < nz; z++) { edgeN += 2; if (sea[z * nx] > 0f) edgeS++; if (sea[z * nx + nx - 1] > 0f) edgeS++; }
        mat.SetFloat("_SeaOut", edgeS > edgeN * 0.6f ? 1f : 0f);
        // and side by side: a coast running along one side of the map carries on past it
        int sL = 0, sR = 0, sB = 0, sT = 0;
        for (int z = 0; z < nz; z++) { if (sea[z * nx] > 0f) sL++; if (sea[z * nx + nx - 1] > 0f) sR++; }
        for (int x = 0; x < nx; x++) { if (sea[x] > 0f) sB++; if (sea[(nz - 1) * nx + x] > 0f) sT++; }
        mat.SetVector("_SeaSides", new Vector4(sL > nz * 0.45f ? 1 : 0, sR > nz * 0.45f ? 1 : 0, sB > nx * 0.45f ? 1 : 0, sT > nx * 0.45f ? 1 : 0));
        mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
        On = true;
        int deepN = 0; foreach (var d0 in deep) if (d0) deepN++;
        Debug.Log("Ashen Hollow: painted sea found (" + (seaN * Cell * Cell).ToString("F0") + " m², " + (deepN * Cell * Cell).ToString("F0") + " m² too deep to walk); water laid over it");
    }

    // cells no ground triangle covers, joined to the edge of the ground's bounds: marked sea; how many
    static int Gaps(List<Renderer> grounds, float[] sea)
    {
        var cov = new bool[nx * nz]; bool any = false;
        foreach (var r in grounds)
        {
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
            var m = mf.sharedMesh; if (!m.isReadable) { Debug.Log("Ashen Hollow: ground mesh " + m.name + " not readable: gaps not checked"); return 0; }
            var v = m.vertices; var tr = m.triangles; var tf = r.transform; var w = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++) { var p = tf.TransformPoint(v[i]); w[i] = new Vector2((p.x - x0) / Cell, (p.z - z0) / Cell); }
            for (int t = 0; t + 2 < tr.Length; t += 3)
            {
                Vector2 a = w[tr[t]], b = w[tr[t + 1]], c = w[tr[t + 2]];
                float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y); if (Mathf.Abs(d) < 1e-6f) continue;
                int xa = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), xb = Mathf.Min(nx - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
                int za = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)))), zb = Mathf.Min(nz - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
                for (int z = za; z <= zb; z++)
                    for (int x = xa; x <= xb; x++)
                    {
                        float px = x + 0.5f, pz = z + 0.5f;
                        float l1 = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (pz - c.y)) / d, l2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (pz - c.y)) / d;
                        if (l1 >= -0.02f && l2 >= -0.02f && l1 + l2 <= 1.02f) { cov[z * nx + x] = true; any = true; }
                    }
            }
        }
        if (!any) return 0;
        var q = new Queue<int>(); var seen = new bool[cov.Length];
        for (int x = 0; x < nx; x++) { foreach (int i in new[] { x, (nz - 1) * nx + x }) if (!cov[i] && !seen[i]) { seen[i] = true; q.Enqueue(i); } }
        for (int z = 0; z < nz; z++) { foreach (int i in new[] { z * nx, z * nx + nx - 1 }) if (!cov[i] && !seen[i]) { seen[i] = true; q.Enqueue(i); } }
        int n = 0;
        while (q.Count > 0)
        {
            int i = q.Dequeue(); if (sea[i] <= 0f) { sea[i] = 1f; n++; }
            int x = i % nx, z = i / nx;
            if (x > 0 && !cov[i - 1] && !seen[i - 1]) { seen[i - 1] = true; q.Enqueue(i - 1); }
            if (x < nx - 1 && !cov[i + 1] && !seen[i + 1]) { seen[i + 1] = true; q.Enqueue(i + 1); }
            if (z > 0 && !cov[i - nx] && !seen[i - nx]) { seen[i - nx] = true; q.Enqueue(i - nx); }
            if (z < nz - 1 && !cov[i + nx] && !seen[i + nx]) { seen[i + nx] = true; q.Enqueue(i + nx); }
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + (n * Cell * Cell).ToString("F0") + " m² along the edge with no ground under it: sea");
        return n;
    }

    // keep a point out of the deep water: onto the nearest shore it gives onto
    public static Vector3 Keep(Vector3 p)
    {
        if (!On || deep == null) return p;
        int x = Mathf.FloorToInt((p.x - x0) / Cell), z = Mathf.FloorToInt((p.z - z0) / Cell);
        if (x < 0 || z < 0 || x >= nx || z >= nz) return p;
        int i = z * nx + x; if (!deep[i]) return p;
        int j = nearest[i]; if (j == i) return p;
        // to the edge of the shore cell nearest the water (not its middle: no jump back)
        float sx = x0 + (j % nx + 0.5f) * Cell, sz = z0 + (j / nx + 0.5f) * Cell;
        Vector3 to = new Vector3(sx - p.x, 0f, sz - p.z); float len = to.magnitude;
        if (len < 1e-4f) return p;
        // walk back along the line to the first cell that is not deep
        Vector3 step = to / len * (Cell * 0.5f); Vector3 a = p;
        for (int k = 0; k < 400; k++)
        {
            a += step; int X = Mathf.FloorToInt((a.x - x0) / Cell), Z = Mathf.FloorToInt((a.z - z0) / Cell);
            if (X < 0 || Z < 0 || X >= nx || Z >= nz || !deep[Z * nx + X]) return new Vector3(a.x, p.y, a.z);
        }
        return new Vector3(sx, p.y, sz);
    }

    public static bool Deep(Vector3 p)
    {
        if (!On || deep == null) return false;
        int x = Mathf.FloorToInt((p.x - x0) / Cell), z = Mathf.FloorToInt((p.z - z0) / Cell);
        return x >= 0 && z >= 0 && x < nx && z < nz && deep[z * nx + x];
    }

    // sea here (deep or shallow), or within r metres of it
    public static bool Sea(Vector3 p, float r = 0f)
    {
        if (!On || wet == null) return false;
        // (past the edge of the ground: as at the edge, where the open sea carries on)
        int R = Mathf.CeilToInt(r / Cell), cx = Mathf.Clamp(Mathf.FloorToInt((p.x - x0) / Cell), 0, nx - 1), cz = Mathf.Clamp(Mathf.FloorToInt((p.z - z0) / Cell), 0, nz - 1);
        for (int z = cz - R; z <= cz + R; z += Mathf.Max(1, R / 3))
            for (int x = cx - R; x <= cx + R; x += Mathf.Max(1, R / 3))
                if (x >= 0 && z >= 0 && x < nx && z < nz && wet[z * nx + x]) return true;
        return false;
    }
}
