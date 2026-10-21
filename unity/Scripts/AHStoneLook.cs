// Ashen Hollow: stone that looks like stone. The ruins, plinths, fallen columns and stone blocks along the roads were
// plain flat grey (at night, under the blue moonlight, they read as bright blue boxes on a phone), and some of the
// old fallen columns were only drawn from one side, so you could see through them. Every plain, untextured grey
// piece of a land gets a weathered stone picture (made here in code: mottled grain, darker cracks and pits) and is
// drawn from both sides. Wood, cloth, metal and anything with its own picture are left alone.
using System.Collections.Generic;
using UnityEngine;

public static class AHStoneLook
{
    static Texture2D tex;
    static readonly Dictionary<Material, Material> done = new Dictionary<Material, Material>();
    static Shader lit;

    public static Texture2D Tex
    {
        get
        {
            if (tex != null) return tex;
            const int N = 256; tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "AH stone", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color32[N * N]; var rnd = new System.Random(7);
            // a few dark cracks wandering across (tiled)
            var crack = new float[N * N];
            for (int c = 0; c < 9; c++)
            {
                float x = (float)rnd.NextDouble() * N, y = (float)rnd.NextDouble() * N, a = (float)rnd.NextDouble() * 6.283f;
                int len = 40 + rnd.Next(90);
                for (int s = 0; s < len; s++)
                {
                    a += ((float)rnd.NextDouble() - 0.5f) * 0.7f; x += Mathf.Cos(a); y += Mathf.Sin(a);
                    int ix = ((int)x % N + N) % N, iy = ((int)y % N + N) % N; crack[iy * N + ix] = 1f;
                    crack[iy * N + (ix + 1) % N] = Mathf.Max(crack[iy * N + (ix + 1) % N], 0.4f);
                }
            }
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N, n = 0f, amp = 0.5f, f = 4f;
                    for (int o = 0; o < 5; o++) { n += amp * (Tile(u * f, v * f, f) - 0.5f); amp *= 0.5f; f *= 2f; }
                    float speck = rnd.NextDouble() < 0.02 ? -0.12f : rnd.NextDouble() < 0.02 ? 0.08f : 0f;
                    float k = 0.86f + n * 0.42f + speck - crack[y * N + x] * 0.32f;
                    byte b = (byte)Mathf.Clamp(k * 255f, 0, 255), g2 = (byte)Mathf.Clamp(k * 252f, 0, 255);
                    px[y * N + x] = new Color32(b, g2, (byte)Mathf.Clamp(k * 246f, 0, 255), 255);
                }
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
    }
    // tileable value noise at a given period
    static float Tile(float x, float y, float p)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi; int P = Mathf.Max(1, Mathf.RoundToInt(p));
        float a = H(xi % P, yi % P), b = H((xi + 1) % P, yi % P), c = H(xi % P, (yi + 1) % P), d = H((xi + 1) % P, (yi + 1) % P);
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }
    static float H(int x, int y) { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return (h & 0xffff) / 65535f; }

    public static void Apply(AHGame g)
    {
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit"); if (lit == null) return;
        int n = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || r.name.StartsWith("AH_") || Skip(r.transform)) continue;
            var b = r.bounds; float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (big < 0.25f || big > 9f) continue;
            var ms = r.sharedMaterials; bool ch = false;
            for (int i = 0; i < ms.Length; i++)
            {
                var m = ms[i]; if (m == null) continue;
                Material nm;
                if (!done.TryGetValue(m, out nm)) { nm = Stone(m); done[m] = nm; }
                if (nm != null && nm != m) { ms[i] = nm; ch = true; }
            }
            if (ch) { r.sharedMaterials = ms; n++; }
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " plain grey stone pieces given a stone picture");
    }
    static bool Skip(Transform t)
    {
        for (; t != null; t = t.parent)
        {
            string s = t.name;
            if (s == "Hero" || s == "Pet" || s == "Mount" || s == "HUD" || s == "ItemStudio" || s.StartsWith("Pass signpost") || s == "Bridges" || s == "Stations" || s.StartsWith("Ore")) return true;
            if (t.GetComponent<AHMob>() != null || t.GetComponent<AHNpc>() != null) return true;
        }
        return false;
    }
    // a plain grey material becomes stone (null: leave it)
    static Material Stone(Material m)
    {
        if (m.shader == null) return null;
        foreach (var tn in m.GetTexturePropertyNames()) { var t = m.GetTexture(tn); if (t != null && !tn.ToLowerInvariant().Contains("noise")) return null; }   // has its own picture
        Color c = Color.white; bool got = false;
        foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) { c = m.GetColor(pr); got = true; break; }
        if (!got) return null;
        if (m.HasProperty("_Metallic") && m.GetFloat("_Metallic") > 0.4f) return null;
        if (m.IsKeywordEnabled("_EMISSION")) return null;
        Color gm = c.gamma; float mx = gm.maxColorComponent, mn = Mathf.Min(gm.r, Mathf.Min(gm.g, gm.b));
        if (mx - mn > 0.16f || mx < 0.22f || mx > 0.92f) return null;   // only plain greys (not wood, cloth, snow or soot)
        var n = new Material(lit) { name = m.name + " (stone)" };
        n.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1f));
        n.SetTexture("_BaseMap", Tex); n.SetTextureScale("_BaseMap", new Vector2(1.5f, 1.5f));
        n.SetFloat("_Smoothness", 0.12f); n.SetFloat("_Metallic", 0f);
        n.SetFloat("_Cull", 0f); n.doubleSidedGI = true;   // drawn from both sides: no seeing through a column turned the wrong way
        n.enableInstancing = true;
        return n;
    }
}
