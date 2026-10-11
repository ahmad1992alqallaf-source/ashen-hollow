// Ashen Hollow: the Abyssal Realm (levels 60-75). The kingdom of the fallen heroes, torn from the world and left
// hanging in the void: islands of pale cracked marble with violet rift light seeping through, joined by balustraded
// bridges over nothing; broken fluted columns, torn banners with the Ashen crest, rocks drifting up out of the dark,
// a cold violet sky. Built in code from the user's map concept (abyss.json: the islands, bridges and who lives where;
// tools/abyss/make_abyss.py). The way in is a rift in the Fossil Lands; the way out a rift by the broken gate.
using System.Collections.Generic;
using UnityEngine;

public class AHAbyss : MonoBehaviour
{
    public const string Area = "abyss";
    public const int MinLevel = 60;
    static AHAbyss inst;
    AHGame g;
    object data;
    readonly List<Transform> drift = new List<Transform>(); readonly List<Vector3> driftBase = new List<Vector3>(); readonly List<float> driftPhase = new List<float>();
    readonly List<Transform> cloths = new List<Transform>();
    Transform motes;

    public static void Setup(AHGame g)
    {
        if (inst != null) { Destroy(inst.gameObject); inst = null; }
        if (AHGame.AreaId == "fossil") { Entrance(g); return; }
        if (AHGame.AreaId != Area) return;
        var ta = Resources.Load<TextAsset>("AH/Areas/" + Area); if (ta == null) return;
        inst = new GameObject("The Abyssal Realm").AddComponent<AHAbyss>(); inst.g = g; inst.data = AHJson.Parse(ta.text);
        inst.Build();
    }

