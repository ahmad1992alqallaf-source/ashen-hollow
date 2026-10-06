// Ashen Hollow: surface textures for worn gear, made once at start (no image files): woven cloth for robes, hoods,
// hats and capes; grained leather with stitching for boots, gloves and vests; brushed metal with fine scratches for
// helms, plates and gauntlets. Each is a light grey detail map (the piece's own colour is kept) and a matching normal
// map, so the light catches the weave, the grain and the brushing.
using UnityEngine;

public static class AHGearTex
{
    public enum Kind { Cloth, Leather, Metal }
    const int N = 256;
    static Texture2D[] baseMaps = new Texture2D[3], normals = new Texture2D[3];

    public static Texture2D Base(Kind k) { Make(k); return baseMaps[(int)k]; }
    public static Texture2D Normal(Kind k) { Make(k); return normals[(int)k]; }

    static float P(float x, float y) { return Mathf.PerlinNoise(x, y); }
    // tileable noise: blend four offset samples across the wrap
    static float T(float u, float v, float f, float ox)
    {
        float a = P(u * f + ox, v * f), b = P((u - 1f) * f + ox, v * f), c = P(u * f + ox, (v - 1f) * f), d = P((u - 1f) * f + ox, (v - 1f) * f);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    static void Make(Kind k)
    {
        int i = (int)k; if (baseMaps[i] != null) return;
        var h = new float[N * N]; var alb = new float[N * N];
        var rnd = new System.Random(7 + i);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, v = y / (float)N, hh, aa;
                if (k == Kind.Cloth)
                {
                    // a twill weave: threads over-under along a diagonal, with soft unevenness in the yarn
                    float tx = u * 48f, ty = v * 48f;
                    float warp = 0.5f + 0.5f * Mathf.Sin(tx * Mathf.PI * 2f), weft = 0.5f + 0.5f * Mathf.Sin(ty * Mathf.PI * 2f);
                    bool over = ((Mathf.FloorToInt(tx) + Mathf.FloorToInt(ty)) & 3) < 2;
                    float thread = over ? warp * 0.7f + 0.3f * weft : weft * 0.7f + 0.3f * warp;
                    float slub = T(u, v, 6f, 11f), fib = T(u, v, 40f, 3f);
                    hh = thread * 0.75f + fib * 0.25f;
                    aa = 0.9f + 0.08f * thread + 0.08f * (slub - 0.5f) + 0.04f * (fib - 0.5f);
                }
                else if (k == Kind.Leather)
                {
                    // pebbled grain with creases, a little darker in the hollows
                    float g1 = T(u, v, 24f, 5f), g2 = T(u, v, 60f, 9f), crease = Mathf.Abs(T(u, v, 5f, 21f) - 0.5f) * 2f;
                    hh = Mathf.Pow(g1, 1.6f) * 0.6f + g2 * 0.3f + Mathf.SmoothStep(0f, 0.12f, crease) * 0.1f;
                    aa = 0.86f + 0.12f * g1 + 0.06f * g2 - 0.08f * (1f - Mathf.SmoothStep(0f, 0.08f, crease));
                    // a row of stitches along one edge of every tile
                    float sv = Mathf.Abs(v - 0.06f), su = (u * 16f) % 1f;
                    if (sv < 0.012f && su > 0.2f && su < 0.75f) { hh += 0.35f; aa *= 0.86f; }
                }
                else
                {
                    // brushed metal: long fine streaks, gentle mottling, the odd bright scratch
                    float streak = T(u * 0.08f, v, 90f, 13f), mott = T(u, v, 4f, 17f);
                    hh = streak * 0.5f + mott * 0.15f;
                    aa = 0.92f + 0.08f * (streak - 0.5f) + 0.1f * (mott - 0.5f);
                }
                h[y * N + x] = hh; alb[y * N + x] = Mathf.Clamp01(aa);
            }
        if (k == Kind.Metal)
            for (int s = 0; s < 70; s++)
            {
                // scratches: short straight lines, slightly brighter and cut in
                float x0 = (float)rnd.NextDouble() * N, y0 = (float)rnd.NextDouble() * N, ang = (float)rnd.NextDouble() * Mathf.PI, len = 8f + (float)rnd.NextDouble() * 40f;
                for (float t = 0; t < len; t += 0.5f)
                {
                    int px = ((int)(x0 + Mathf.Cos(ang) * t) % N + N) % N, py = ((int)(y0 + Mathf.Sin(ang) * t) % N + N) % N;
                    h[py * N + px] -= 0.25f; alb[py * N + px] = Mathf.Min(1f, alb[py * N + px] + 0.08f);
                }
            }

        var bt = new Texture2D(N, N, TextureFormat.RGBA32, true, false) { name = "gear_" + k, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var nt = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "gear_" + k + "_n", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var bc = new Color32[N * N]; var nc = new Color32[N * N];
        float str = k == Kind.Cloth ? 2.2f : k == Kind.Leather ? 3f : 1.4f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                byte a = (byte)(alb[y * N + x] * 255f); bc[y * N + x] = new Color32(a, a, a, 255);
                float dx = h[y * N + (x + 1) % N] - h[y * N + (x + N - 1) % N], dy = h[((y + 1) % N) * N + x] - h[((y + N - 1) % N) * N + x];
                var n = new Vector3(-dx * str, -dy * str, 1f).normalized;
                nc[y * N + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
            }
        bt.SetPixels32(bc); bt.Apply(true, true); nt.SetPixels32(nc); nt.Apply(true, true);
        baseMaps[i] = bt; normals[i] = nt;
    }

    // a mesh without UVs gets them projected from its own shape (each vertex from the side it mostly faces), and
    // tangents for the normal map
    public static void AutoUV(Mesh m, float perUnit)
    {
        if (m == null) return;
        var v = m.vertices; var n = m.normals; if (n == null || n.Length != v.Length) { m.RecalculateNormals(); n = m.normals; }
        var uv = new Vector2[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z)), p = v[i] * perUnit;
            uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z) : a.x >= a.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
        }
        m.uv = uv; m.RecalculateTangents();
    }
}
