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

    // a plain block the size of a whole town square (the web game's light volumes and plaza slabs, tens of metres
    // across and metres tall) only ever shows as a wall of flat colour: put away
    public static int HideGiants()
    {
        int n = 0;
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            var m = mf.sharedMesh; if (m == null || !(IsCube(m) || m.name == "Bevelled box")) continue;
            var r = mf.GetComponent<MeshRenderer>(); if (r == null || !r.enabled) continue;
            var mat = r.sharedMaterial; if (HasTex(mat)) continue;
            var b = r.bounds; if (Mathf.Min(b.size.x, b.size.z) < 15f || Mathf.Max(b.size.x, b.size.z) < 30f || b.size.y < 3f) continue;   // a block, not a long wall
            r.enabled = false; n++;
            Debug.Log("Ashen Hollow: giant plain block put away: " + mf.name + " (" + b.size.ToString("F0") + ")");
        }
        return n;
    }

    // the caves, dungeons and the Sunken Forge were built of plain flat-coloured blocks and wedges in the web game: they
    // get a rough stone surface (a noise texture made here, tinted with each block's own colour, tiled by its size)
    static Texture2D stoneTex; static readonly Dictionary<string, Material> stoneMats = new Dictionary<string, Material>();
    static Texture2D StoneTex()
    {
        if (stoneTex != null) return stoneTex;
        const int N = 256; stoneTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "AH stone" };
        var px = new Color32[N * N]; var rnd = new System.Random(7);
        float[] g = new float[17 * 17]; for (int i = 0; i < g.Length; i++) g[i] = (float)rnd.NextDouble();
        float[] h = new float[33 * 33]; for (int i = 0; i < h.Length; i++) h[i] = (float)rnd.NextDouble();
        System.Func<float[], int, float, float, float> Val = (a, n, x, y) =>
        {
            int x0 = (int)x % n, y0 = (int)y % n, x1 = (x0 + 1) % n, y1 = (y0 + 1) % n; float fx = x - Mathf.Floor(x), fy = y - Mathf.Floor(y);
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            return Mathf.Lerp(Mathf.Lerp(a[y0 * (n + 1) + x0], a[y0 * (n + 1) + x1], fx), Mathf.Lerp(a[y1 * (n + 1) + x0], a[y1 * (n + 1) + x1], fx), fy);
        };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, v = y / (float)N;
                float n = Val(g, 16, u * 16, v * 16) * 0.55f + Val(h, 32, u * 32, v * 32) * 0.3f + (float)rnd.NextDouble() * 0.15f;
                float crack = Mathf.Abs(Val(g, 16, u * 16 + 3.7f, v * 16 + 1.3f) - 0.5f); float c = crack < 0.025f ? 0.55f : 1f;
                byte b = (byte)Mathf.Clamp(255f * (0.62f + 0.45f * n) * c, 0, 255);
                px[y * N + x] = new Color32(b, b, b, 255);
            }
        stoneTex.SetPixels32(px); stoneTex.Apply(true);
        return stoneTex;
    }
    // a material that paints a picture (any of the usual texture slots), without asking for _MainTex where there is none
    public static bool HasTex(Material m)
    {
        if (m == null) return false;
        foreach (var p in new[] { "_BaseMap", "_MainTex", "baseColorTexture" }) if (m.HasProperty(p) && m.GetTexture(p) != null) return true;
        return false;
    }
    // wood grain (long streaks with knots) and cloth weave, made here like the stone
    static Texture2D woodTex, clothTex;
    static Texture2D WoodTex()
    {
        if (woodTex != null) return woodTex;
        const int N = 256; woodTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "AH wood" };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, v = y / (float)N;
                float warp = Mathf.PerlinNoise(u * 3f, v * 0.6f) * 6f;
                float ring = Mathf.Abs(Mathf.Sin((u * 22f + warp) * Mathf.PI));
                float fine = Mathf.PerlinNoise(u * 90f, v * 4f);
                float plank = Mathf.Repeat(u * 4f, 1f) < 0.02f ? 0.55f : 1f;   // the gaps between boards
                float k = (0.7f + 0.22f * Mathf.Pow(ring, 3f) + 0.12f * fine) * plank;
                byte b = (byte)Mathf.Clamp(255f * k, 0, 255); px[y * N + x] = new Color32(b, b, b, 255);
            }
        woodTex.SetPixels32(px); woodTex.Apply(true); return woodTex;
    }
    static Texture2D ClothTex()
    {
        if (clothTex != null) return clothTex;
        const int N = 128; clothTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "AH cloth" };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                bool warp = ((x / 2) + (y / 2)) % 2 == 0; float n = Mathf.PerlinNoise(x * 0.11f, y * 0.11f);
                float k = (warp ? 0.92f : 0.8f) + 0.12f * n;
                byte b = (byte)Mathf.Clamp(255f * k, 0, 255); px[y * N + x] = new Color32(b, b, b, 255);
            }
        clothTex.SetPixels32(px); clothTex.Apply(true); return clothTex;
    }
    // a room built of plain coloured blocks (your house, the guild hall): each untextured piece gets the surface its
    // colour says it is: browns are wood, greys are stone, anything brighter is cloth
    public static int DressRoom(Transform room)
    {
        int n = 0;
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = r.sharedMaterial; if (m == null || HasTex(m) || m.name.StartsWith("AH")) continue;
            if (m.IsKeywordEnabled("_EMISSION")) continue;   // fire, candle flames, window light
            string shn = m.shader != null ? m.shader.name : "";
            if (!(shn.StartsWith("Universal Render Pipeline/Lit") || shn.Contains("glTF-pbr"))) continue;   // the ground, water, sky and effects keep their own shaders
            if (r.GetComponentInParent<AHMob>() != null) continue;
            var rmf = r.GetComponent<MeshFilter>();
            if (rmf == null || rmf.sharedMesh == null || rmf.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) continue;   // painted with vertex colours (cacti, coral): a texture would wash them out
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : Color.grey;
            if (c.a < 0.95f) continue;
            float h, sat, v; Color.RGBToHSV(c, out h, out sat, out v);
            string kind = sat < 0.18f || v < 0.25f ? "stone" : (h > 0.02f && h < 0.15f && v < 0.8f) ? "wood" : "cloth";
            var b = r.bounds; float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            float tile = kind == "cloth" ? Mathf.Clamp(Mathf.Round(big * 3f), 2f, 24f) : Mathf.Clamp(Mathf.Round(big / (kind == "wood" ? 1.2f : 1.6f)), 1f, 10f);
            string key = kind + ColorUtility.ToHtmlStringRGB(c) + "_" + tile; Material sm;
            if (!stoneMats.TryGetValue(key, out sm) || sm == null)
            {
                sm = new Material(m) { name = "AH" + kind + " " + key };
                if (!sm.HasProperty("_BaseMap")) { sm = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "AH" + kind + " " + key }; sm.SetColor("_BaseColor", c); }
                sm.SetTexture("_BaseMap", kind == "wood" ? WoodTex() : kind == "cloth" ? ClothTex() : StoneTex());
                sm.SetTextureScale("_BaseMap", new Vector2(tile, tile));
                if (kind == "stone") sm.SetColor("_BaseColor", c * 1.15f);
                stoneMats[key] = sm;
            }
            var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) if (mats[i] == m) mats[i] = sm; r.sharedMaterials = mats; n++;
        }
        return n;
    }
    public static int StoneSkin()
    {
        string a = AHGame.AreaId ?? ""; bool inside = a.StartsWith("d_") || a == "forge";
        int n = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled) continue;
            var m = r.sharedMaterial; if (m == null || m.name.StartsWith("AHStone") || HasTex(m) || m.shader.name.StartsWith("AshenHollow/")) continue;
            if (m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION")) continue;   // lava, glow
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount > 600 || mf.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) continue;
            bool web = false; for (var t = r.transform; t != null; t = t.parent) if (t.name.StartsWith("obj")) { web = true; break; }
            if (!web && !inside) continue;
            var b = r.bounds; float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)); if (big < 2.5f) continue;
            Color c = m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.grey;
            if (c.a < 0.95f) continue;   // glass, water, light shafts
            float ch, cs, cv; Color.RGBToHSV(c, out ch, out cs, out cv);
            if (!inside && cs > 0.25f) continue;   // out in the lands only the grey stone pieces (not the painted wood)
            float tile = Mathf.Clamp(Mathf.Round(big / 3f), 1f, 12f);
            string key = ColorUtility.ToHtmlStringRGB(c) + "_" + tile; Material sm;
            if (!stoneMats.TryGetValue(key, out sm) || sm == null)
            {
                sm = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "AHStone " + key };
                sm.SetTexture("_BaseMap", StoneTex()); sm.SetColor("_BaseColor", Color.Lerp(c, new Color(0.5f, 0.49f, 0.47f), 0.3f) * 1.15f); sm.SetFloat("_Smoothness", 0.12f);
                sm.SetTextureScale("_BaseMap", new Vector2(tile, tile)); sm.enableInstancing = true;
                stoneMats[key] = sm;
            }
            var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = sm; r.sharedMaterials = mats; n++;
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " plain blocks given a stone surface");
        return n;
    }

    // a rock-grain material in one colour, for code-built things with no texture layout (needs vertex colours: white)
    static readonly Dictionary<string, Material> grainMats = new Dictionary<string, Material>();
    public static Material GrainMat(string key, Color tint, float scale)
    {
        Material m; if (grainMats.TryGetValue(key, out m) && m != null) return m;
        m = AHGame.LoadMat("AH/Materials/CaveStone", "AshenHollow/CaveStone"); if (m == null) return null;
        m.SetTexture("_BaseMap", StoneTex()); m.SetColor("_Tint", tint); m.SetFloat("_Scale", scale); m.enableInstancing = true; m.name = "AHGrain " + key;
        grainMats[key] = m; return m;
    }
    // the cave and dungeon walls (one batch of flat painted pieces): the same colours with a rock grain over them
    static Material caveMat;
    public static int CaveWalls()
    {
        if (!AHDungeon.IsDungeon(AHGame.AreaId) && AHGame.AreaId != "forge") return 0;
        int n = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.name.StartsWith("AH_INST")) continue;
            var m = r.sharedMaterial; if (m == null || m.shader.name.StartsWith("AshenHollow/") || HasTex(m)) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) continue;
            if (caveMat == null)
            {
                caveMat = AHGame.LoadMat("AH/Materials/CaveStone", "AshenHollow/CaveStone"); if (caveMat == null) return 0;
                caveMat.SetTexture("_BaseMap", StoneTex()); caveMat.enableInstancing = true; caveMat.name = "AHCaveStone";
            }
            var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = caveMat; r.sharedMaterials = mats; n++;
        }
        if (n > 0) Debug.Log("Ashen Hollow: cave walls given a rock grain (" + n + ")");
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
            HideGiants();
            StoneSkin();
            CaveWalls();
            // the props built here in code out in the lands (stalls, benches, looms, cauldrons, tents, carts)
            // (every still thing in the land: not the people, beasts, pets or the hero)
            foreach (var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (top.GetComponent<AHMob>() != null || top.GetComponent<AHNpc>() != null || top.GetComponent<AHAlly>() != null || top.GetComponent<AHPetFollow>() != null) continue;
                if (AHGame.I != null && (top == AHGame.I.gameObject || (AHGame.I.player != null && top == AHGame.I.player.transform.root.gameObject))) continue;
                DressRoom(top.transform);
            }
        }
    }
}