    // ---------- the way in and out ----------
    static void Entrance(AHGame g)
    {
        if (g.data == null || g.data.spawn == null) return;
        Vector3 at = g.Resolve(g.W(g.data.spawn.x + 14f, g.data.spawn.z + 6f), 1.4f);
        Rift(g, at, "Enter the Abyssal Realm", () =>
        {
            if (g.player.level < MinLevel) { g.ui.Toast("The rift will not take you yet. The Abyssal Realm opens at level " + MinLevel + ".", 3f); return; }
            var ta = Resources.Load<TextAsset>("AH/Areas/" + Area); if (ta == null) return;
            var d = AHJson.Parse(ta.text); var sp = AHJson.O(d, "spawn");
            g.Travel(Area, (float)AHJson.N(sp, "x"), (float)AHJson.N(sp, "z") + 4f, Mathf.PI / 2f);
        });
    }
    static void Rift(AHGame g, Vector3 at, string label, System.Action use)
    {
        var root = new GameObject("Rift").transform; root.position = at;
        // a tall tear in the air: two glowing violet blades of light around a dark core, turning slowly
        var glow = Glow(new Color(0.62f, 0.4f, 1f), 2.6f);
        var core = Glow(new Color(0.05f, 0.02f, 0.1f), 0f);
        for (int i = 0; i < 2; i++)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "RiftLight";
            q.transform.SetParent(root, false); q.transform.localPosition = new Vector3(0f, 2.1f, 0f); q.transform.localScale = new Vector3(1.5f - i * 0.5f, 3.8f - i * 0.6f, 1f);
            q.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f); q.GetComponent<Renderer>().sharedMaterial = i == 0 ? glow : core;
            q.AddComponent<AHAbyssSpin>().speed = 25f + i * 20f;
        }
        var l = new GameObject("RiftGlow").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.localPosition = Vector3.up * 2f;
        l.type = LightType.Point; l.color = new Color(0.65f, 0.45f, 1f); l.range = 9f; l.intensity = 2.5f; l.shadows = LightShadows.None;
        AHGather.Spots.Add(new AHSpot { kind = "use", type = "rift", name = label, pos = at, r = 0.5f, reach = 3f, use = use });
    }

    // ---------- materials ----------
    static Material marble, rock, trim, cloth, crestMat, voidMat, glowLine;
    static Texture2D marbleTex, marbleEmit;
    static Material Lit(Color c, float smooth = 0.15f)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true; return m;
    }
    static Material Glow(Color c, float k)
    {
        var m = Lit(c, 0f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; return m;
    }
    // pale marble: soft grey veins, and thin violet cracks that glow (the void leaking through)
    static void MakeMarble()
    {
        if (marbleTex != null) return;
        const int N = 512; marbleTex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "abyss marble" };
        marbleEmit = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "abyss cracks" };
        var px = new Color32[N * N]; var ex = new Color32[N * N]; var rnd = new System.Random(11);
        float[] a = new float[33 * 33]; for (int i = 0; i < a.Length; i++) a[i] = (float)rnd.NextDouble();
        float[] b = new float[17 * 17]; for (int i = 0; i < b.Length; i++) b[i] = (float)rnd.NextDouble();
        System.Func<float[], int, float, float, float> V = (t, n, x, y) =>
        {
            int x0 = ((int)Mathf.Floor(x) % n + n) % n, y0 = ((int)Mathf.Floor(y) % n + n) % n, x1 = (x0 + 1) % n, y1 = (y0 + 1) % n; float fx = x - Mathf.Floor(x), fy = y - Mathf.Floor(y);
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            return Mathf.Lerp(Mathf.Lerp(t[y0 * (n + 1) + x0], t[y0 * (n + 1) + x1], fx), Mathf.Lerp(t[y1 * (n + 1) + x0], t[y1 * (n + 1) + x1], fx), fy);
        };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, v = y / (float)N;
                // veins: the classic marble ripple, bent by noise
                float warp = V(a, 32, u * 8f, v * 8f) * 2.4f + V(b, 16, u * 4f, v * 4f) * 1.6f;
                float vein = Mathf.Abs(Mathf.Sin((u * 3f + v * 1.6f) * 6.283f + warp * 3.2f));
                float shade = 0.9f + 0.06f * V(a, 32, u * 16f + 5f, v * 16f) - 0.1f * Mathf.Pow(1f - vein, 10f);
                // slabs: faint seams every half (big worn flagstones)
                float sx = Mathf.Abs((u * 2f) % 1f - 0.5f), sy = Mathf.Abs((v * 2f) % 1f - 0.5f); if (sx > 0.494f || sy > 0.494f) shade *= 0.86f;
                byte c = (byte)Mathf.Clamp(255f * shade, 0, 255), cb = (byte)Mathf.Clamp(255f * shade * 1.03f, 0, 255);
                px[y * N + x] = new Color32(c, c, cb, 255);
                // cracks: a few long jagged lines that glow violet
                float cr = Mathf.Abs(V(b, 16, u * 5f + 2.7f, v * 5f + 9.1f) - 0.5f);
                float k = cr < 0.005f ? 1f : cr < 0.009f ? 0.3f : 0f;
                if (V(a, 32, u * 3f + 11f, v * 3f + 4f) < 0.5f) k = 0f;   // the cracks only in places, not everywhere
                if (k > 0f) px[y * N + x] = new Color32((byte)(c * 0.35f), (byte)(c * 0.3f), (byte)(cb * 0.45f), 255);
                ex[y * N + x] = new Color32((byte)(150 * k), (byte)(90 * k), (byte)(255 * k), 255);
            }
        marbleTex.SetPixels32(px); marbleTex.Apply(true); marbleEmit.SetPixels32(ex); marbleEmit.Apply(true);
    }
    static void Mats()
    {
        if (marble != null) return;
        MakeMarble();
        marble = Lit(Color.white, 0.32f); marble.SetTexture("_BaseMap", marbleTex); marble.SetTextureScale("_BaseMap", Vector2.one);
        marble.EnableKeyword("_EMISSION"); marble.SetTexture("_EmissionMap", marbleEmit); marble.SetColor("_EmissionColor", new Color(0.9f, 0.7f, 1.5f));
        rock = Lit(new Color(0.24f, 0.22f, 0.3f), 0.1f); rock.SetFloat("_Cull", 0f); rock.SetTexture("_BaseMap", marbleTex); rock.SetTextureScale("_BaseMap", new Vector2(0.5f, 2f));
        trim = Lit(new Color(0.78f, 0.62f, 0.3f), 0.55f); trim.SetFloat("_Metallic", 0.7f);   // old gold
        cloth = Lit(new Color(0.12f, 0.08f, 0.26f), 0.05f); cloth.SetFloat("_Cull", 0f);     // deep indigo, both faces
        var ct = Resources.Load<Texture2D>("AH/Textures/Crest/ashen_crest_gold");
        crestMat = Lit(Color.white, 0.3f); crestMat.SetFloat("_Cull", 0f);
        if (ct != null)
        {
            crestMat.SetTexture("_BaseMap", ct); crestMat.SetFloat("_AlphaClip", 1f); crestMat.SetFloat("_Cutoff", 0.45f); crestMat.EnableKeyword("_ALPHATEST_ON");
            crestMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        voidMat = Glow(new Color(0.05f, 0.02f, 0.1f), 1f);
        glowLine = Glow(new Color(0.5f, 0.3f, 0.95f), 1.1f);
    }

    // ---------- building ----------
    Rect U(object box) { return g.MapRect((float)AHJson.N(box, "x"), (float)AHJson.N(box, "z"), (float)AHJson.N(box, "w"), (float)AHJson.N(box, "h")); }
    System.Random rnd;
    static GameObject[] crags;

    void Build()
    {
        Mats(); rnd = new System.Random(75);
        if (crags == null) { crags = new GameObject[3]; for (int i = 0; i < 3; i++) crags[i] = Resources.Load<GameObject>("AH/Models/Nature/mtnlite_" + i); }
        var cam = g.cam; if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.07f, 0.04f, 0.13f); }

        var isl = AHJson.O(data, "islands"); var rects = new List<Rect>();
        if (isl != null) foreach (var kv in isl) { var r = U(kv.Value); rects.Add(r); Island(kv.Key, r); }
        var br = AHJson.A(data, "bridges"); if (br != null) foreach (var b in br) Bridge(U(b));

        // the void below: a faint violet glow far down, rocks drifting up out of it, motes of light rising past you
        var floorV = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(floorV.GetComponent<Collider>()); floorV.name = "Void";
        floorV.transform.SetParent(transform, false);
        Rect all = g.MapRect(10000f - 150f, -150f, 550f, 550f);
        floorV.transform.position = new Vector3(all.center.x, -70f, all.center.y); floorV.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        floorV.transform.localScale = new Vector3(all.width, all.height, 1f); floorV.GetComponent<Renderer>().sharedMaterial = voidMat;
        Rect area = g.MapRect(10000f - 40f, -40f, 330f, 330f);
        for (int i = 0, tries = 0; i < 70 && tries < 900; tries++)
        {
            var p = new Vector3(Mathf.Lerp(area.xMin, area.xMax, (float)rnd.NextDouble()), 0f, Mathf.Lerp(area.yMin, area.yMax, (float)rnd.NextDouble()));
            bool clear = true; foreach (var r in rects) if (p.x > r.xMin - 10f && p.x < r.xMax + 10f && p.z > r.yMin - 10f && p.z < r.yMax + 10f) { clear = false; break; }
            if (!clear) continue;
            float s = 1.5f + (float)rnd.NextDouble() * 6f; p.y = -30f + (float)rnd.NextDouble() * 34f;
            var c = Crag(p, new Vector3(s, s * (0.7f + (float)rnd.NextDouble() * 0.8f), s), rock, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() < 0.5f);
            drift.Add(c); driftBase.Add(p); driftPhase.Add((float)rnd.NextDouble() * 6.28f); i++;
        }
        Motes();
        // the way back: a rift beside the broken gate
        var sp = AHJson.O(data, "spawn");
        if (sp != null)
        {
            Vector3 at = g.W((float)AHJson.N(sp, "x") - 12f, (float)AHJson.N(sp, "z") + 2f);
            Rift(g, at, "Leave the Abyssal Realm", () => { g.Travel("fossil", FossilBack.x, FossilBack.y, -Mathf.PI / 2f); });
        }
        StartCoroutine(Hello());
    }
    // where the rift in the Fossil Lands stands (web metres), for the way back
    static Vector2 FossilBack { get { var ta = Resources.Load<TextAsset>("AH/Areas/fossil"); if (ta == null) return Vector2.zero; var sp = AHJson.O(AHJson.Parse(ta.text), "spawn"); return new Vector2((float)AHJson.N(sp, "x") + 14f, (float)AHJson.N(sp, "z") + 2f); } }

    System.Collections.IEnumerator Hello()
    {
        yield return new WaitForSeconds(1.2f);
        if (g.ui != null) g.ui.Banner(AHLoc.T("The Abyssal Realm", "عالم الهاوية"), AHLoc.T("The fallen kingdom, torn into the void · levels 60–75", "المملكة الساقطة، ممزقة في العدم · المستويات 60–75"));
    }

    // an island: a slab of marble with a jagged rim, a cliff of dark stone under its edge, and broken rock hanging below
    void Island(string name, Rect r)
    {
        var root = new GameObject("Island " + name).transform; root.SetParent(transform, false);
        const float margin = 2.6f;
        Rect o = new Rect(r.xMin - margin, r.yMin - margin, r.width + margin * 2f, r.height + margin * 2f);
        Vector2 c = o.center; int n = 64;
        var rim = new List<Vector3>();
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            // a rounded rectangle (corner radius a fifth of the short side), its edge broken a little
            float hx = o.width * 0.5f, hz = o.height * 0.5f, rr = Mathf.Min(hx, hz) * 0.4f;
            float t = RayRoundRect(d, hx, hz, rr) + ((float)rnd.NextDouble() - 0.5f) * 2.2f;
            rim.Add(new Vector3(c.x + d.x * t, 0f, c.y + d.y * t));
        }
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
        // top: a fan from the middle
        v.Add(new Vector3(c.x, 0f, c.y)); uv.Add(new Vector2(c.x, c.y) / 7f);
        foreach (var p in rim) { v.Add(p); uv.Add(new Vector2(p.x, p.z) / 7f); }
        for (int i = 0; i < n; i++) { tris.Add(0); tris.Add(1 + (i + 1) % n); tris.Add(1 + i); }
        var top = Up(MeshOf(v, uv, tris, "top"));
        Part(root, "Floor", top, marble);
        // the cliff below the rim: three rings going down and in, then closed underneath
        v.Clear(); uv.Clear(); tris.Clear();
        float[] ys = { 0f, -2.2f, -5.5f, -9f }; float[] ins = { 0f, 0.4f, 2.6f, 7f };
        for (int k = 0; k < ys.Length; k++)
            for (int i = 0; i <= n; i++)
            {
                var p = rim[i % n]; Vector3 d = new Vector3(c.x - p.x, 0f, c.y - p.z).normalized;
                float j = k == 0 ? 0f : ((float)rnd.NextDouble() - 0.5f) * 1.4f;
                v.Add(p + d * (ins[k] + j) + Vector3.up * (ys[k] + j)); uv.Add(new Vector2(i / (float)n * 18f, ys[k] / 6f));
            }
        int w = n + 1;
        for (int k = 0; k < ys.Length - 1; k++)
            for (int i = 0; i < n; i++)
            {
                int a0 = k * w + i, a1 = a0 + 1, b0 = a0 + w, b1 = b0 + 1;
                tris.Add(a0); tris.Add(a1); tris.Add(b0); tris.Add(a1); tris.Add(b1); tris.Add(b0);
            }
        int ctr = v.Count; v.Add(new Vector3(c.x, -12f, c.y)); uv.Add(Vector2.zero);
        for (int i = 0; i < n; i++) { int a0 = (ys.Length - 1) * w + i; tris.Add(ctr); tris.Add(a0); tris.Add(a0 + 1); }
        Part(root, "Cliff", MeshOf(v, uv, tris, "cliff"), rock);
        // broken rock hanging under it (crags turned upside down)
        int hang = Mathf.Clamp((int)(o.width * o.height / 500f), 2, 9);
        for (int i = 0; i < hang; i++)
        {
            var p = new Vector3(Mathf.Lerp(o.xMin + 4f, o.xMax - 4f, (float)rnd.NextDouble()), -6f, Mathf.Lerp(o.yMin + 4f, o.yMax - 4f, (float)rnd.NextDouble()));
            float s = Mathf.Min(o.width, o.height) * (0.25f + (float)rnd.NextDouble() * 0.25f);
            Crag(p, new Vector3(s, s * (1.3f + (float)rnd.NextDouble()), s), rock, (float)rnd.NextDouble() * 360f, true).SetParent(root, true);
        }
        Ruins(name, r, root);
        Fissures(r, root);
        // rubble: broken stone strewn over the floor, more of it near the edges
        int bits = Mathf.Clamp((int)(r.width * r.height / 70f), 6, 70);
        for (int i = 0; i < bits; i++)
        {
            float u = (float)rnd.NextDouble(), w2 = (float)rnd.NextDouble(); if (rnd.NextDouble() < 0.6) { if (rnd.NextDouble() < 0.5) u = u < 0.5f ? u * 0.25f : 1f - (1f - u) * 0.25f; else w2 = w2 < 0.5f ? w2 * 0.25f : 1f - (1f - w2) * 0.25f; }
            var p = new Vector3(Mathf.Lerp(r.xMin, r.xMax, u), 0f, Mathf.Lerp(r.yMin, r.yMax, w2));
            float sz = 0.35f + (float)rnd.NextDouble() * (rnd.NextDouble() < 0.15 ? 1.6f : 0.6f);
            Crag(p - Vector3.up * sz * 0.25f, new Vector3(sz * 1.3f, sz, sz), (rnd.NextDouble() < 0.7 ? marble : rock), (float)rnd.NextDouble() * 360f, false).SetParent(root, true);
        }
        Landmark(name, r, root);
    }
    // distance from the centre of a rounded rectangle to its edge along d
    static float RayRoundRect(Vector2 d, float hx, float hz, float rr)
    {
        float lo = 0f, hi = Mathf.Max(hx, hz) * 1.5f;
        for (int it = 0; it < 24; it++)
        {
            float m = (lo + hi) * 0.5f; Vector2 p = d * m;
            float qx = Mathf.Abs(p.x) - (hx - rr), qz = Mathf.Abs(p.y) - (hz - rr);
            float dist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qz, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qz), 0f) - rr;
            if (dist < 0f) lo = m; else hi = m;
        }
        return lo;
    }

    // a bridge: a marble deck with gold-capped columns along both sides, a glowing seam down its middle
    void Bridge(Rect r)
    {
        var root = new GameObject("Bridge").transform; root.SetParent(transform, false);
        bool alongZ = r.height >= r.width; float len = alongZ ? r.height : r.width, wid = alongZ ? r.width : r.height;
        Vector3 c = new Vector3(r.center.x, 0f, r.center.y), fwd = alongZ ? Vector3.forward : Vector3.right, side = alongZ ? Vector3.right : Vector3.forward;
        // the deck runs a little into the islands at both ends
        var deck = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
        float L = len * 0.5f + 3f, W = wid * 0.5f + 0.6f;
        Vector3[] corners = { c - fwd * L - side * W, c + fwd * L - side * W, c + fwd * L + side * W, c - fwd * L + side * W };
        foreach (var p in corners) { deck.Add(p + Vector3.up * 0.02f); uv.Add(new Vector2(p.x, p.z) / 7f); }
        foreach (var p in corners) { deck.Add(p - Vector3.up * 1.4f); uv.Add(new Vector2(p.x, p.z) / 7f); }
        tris.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
        int[][] faces = { new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };
        foreach (var f in faces) { tris.Add(f[0]); tris.Add(f[1]); tris.Add(f[2]); tris.Add(f[0]); tris.Add(f[2]); tris.Add(f[3]); }
        tris.AddRange(new[] { 4, 5, 6, 4, 6, 7 });
        Part(root, "Deck", MeshOf(deck, uv, tris, "deck"), marble);   // (its top is the first face)
        // an arch under the middle: a curved rib of dark stone (looks held up by nothing but old magic)
        for (float t = -L + 2f; t <= L - 2f; t += 2.2f)
        {
            float sag = 1.6f + 3.2f * (1f - Mathf.Pow(t / L, 2f));
            var rib = Crag(c + fwd * t - Vector3.up * (1.4f + sag * 0.5f), new Vector3(wid * 0.9f, sag, 2.4f), rock, alongZ ? 0f : 90f, true); rib.SetParent(root, true);
        }
        // columns and a gold rail along both edges
        for (int s = -1; s <= 1; s += 2)
        {
            for (float t = -L + 1.5f; t <= L - 1.5f + 0.01f; t += 3f)
            {
                var p = c + fwd * t + side * (s * (W - 0.3f));
                Column(root, p, 0.28f, 1.15f + (rnd.NextDouble() < 0.18 ? -0.6f : 0f), false);
            }
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(rail.GetComponent<Collider>()); rail.name = "Rail";
            rail.transform.SetParent(root, false); rail.transform.position = c + side * (s * (W - 0.3f)) + Vector3.up * 1.2f;
            rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, fwd);
            rail.transform.localScale = new Vector3(0.14f, L - 1f, 0.14f); rail.GetComponent<Renderer>().sharedMaterial = trim;
        }
        var seam = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(seam.GetComponent<Collider>()); seam.name = "Seam";
        seam.transform.SetParent(root, false); seam.transform.position = c + Vector3.up * 0.04f; seam.transform.rotation = Quaternion.LookRotation(Vector3.down, fwd);
        seam.transform.localScale = new Vector3(0.12f, L * 2f - 2f, 1f); seam.GetComponent<Renderer>().sharedMaterial = glowLine;
    }

    // ---------- glowing rifts across the floor: jagged violet lines where the island is coming apart ----------
    void Fissures(Rect r, Transform root)
    {
        int n = Mathf.Clamp((int)(r.width * r.height / 900f), 1, 5);
        for (int f = 0; f < n; f++)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            // from one edge inward, wandering, narrowing to nothing
            bool fromX = rnd.NextDouble() < 0.5; float u0 = 0.2f + (float)rnd.NextDouble() * 0.6f;
            Vector3 p = fromX ? new Vector3(rnd.NextDouble() < 0.5 ? r.xMin : r.xMax, 0.03f, Mathf.Lerp(r.yMin, r.yMax, u0)) : new Vector3(Mathf.Lerp(r.xMin, r.xMax, u0), 0.03f, rnd.NextDouble() < 0.5 ? r.yMin : r.yMax);
            Vector3 dir = (new Vector3(r.center.x, 0.03f, r.center.y) - p).normalized;
            int steps = 9 + rnd.Next(8); float len = Mathf.Min(r.width, r.height) * (0.35f + (float)rnd.NextDouble() * 0.3f) / steps;
            for (int i = 0; i <= steps; i++)
            {
                float wdt = 0.55f * (1f - i / (float)steps) + 0.05f;
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * wdt;
                v.Add(p - side); v.Add(p + side); uv.Add(new Vector2(0, i)); uv.Add(new Vector2(1, i));
                if (i > 0) { int b = v.Count - 4; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
                dir = Quaternion.Euler(0f, ((float)rnd.NextDouble() - 0.5f) * 70f, 0f) * dir; p += dir * len;
            }
            var go = new GameObject("Rift"); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = Up(MeshOf(v, uv, t, "rift")); go.AddComponent<MeshRenderer>().sharedMaterial = glowLine;
        }
    }

    // ---------- each island's heart: the gate's arch of columns, the shrine's crest altar, the forge's anvils of stone,
    // the riven island's floating shard, the throne on its dais ----------
    void Landmark(string name, Rect r, Transform root)
    {
        Vector3 c = new Vector3(r.center.x, 0f, r.center.y);
        switch (name)
        {
            case "gate":
                for (int s = -1; s <= 1; s += 2) { Column(root, c + new Vector3(s * 5f, 0f, -6f), 1f, 11f, true); g.AddBlocker(c + new Vector3(s * 5f, 0f, -6f), 1.2f); }
                CrestDisc(root, c + new Vector3(0f, 9.5f, -6f), 4.2f, Vector3.forward);
                break;
            case "shrine":
                Dais(root, c, 9f, 2);
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; var p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 12f; Column(root, p, 0.8f, i % 3 == 0 ? 3f : 8f, true); g.AddBlocker(p, 1f); }
                CrestDisc(root, c + Vector3.up * 5.5f, 4.5f, Vector3.forward); g.AddBlocker(c, 2f);
                break;
            case "rift":
                {   // a great shard of the old kingdom turning slowly above a hole torn in the floor
                    var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(hole.GetComponent<Collider>()); hole.name = "Tear";
                    hole.transform.SetParent(root, false); hole.transform.position = c + Vector3.up * 0.02f; hole.transform.localScale = new Vector3(7f, 0.02f, 7f);
                    hole.GetComponent<Renderer>().sharedMaterial = voidMat; g.AddBlocker(c, 3.6f);
                    var shard = Crag(c + Vector3.up * 7f, new Vector3(4f, 7f, 4f), marble, 0f, false); shard.SetParent(root, true);
                    shard.gameObject.AddComponent<AHAbyssSpin>().speed = 9f; drift.Add(shard); driftBase.Add(shard.position); driftPhase.Add(1f);
                    var l = new GameObject("TearLight").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = c + Vector3.up * 2f;
                    l.type = LightType.Point; l.color = new Color(0.55f, 0.35f, 1f); l.range = 16f; l.intensity = 3f; l.shadows = LightShadows.None;
                }
                break;
            case "forge":
                for (int i = 0; i < 4; i++) { var p = c + new Vector3((i % 2 == 0 ? -1 : 1) * 10f, 0f, (i < 2 ? -1 : 1) * 8f); Dais(root, p, 3f, 1); Column(root, p, 1.2f, 2.2f, true); g.AddBlocker(p, 1.6f); }
                break;
            case "citadel":
                {   // the empty throne of the fallen king on a three-step dais, the crest towering behind it
                    var t = c + new Vector3(0f, 0f, r.height * 0.28f);
                    Dais(root, t, 12f, 3);
                    var seat = Crag(t + Vector3.up * 1.5f, new Vector3(3.4f, 6f, 2.6f), marble, 0f, false); seat.SetParent(root, true);
                    CrestDisc(root, t + new Vector3(0f, 10f, 3f), 8f, Vector3.forward); g.AddBlocker(t, 3f);
                    for (int s = -1; s <= 1; s += 2) for (int k = 0; k < 3; k++) { var p = t + new Vector3(s * (8f + k * 7f), 0f, -2f - k * 4f); Column(root, p, 1f, 9f + k * 2f, true); g.AddBlocker(p, 1.2f); }
                }
                break;
        }
    }
    // a round stepped platform of marble (tiers going up), gold edge on the top step
    void Dais(Transform root, Vector3 at, float radius, int tiers)
    {
        for (int i = 0; i < tiers; i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(d.GetComponent<Collider>()); d.name = "Dais";
            d.transform.SetParent(root, false); float rr = radius * (1f - i * 0.22f);
            d.transform.position = at + Vector3.up * (0.18f + i * 0.36f); d.transform.localScale = new Vector3(rr * 2f, 0.18f, rr * 2f);
            d.GetComponent<Renderer>().sharedMaterial = marble;
        }
    }
    // the Ashen crest as a great disc of stone and gold, standing upright
    void CrestDisc(Transform root, Vector3 at, float size, Vector3 facing)
    {
        var back = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(back.GetComponent<Collider>()); back.name = "CrestStone";
        back.transform.SetParent(root, false); back.transform.position = at; back.transform.rotation = Quaternion.LookRotation(Vector3.up, -facing) * Quaternion.Euler(0f, 0f, 0f);
        back.transform.rotation = Quaternion.FromToRotation(Vector3.up, -facing); back.transform.localScale = new Vector3(size * 1.08f, 0.25f, size * 1.08f);
        back.GetComponent<Renderer>().sharedMaterial = rock;
        foreach (float side in new[] { -1f, 1f })
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "Crest";
            q.transform.SetParent(root, false); q.transform.position = at + facing * (0.27f * side);
            q.transform.rotation = Quaternion.LookRotation(facing * side); q.transform.localScale = new Vector3(size, size, 1f);
            q.GetComponent<Renderer>().sharedMaterial = crestMat;
        }
        var l = new GameObject("CrestLight").AddComponent<Light>(); l.transform.SetParent(root, false); l.transform.position = at - facing * 3f;
        l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.6f); l.range = size * 2.2f; l.intensity = 1.4f; l.shadows = LightShadows.None;
    }

    // ---------- ruins: broken columns, fallen drums, banners with the crest, rift lights ----------
    void Ruins(string name, Rect r, Transform root)
    {
        int cols = Mathf.Clamp((int)(r.width * r.height / 260f), 4, 22);
        for (int i = 0; i < cols; i++)
        {
            // along the edges mostly, so the middle stays open to fight in
            bool edgeX = rnd.NextDouble() < 0.5; float u = (float)rnd.NextDouble(), e = rnd.NextDouble() < 0.5 ? 0.06f : 0.94f;
            var p = new Vector3(Mathf.Lerp(r.xMin, r.xMax, edgeX ? e : u), 0f, Mathf.Lerp(r.yMin, r.yMax, edgeX ? u : e));
            float h = rnd.NextDouble() < 0.35 ? 2f + (float)rnd.NextDouble() * 2f : 5f + (float)rnd.NextDouble() * 4f;
            Column(root, p, 0.6f + (float)rnd.NextDouble() * 0.2f, h, true);
            g.AddBlocker(p, 0.8f);
            if (h < 4.5f && rnd.NextDouble() < 0.6)
            {   // the fallen top lying beside it
                var drum = Column(root, p + new Vector3((float)rnd.NextDouble() * 3f - 1.5f, 0.55f, (float)rnd.NextDouble() * 3f - 1.5f), 0.55f, 1.6f + (float)rnd.NextDouble() * 1.6f, true);
                drum.rotation = Quaternion.Euler(90f, (float)rnd.NextDouble() * 360f, 0f);
            }
        }
        // banners with the crest, in pairs, at the island's ends (the gate, the shrine and the citadel get more)
        int pairs = name == "gate" || name == "citadel" || name == "shrine" ? 3 : 1;
        for (int i = 0; i < pairs; i++)
        {
            float u = (i + 1f) / (pairs + 1f);
            Banner(root, new Vector3(Mathf.Lerp(r.xMin, r.xMax, u), 0f, r.yMin + 1.2f));
            Banner(root, new Vector3(Mathf.Lerp(r.xMin, r.xMax, u), 0f, r.yMax - 1.2f));
        }
        // violet rift light (no shadows) so the dark reads as void, not black
        var l = new GameObject("RiftLight").AddComponent<Light>(); l.transform.SetParent(root, false);
        l.transform.position = new Vector3(r.center.x, 6f, r.center.y); l.type = LightType.Point; l.color = new Color(0.6f, 0.42f, 1f);
        l.range = Mathf.Max(r.width, r.height) * 0.75f; l.intensity = 1.6f; l.shadows = LightShadows.None;
    }

    // a fluted column of marble (16 grooves), gold base ring; broken ones end in a jagged top
    static Mesh colMesh, brokenMesh;
    Transform Column(Transform parent, Vector3 at, float radius, float height, bool solid)
    {
        if (colMesh == null) { colMesh = Fluted(false); brokenMesh = Fluted(true); }
        var go = new GameObject("Column"); go.transform.SetParent(parent, false);
        go.transform.position = at; go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
        go.AddComponent<MeshFilter>().sharedMesh = height < 4.5f ? brokenMesh : colMesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { marble, trim };
        return go.transform;
    }
    static Mesh Fluted(bool broken)
    {
        const int seg = 32; var v = new List<Vector3>(); var uv = new List<Vector2>(); var shaft = new List<int>(); var ring = new List<int>();
        var rr = new System.Random(broken ? 5 : 3);
        // shaft: grooves made by pulling every other ring of vertices in
        for (int k = 0; k <= 1; k++)
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg, rad = 0.5f * (i % 2 == 0 ? 1f : 0.9f) * (k == 1 ? 0.92f : 1f);
                float y = k == 0 ? 0.08f : broken ? 0.82f + (float)rr.NextDouble() * 0.18f : 0.94f;
                v.Add(new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad)); uv.Add(new Vector2(i / (float)seg * 2f, y * 3f));
            }
        for (int i = 0; i < seg; i++) { int a0 = i, a1 = i + 1, b0 = a0 + seg + 1, b1 = b0 + 1; shaft.Add(a0); shaft.Add(b0); shaft.Add(a1); shaft.Add(a1); shaft.Add(b0); shaft.Add(b1); }
        // top: flat (whole) or a cap over the broken edge
        int tc = v.Count; v.Add(new Vector3(0f, broken ? 0.86f : 0.94f, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
        for (int i = 0; i < seg; i++) { shaft.Add(tc); shaft.Add(seg + 1 + i + 1); shaft.Add(seg + 1 + i); }
        // gold rings: a band at the foot (and a capital at the top of a whole column)
        System.Action<float, float, float> band = (y0, y1, rad) =>
        {
            int b = v.Count;
            for (int k = 0; k < 2; k++) for (int i = 0; i <= seg; i++) { float a = i * Mathf.PI * 2f / seg; v.Add(new Vector3(Mathf.Cos(a) * rad, k == 0 ? y0 : y1, Mathf.Sin(a) * rad)); uv.Add(new Vector2(i / (float)seg, k)); }
            for (int i = 0; i < seg; i++) { int a0 = b + i, a1 = a0 + 1, b0 = a0 + seg + 1, b1 = b0 + 1; ring.Add(a0); ring.Add(b0); ring.Add(a1); ring.Add(a1); ring.Add(b0); ring.Add(b1); }
            int cc = v.Count; v.Add(new Vector3(0f, y1, 0f)); uv.Add(Vector2.zero);
            for (int i = 0; i < seg; i++) { ring.Add(cc); ring.Add(b + seg + 1 + i + 1); ring.Add(b + seg + 1 + i); }
        };
        band(0f, 0.08f, 0.6f);
        if (!broken) band(0.94f, 1f, 0.62f);
        var m = new Mesh { name = broken ? "broken column" : "column" }; m.SetVertices(v); m.SetUVs(0, uv); m.subMeshCount = 2;
        m.SetTriangles(shaft, 0); m.SetTriangles(ring, 1); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    // a tall banner: a gold-tipped pole, indigo cloth with the Ashen crest, torn short of the ground; it sways
    void Banner(Transform parent, Vector3 at)
    {
        var root = new GameObject("Banner").transform; root.SetParent(parent, false); root.position = at;
        root.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, 1f));
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(pole.GetComponent<Collider>()); pole.name = "Pole";
        pole.transform.SetParent(root, false); pole.transform.localPosition = new Vector3(0f, 3f, 0f); pole.transform.localScale = new Vector3(0.16f, 3f, 0.16f);
        pole.GetComponent<Renderer>().sharedMaterial = trim;
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(bar.GetComponent<Collider>()); bar.name = "Bar";
        bar.transform.SetParent(root, false); bar.transform.localPosition = new Vector3(0f, 5.7f, 0f); bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); bar.transform.localScale = new Vector3(0.1f, 1.05f, 0.1f);
        bar.GetComponent<Renderer>().sharedMaterial = trim;
        var hang = new GameObject("Cloth").transform; hang.SetParent(root, false); hang.localPosition = new Vector3(0f, 5.65f, 0.08f);
        var cl = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(cl.GetComponent<Collider>()); cl.name = "Indigo";
        cl.transform.SetParent(hang, false); cl.transform.localPosition = new Vector3(0f, -1.7f, 0f); cl.transform.localScale = new Vector3(1.8f, 3.4f, 1f);
        cl.GetComponent<Renderer>().sharedMaterial = cloth;
        var cr = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(cr.GetComponent<Collider>()); cr.name = "Crest";
        cr.transform.SetParent(hang, false); cr.transform.localPosition = new Vector3(0f, -1.55f, -0.02f); cr.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        cr.GetComponent<Renderer>().sharedMaterial = crestMat;
        cloths.Add(hang);
        g.AddBlocker(at, 0.3f);
    }

    Transform Crag(Vector3 at, Vector3 size, Material m, float yaw, bool upsideDown)
    {
        GameObject pf = crags != null ? crags[rnd.Next(3)] : null;
        GameObject go;
        if (pf != null)
        {
            go = Instantiate(pf, transform, false);
            var mf = go.GetComponentInChildren<MeshFilter>(); Vector3 sz = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.size : Vector3.one;
            go.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, sz.x), size.y / Mathf.Max(0.01f, sz.y), size.z / Mathf.Max(0.01f, sz.z));
        }
        else { go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.transform.SetParent(transform, false); go.transform.localScale = size; }
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
        go.name = "Crag";
        go.transform.position = at; go.transform.rotation = Quaternion.Euler(upsideDown ? 180f : 0f, yaw, 0f);
        return go.transform;
    }

    void Motes()
    {
        var go = new GameObject("Motes"); go.transform.SetParent(transform, false); motes = go.transform;
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.startLifetime = 7f; main.startSpeed = 0.6f; main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 0.5f, 1f, 0.9f), new Color(0.45f, 0.7f, 1f, 0.8f)); main.maxParticles = 220; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;
        var em = ps.emission; em.rateOverTime = 26f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(36f, 2f, 36f); sh.position = new Vector3(0f, -4f, 0f);
        var col = ps.colorOverLifetime; col.enabled = true; var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }); col.color = gr;
        var r = go.GetComponent<ParticleSystemRenderer>(); var m = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark"); if (m != null) r.sharedMaterial = m;
        ps.Play();
    }

    void Update()
    {
        float t = Time.time;
        for (int i = 0; i < drift.Count; i++)
        {
            if (drift[i] == null) continue;
            drift[i].position = driftBase[i] + Vector3.up * Mathf.Sin(t * 0.25f + driftPhase[i]) * 1.6f;
            drift[i].Rotate(0f, Time.deltaTime * (4f + driftPhase[i]), 0f, Space.World);
        }
        for (int i = 0; i < cloths.Count; i++) if (cloths[i] != null) cloths[i].localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f + i) * 6f, 0f, Mathf.Sin(t * 0.6f + i * 1.7f) * 3f);
        if (motes != null && g != null && g.player != null) motes.position = g.player.transform.position;
    }

    static Mesh MeshOf(List<Vector3> v, List<Vector2> uv, List<int> t, string name)
    {
        var m = new Mesh { name = name }; if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents(); return m;
    }
    // a floor seen from above: turn its triangles over if they were wound the other way
    static Mesh Up(Mesh m)
    {
        if (m.normals.Length > 0 && m.normals[0].y < 0f) { var t = m.triangles; for (int i = 0; i < t.Length; i += 3) { int x = t[i]; t[i] = t[i + 1]; t[i + 1] = x; } m.triangles = t; m.RecalculateNormals(); }
        return m;
    }
    void Part(Transform parent, string name, Mesh mesh, Material m)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
    }

    // the light of the void, called by the day cycle every frame while you are here
    public static bool Sky(Light sun)
    {
        if (AHGame.AreaId != Area) return false;
        RenderSettings.fogColor = new Color(0.16f, 0.1f, 0.26f);
        RenderSettings.ambientSkyColor = new Color(0.38f, 0.36f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.27f, 0.25f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.14f, 0.1f, 0.22f);
        if (sun != null) { sun.intensity = 0.7f; sun.color = new Color(0.86f, 0.84f, 0.98f); }
        return true;
    }
}

public class AHAbyssSpin : MonoBehaviour { public float speed = 30f; void Update() { transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World); } }
