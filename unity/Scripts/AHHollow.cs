// Ashen Hollow: Hollow Meadow, the town you start in, rebuilt as a small round walled town.
//  - A ring of stone wall with towers, and a gatehouse wherever a road comes in (the road to the Old Mill in the east,
//    the road to Fenwick in the south). Inside, a ring street runs along the wall.
//  - In the middle, a round square with the fountain, the market stalls and the bounty board. Main streets run from the
//    square out to the gates, and two lanes run west and north.
//  - Between the streets stand the town's buildings. Each one has a job and a big sign over its door with its name in
//    its own colour: the Forge (furnace and anvils), the Hearth Kitchen (cooking pots), the Apothecary (brewing
//    cauldron), the Tannery (the loom), the Jeweler, the Bank, Tobin's General Goods, the Guild Hall, the inn, the
//    barber, the auction house, the barracks and the woodcarver. You walk in through the door: the roof and upper walls
//    are lifted away while you are inside so you can see the room. The townsfolk stand in their own buildings.
//  - The whole town is cobbled, nothing old is left standing inside it (no stray rocks, trees or old web pieces, and
//    nothing invisible in the way: their blockers go too), and lamps line the streets and the roads out, lit at night.
// Collision comes from the walls' own shapes (AHSolidGrid), so a doorway is open and a wall is solid.
using System.Collections.Generic;
using UnityEngine;

public static class AHHollow
{
    public const string Area = "meadow";
    public static bool Active;
    public static Vector3 C;
    public const float R = 42f;          // the wall's middle line, metres from the middle of the square
    const float WallT = 1.4f, WallH = 4.6f, Plaza = 14f, RingStreet = 4.6f;
    static Vector3 E, S;                 // world directions of the map's +x (east) and +z (south)
    static float gy;
    static Transform root;

    public class Hall
    {
        public string id, name; public Color col;
        public Vector3 O, b, r;           // front middle on the ground; b: into the building; r: across
        public float w, d;
        public Transform go;
        public Vector3 At(float x, float z) { return O + r * x + b * z; }
        public bool Contains(Vector3 p, float pad) { Vector3 q = p - O; float x = Vector3.Dot(q, r), z = Vector3.Dot(q, b); return Mathf.Abs(x) < w / 2f + pad && z > -pad && z < d + pad; }
        public Vector3 Door { get { return O - b * 2.2f; } }
    }
    public static readonly List<Hall> Halls = new List<Hall>();
    // for the town's life (AHTownLife): the gates and their portcullis, the chimneys, the bell, the house plot
    public class Gate { public Vector3 at, dir, tan; public float hw; public Transform grid; }
    public static readonly List<Gate> Gates = new List<Gate>();
    public static readonly List<Vector3> Chimneys = new List<Vector3>();
    public static Transform Root { get { return root; } }
    public static Transform Bell, ClockHour, ClockMin;
    public static Vector3 BellAt, FountainAt;
    public static Hall Plot; static bool PlotOutside;
    public static GameObject PlotForSale;
    struct Street { public float a, hw; public bool gate; public string label; }
    static readonly List<Street> streets = new List<Street>();
    static readonly List<Vector3> keep = new List<Vector3>();
    static readonly HashSet<AHSpot> mine = new HashSet<AHSpot>();

    // what each building is
    class Def { public string id, name, hex; public float w, d; public bool outer; public Def(string id, string name, string hex, float w, float d, bool outer = false) { this.id = id; this.name = name; this.hex = hex; this.w = w; this.d = d; this.outer = outer; } }
    static readonly Def[] Defs =
    {
        new Def("forge", "The Forge", "ff7a2e", 12f, 8.5f),
        new Def("kitchen", "Hearth Kitchen", "ffd54a", 11f, 8f),
        new Def("apothecary", "The Apothecary", "6cff7a", 11f, 8f),
        new Def("tannery", "The Tannery", "e0a87a", 11f, 8f),
        new Def("jeweler", "The Jeweler", "6fe7ff", 10f, 7.5f),
        new Def("bank", "Bank of Hollow", "ffc23a", 11f, 8f),
        new Def("store", "Tobin's General Goods", "b9f27a", 12f, 8f),
        new Def("guild", "Guild Hall", "6fa6ff", 12f, 8.5f),
        new Def("inn", "The Ashen Hearth Inn", "ff9f5a", 12f, 8.5f, true),
        new Def("barracks", "Barracks & Arena", "ff4e4e", 12f, 8f, true),
        new Def("auction", "Auction House", "c59bff", 11f, 8f, true),
        new Def("barber", "Barber & Wardrobe", "ff8acf", 10f, 7.5f, true),
        new Def("carver", "Woodcarver", "ffe7b0", 10f, 7.5f, true),
        new Def("stable", "Hollow Stables", "d8a860", 12f, 8f, true),
    };

    static Vector3 L(float x, float z) { return C + E * x + S * z; }
    static Vector3 Dir(float a) { return E * Mathf.Cos(a) + S * Mathf.Sin(a); }
    static Vector2 Loc(Vector3 p) { Vector3 d = p - C; return new Vector2(Vector3.Dot(d, E), Vector3.Dot(d, S)); }
    static float Ang(Vector3 p) { var l = Loc(p); return Mathf.Atan2(l.y, l.x); }
    public static bool Inside(Vector3 p, float pad = 0f) { if (!Active) return false; Vector3 d = p - C; d.y = 0f; return d.sqrMagnitude < (R + pad) * (R + pad); }

    // ======================================================================================================
    // while the land is built (before the trees, the stations and the props)
    public static void Setup(AHGame g, Transform world)
    {
        StableHall = null; Gates.Clear(); Chimneys.Clear(); Bell = ClockHour = ClockMin = null; BellAt = FountainAt = Vector3.zero; Plot = null; PlotForSale = null; PlotOutside = false;
        Active = false; bankAt = Vector3.zero; shopAt = Vector3.zero; pendingNpc.Clear(); pendingFace.Clear(); Halls.Clear(); streets.Clear(); keep.Clear(); mine.Clear(); root = null;
        if (world != null)   // (a ground material shared with another land carries no town circle)
            foreach (var r in world.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("AH_GROUND") && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_PaveCircle")) { r.sharedMaterial.SetVector("_PaveCircle", Vector4.zero); r.sharedMaterial.SetColor("_PaveCol", new Color(0.56f, 0.53f, 0.48f, 0f)); }
        if (AHGame.AreaId != Area || world == null || g.data == null || g.data.spawn == null) return;
        Active = true;
        C = g.W(g.data.spawn.x, g.data.spawn.z);
        E = g.W(1f, 0f) - g.W(0f, 0f); E.y = 0f; E.Normalize();
        S = g.W(0f, 1f) - g.W(0f, 0f); S.y = 0f; S.Normalize();
        gy = world.position.y;
        foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { gy = r.bounds.max.y; break; }
        C.y = gy;
        root = new GameObject("AH_Hollow town").transform; g0 = g;

        // nothing to gather inside the walls: the rocks, herbs and trees there go (they grow outside)
        AHGather.Spots.RemoveAll(s => (s.kind == "mine" || s.kind == "herb" || s.kind == "chop") && Inside(s.pos, 4f));
        g.RemoveBlockersNear(C, R + 3f); g.RemoveBoxesNear(C, R + 3f);

        FindStreets(g);
        Keepers(g);
        Pave(world);
        Walls(g);
        Square(g);
        Buildings(g);
        Lamps(g);
        Debug.Log("Ashen Hollow: Hollow Meadow walled town built (" + Halls.Count + " buildings, " + streets.Count + " streets)");
    }

    // ---------- where the streets go: a gate wherever a road out crosses the wall ----------
    static void FindStreets(AHGame g)
    {
        var gates = new List<float>();
        if (g.data.exits != null)
            foreach (var e in g.data.exits)
            {
                Vector3 ex = e.r > 0f ? g.W(e.x, e.z) : g.W((e.x0 + e.x1) / 2f, (e.z0 + e.z1) / 2f);
                float a0 = Ang(ex), best = a0; int bestRun = 0, run = 0; float runStart = 0f;
                // the road's own crossing near that way: the middle of the longest stretch of road on the wall line
                for (int i = -60; i <= 60; i++)
                {
                    float a = a0 + i * Mathf.Deg2Rad * 0.5f;
                    bool on = AHNav.IsDirt(g, C + Dir(a) * R) || AHNav.IsDirt(g, C + Dir(a) * (R + 4f)) || AHNav.IsDirt(g, C + Dir(a) * (R - 4f));
                    if (on) { if (run == 0) runStart = a; run++; if (run > bestRun) { bestRun = run; best = (runStart + a) / 2f; } } else run = 0;
                }
                bool dup = false; foreach (var q in gates) if (Mathf.Abs(Mathf.DeltaAngle(q * Mathf.Rad2Deg, best * Mathf.Rad2Deg)) < 25f) dup = true;
                if (!dup) gates.Add(best);
            }
        foreach (var a in gates) streets.Add(new Street { a = a, hw = 5f, gate = true, label = Compass(a) + " Gate" });
        // lanes opposite the gates, so the town is crossed both ways
        foreach (var a in gates)
        {
            float o = a + Mathf.PI; bool near = false;
            foreach (var s in streets) if (Mathf.Abs(Mathf.DeltaAngle(s.a * Mathf.Rad2Deg, o * Mathf.Rad2Deg)) < 40f) near = true;
            if (!near) streets.Add(new Street { a = o, hw = 3.5f, gate = false, label = Compass(o) + " Lane" });
        }
        streets.Sort((u, v) => Norm(u.a).CompareTo(Norm(v.a)));
    }
    static float Norm(float a) { a %= Mathf.PI * 2f; return a < 0f ? a + Mathf.PI * 2f : a; }
    static string Compass(float a)
    {
        // local +x is east, +z is south
        float d = Norm(a) * Mathf.Rad2Deg;
        return d < 45f || d >= 315f ? "East" : d < 135f ? "South" : d < 225f ? "West" : "North";
    }

    // things placed later by other parts of the game, which a building must not stand on
    static void Keepers(AHGame g)
    {
        var sp = g.data.spawn;
        System.Action<float, float> K = (x, z) => keep.Add(g.W(sp.x + x, sp.z + z));
        K(0f, 0f); K(10f, -6f); K(-11f, -9f);                          // the spawn, the Training Golem, the way into the Ashen Deep
        K(5f, 3f); K(-5f, 3f); K(0f, 15f); K(0f, 19f); K(0f, 9f);        // the story's people and the hero's statue
        var ws = AHDB.List("world", "WAYS");
        if (ws != null) foreach (var w in ws) if (AHJson.S(w, "area") == Area) keep.Add(g.W((float)AHJson.N(w, "x") * AHDB.S, (float)AHJson.N(w, "y") * AHDB.S));
    }

    // ---------- cobbles over the whole town ----------
    static void Pave(Transform world)
    {
        Renderer ground = null; foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { ground = r; break; }
        if (ground == null) return;
        var m = ground.sharedMaterial; if (m == null || !m.HasProperty("_PaveMap")) return;
        var cob = Resources.Load<Texture2D>("AH/Textures/cobble"); if (cob == null) return;
        m.SetTexture("_PaveMap", cob); m.SetFloat("_PaveScale", 0.16f); m.SetFloat("_PaveAmt", 1f); m.SetFloat("_PaveGrass", 1f);
        m.SetVector("_PaveRect", new Vector4(C.x - R - 3f, C.z - R - 3f, C.x + R + 3f, C.z + R + 3f));
        if (m.HasProperty("_PaveCircle")) m.SetVector("_PaveCircle", new Vector4(C.x, C.z, R + 1.2f, 0f));
        if (m.HasProperty("_PaveCol")) m.SetColor("_PaveCol", new Color(0.56f, 0.53f, 0.48f, 1f));
    }

    // ======================================================================================================
    // building kit: parts are merged by material, so a whole house is a handful of meshes
    class Kit
    {
        readonly Dictionary<Material, List<CombineInstance>> by = new Dictionary<Material, List<CombineInstance>>();
        public void Box(Material m, Matrix4x4 M) { Add(m, BoxMesh, M); }
        public void Add(Material m, Mesh mesh, Matrix4x4 M)
        {
            List<CombineInstance> l; if (!by.TryGetValue(m, out l)) by[m] = l = new List<CombineInstance>();
            // the part's own size goes into its mesh, with texture coordinates in metres: bricks, planks and shingles
            // keep one size on every part, however big
            Vector3 sc = M.lossyScale; Vector3 pos = M.GetColumn(3); Quaternion rot = M.rotation;
            l.Add(new CombineInstance { mesh = Bake(mesh, sc), transform = Matrix4x4.TRS(pos, rot, Vector3.one) });
        }
        public List<Renderer> Build(Transform parent, string name)
        {
            var list = new List<Renderer>();
            foreach (var kv in by)
            {
                var go = new GameObject(name + " " + kv.Key.name); go.transform.SetParent(parent, false);
                go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = kv.Key; list.Add(mr);
            }
            return list;
        }
    }
    static readonly Dictionary<string, Mesh> baked = new Dictionary<string, Mesh>();
    static readonly Dictionary<Mesh, int> meshIds = new Dictionary<Mesh, int>();
    static Mesh Bake(Mesh src, Vector3 sc)
    {
        sc = new Vector3(Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z));
        int sid; if (!meshIds.TryGetValue(src, out sid)) { sid = meshIds.Count; meshIds[src] = sid; }
        string key = sid + ":" + Mathf.RoundToInt(sc.x * 200) + "," + Mathf.RoundToInt(sc.y * 200) + "," + Mathf.RoundToInt(sc.z * 200);
        Mesh m; if (baked.TryGetValue(key, out m) && m != null) return m;
        var v = src.vertices; var n = src.normals; var t = src.triangles;
        var nv = new Vector3[v.Length]; var nn = new Vector3[v.Length]; var uv = new Vector2[v.Length];
        Vector3 inv = new Vector3(1f / Mathf.Max(1e-4f, sc.x), 1f / Mathf.Max(1e-4f, sc.y), 1f / Mathf.Max(1e-4f, sc.z));
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 q = Vector3.Scale(v[i], sc); nv[i] = q;
            Vector3 d = n.Length == v.Length ? Vector3.Scale(n[i], inv).normalized : Vector3.up; nn[i] = d;
            Vector3 a = new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z));
            uv[i] = a.x >= a.y && a.x >= a.z ? new Vector2(q.z, q.y) : a.y >= a.z ? new Vector2(q.x, q.z) : new Vector2(q.x, q.y);
        }
        m = new Mesh { name = src.name + " " + key }; m.vertices = nv; m.normals = nn; m.uv = uv; m.triangles = t; m.RecalculateBounds();
        baked[key] = m; return m;
    }

    // ---------- pictures for the surfaces, made here: stone blocks, roof shingles, planks and plaster (1 m each) ----------
    static Texture2D texBrick, texShingle, texPlank, texPlaster;
    static float Hs(int x, int y, int k) { uint h = (uint)(x * 374761393 + y * 668265263 + k * 1442695041); h = (h ^ (h >> 13)) * 1274126177u; return (h & 0xffff) / 65535f; }
    static float Vn(float x, float y, int k)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        return Mathf.Lerp(Mathf.Lerp(Hs(xi, yi, k), Hs(xi + 1, yi, k), fx), Mathf.Lerp(Hs(xi, yi + 1, k), Hs(xi + 1, yi + 1, k), fx), fy);
    }
    static Texture2D MakeTex(string name, System.Func<int, int, float> f, bool tint = false)
    {
        const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { byte b = (byte)Mathf.Clamp(f(x, y) * 255f, 0, 255); px[y * N + x] = new Color32(b, b, (byte)Mathf.Clamp(f(x, y) * (tint ? 236f : 255f), 0, 255), 255); }
        t.SetPixels32(px); t.Apply(true, true); return t;
    }
    // tileable noise with a period of 256 pixels
    static float Tn(int x, int y, float cell, int k) { int P = Mathf.RoundToInt(256f / cell); float u = x / cell, v = y / cell; int xi = Mathf.FloorToInt(u), yi = Mathf.FloorToInt(v); float fx = u - xi, fy = v - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); return Mathf.Lerp(Mathf.Lerp(Hs(xi % P, yi % P, k), Hs((xi + 1) % P, yi % P, k), fx), Mathf.Lerp(Hs(xi % P, (yi + 1) % P, k), Hs((xi + 1) % P, (yi + 1) % P, k), fx), fy); }
    static Texture2D Bricks
    {
        get
        {
            if (texBrick != null) return texBrick;
            // four courses of cut stone a metre, each course its own run of blocks (joints staggered), dark mortar
            var cuts = new List<int>[4]; var rnd = new System.Random(11);
            for (int r = 0; r < 4; r++) { cuts[r] = new List<int>(); int x = rnd.Next(0, 60); int first = x; cuts[r].Add(x); while (true) { x += 70 + rnd.Next(0, 60); if (x >= first + 256 - 50) break; cuts[r].Add(x % 256); } }
            texBrick = MakeTex("AH hollow stone", (x, y) =>
            {
                int r = y / 64, yy = y % 64; int bi = 0, dist = 999;
                foreach (var c in cuts[r]) { int d = Mathf.Abs(x - c); d = Mathf.Min(d, 256 - d); dist = Mathf.Min(dist, d); }
                foreach (var c in cuts[r]) if (((x - c) % 256 + 256) % 256 < 256) bi += x >= c ? 1 : 0;
                float edge = Mathf.Min(Mathf.Min(yy, 63 - yy), dist);
                if (edge < 2.5f) return 0.42f + Tn(x, y, 8, 3) * 0.06f;
                float shade = 0.74f + Hs(r, bi, 5) * 0.2f;
                float bevel = edge < 6f ? (yy < 6 ? 0.08f : -0.06f) * (1f - edge / 6f) : 0f;
                return shade + bevel + (Tn(x, y, 16, 7) - 0.5f) * 0.12f + (Tn(x, y, 4, 9) - 0.5f) * 0.06f;
            });
            return texBrick;
        }
    }
    static Texture2D Shingles
    {
        get
        {
            if (texShingle != null) return texShingle;
            texShingle = MakeTex("AH hollow shingles", (x, y) =>
            {
                int r = y / 32, yy = y % 32; int off = (r % 2) * 20; int xx = ((x + off) % 256 + 256) % 256; int ti = xx / 40, tx = xx % 40;
                if (xx >= 240) { ti = 6; tx = xx - 240; }
                float gap = tx < 2 ? 0.45f : 1f;
                float lip = yy < 5 ? 0.62f + yy * 0.05f : 1f;   // the shadow under the row above
                float sh = 0.72f + Hs(r, ti, 13) * 0.22f + (Tn(x, y, 8, 15) - 0.5f) * 0.1f;
                return sh * gap * lip;
            });
            return texShingle;
        }
    }
    static Texture2D Planks
    {
        get
        {
            if (texPlank != null) return texPlank;
            texPlank = MakeTex("AH hollow planks", (x, y) =>
            {
                int r = y / 51; if (r > 4) r = 4; int yy = y - r * 51;
                if (yy < 2) return 0.38f;
                int joint = (int)(Hs(r, 1, 21) * 256f); int dj = Mathf.Abs(x - joint); dj = Mathf.Min(dj, 256 - dj);
                if (dj < 1) return 0.42f;
                float grain = Mathf.Sin((y * 0.9f + Tn(x, y, 32, 23) * 18f) * 0.8f) * 0.05f + (Tn(x, y * 6 % 256, 64, 25) - 0.5f) * 0.12f;
                return 0.78f + Hs(r, 2, 27) * 0.16f + grain;
            }, true);
            return texPlank;
        }
    }
    static Texture2D Plaster
    {
        get
        {
            if (texPlaster != null) return texPlaster;
            texPlaster = MakeTex("AH hollow plaster", (x, y) => 0.9f + (Tn(x, y, 64, 31) - 0.5f) * 0.08f + (Tn(x, y, 8, 33) - 0.5f) * 0.05f + (Hs(x, y, 35) < 0.01f ? -0.06f : 0f));
            return texPlaster;
        }
    }
    static Material TexMat(string key, Color c, Texture2D t, float smooth = 0.12f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = Mat(key, c, smooth); m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", Vector2.one); return m;
    }
    static Mesh boxM, cylM, prismM, coneM, sphM;
    static Mesh SphMesh { get { if (sphM == null) sphM = Prim(PrimitiveType.Sphere); return sphM; } }
    static Mesh BoxMesh { get { if (boxM == null) boxM = Prim(PrimitiveType.Cube); return boxM; } }
    static Mesh CylMesh { get { if (cylM == null) cylM = Prim(PrimitiveType.Cylinder); return cylM; } }
    static Mesh Prim(PrimitiveType t) { var go = GameObject.CreatePrimitive(t); var m = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh); Object.Destroy(go); return m; }
    // a triangle prism: base 1 deep along z (centred), 1 high to the apex, 1 thick along x
    static Mesh Prism
    {
        get
        {
            if (prismM != null) return prismM;
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 a0 = new Vector3(-0.5f, 0, -0.5f), b0 = new Vector3(-0.5f, 0, 0.5f), c0 = new Vector3(-0.5f, 1, 0), a1 = new Vector3(0.5f, 0, -0.5f), b1 = new Vector3(0.5f, 0, 0.5f), c1 = new Vector3(0.5f, 1, 0);
            System.Action<Vector3, Vector3, Vector3> T = (p, q, r) => { int i = v.Count; v.Add(p); v.Add(q); v.Add(r); t.Add(i); t.Add(i + 1); t.Add(i + 2); };
            System.Action<Vector3, Vector3, Vector3, Vector3> Q = (p, q, r, s) => { T(p, q, r); T(p, r, s); };
            T(a0, c0, b0); T(a1, b1, c1);
            Q(a0, a1, c1, c0); Q(b0, c0, c1, b1); Q(a0, b0, b1, a1);
            prismM = new Mesh { name = "Gable" }; prismM.SetVertices(v); prismM.SetTriangles(t, 0); prismM.RecalculateNormals(); prismM.RecalculateBounds();
            return prismM;
        }
    }
    // a cone 1 wide, 1 high, standing on y = 0
    static Mesh Cone
    {
        get
        {
            if (coneM != null) return coneM;
            int n = 12; var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n, b = (i + 1) * Mathf.PI * 2f / n;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f), q = new Vector3(Mathf.Cos(b) * 0.5f, 0, Mathf.Sin(b) * 0.5f);
                int k = v.Count; v.Add(p); v.Add(new Vector3(0, 1, 0)); v.Add(q); t.Add(k); t.Add(k + 1); t.Add(k + 2);
                k = v.Count; v.Add(p); v.Add(q); v.Add(Vector3.zero); t.Add(k); t.Add(k + 1); t.Add(k + 2);
            }
            coneM = new Mesh { name = "Cone" }; coneM.SetVertices(v); coneM.SetTriangles(t, 0); coneM.RecalculateNormals(); coneM.RecalculateBounds();
            return coneM;
        }
    }

    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.15f, float glow = 0f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f); m.enableInstancing = true;
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * glow); }
        mats[key] = m; return m;
    }
    static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString("#" + h, out c); return c; }
    static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s) { return Matrix4x4.TRS(p, q, s); }

    // ======================================================================================================
    // ---------- the wall, its towers and the gatehouses ----------
    static void Walls(AHGame g)
    {
        var kit = new Kit();
        Material stone = TexMat("hw_stone", new Color(0.78f, 0.73f, 0.66f), Bricks), stone2 = TexMat("hw_stone_dark", new Color(0.6f, 0.57f, 0.52f), Bricks),
                 wood = TexMat("hw_beam", new Color(0.45f, 0.31f, 0.19f), Planks);
        int n = Mathf.RoundToInt(2f * Mathf.PI * R / 4f); float step = Mathf.PI * 2f / n, chord = 2f * R * Mathf.Sin(step / 2f);
        for (int i = 0; i < n; i++)
        {
            float a = (i + 0.5f) * step;
            if (InGate(a, chord / 2f + 0.2f)) continue;
            Vector3 dir = Dir(a), tan = Vector3.Cross(Vector3.up, dir);
            var q = Quaternion.LookRotation(tan, Vector3.up);
            kit.Box(stone, TRS(C + dir * R + Vector3.up * WallH / 2f, q, new Vector3(WallT, WallH, chord + 0.12f)));
            kit.Box(stone2, TRS(C + dir * R + Vector3.up * 0.35f, q, new Vector3(WallT + 0.3f, 0.7f, chord + 0.14f)));   // the footing
            // battlements on the outer edge, and a lower parapet on the inner edge
            for (int k = 0; k < 2; k++)
            {
                float off = (k - 0.5f) * chord * 0.5f;
                kit.Box(stone, TRS(C + dir * (R + WallT / 2f - 0.25f) + tan * off + Vector3.up * (WallH + 0.35f), q, new Vector3(0.5f, 0.7f, chord * 0.28f)));
            }
            kit.Box(stone2, TRS(C + dir * (R - WallT / 2f + 0.15f) + Vector3.up * (WallH + 0.2f), q, new Vector3(0.3f, 0.4f, chord + 0.1f)));
        }
        // towers: either side of each gate, and between, about every 45 degrees
        var towers = new List<float>();
        foreach (var s in streets) if (s.gate) { float da = (s.hw + 2.6f) / R; towers.Add(s.a - da); towers.Add(s.a + da); }
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + Mathf.PI / 8f; bool near = false;
            foreach (var t in towers) if (Mathf.Abs(Mathf.DeltaAngle(t * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) < 22f) near = true;
            if (!near) towers.Add(a);
        }
        string col; if (!AHCity.ColourOf(Area, out col)) col = "green";
        int ti = 0;
        foreach (var a in towers) Tower(C + Dir(a) * R, -Dir(a), (ti++ % 3 == 1 ? "kk_building_tower_B_" : "kk_building_tower_A_") + col);
        // gatehouses: an arch over each road in, with its name
        foreach (var s in streets)
        {
            if (!s.gate) continue;
            Vector3 dir = Dir(s.a), tan = Vector3.Cross(Vector3.up, dir), at = C + dir * R;
            var q = Quaternion.LookRotation(tan, Vector3.up);
            kit.Box(stone, TRS(at + Vector3.up * (WallH + 0.9f), q, new Vector3(WallT + 0.4f, 1.8f, s.hw * 2f + 1.6f)));
            kit.Box(wood, TRS(at + Vector3.up * (WallH - 0.1f), q, new Vector3(WallT + 0.5f, 0.3f, s.hw * 2f + 0.2f)));
            for (int k = 0; k < 5; k++)
            {
                float off = (k - 2) * (s.hw * 2f + 1.2f) / 5f;
                kit.Box(stone, TRS(at + tan * off + dir * 0.4f + Vector3.up * (WallH + 2.15f), q, new Vector3(0.5f, 0.7f, 0.9f)));
            }
            // a gatehouse storey over the arch (the portcullis rises into it), with arrow slits and battlements
            kit.Box(stone, TRS(at + Vector3.up * (WallH + 3.0f), q, new Vector3(WallT + 0.2f, 2.4f, s.hw * 2f + 1.2f)));
            kit.Box(stone2, TRS(at + Vector3.up * (WallH + 4.25f), q, new Vector3(WallT + 0.5f, 0.25f, s.hw * 2f + 1.5f)));
            for (int k = 0; k < 7; k++)
            {
                float off = (k - 3) * (s.hw * 2f + 1.0f) / 7f;
                foreach (var sd in new[] { -1f, 1f }) kit.Box(stone, TRS(at + tan * off + dir * sd * (WallT / 2f) + Vector3.up * (WallH + 4.7f), q, new Vector3(0.35f, 0.7f, 0.85f)));
            }
            var slit = Mat("hw_slit", new Color(0.06f, 0.05f, 0.05f));
            foreach (var sd in new[] { -1f, 1f }) foreach (var off in new[] { -s.hw * 0.55f, s.hw * 0.55f }) kit.Box(slit, TRS(at + tan * off + dir * sd * (WallT / 2f + 0.11f) + Vector3.up * (WallH + 3.1f), q, new Vector3(0.04f, 1.0f, 0.22f)));
            Portcullis(s, at, dir, tan);
            // the town's name over the road in, and which gate it is over the road out
            Sign(root, at - dir * (WallT / 2f + 0.3f) + Vector3.up * (WallH + 0.9f), -dir, s.label, Hex("ffd27a"), s.hw * 2f, 1.1f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
            Sign(root, at + dir * (WallT / 2f + 0.3f) + Vector3.up * (WallH + 0.9f), dir, "Hollow Meadow", Hex("ffd27a"), s.hw * 2f, 1.1f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
            Banner(at + tan * (s.hw + 2.6f) - dir * 2.45f, -dir);
            Banner(at - tan * (s.hw + 2.6f) - dir * 2.45f, -dir);
        }
        var rs = kit.Build(root, "AH_TownWall");
        foreach (var r in rs) AHModel.SetShadows(r.gameObject);
    }
    // a proper stone tower (KayKit) standing astride the wall, at least five metres across
    static void Tower(Vector3 at, Vector3 face, string model)
    {
        var go = Prop(model, root, at, 11f, face); if (go == null) return;
        go.name = "AH_Wall tower";
        var b = Measure(go); float foot = Mathf.Min(b.size.x, b.size.z);
        if (foot < 5.4f) { go.transform.localScale *= 5.4f / Mathf.Max(0.5f, foot); b = Measure(go); go.transform.position += new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z); }
    }
    static bool InGate(float a, float pad)
    {
        foreach (var s in streets) if (s.gate && Mathf.Abs(Mathf.DeltaAngle(s.a * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) * Mathf.Deg2Rad * R < s.hw + pad) return true;
        return false;
    }
    // an iron portcullis filling the gateway, built standing on the road; it hangs raised (AHTownLife lowers it at night)
    static void Portcullis(Street s, Vector3 at, Vector3 dir, Vector3 tan)
    {
        var gate = new Gate { at = at, dir = dir, tan = tan, hw = s.hw };
        var gp = new GameObject("AH_Portcullis").transform; gp.SetParent(root, false); gp.position = Vector3.zero;
        var kit = new Kit(); var iron = Mat("hw_portcullis", new Color(0.2f, 0.19f, 0.19f), 0.45f); var rust = Mat("hw_rivet", new Color(0.32f, 0.22f, 0.15f), 0.3f);
        var q = Quaternion.LookRotation(dir, Vector3.up);   // local x: across the road, z: through the wall
        float W = s.hw * 2f + 0.2f, Hh = WallH - 0.25f;
        int nb = Mathf.RoundToInt(W / 0.55f);
        for (int i = 0; i <= nb; i++)
        {
            float x = -W / 2f + i * W / nb;
            kit.Box(iron, TRS(at + tan * x + Vector3.up * (0.3f + (Hh - 0.3f) / 2f), q, new Vector3(0.1f, Hh - 0.3f, 0.12f)));
            kit.Add(iron, Cone, TRS(at + tan * x + Vector3.up * 0.32f, q * Quaternion.Euler(180f, 0, 0), new Vector3(0.13f, 0.32f, 0.13f)));
        }
        foreach (var y in new[] { 0.55f, 1.45f, 2.35f, 3.25f, Hh - 0.2f })
        {
            kit.Box(iron, TRS(at + Vector3.up * y, q, new Vector3(W, 0.11f, 0.16f)));
            for (int i = 0; i <= nb; i++) kit.Add(rust, SphMesh, TRS(at + tan * (-W / 2f + i * W / nb) + Vector3.up * y, q, new Vector3(0.11f, 0.11f, 0.2f)));
        }
        foreach (var r in kit.Build(gp, "AH_PortcullisGrid")) AHModel.SetShadows(r.gameObject);
        gp.position = Vector3.up * (Hh - 0.35f);   // raised: only its spikes show under the arch
        gate.grid = gp; Gates.Add(gate);
    }

    // the bell tower: a stone tower with a clock and an open belfry; the bell swings and rings the hours (AHTownLife)
    static void BellTower(Slot s)
    {
        var go = new GameObject("AH_Bell tower").transform; go.SetParent(root, false);
        Vector3 c = s.O + s.b * (s.d * 0.5f); c.y = gy; BellAt = c;
        Vector3 f = -s.b, rgt = Vector3.Cross(Vector3.up, f);
        var q = Quaternion.LookRotation(f, Vector3.up);
        Material stone = TexMat("hw_stone", new Color(0.78f, 0.73f, 0.66f), Bricks), stone2 = TexMat("hw_stone_dark", new Color(0.6f, 0.57f, 0.52f), Bricks),
                 timber = TexMat("hw_timber", new Color(0.42f, 0.28f, 0.17f), Planks), roof = TexMat("hw_roof_bell", new Color(0.42f, 0.2f, 0.16f), Shingles, 0.18f),
                 dark = Mat("hw_slit", new Color(0.06f, 0.05f, 0.05f)), bronze = Mat("hw_bronze", new Color(0.78f, 0.55f, 0.28f), 0.65f), gold = Mat("hw_gold", new Color(1f, 0.8f, 0.35f), 0.7f, 0.25f),
                 face = Mat("hw_clockface", new Color(0.95f, 0.92f, 0.84f), 0.3f);
        const float Wd = 3.8f, Sh = 9f, Bel = 3.0f;
        var kit = new Kit();
        kit.Box(stone2, TRS(c + Vector3.up * 0.4f, q, new Vector3(Wd + 0.6f, 0.8f, Wd + 0.6f)));
        kit.Box(stone, TRS(c + Vector3.up * Sh / 2f, q, new Vector3(Wd, Sh, Wd)));
        kit.Box(stone2, TRS(c + Vector3.up * (Sh + 0.15f), q, new Vector3(Wd + 0.4f, 0.3f, Wd + 0.4f)));
        // the door, and windows up the shaft
        kit.Box(timber, TRS(c + f * (Wd / 2f + 0.02f) + Vector3.up * 1.15f, q, new Vector3(1.3f, 2.3f, 0.06f)));
        kit.Box(stone2, TRS(c + f * (Wd / 2f + 0.05f) + Vector3.up * 2.4f, q, new Vector3(1.7f, 0.25f, 0.12f)));
        foreach (var y in new[] { 4.2f }) foreach (var dd in new[] { rgt, -rgt, -f }) kit.Box(dark, TRS(c + dd * (Wd / 2f + 0.02f) + Vector3.up * y, Quaternion.LookRotation(dd), new Vector3(0.5f, 1.1f, 0.04f)));
        // the open belfry: four corner piers, a rail, the beam the bell hangs from
        foreach (var x in new[] { -1f, 1f }) foreach (var z in new[] { -1f, 1f })
                kit.Box(stone, TRS(c + rgt * x * (Wd / 2f - 0.3f) + f * z * (Wd / 2f - 0.3f) + Vector3.up * (Sh + 0.3f + Bel / 2f), q, new Vector3(0.6f, Bel, 0.6f)));
        foreach (var dd in new[] { f, -f, rgt, -rgt }) kit.Box(timber, TRS(c + dd * (Wd / 2f - 0.3f) + Vector3.up * (Sh + 1.2f), Quaternion.LookRotation(dd), new Vector3(Wd - 1.0f, 0.12f, 0.12f)));
        kit.Box(timber, TRS(c + Vector3.up * (Sh + Bel - 0.1f), q, new Vector3(Wd - 0.4f, 0.3f, 0.3f)));
        kit.Box(stone2, TRS(c + Vector3.up * (Sh + 0.3f + Bel + 0.15f), q, new Vector3(Wd + 0.3f, 0.3f, Wd + 0.3f)));
        // a pointed roof with a gold finial
        kit.Add(roof, Pyramid, TRS(c + Vector3.up * (Sh + 0.3f + Bel + 0.3f), q, new Vector3(Wd + 0.8f, 3.2f, Wd + 0.8f)));
        kit.Add(gold, SphMesh, TRS(c + Vector3.up * (Sh + Bel + 3.85f), q, Vector3.one * 0.32f));
        kit.Box(gold, TRS(c + Vector3.up * (Sh + Bel + 4.25f), q, new Vector3(0.06f, 0.7f, 0.06f)));
        // the clock face over the door
        Vector3 cc = c + f * (Wd / 2f + 0.06f) + Vector3.up * 7.2f;
        kit.Add(face, SphMesh, TRS(cc, q, new Vector3(1.8f, 1.8f, 0.08f)));
        kit.Add(gold, SphMesh, TRS(cc - f * 0.015f, q, new Vector3(1.95f, 1.95f, 0.05f)));
        for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f; kit.Box(dark, TRS(cc + f * 0.05f + (rgt * Mathf.Sin(a) + Vector3.up * Mathf.Cos(a)) * 0.74f, q * Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg), new Vector3(0.05f, i % 3 == 0 ? 0.2f : 0.1f, 0.02f))); }
        foreach (var r in kit.Build(go, "AH_BellTower")) AHModel.SetShadows(r.gameObject);
        Sign(go, c + f * (Wd / 2f + 0.12f) + Vector3.up * 2.9f, f, "Bell Tower", Hex("ffd27a"), Wd - 0.4f, 0.7f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
        // the hands, each on its own pivot at the middle of the face
        foreach (var hand in new[] { 0, 1 })
        {
            var pv = new GameObject(hand == 0 ? "Clock hour" : "Clock minute").transform; pv.SetParent(go, false); pv.position = cc + f * 0.07f; pv.rotation = q;
            var hk = new Kit(); float len = hand == 0 ? 0.45f : 0.68f;
            hk.Box(dark, TRS(cc + f * 0.07f + Vector3.up * len / 2f, q, new Vector3(hand == 0 ? 0.08f : 0.05f, len, 0.03f)));
            var hp = new GameObject("hand").transform; hp.SetParent(pv, true); hp.position = Vector3.zero; hp.rotation = Quaternion.identity;
            hk.Build(hp, "AH_ClockHand");
            if (hand == 0) ClockHour = pv; else ClockMin = pv;
        }
        // the bell, hung on its own pivot so it can swing
        var bp = new GameObject("Bell pivot").transform; bp.SetParent(go, false); bp.position = c + Vector3.up * (Sh + Bel - 0.25f); bp.rotation = q;
        var bk = new Kit();
        bk.Add(bronze, BellMesh, TRS(bp.position - Vector3.up * 1.25f, q, new Vector3(1.3f, 1.2f, 1.3f)));
        bk.Box(timber, TRS(bp.position - Vector3.up * 0.06f, q, new Vector3(0.9f, 0.16f, 0.22f)));
        bk.Add(bronze, SphMesh, TRS(bp.position - Vector3.up * 1.2f, q, new Vector3(0.16f, 0.16f, 0.16f)));   // the clapper
        bk.Box(Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f), TRS(bp.position - Vector3.up * 0.75f, q, new Vector3(0.04f, 0.8f, 0.04f)));
        var bh = new GameObject("bell").transform; bh.SetParent(bp, true); bh.position = Vector3.zero; bh.rotation = Quaternion.identity;
        foreach (var r in bk.Build(bh, "AH_Bell")) AHModel.SetShadows(r.gameObject);
        Bell = bp;
        g0.AddBlocker(new Vector3(c.x, 0f, c.z), Wd * 0.62f);
    }
    static AHGame g0;
    // a bell's shape turned on a lathe: open at the mouth, a lip that flares out, a rounded crown
    static Mesh bellM;
    static Mesh BellMesh
    {
        get
        {
            if (bellM != null) return bellM;
            Vector2[] pr = { new Vector2(0f, 1f), new Vector2(0.16f, 0.99f), new Vector2(0.27f, 0.93f), new Vector2(0.31f, 0.8f), new Vector2(0.33f, 0.6f), new Vector2(0.37f, 0.4f), new Vector2(0.44f, 0.2f), new Vector2(0.5f, 0.07f), new Vector2(0.52f, 0.02f), new Vector2(0.47f, 0f), new Vector2(0.42f, 0.06f), new Vector2(0.3f, 0.35f), new Vector2(0.25f, 0.8f), new Vector2(0f, 0.9f) };
            int n = 24; var v = new List<Vector3>(); var t = new List<int>();
            for (int j = 0; j < pr.Length; j++) for (int i = 0; i <= n; i++) { float a = i * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a) * pr[j].x, pr[j].y, Mathf.Sin(a) * pr[j].x)); }
            for (int j = 0; j < pr.Length - 1; j++) for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c2 = a + n + 1, d2 = c2 + 1;
                    t.Add(a); t.Add(b); t.Add(c2); t.Add(b); t.Add(d2); t.Add(c2);
                }
            bellM = new Mesh { name = "Bell" }; bellM.SetVertices(v); bellM.SetTriangles(t, 0); bellM.RecalculateNormals(); bellM.RecalculateBounds();
            return bellM;
        }
    }
    static Mesh pyrM;
    static Mesh Pyramid   // 1 wide, 1 deep, 1 high to the point, standing on y = 0
    {
        get
        {
            if (pyrM != null) return pyrM;
            var v = new List<Vector3>(); var t = new List<int>(); Vector3 top = new Vector3(0, 1, 0);
            Vector3[] b = { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            for (int i = 0; i < 4; i++) { int k = v.Count; v.Add(b[i]); v.Add(top); v.Add(b[(i + 1) % 4]); t.Add(k); t.Add(k + 1); t.Add(k + 2); }
            { int k = v.Count; v.Add(b[0]); v.Add(b[1]); v.Add(b[2]); v.Add(b[3]); t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k); t.Add(k + 2); t.Add(k + 3); }
            pyrM = new Mesh { name = "Pyramid" }; pyrM.SetVertices(v); pyrM.SetTriangles(t, 0); pyrM.RecalculateNormals(); pyrM.RecalculateBounds();
            return pyrM;
        }
    }

    // a house plot for sale: a fenced patch of dug earth with a sign (AHTownLife builds your house on it)
    static void HousePlot(Slot s)
    {
        var h = new Hall { id = "plot", name = "House plot", col = Hex("9fe07a"), O = s.O, b = s.b, r = s.r, w = s.w, d = s.d };
        h.O.y = gy; Plot = h;
        var go = new GameObject("AH_House plot").transform; go.SetParent(root, false); h.go = go;
        var kit = new Kit(); var q = Quaternion.LookRotation(h.b, Vector3.up);
        Material soil = TexMat("hw_soilplot", new Color(0.42f, 0.3f, 0.2f), Planks, 0.05f), post = DarkWood, rail = Wood, grass = Mat("hw_plotgrass", new Color(0.36f, 0.52f, 0.26f), 0.05f);
        kit.Box(soil, TRS(h.At(0, h.d / 2f) + Vector3.up * 0.03f, q, new Vector3(h.w - 0.4f, 0.06f, h.d - 0.4f)));
        var rnd = new System.Random(77);
        for (int i = 0; i < 22; i++) kit.Add(grass, SphMesh, TRS(h.At(((float)rnd.NextDouble() - 0.5f) * (h.w - 1f), 0.3f + (float)rnd.NextDouble() * (h.d - 0.8f)) + Vector3.up * 0.05f, q, new Vector3(0.35f, 0.12f, 0.3f)));
        // the fence: posts every two metres with two rails, open in the front middle
        System.Action<Vector3, Vector3> Run = (a, b) =>
        {
            float len = Vector3.Distance(a, b); int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f)); Vector3 u = (b - a).normalized; var rq = Quaternion.LookRotation(u, Vector3.up);
            for (int i = 0; i <= n; i++) { kit.Box(post, TRS(a + u * (len * i / n) + Vector3.up * 0.55f, rq, new Vector3(0.14f, 1.1f, 0.14f))); kit.Box(post, TRS(a + u * (len * i / n) + Vector3.up * 1.13f, rq, new Vector3(0.18f, 0.06f, 0.18f))); }
            foreach (var y in new[] { 0.45f, 0.9f }) kit.Box(rail, TRS((a + b) / 2f + Vector3.up * y, rq, new Vector3(0.07f, 0.1f, len)));
        };
        float hw = h.w / 2f - 0.15f, dd = h.d - 0.15f;
        Run(h.At(-hw, 0.15f), h.At(-1.6f, 0.15f)); Run(h.At(1.6f, 0.15f), h.At(hw, 0.15f));
        Run(h.At(-hw, 0.15f), h.At(-hw, dd)); Run(h.At(hw, 0.15f), h.At(hw, dd)); Run(h.At(-hw, dd), h.At(hw, dd));
        foreach (var r in kit.Build(go, "AH_PlotFence")) AHModel.SetShadows(r.gameObject);
        // the sign on two posts, in the middle of the plot
        var sg = new GameObject("AH_ForSale").transform; sg.SetParent(go, false);
        var sk = new Kit(); Vector3 sp = h.At(0, h.d * 0.45f);
        foreach (var x in new[] { -0.9f, 0.9f }) sk.Box(post, TRS(sp + h.r * x * 1.3f + Vector3.up * 1.0f, q, new Vector3(0.12f, 2.0f, 0.12f)));
        foreach (var r in sk.Build(sg, "AH_ForSalePosts")) AHModel.SetShadows(r.gameObject);
        Sign(sg, sp + Vector3.up * 1.75f - h.b * 0.08f, -h.b, "Plot for sale", Hex("9fe07a"), 3.2f, 0.85f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
        PlotForSale = sg.gameObject;
    }
    // a clear spot on the square near a compass bearing (off the streets, away from stalls, people, the fountain, the boards)
    public static Vector3 FreeSpot(float deg, float r, float clear)
    {
        var busy = new List<Vector3>();
        foreach (Transform t in root) if (t.name == "AH_Stall" || t.name == "AH_JobsBoard") busy.Add(t.position);
        var bb = GameObject.Find("BountyBoard"); if (bb != null) busy.Add(bb.transform.position);
        if (FountainAt != Vector3.zero) busy.Add(FountainAt);
        foreach (var n in AHNpc.All) if (n != null) busy.Add(n.transform.position);
        for (int i = 0; i <= 30; i++)
        {
            float a = (deg + (i % 2 == 0 ? 1 : -1) * (i / 2) * 6f) * Mathf.Deg2Rad; Vector3 p = C + Dir(a) * r;
            if (OnStreet(p, 1.0f) || NearKeep(p, 1.2f)) continue;
            bool bad = false; foreach (var q in busy) { Vector3 dd = q - p; dd.y = 0f; if (dd.magnitude < clear + (q == FountainAt ? 1.6f : 0f)) { bad = true; break; } }
            if (!bad) return p;
        }
        return C + Dir(deg * Mathf.Deg2Rad) * r;
    }
    // the town jobs board: two posts, a planked board under a little roof, notes pinned on it
    public static GameObject JobsBoard(Vector3 at, Vector3 face)
    {
        face.y = 0f; face.Normalize(); var q = Quaternion.LookRotation(face, Vector3.up); var rgt = Vector3.Cross(Vector3.up, face);
        var go = new GameObject("AH_JobsBoard"); go.transform.SetParent(root, false); go.transform.position = at;
        var kit = new Kit(); Material paper = Mat("hw_paper", new Color(0.95f, 0.91f, 0.8f), 0.1f), pin = Mat("hw_pin", new Color(0.8f, 0.2f, 0.15f), 0.5f),
                 roof = TexMat("hw_roof_jobs", new Color(0.3f, 0.42f, 0.28f), Shingles, 0.18f);
        foreach (var x in new[] { -0.95f, 0.95f }) kit.Box(DarkWood, TRS(at + rgt * x + Vector3.up * 1.25f, q, new Vector3(0.14f, 2.5f, 0.14f)));
        kit.Box(Wood, TRS(at + Vector3.up * 1.45f, q, new Vector3(1.85f, 1.15f, 0.08f)));
        kit.Box(DarkWood, TRS(at + Vector3.up * 0.86f, q, new Vector3(1.95f, 0.08f, 0.16f)));
        kit.Add(roof, Prism, TRS(at + Vector3.up * 2.42f, q * Quaternion.Euler(0, 90f, 0), new Vector3(2.3f, 0.45f, 0.75f)));
        var rnd = new System.Random(11);
        for (int i = 0; i < 6; i++)
        {
            float x = -0.6f + (i % 3) * 0.6f + ((float)rnd.NextDouble() - 0.5f) * 0.12f, y = 1.72f - (i / 3) * 0.52f;
            kit.Box(paper, TRS(at + rgt * x + Vector3.up * y + face * 0.046f, q * Quaternion.Euler(0, 0, ((float)rnd.NextDouble() - 0.5f) * 10f), new Vector3(0.42f, 0.4f, 0.01f)));
            kit.Add(pin, SphMesh, TRS(at + rgt * x + Vector3.up * (y + 0.15f) + face * 0.065f, q, Vector3.one * 0.05f));
        }
        foreach (var r in kit.Build(go.transform, "AH_JobsBoardParts")) AHModel.SetShadows(r.gameObject);
        Sign(go.transform, at + Vector3.up * 2.12f + face * 0.06f, face, "Town Jobs", Hex("9fe07a"), 1.7f, 0.32f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
        g0.AddBlocker(new Vector3(at.x, 0f, at.z), 0.9f);
        return go;
    }
    // a sign anywhere in the town (AHTownLife: your house's name)
    public static void TownSign(Transform parent, Vector3 at, Vector3 face, string text, string hex) { Sign(parent ?? root, at, face, text, Hex(hex), Mathf.Max(2.6f, text.Length * 0.32f), 0.65f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f))); }
    public static GameObject TownProp(string id, Transform parent, Vector3 at, float size, Vector3 face) { return Prop(id, parent ?? root, at, size, face); }
    // a planked display bench (your house's trophies stand on it; its top at 1.04 m): returns the bench's own frame
    public static Transform DisplayStand(Transform parent, Vector3 at, Vector3 face, float len)
    {
        face.y = 0f; face.Normalize(); var q = Quaternion.LookRotation(face, Vector3.up); var side = Vector3.Cross(Vector3.up, face);
        var t = new GameObject("AH_DisplayStand").transform; t.SetParent(parent ?? root, false); t.position = at; t.rotation = q;
        var kit = new Kit(); var st = TexMat("hw_base", new Color(0.7f, 0.66f, 0.6f), Bricks);
        kit.Box(st, TRS(at + Vector3.up * 0.48f, q, new Vector3(len, 0.96f, 0.62f)));
        kit.Box(DarkWood, TRS(at + Vector3.up * 1.0f, q, new Vector3(len + 0.12f, 0.08f, 0.74f)));
        foreach (Renderer r in kit.Build(t, "AH_DisplayBench")) AHModel.SetShadows(r.gameObject);
        return t;
    }
    public static string TownColour { get { string col; return AHCity.ColourOf(Area, out col) ? col : "green"; } }

    static void Banner(Vector3 at, Vector3 face)
    {
        var go = Prop("kk_dg_banner_blue", root, at + Vector3.up * 3.2f, 2.6f, face);
        if (go != null) go.name = "Banner";
    }

    // a KayKit prop, sized to 'size' (its largest side), standing at 'at' facing 'face'
    static GameObject Prop(string id, Transform parent, Vector3 at, float size, Vector3 face)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/" + id); if (pf == null) return null;
        var go = Object.Instantiate(pf, parent, false); go.name = "AH_prop " + id;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        face.y = 0f; go.transform.rotation = face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face.normalized) : Quaternion.identity;
        go.transform.position = Vector3.zero; go.transform.localScale = Vector3.one;
        var b = Measure(go); float k = size / Mathf.Max(0.05f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
        go.transform.localScale = Vector3.one * k; b = Measure(go);
        go.transform.position = new Vector3(at.x - b.center.x, at.y - b.min.y, at.z - b.center.z);
        AHModel.SetShadows(go);
        return go;
    }
    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
    static GameObject Place(GameObject go, Vector3 at, Vector3 face)
    {
        if (go == null) return null;
        face.y = 0f; go.transform.position = at; go.transform.rotation = face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face.normalized) : Quaternion.identity;
        AHModel.SetShadows(go);
        foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("flame")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // ---------- signs: big letters in the building's colour on a dark board with a coloured rim ----------
    static Font font; static Material textMat;
    static Material TextMat()
    {
        if (font == null) font = Resources.Load<Font>("AH/Fonts/Nunito-ExtraBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (textMat != null) return textMat;
        var sh = Shader.Find("AshenHollow/Text3D"); if (sh == null) return font.material;
        textMat = new Material(sh) { name = "Town sign letters" }; textMat.mainTexture = font.material.mainTexture;
        Font.textureRebuilt += f => { if (f == font && textMat != null) textMat.mainTexture = f.material.mainTexture; };
        return textMat;
    }
    // at: the middle of the board's face; face: the way the words face (towards the reader)
    static void Sign(Transform parent, Vector3 at, Vector3 face, string text, Color col, float maxW, float h, Material board)
    {
        face.y = 0f; face.Normalize();
        var sp = new GameObject("Sign " + text).transform; sp.SetParent(parent, false);
        sp.position = at; sp.rotation = Quaternion.LookRotation(-face);   // the board's back is towards +z
        float bw = Mathf.Min(maxW, text.Length * h * 0.36f + h * 1.2f);
        var kit = new Kit();
        kit.Box(board, sp.localToWorldMatrix * TRS(new Vector3(0, 0, 0.06f), Quaternion.identity, new Vector3(bw, h, 0.12f)));
        var rim = Mat("hw_rim_" + ColorUtility.ToHtmlStringRGB(col), col, 0.3f, 0.6f);
        kit.Box(rim, sp.localToWorldMatrix * TRS(new Vector3(0, h / 2f, 0.02f), Quaternion.identity, new Vector3(bw + 0.12f, 0.08f, 0.1f)));
        kit.Box(rim, sp.localToWorldMatrix * TRS(new Vector3(0, -h / 2f, 0.02f), Quaternion.identity, new Vector3(bw + 0.12f, 0.08f, 0.1f)));
        kit.Box(rim, sp.localToWorldMatrix * TRS(new Vector3(bw / 2f, 0, 0.02f), Quaternion.identity, new Vector3(0.08f, h + 0.12f, 0.1f)));
        kit.Box(rim, sp.localToWorldMatrix * TRS(new Vector3(-bw / 2f, 0, 0.02f), Quaternion.identity, new Vector3(0.08f, h + 0.12f, 0.1f)));
        kit.Build(sp, "AH_SignBoard");
        var t = new GameObject("Sign text").AddComponent<TextMesh>(); t.transform.SetParent(sp, false);
        var tm = TextMat(); t.font = font; t.GetComponent<MeshRenderer>().sharedMaterial = tm;
        t.text = "<color=#" + ColorUtility.ToHtmlStringRGB(col * 0.82f) + ">" + text + "</color>"; t.richText = true; t.fontSize = 120; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = col;
        t.characterSize = Mathf.Min(h * 0.055f, (bw - h * 0.5f) / Mathf.Max(1, text.Length) * 0.105f);
        t.transform.localPosition = new Vector3(0, 0.02f, -0.015f);
    }

    // ======================================================================================================
    // ---------- the square ----------
    static void Square(AHGame g)
    {
        // the fountain where the old well stood, north of the middle
        var f = AHStations.Fountain(root, 2.6f); Place(f, L(0f, -8f), S);
        f.name = "AH_Fountain"; FountainAt = L(0f, -8f);
        // market stalls round the square, clear of the streets and the people who stand there
        int k = 0;
        foreach (float deg in new[] { 200f, 225f, 340f, 125f, 100f, 20f })
        {
            float a = deg * Mathf.Deg2Rad; Vector3 at = C + Dir(a) * 11f;
            if (OnStreet(at, 1.6f) || NearKeep(at, 3f)) continue;
            var st = AHStations.Stall(root, 2.2f, 1.5f, 11 + k); Place(st, at, C - at); st.name = "AH_Stall"; k++;
            if (k >= 4) break;
        }
    }
    static bool OnStreet(Vector3 p, float pad)
    {
        Vector2 l = Loc(p);
        foreach (var s in streets)
        {
            Vector2 u = new Vector2(Mathf.Cos(s.a), Mathf.Sin(s.a)); float t = Vector2.Dot(l, u);
            if (t < 0f) continue;
            float lat = Mathf.Abs(l.x * u.y - l.y * u.x); if (lat < s.hw + pad) return true;
        }
        return false;
    }
    static bool NearKeep(Vector3 p, float r) { foreach (var k in keep) { Vector3 d = k - p; d.y = 0; if (d.sqrMagnitude < r * r) return true; } return false; }

    // ---------- where the buildings stand: two rings between the streets ----------
    class Slot { public Vector3 O, b, r; public float w, d; public bool outer; public float a; }
    static List<Slot> Slots(bool outer, float w, float d)
    {
        var list = new List<Slot>();
        float rf = outer ? R - WallT / 2f - RingStreet - 0.6f : Plaza + 2.5f;   // the front's distance from the middle
        float step = 0.4f * Mathf.Deg2Rad;
        for (float a = 0f; a < Mathf.PI * 2f; a += step)
        {
            var s = MakeSlot(a, rf, w, d, outer);
            if (!Fits(s, list)) continue;
            list.Add(s); a += (w + 1.2f) / (rf + (outer ? -d : d) * 0.5f) - step;
        }
        return list;
    }
    static Slot MakeSlot(float a, float rf, float w, float d, bool outer)
    {
        Vector3 dir = Dir(a);
        var s = new Slot { a = a, w = w, d = d, outer = outer };
        s.O = C + dir * rf;
        s.b = outer ? -dir : dir;                         // into the building: away from the street it faces
        s.r = Vector3.Cross(Vector3.up, s.b);
        return s;
    }
    static IEnumerable<Vector3> Corners(Slot s, float pad)
    {
        for (int i = 0; i <= 4; i++) for (int j = 0; j <= 2; j++)
                yield return s.O + s.r * ((i / 4f - 0.5f) * (s.w + pad * 2f)) + s.b * (j / 2f * (s.d + pad) - pad * (j == 0 ? 1f : 0f));
    }
    static bool Fits(Slot s, List<Slot> others)
    {
        foreach (var p in Corners(s, 0f))
        {
            if (OnStreet(p, 0.9f)) return false;
            Vector3 dc = p - C; dc.y = 0; float r = dc.magnitude;
            if (r < Plaza + 1f || r > R - WallT / 2f - RingStreet) return false;
        }
        foreach (var k in keep) { if (Inside(s, k, 2f)) return false; }
        foreach (var o in others) foreach (var p in Corners(s, 0.6f)) if (Inside(o, p, 0.6f)) return false;
        foreach (var o in others) foreach (var p in Corners(o, 0.6f)) if (Inside(s, p, 0.6f)) return false;
        foreach (var h in Halls) foreach (var p in Corners(s, 0.6f)) if (h.Contains(p, 0.6f)) return false;
        return true;
    }
    static bool Inside(Slot s, Vector3 p, float pad) { Vector3 q = p - s.O; float x = Vector3.Dot(q, s.r), z = Vector3.Dot(q, s.b); return Mathf.Abs(x) < s.w / 2f + pad && z > -pad && z < s.d + pad; }

    static void Buildings(AHGame g)
    {
        var inner = Slots(false, 11.5f, 8f);
        var outer = Slots(true, 12f, 8f);
        Debug.Log("Ashen Hollow: town lots " + inner.Count + " inner, " + outer.Count + " outer");
        // the workshops and the bank nearest the square, the rest out along the ring street
        int ii = 0, oi = 0; var late = new List<Def>();
        foreach (var def in Defs)
        {
            Slot s = null;
            if (!def.outer && ii < inner.Count) s = inner[ii++];
            else if (oi < outer.Count) s = outer[oi++];
            else if (ii < inner.Count) s = inner[ii++];
            if (s == null) { late.Add(def); continue; }
            Build(g, def, s);
        }
        // anything without a lot yet: a narrower one wherever there is still room
        foreach (var def in late)
        {
            Slot s = null;
            foreach (var w in new[] { 10f, 9f, 8f })
            {
                var l = Slots(true, w, 7.5f); if (l.Count > 0) { s = l[0]; break; }
                l = Slots(false, w, 7.5f); if (l.Count > 0) { s = l[0]; break; }
            }
            if (s == null) { Debug.LogWarning("Ashen Hollow: no lot left for " + def.name); continue; }
            Build(g, def, s);
        }
        // spare lots: a pair of market stalls each, with a lamp
        var spare = new List<Slot>(); for (; ii < inner.Count; ii++) spare.Add(inner[ii]); for (; oi < outer.Count; oi++) spare.Add(outer[oi]);
        int n = 0;
        spare.RemoveAll(s => { foreach (var h in Halls) if (h.Contains(s.O + s.b * (s.d / 2f), 1f)) return true; return false; });
        // the bell tower on a lot by the square, the house plot out on the ring street
        Slot bell = spare.Find(s => !s.outer) ?? (spare.Count > 0 ? spare[0] : null);
        if (bell != null) { spare.Remove(bell); BellTower(bell); }
        else
        {
            // no lot left: the tower stands on the square (clear of the middle, where heroes arrive), its clock facing the middle
            var stalls = new List<Vector3>(); foreach (Transform t in root) if (t.name == "AH_Stall") stalls.Add(t.position);
            Vector3 at = Vector3.zero;
            int nStreet = 0, nStall = 0, nPeople = 0;
            foreach (var rad in new[] { 9.5f, 10.5f, 8.5f, 11.5f })
                for (int i = 0; i < 72 && at == Vector3.zero; i++)
                {
                    float deg = 90f + (i % 2 == 0 ? 1 : -1) * (i / 2) * 5f, a = deg * Mathf.Deg2Rad;
                    if (Mathf.Abs(Mathf.DeltaAngle(deg, 160f)) < 18f || Mathf.Abs(Mathf.DeltaAngle(deg, 235f)) < 18f || Mathf.Abs(Mathf.DeltaAngle(deg, 315f)) < 18f) { nPeople++; continue; }   // Tess, the Warden, the bounty board
                    Vector3 p = C + Dir(a) * rad;
                    if (OnStreet(p, 2.3f) || Vector3.Distance(p, L(0f, -8f)) < 4.6f) { nStreet++; continue; }
                    bool bad = false; foreach (var q in stalls) { Vector3 dd = q - p; dd.y = 0f; if (dd.magnitude < 3.3f) { bad = true; break; } }
                    if (bad) { nStall++; continue; }
                    at = p;
                }
            if (at == Vector3.zero) Debug.LogWarning("Ashen Hollow: bell tower: no room (streets " + nStreet + ", stalls " + nStall + ", people " + nPeople + ")");
            if (at != Vector3.zero)
            {
                Vector3 f = C - at; f.y = 0f; f.Normalize();
                var sq = new Slot { O = at + f * 1.9f, b = -f, r = Vector3.Cross(Vector3.up, -f), w = 3.8f, d = 3.8f, outer = false };
                BellTower(sq);
            }
            else Debug.LogWarning("Ashen Hollow: no room for the bell tower");
        }
        Slot plot = spare.Find(s => s.outer) ?? (spare.Count > 0 ? spare[0] : null);
        if (plot != null) { spare.Remove(plot); HousePlot(plot); }
        else
        {
            // no lot left inside: a plot just outside a gate, beside the road out
            Slot best = null;
            foreach (var st in streets)
            {
                if (!st.gate || best != null) continue;
                Vector3 dir = Dir(st.a), tan = Vector3.Cross(Vector3.up, dir);
                foreach (var side in new[] { 1f, -1f })
                    foreach (var rr in new[] { R + 8f, R + 12f, R + 16f })
                    {
                        if (best != null) break;
                        var sl = new Slot { b = tan * side, w = 10f, d = 8f, outer = true };
                        sl.r = Vector3.Cross(Vector3.up, sl.b); sl.O = C + dir * rr + tan * side * (st.hw + 3.5f); sl.O.y = gy;
                        bool ok = true;
                        foreach (var cp in Corners(sl, 0.5f)) if (AHNav.IsDirt(g, cp) || Inside(cp, 3f)) { ok = false; break; }
                        if (ok) best = sl;
                    }
            }
            if (best != null) { HousePlot(best); PlotOutside = true; } else Debug.LogWarning("Ashen Hollow: no room for the house plot");
        }
        foreach (var s in spare)
        {
            for (int k = -1; k <= 1; k += 2)
            {
                var st = AHStations.Stall(root, 2.2f, 1.5f, 40 + n++);
                Place(st, s.O + s.r * (k * s.w * 0.22f) + s.b * 2.4f, -s.b); st.name = "AH_Stall";
            }
        }
    }

    // ---------- one building ----------
    static void Build(AHGame g, Def def, Slot s)
    {
        var h = new Hall { id = def.id, name = def.name, col = Hex(def.hex), O = s.O, b = s.b, r = s.r, w = s.w, d = s.d };
        h.O.y = gy;
        var go = new GameObject("AH_Hall " + def.name).transform; go.SetParent(root, false); h.go = go;
        Halls.Add(h);
        const float H = 3.4f, T = 0.3f, Low = 1.1f, DoorW = 3.0f, DoorH = 2.5f, Rise = 2.2f;
        float w = h.w, d = h.d;
        var Q = Quaternion.LookRotation(h.b, Vector3.up);   // local +z: into the building, local +x: -r
        System.Func<float, float, float, Vector3> P = (x, y, z) => h.O + h.r * x + Vector3.up * y + h.b * z;
        Material plaster = TexMat("hw_plaster", new Color(0.93f, 0.87f, 0.74f), Plaster), stoneB = TexMat("hw_base", new Color(0.7f, 0.66f, 0.6f), Bricks), timber = TexMat("hw_timber", new Color(0.42f, 0.28f, 0.17f), Planks),
                 floor = TexMat("hw_floor", new Color(0.86f, 0.68f, 0.48f), Planks, 0.2f), roof = TexMat("hw_roof_" + def.id, Color.Lerp(h.col, new Color(0.3f, 0.26f, 0.22f), 0.5f), Shingles, 0.18f),
                 glass = Mat("hw_window", new Color(1f, 0.78f, 0.42f), 0.6f, 0.9f), board = Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f));
        var low = new Kit(); var up = new Kit(); var top = new Kit();
        // the floor and a stone footing
        low.Box(floor, TRS(P(0, 0.05f, d / 2f), Q, new Vector3(w - 0.1f, 0.1f, d - 0.1f)));
        low.Box(stoneB, TRS(P(0, 0.2f, d / 2f), Q, new Vector3(w + 0.2f, 0.4f, d + 0.2f)) * TRS(Vector3.zero, Quaternion.identity, Vector3.one));
        // walls, split at knee height: the upper part is lifted away while you are inside
        System.Action<Kit, Material, float, float, float, float, float, float> Wall = (k, m, x, z, sx, sz, y0, y1) => k.Box(m, TRS(P(x, (y0 + y1) / 2f, z), Q, new Vector3(sx, y1 - y0, sz)));
        foreach (var band in new[] { new Vector2(0.4f, Low), new Vector2(Low, H) })
        {
            var k = band.x < 1f ? low : up; var m = band.x < 1f ? stoneB : plaster;
            Wall(k, m, 0, d - T / 2f, w, T, band.x, band.y);                              // back
            Wall(k, m, -w / 2f + T / 2f, d / 2f, T, d, band.x, band.y);                    // sides
            Wall(k, m, w / 2f - T / 2f, d / 2f, T, d, band.x, band.y);
            float seg = (w - DoorW) / 2f;                                                  // front, either side of the door
            Wall(k, m, -(DoorW / 2f + seg / 2f), T / 2f, seg, T, band.x, band.y);
            Wall(k, m, DoorW / 2f + seg / 2f, T / 2f, seg, T, band.x, band.y);
        }
        Wall(up, plaster, 0, T / 2f, DoorW, T, DoorH, H);                                 // over the door
        // the timber frame: corner posts, a beam round the top and round the knee, the door frame
        foreach (var x in new[] { -w / 2f + 0.15f, w / 2f - 0.15f }) foreach (var z in new[] { 0.15f, d - 0.15f }) up.Box(timber, TRS(P(x, H / 2f, z), Q, new Vector3(0.36f, H, 0.36f)));
        foreach (var x in new[] { -DoorW / 2f - 0.12f, DoorW / 2f + 0.12f }) up.Box(timber, TRS(P(x, H / 2f, 0.1f), Q, new Vector3(0.26f, H, 0.42f)));
        up.Box(timber, TRS(P(0, DoorH + 0.1f, 0.1f), Q, new Vector3(DoorW + 0.5f, 0.22f, 0.42f)));
        foreach (var yy in new[] { Low, H - 0.1f })
        {
            var k = yy < 2f ? low : up;
            k.Box(timber, TRS(P(0, yy, -0.02f), Q, new Vector3(w + 0.1f, 0.16f, 0.1f)));
            k.Box(timber, TRS(P(0, yy, d + 0.02f), Q, new Vector3(w + 0.1f, 0.16f, 0.1f)));
            k.Box(timber, TRS(P(-w / 2f - 0.02f, yy, d / 2f), Q, new Vector3(0.1f, 0.16f, d + 0.1f)));
            k.Box(timber, TRS(P(w / 2f + 0.02f, yy, d / 2f), Q, new Vector3(0.1f, 0.16f, d + 0.1f)));
        }
        // a step at the door
        low.Box(stoneB, TRS(P(0, 0.08f, -0.45f), Q, new Vector3(DoorW + 0.6f, 0.16f, 0.6f)));
        // windows on the sides and the back, glowing warm
        for (int i = 0; i < 2; i++)
        {
            float z = d * (0.3f + 0.4f * i);
            foreach (var sx in new[] { -1f, 1f })
            {
                up.Box(timber, TRS(P(sx * (w / 2f + 0.03f), 2.05f, z), Q, new Vector3(0.1f, 1.0f, 1.2f)));
                up.Box(glass, TRS(P(sx * (w / 2f + 0.07f), 2.05f, z), Q, new Vector3(0.04f, 0.8f, 1.0f)));
            }
        }
        foreach (var x in new[] { -w * 0.27f, w * 0.27f })
        {
            up.Box(timber, TRS(P(x, 2.05f, d + 0.03f), Q, new Vector3(1.2f, 1.0f, 0.1f)));
            up.Box(glass, TRS(P(x, 2.05f, d + 0.07f), Q, new Vector3(1.0f, 0.8f, 0.04f)));
        }
        foreach (var x in new[] { -w * 0.33f, w * 0.33f })
        {
            up.Box(timber, TRS(P(x, 1.95f, -0.03f), Q, new Vector3(1.2f, 1.0f, 0.1f)));
            up.Box(glass, TRS(P(x, 1.95f, -0.07f), Q, new Vector3(1.0f, 0.8f, 0.04f)));
        }
        // half-timbering: braces across the upper walls, and posts between the windows
        foreach (var sx in new[] { -1f, 1f })
        {
            float x = sx * (w / 2f + 0.01f);
            up.Box(timber, TRS(P(x, (Low + H) / 2f, d / 2f), Q, new Vector3(0.12f, H - Low, 0.22f)));
            foreach (var zz in new[] { d * 0.12f, d * 0.88f })
            {
                float run = d * 0.22f, rise = H - Low - 0.2f, len = Mathf.Sqrt(run * run + rise * rise), ang2 = Mathf.Atan2(run, rise) * Mathf.Rad2Deg;
                up.Box(timber, TRS(P(x, (Low + H) / 2f, zz), Q * Quaternion.Euler((zz < d / 2f ? 1 : -1) * ang2, 0, 0), new Vector3(0.11f, len, 0.18f)));
            }
        }
        foreach (var xx in new[] { -w * 0.5f + 1.2f, -w * 0.12f, w * 0.12f, w * 0.5f - 1.2f })
        {
            if (Mathf.Abs(xx) < DoorW / 2f + 0.3f) continue;
            up.Box(timber, TRS(P(xx, (Low + H) / 2f, -0.01f), Q, new Vector3(0.18f, H - Low, 0.1f)));
            up.Box(timber, TRS(P(xx, (Low + H) / 2f, d + 0.01f), Q, new Vector3(0.18f, H - Low, 0.1f)));
        }
        // shutters either side of the front windows, and a flower box under each
        Material shutter = TexMat("hw_shutter_" + def.id, Color.Lerp(h.col, new Color(0.35f, 0.25f, 0.15f), 0.6f), Planks), soil = Mat("hw_soil", new Color(0.25f, 0.17f, 0.1f)), bloom = Mat("hw_bloom_" + def.id, Color.Lerp(h.col, Color.white, 0.25f), 0.3f, 0.15f), leaf = Mat("hw_leaf", new Color(0.25f, 0.5f, 0.2f));
        foreach (var x in new[] { -w * 0.33f, w * 0.33f })
        {
            foreach (var sx in new[] { -1f, 1f }) up.Box(shutter, TRS(P(x + sx * 0.78f, 1.95f, -0.09f), Q, new Vector3(0.36f, 1.0f, 0.05f)));
            up.Box(timber, TRS(P(x, 1.38f, -0.2f), Q, new Vector3(1.2f, 0.22f, 0.28f)));
            up.Box(soil, TRS(P(x, 1.5f, -0.2f), Q, new Vector3(1.1f, 0.04f, 0.22f)));
            for (int k = 0; k < 6; k++)
            {
                float fx = x - 0.45f + k * 0.18f;
                up.Add(leaf, SphMesh, TRS(P(fx, 1.58f, -0.2f), Q, new Vector3(0.2f, 0.14f, 0.18f)));
                up.Add(bloom, SphMesh, TRS(P(fx + 0.04f, 1.68f, -0.22f - (k % 2) * 0.04f), Q, Vector3.one * 0.09f));
            }
        }
        // the door: two plank leaves standing open inside the doorway, on iron straps
        Material iron = Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f);
        foreach (var sx in new[] { -1f, 1f })
        {
            Vector3 hinge = P(sx * DoorW / 2f, DoorH / 2f, T + 0.04f);
            up.Box(timber, TRS(hinge + h.b * 0.72f - h.r * sx * 0.06f, Q, new Vector3(0.07f, DoorH - 0.05f, 1.42f)));
            foreach (var yy in new[] { 0.5f, DoorH - 0.5f }) up.Box(iron, TRS(P(sx * DoorW / 2f - sx * 0.1f, yy, T + 0.76f), Q, new Vector3(0.09f, 0.07f, 1.2f)));
        }
        // the roof: two slopes and the gable ends, the ridge across the frontage
        float over = 0.55f, hd = d / 2f + over, ang = Mathf.Atan2(Rise, hd) * Mathf.Rad2Deg, sl = Mathf.Sqrt(hd * hd + Rise * Rise);
        top.Box(roof, TRS(P(0, H + Rise / 2f + 0.1f, d / 2f - hd / 2f), Q * Quaternion.Euler(-ang, 0, 0), new Vector3(w + 1.0f, 0.2f, sl)));
        top.Box(roof, TRS(P(0, H + Rise / 2f + 0.1f, d / 2f + hd / 2f), Q * Quaternion.Euler(ang, 0, 0), new Vector3(w + 1.0f, 0.2f, sl)));
        top.Box(timber, TRS(P(0, H + Rise + 0.15f, d / 2f), Q, new Vector3(w + 1.1f, 0.24f, 0.3f)));
        foreach (var sx in new[] { -1f, 1f }) top.Add(plaster, Prism, TRS(P(sx * (w / 2f - T / 2f), H, d / 2f), Q, new Vector3(T, Rise, d)));
        // a chimney where there is a fire inside
        if (def.id == "forge" || def.id == "kitchen" || def.id == "inn" || def.id == "apothecary")
        {
            top.Box(stoneB, TRS(P(-w * 0.28f, H + Rise * 0.7f, d * 0.78f), Q, new Vector3(0.9f, Rise * 1.4f + 1f, 0.9f)));
            top.Box(Mat("hw_soot", new Color(0.12f, 0.1f, 0.09f)), TRS(P(-w * 0.28f, H + Rise * 1.4f + 0.52f, d * 0.78f), Q, new Vector3(0.6f, 0.06f, 0.6f)));
            Chimneys.Add(P(-w * 0.28f, H + Rise * 1.4f + 0.7f, d * 0.78f));
        }
        var lowR = low.Build(go, "AH_HallLow"); var upR = up.Build(go, "AH_HallUpper"); var topR = top.Build(go, "AH_HallRoof");
        foreach (var r in lowR) AHModel.SetShadows(r.gameObject);
        foreach (var r in upR) AHModel.SetShadows(r.gameObject);
        foreach (var r in topR) AHModel.SetShadows(r.gameObject);
        // the big sign over the door, in the building's own colour
        Sign(go, P(0, H - 0.42f, -0.16f), -h.b, def.name, h.col, w - 1.2f, 0.95f, board);
        // a lamp by the door, and a light inside for the night
        Vector3 lampAt = P(DoorW / 2f + 1.3f, 0, -1.3f);
        var lamp = AHStations.Lamp(root, Halls.Count); Place(lamp, lampAt, -h.r);
        var lg = new GameObject("Hall light"); lg.transform.SetParent(go, false); lg.transform.position = P(0, H - 0.5f, d / 2f);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.78f, 0.5f); li.range = Mathf.Max(w, d) * 0.9f; li.intensity = 1.6f; li.shadows = LightShadows.None; li.enabled = false;
        // the roof and upper walls lift away while you are inside
        Furnish(g, h);
        var fade = go.gameObject.AddComponent<AHHallFade>(); fade.hall = h; fade.lamp = li; li.intensity = 5f; li.range = Mathf.Max(w, d) * 1.2f;
        var hide = new List<Renderer>(); hide.AddRange(upR); hide.AddRange(topR);
        foreach (Transform c in go) if (c.name.StartsWith("Sign ") || c.name.StartsWith("AH_Upper")) hide.AddRange(c.GetComponentsInChildren<Renderer>());
        fade.hide = hide.ToArray();
    }

    // ---------- inside each building: its work station, its people and real furniture ----------
    static void Furnish(AHGame g, Hall h)
    {
        float w = h.w, d = h.d; Vector3 front = -h.b, back = h.b, right = h.r, left = -h.r;
        Transform p = h.go;
        System.Func<float, float, Vector3> A = (x, z) => h.At(x, z) + Vector3.up * 0.1f;   // on the floorboards
        float sx = w / 2f - 0.42f;                                                        // against a side wall
        int seed = h.id.GetHashCode();
        // every room: a rug down the middle and a torch either side of the back wall
        Rug(p, A(0f, d * 0.5f), back, Mathf.Min(3.2f, w * 0.3f), d * 0.55f, Color.Lerp(h.col, new Color(0.35f, 0.12f, 0.1f), 0.55f));
        foreach (var x in new[] { -w * 0.42f, w * 0.42f }) { var t = Prop("kk_dg_torch_mounted", p, h.At(x, d - 0.32f) + Vector3.up * 1.9f, 0.7f, front); if (t != null) t.name = "AH_Upper torch"; }
        switch (h.id)
        {
            case "forge":
            {
                var fu = Place(AHStations.Furnace(p, 1.8f, 1.3f, 3), h.At(-w * 0.25f, d - 1.3f), front); fu.name = "AH_Furnace";
                Station(g, "furnace", "Furnace", h.At(-w * 0.25f, d - 1.3f), 1.0f);
                var a1 = Place(AHStations.Anvil(p, 1.3f), h.At(w * 0.1f, d * 0.52f), h.r); a1.name = "AH_Anvil";
                var a2 = Place(AHStations.Anvil(p, 1.3f), h.At(w * 0.28f, d * 0.68f), h.r); a2.name = "AH_Anvil";
                Station(g, "anvil", "Anvil", h.At(w * 0.1f, d * 0.52f), 0.7f);
                Station(g, "anvil", "Anvil", h.At(w * 0.28f, d * 0.68f), 0.7f);
                Prop("kk_weaponrack", p, A(sx - 0.05f, d * 0.82f), 1.7f, left);
                Shelf(p, A(-sx, d * 0.42f), right, 1.8f, "ingots", seed);
                Trough(p, A(w * 0.3f, d * 0.3f), front);
                Prop("kk_barrel", p, A(-sx + 0.2f, 1.1f), 1.0f, right);
                Prop("kk_barrel", p, A(-sx + 0.95f, 0.95f), 0.9f, right);
                Prop("kk_dg_crates_stacked", p, A(sx - 0.4f, 1.2f), 1.4f, left);
                CoalHeap(p, A(-w * 0.25f + 1.5f, d - 1.0f));
                break;
            }
            case "kitchen":
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 at = h.At((i == 0 ? -1 : 1) * w * 0.24f, d * 0.6f);
                    Place(Cookpot(p, i), at, front);
                    Station(g, "fire", "Cooking pot", at, 0.8f);
                }
                Table(p, A(0f, d - 1.55f), front, 2.6f, 0.9f); Food(p, A(0f, d - 1.55f) + Vector3.up * 0.82f, front, 2.3f, seed);
                Npc(g, "Baker Maudie", h, 0f, d - 0.7f);
                Shelf(p, A(-sx, d * 0.35f), right, 1.8f, "jars", seed + 1);
                Shelf(p, A(sx, d * 0.35f), left, 1.8f, "jars", seed + 2);
                Prop("kk_sack", p, A(-sx + 0.2f, d - 0.6f), 0.9f, right);
                Prop("kk_sack", p, A(-sx + 0.9f, d - 0.5f), 0.8f, right);
                Prop("kk_dg_barrel_small_stack", p, A(sx - 0.6f, d - 0.8f), 1.3f, left);
                Prop("kk_crate_A_big", p, A(sx - 0.5f, 1.1f), 1.0f, left);
                Prop("kk_bucket_water", p, A(-sx + 0.4f, 1.0f), 0.5f, right);
                break;
            }
            case "apothecary":
            {
                var c = Place(AHStations.Cauldron(p, 5), h.At(0f, d * 0.6f), front); c.name = "AH_Cauldron";
                Station(g, "brew", "Brewing cauldron", h.At(0f, d * 0.6f), 0.9f);
                Shelf(p, A(-sx, d * 0.5f), right, 2.0f, "bottles", seed);
                Shelf(p, A(sx, d * 0.5f), left, 2.0f, "bottles", seed + 1);
                Table(p, A(-w * 0.28f, 1.6f), front, 1.5f, 0.8f); Candles(p, A(-w * 0.28f - 0.4f, 1.6f) + Vector3.up * 0.82f); Book(p, A(-w * 0.28f + 0.3f, 1.6f) + Vector3.up * 0.82f, front, new Color(0.3f, 0.5f, 0.3f));
                Chair(p, A(-w * 0.28f, 2.3f), front);
                Prop("kk_dg_keg", p, A(-sx + 0.4f, d - 0.7f), 1.0f, right);
                Prop("kk_dg_barrel_large", p, A(sx - 0.6f, d - 0.8f), 1.2f, left);
                Prop("kk_bucket_water", p, A(w * 0.2f, d * 0.78f), 0.5f, front);
                Prop("kk_crate_B_small", p, A(sx - 0.5f, 1.1f), 0.7f, left);
                break;
            }
            case "tannery":
            {
                var lo = Place(AHStations.Loom(p, 7), h.At(-w * 0.2f, d * 0.62f), front); lo.name = "AH_Loom";
                Station(g, "loom", "Loom", h.At(-w * 0.2f, d * 0.62f), 0.9f);
                HideRack(p, A(w * 0.24f, d * 0.72f), front); HideRack(p, A(w * 0.24f, d * 0.42f), front);
                Prop("kk_barrel", p, A(sx - 0.5f, d - 0.7f), 1.1f, left); Prop("kk_barrel", p, A(sx - 0.5f, 1.1f), 1.0f, left);
                Prop("kk_barrel", p, A(-sx + 0.4f, d - 0.7f), 1.0f, right);
                Shelf(p, A(-sx, d * 0.32f), right, 1.6f, "cloth", seed);
                Bench(p, A(-w * 0.3f, 1.2f), front, 1.6f);
                break;
            }
            case "jeweler":
            {
                var j = Place(AHStations.JewelBench(p, 9), h.At(0f, d * 0.62f), front); j.name = "AH_JewelBench";
                Station(g, "jewel", "Jeweler’s bench", h.At(0f, d * 0.62f), 0.9f);
                Showcase(p, A(-sx + 0.15f, d * 0.45f), right, seed); Showcase(p, A(sx - 0.15f, d * 0.45f), left, seed + 1);
                Prop("kk_dg_chest_gold", p, A(sx - 0.7f, d - 0.7f), 0.9f, left);
                Prop("kk_dg_coin_stack_large", p, A(-sx + 0.5f, d - 0.6f), 0.6f, right);
                Shelf(p, A(-w * 0.3f, d - 0.42f), front, 1.6f, "gems", seed + 2);
                Chair(p, A(0f, d * 0.62f + 0.9f), front);
                break;
            }
            case "bank":
            {
                bankAt = h.At(0f, d * 0.68f); bankFace = front;   // the vault is moved in once it is built (Late)
                Prop("kk_dg_chest_gold", p, A(-sx + 0.5f, d - 0.7f), 1.0f, right);
                Prop("kk_dg_chest_gold", p, A(sx - 0.5f, d - 0.7f), 1.0f, left);
                Prop("kk_dg_chest", p, A(-sx + 0.5f, d - 1.8f), 0.9f, right);
                Table(p, A(sx - 0.5f, d * 0.45f), left, 1.6f, 0.8f); Coins(p, A(sx - 0.5f, d * 0.45f) + Vector3.up * 0.82f, seed);
                Shelf(p, A(-sx, d * 0.42f), right, 1.8f, "books", seed);
                Bench(p, A(-w * 0.28f, 1.1f), front, 1.8f); Bench(p, A(w * 0.28f, 1.1f), front, 1.8f);
                Pillar(p, A(-w * 0.18f, d * 0.4f)); Pillar(p, A(w * 0.18f, d * 0.4f));
                break;
            }
            case "store":
            {
                Counter(p, h, d * 0.55f, w * 0.55f);
                shopAt = h.At(0f, d * 0.4f);
                Npc(g, "Farmer Tobin", h, 0f, d * 0.75f);
                Shelf(p, A(-w * 0.2f, d - 0.42f), front, 2.0f, "goods", seed); Shelf(p, A(w * 0.2f, d - 0.42f), front, 2.0f, "jars", seed + 1);
                Shelf(p, A(-sx, d * 0.32f), right, 1.6f, "goods", seed + 2); Shelf(p, A(sx, d * 0.32f), left, 1.6f, "goods", seed + 3);
                Prop("kk_sack", p, A(-sx + 0.3f, 1.0f), 0.9f, right); Prop("kk_sack", p, A(-sx + 1.0f, 0.9f), 0.8f, right);
                Prop("kk_barrel", p, A(sx - 0.4f, 1.0f), 1.0f, left); Prop("kk_crate_B_small", p, A(sx - 1.2f, 0.9f), 0.7f, left);
                break;
            }
            case "guild":
            {
                Counter(p, h, d * 0.58f, w * 0.4f);
                Npc(g, "Guild Clerk Mina", h, 0f, d * 0.78f);
                Prop("kk_dg_banner_blue", p, A(-w * 0.3f, d - 0.3f), 2.4f, front); Prop("kk_dg_banner_blue", p, A(w * 0.3f, d - 0.3f), 2.4f, front);
                Table(p, A(-w * 0.28f, d * 0.32f), right, 2.0f, 1.0f);
                foreach (var dz in new[] { -0.55f, 0.55f }) { Chair(p, A(-w * 0.28f - 0.75f, d * 0.32f + dz), right); Chair(p, A(-w * 0.28f + 0.75f, d * 0.32f + dz), left); }
                Book(p, A(-w * 0.28f, d * 0.32f) + Vector3.up * 0.82f, front, new Color(0.25f, 0.35f, 0.6f)); Candles(p, A(-w * 0.28f, d * 0.32f + 0.6f) + Vector3.up * 0.82f);
                Shelf(p, A(sx, d * 0.35f), left, 2.0f, "books", seed);
                Prop("kk_dg_trunk_medium_A", p, A(sx - 0.6f, d - 0.8f), 1.0f, left);
                break;
            }
            case "inn":
            {
                Hearth(p, h.At(0f, d - 0.9f), front);
                var spot = new AHSpot { kind = "use", type = "inn_rest", name = "Rest by the inn's hearth", pos = h.At(0f, d - 2.2f), r = 0.6f, reach = 2.6f };
                spot.use = () => Rest(g);
                AHGather.Spots.Add(spot); mine.Add(spot);
                foreach (var x in new[] { -w * 0.27f, w * 0.27f })
                {
                    Table(p, A(x, d * 0.4f), front, 1.5f, 0.9f); Food(p, A(x, d * 0.4f) + Vector3.up * 0.82f, front, 1.2f, seed + (int)x);
                    Chair(p, A(x, d * 0.4f - 0.75f), back); Chair(p, A(x, d * 0.4f + 0.75f), front);
                    Chair(p, A(x - 1.0f, d * 0.4f), right); Chair(p, A(x + 1.0f, d * 0.4f), left);
                }
                Bed(p, A(-sx + 0.9f, d - 1.25f), right, new Color(0.55f, 0.2f, 0.18f)); Bed(p, A(sx - 0.9f, d - 1.25f), left, new Color(0.2f, 0.32f, 0.55f));
                Prop("kk_dg_keg", p, A(-sx + 0.4f, 1.0f), 1.0f, right);
                Prop("kk_dg_barrel_small_stack", p, A(sx - 0.7f, 1.1f), 1.3f, left);
                break;
            }
            case "barracks":
            {
                Npc(g, "Captain Mara", h, -w * 0.12f, d * 0.68f);
                Npc(g, "Arena crier Bess", h, w * 0.14f, d * 0.55f);
                Prop("kk_weaponrack", p, A(-sx + 0.05f, d * 0.4f), 1.7f, right); Prop("kk_weaponrack", p, A(sx - 0.05f, d * 0.4f), 1.7f, left);
                Bed(p, A(-sx + 0.9f, d - 1.25f), right, new Color(0.45f, 0.15f, 0.15f)); Bed(p, A(sx - 0.9f, d - 1.25f), left, new Color(0.45f, 0.15f, 0.15f));
                Prop("kk_dg_banner_red", p, A(0f, d - 0.3f), 2.4f, front);
                Prop("kk_dg_sword_shield_broken", p, A(w * 0.3f, 1.2f), 0.9f, front);
                Table(p, A(-w * 0.3f, 1.4f), front, 1.5f, 0.8f); Candles(p, A(-w * 0.3f, 1.4f) + Vector3.up * 0.82f); Chair(p, A(-w * 0.3f, 2.1f), front);
                break;
            }
            case "auction":
            {
                Counter(p, h, d * 0.55f, w * 0.5f);
                Npc(g, "Auctioneer Wren", h, 0f, d * 0.75f);
                Showcase(p, A(-sx + 0.15f, d * 0.4f), right, seed); Showcase(p, A(sx - 0.15f, d * 0.4f), left, seed + 3);
                Prop("kk_dg_crates_stacked", p, A(-sx + 0.7f, d - 0.8f), 1.4f, right);
                Prop("kk_dg_chest", p, A(sx - 0.7f, d - 0.7f), 0.9f, left);
                Bench(p, A(-w * 0.28f, 1.1f), front, 1.8f); Bench(p, A(w * 0.28f, 1.1f), front, 1.8f);
                break;
            }
            case "barber":
            {
                Npc(g, "Barber Finn", h, 0f, d * 0.6f);
                foreach (var x in new[] { -w * 0.3f, w * 0.3f }) { Mirror(p, h.At(x, d - 0.22f), front); Chair(p, A(x, d - 1.0f), back); }
                Shelf(p, A(-sx, d * 0.4f), right, 1.6f, "cloth", seed);
                Bench(p, A(w * 0.3f, 1.1f), front, 1.6f);
                Table(p, A(sx - 0.45f, d * 0.45f), left, 1.2f, 0.7f); Candles(p, A(sx - 0.45f, d * 0.45f) + Vector3.up * 0.82f);
                break;
            }
            case "stable":
            {
                // three stalls along the back with plank half-walls, straw on the floor, a trough in each
                var kit = new Kit(); var q = Face(front); Material straw = Mat("hw_straw", new Color(0.86f, 0.72f, 0.4f), 0.05f), hay = TexMat("hw_hay", new Color(0.9f, 0.76f, 0.42f), Planks, 0.05f);
                foreach (var x in new[] { -w / 6f, w / 6f }) Part(kit, Wood, BoxMesh, h.At(x, d * 0.73f), q, new Vector3(0, 0.75f, 0), new Vector3(0.12f, 1.3f, d * 0.5f));
                foreach (var x in new[] { -w / 6f, w / 6f }) Part(kit, DarkWood, BoxMesh, h.At(x, d * 0.48f), q, new Vector3(0, 0.75f, 0), new Vector3(0.2f, 1.5f, 0.2f));
                Part(kit, straw, BoxMesh, h.At(0, d * 0.72f), q, new Vector3(0, 0.12f, 0), new Vector3(w - 0.8f, 0.03f, d * 0.5f));
                foreach (var x in new[] { -w / 3f, 0f, w / 3f })
                {
                    var rnd = new System.Random((int)(x * 10f) + 9);
                    for (int i = 0; i < 6; i++) Part(kit, straw, SphMesh, h.At(x + ((float)rnd.NextDouble() - 0.5f) * 2.4f, d * 0.55f + (float)rnd.NextDouble() * d * 0.35f), q, new Vector3(0, 0.15f, 0), new Vector3(0.6f, 0.08f, 0.45f));
                }
                // hay bales stacked by the door, a saddle on its stand, feed sacks
                foreach (var b in new[] { new Vector3(-sx + 0.5f, 0.35f, 1.3f), new Vector3(-sx + 0.5f, 0.35f, 2.05f), new Vector3(-sx + 0.5f, 0.95f, 1.65f) })
                {
                    Part(kit, hay, BoxMesh, h.At(b.x, b.z), Face(right), new Vector3(0, b.y + 0.1f, 0), new Vector3(1.0f, 0.55f, 0.7f));
                    foreach (var t2 in new[] { -0.25f, 0.25f }) Part(kit, Mat("hw_twine", new Color(0.55f, 0.42f, 0.25f)), BoxMesh, h.At(b.x, b.z), Face(right), new Vector3(t2, b.y + 0.1f, 0), new Vector3(0.04f, 0.57f, 0.72f));
                }
                Done(kit, p, "AH_StableFit");
                foreach (var x in new[] { -w / 3f, 0f, w / 3f }) Trough(p, A(x, d - 0.55f), front);
                Sawhorse(p, A(sx - 0.7f, 1.6f), right);
                var sad = new Kit(); var sq = Face(right); Material leather = Mat("hw_saddle", new Color(0.45f, 0.22f, 0.12f), 0.35f);
                Part(sad, leather, SphMesh, A(sx - 0.7f, 1.6f), sq, new Vector3(0, 0.82f, 0), new Vector3(0.7f, 0.22f, 0.5f));
                Part(sad, leather, BoxMesh, A(sx - 0.7f, 1.6f), sq, new Vector3(0, 0.62f, 0), new Vector3(0.6f, 0.4f, 0.04f));
                Part(sad, leather, BoxMesh, A(sx - 0.7f, 1.6f), sq, new Vector3(0, 0.62f, 0.26f), new Vector3(0.5f, 0.4f, 0.04f));
                Done(sad, p, "AH_Saddle");
                Prop("kk_sack", p, A(sx - 0.45f, 2.7f), 0.8f, left); Prop("kk_sack", p, A(sx - 0.9f, 2.9f), 0.7f, left);
                Prop("kk_bucket_water", p, A(w * 0.08f, 1.2f), 0.5f, front);
                StableHall = h;
                break;
            }
            case "carver":
            {
                Npc(g, "Woodcarver Ansel", h, w * 0.08f, d * 0.62f);
                Workbench(p, A(w * 0.28f, d * 0.55f), left);
                Prop("kk_resource_lumber", p, A(-sx + 0.8f, d - 0.9f), 1.6f, right);
                Prop("kk_resource_lumber", p, A(sx - 0.8f, d - 0.9f), 1.6f, left);
                Prop("kk_wheelbarrow", p, A(-w * 0.28f, 1.4f), 1.3f, right);
                Shelf(p, A(-sx, d * 0.42f), right, 1.6f, "carvings", seed);
                Sawhorse(p, A(w * 0.28f, 1.5f), front);
                break;
            }
        }
    }

    static Vector3 bankAt, bankFace, shopAt;
    public static Hall StableHall;
    static void Station(AHGame g, string kind, string name, Vector3 at, float r)
    {
        var s = new AHSpot { kind = kind, type = kind, name = name, pos = at, r = r, reach = 2.9f };
        AHGather.Spots.Add(s); mine.Add(s);
    }
    static void Npc(AHGame g, string who, Hall h, float x, float z) { pendingNpc.Add(new KeyValuePair<string, Vector3>(who, h.At(x, z))); pendingFace[who] = -h.b; }
    static readonly List<KeyValuePair<string, Vector3>> pendingNpc = new List<KeyValuePair<string, Vector3>>();
    static readonly Dictionary<string, Vector3> pendingFace = new Dictionary<string, Vector3>();

    static void Rest(AHGame g)
    {
        var p = g.player; if (p == null) return;
        p.hp = p.maxHp; p.mana = p.maxMana; p.hunger = 100f;
        g.ui.Toast("You rest by the hearth: health, mana and food are full again.", 3f);
        AHSound.Play("level");
    }

    // ---------- furniture, built from shaped parts (planked wood, cloth, iron) ----------
    static Material Wood { get { return TexMat("hw_furn", new Color(0.7f, 0.5f, 0.32f), Planks, 0.22f); } }
    static Material DarkWood { get { return TexMat("hw_furn_dark", new Color(0.46f, 0.31f, 0.19f), Planks, 0.22f); } }
    static Material Cloth(Color c) { return Mat("hw_cloth_" + ColorUtility.ToHtmlStringRGB(c), c, 0.05f); }
    static Material Glow(Color c, float k = 1.2f) { return Mat("hw_glow_" + ColorUtility.ToHtmlStringRGB(c), c, 0.7f, k); }
    static Quaternion Face(Vector3 f) { f.y = 0f; return f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f.normalized) : Quaternion.identity; }
    static void Part(Kit k, Material m, Mesh mesh, Vector3 at, Quaternion q, Vector3 local, Vector3 size, Quaternion? rot = null)
    {
        k.Add(m, mesh, TRS(at + q * local, q * (rot ?? Quaternion.identity), size));
    }
    static void Done(Kit k, Transform p, string name) { foreach (var r in k.Build(p, name)) AHModel.SetShadows(r.gameObject); }

    static GameObject Cookpot(Transform parent, int seed)
    {
        var go = new GameObject("AH_Cookpot"); go.transform.SetParent(parent, false);
        var fire = AHStations.Campfire(go.transform, 0.75f, 20 + seed); fire.transform.localPosition = Vector3.zero;
        var kit = new Kit(); Material iron = Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f), soup = Mat("hw_soup", new Color(0.85f, 0.55f, 0.2f), 0.6f, 0.4f);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f; Vector3 foot = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.75f;
            kit.Box(iron, TRS(foot * 0.5f + Vector3.up * 0.8f, Quaternion.FromToRotation(Vector3.up, (Vector3.up * 1.6f - foot).normalized), new Vector3(0.05f, 1.7f, 0.05f)));
        }
        // a round-bellied pot hung from the tripod's head, with a rolled rim and a lid half off
        kit.Add(iron, SphMesh, TRS(Vector3.up * 0.82f, Quaternion.identity, new Vector3(0.66f, 0.52f, 0.66f)));
        kit.Add(iron, CylMesh, TRS(Vector3.up * 1.04f, Quaternion.identity, new Vector3(0.52f, 0.03f, 0.52f)));
        kit.Add(soup, CylMesh, TRS(Vector3.up * 1.03f, Quaternion.identity, new Vector3(0.46f, 0.02f, 0.46f)));
        kit.Add(iron, SphMesh, TRS(new Vector3(0.18f, 1.1f, 0f), Quaternion.Euler(0, 0, 18f), new Vector3(0.36f, 0.08f, 0.36f)));
        kit.Box(iron, TRS(Vector3.up * 1.34f, Quaternion.identity, new Vector3(0.03f, 0.55f, 0.03f)));
        kit.Box(iron, TRS(Vector3.up * 1.08f, Quaternion.Euler(0, 0, 90f), new Vector3(0.025f, 0.62f, 0.025f)));
        foreach (var r in kit.Build(go.transform, "AH_Pot")) { r.transform.localPosition = Vector3.zero; AHModel.SetShadows(r.gameObject); }
        return go;
    }
    static void Counter(Transform p, Hall h, float z, float len)
    {
        var kit = new Kit(); var q = Face(h.b); Vector3 at = h.At(0, z) + Vector3.up * 0.1f;
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.5f, 0), new Vector3(len, 1.0f, 0.6f));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 1.04f, 0), new Vector3(len + 0.14f, 0.08f, 0.8f));
        for (int i = 0; i <= 4; i++) Part(kit, Wood, BoxMesh, at, q, new Vector3(-len / 2f + i * len / 4f, 0.5f, -0.31f), new Vector3(0.1f, 0.96f, 0.04f));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.06f, -0.31f), new Vector3(len, 0.12f, 0.06f));
        Done(kit, p, "AH_Counter");
        // a few things on the counter
        var k2 = new Kit();
        Part(k2, Mat("hw_brass", new Color(0.85f, 0.65f, 0.25f), 0.7f), CylMesh, at, q, new Vector3(len * 0.3f, 1.13f, 0.05f), new Vector3(0.22f, 0.02f, 0.22f));
        Part(k2, Cloth(new Color(0.88f, 0.84f, 0.72f)), BoxMesh, at, q, new Vector3(-len * 0.25f, 1.1f, 0.0f), new Vector3(0.32f, 0.02f, 0.24f), Quaternion.Euler(0, 12f, 0));
        Done(k2, p, "AH_CounterTop");
    }
    static void Table(Transform p, Vector3 at, Vector3 face, float len, float dep)
    {
        var kit = new Kit(); var q = Face(face); Material wd = Wood;
        Part(kit, wd, BoxMesh, at, q, new Vector3(0, 0.76f, 0), new Vector3(len, 0.07f, dep));
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.66f, 0), new Vector3(len - 0.2f, 0.12f, dep - 0.2f));
        foreach (var x in new[] { -len / 2f + 0.1f, len / 2f - 0.1f }) foreach (var z in new[] { -dep / 2f + 0.1f, dep / 2f - 0.1f })
                Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.36f, z), new Vector3(0.09f, 0.72f, 0.09f));
        Done(kit, p, "AH_Table");
    }
    static void Chair(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face); Material wd = Wood;
        Part(kit, wd, BoxMesh, at, q, new Vector3(0, 0.46f, 0), new Vector3(0.46f, 0.05f, 0.44f));
        foreach (var x in new[] { -0.19f, 0.19f }) foreach (var z in new[] { -0.18f, 0.18f })
                Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.22f, z), new Vector3(0.05f, 0.44f, 0.05f));
        foreach (var x in new[] { -0.19f, 0.19f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.75f, -0.19f), new Vector3(0.05f, 0.58f, 0.05f));
        foreach (var y in new[] { 0.68f, 0.92f }) Part(kit, wd, BoxMesh, at, q, new Vector3(0, y, -0.19f), new Vector3(0.42f, 0.08f, 0.03f));
        Done(kit, p, "AH_Chair");
    }
    static void Bench(Transform p, Vector3 at, Vector3 face, float len)
    {
        var kit = new Kit(); var q = Face(face);
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.44f, 0), new Vector3(len, 0.07f, 0.38f));
        foreach (var x in new[] { -len / 2f + 0.15f, len / 2f - 0.15f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.2f, 0), new Vector3(0.07f, 0.4f, 0.32f));
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.16f, 0), new Vector3(len - 0.3f, 0.06f, 0.06f));
        Done(kit, p, "AH_Bench");
    }
    static void Bed(Transform p, Vector3 at, Vector3 face, Color blanket)
    {
        // 'face' points from the headboard to the foot
        var kit = new Kit(); var q = Face(face);
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.25f, 0), new Vector3(1.0f, 0.18f, 2.0f));
        foreach (var x in new[] { -0.46f, 0.46f }) foreach (var z in new[] { -0.96f, 0.96f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.2f, z), new Vector3(0.08f, 0.4f, 0.08f));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.62f, -0.98f), new Vector3(1.0f, 0.75f, 0.07f));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.45f, 0.98f), new Vector3(1.0f, 0.4f, 0.06f));
        Part(kit, Cloth(new Color(0.9f, 0.88f, 0.8f)), BoxMesh, at, q, new Vector3(0, 0.4f, 0), new Vector3(0.9f, 0.14f, 1.88f));
        Part(kit, Cloth(new Color(0.95f, 0.93f, 0.88f)), SphMesh, at, q, new Vector3(0, 0.5f, -0.72f), new Vector3(0.7f, 0.16f, 0.32f));
        Part(kit, Cloth(blanket), BoxMesh, at, q, new Vector3(0, 0.49f, 0.25f), new Vector3(0.96f, 0.06f, 1.3f));
        Part(kit, Cloth(Color.Lerp(blanket, Color.white, 0.4f)), BoxMesh, at, q, new Vector3(0, 0.5f, -0.38f), new Vector3(0.96f, 0.065f, 0.12f));
        Done(kit, p, "AH_Bed");
    }
    static void Rug(Transform p, Vector3 at, Vector3 face, float w, float d, Color c)
    {
        var kit = new Kit(); var q = Face(face);
        Part(kit, Cloth(Color.Lerp(c, Color.black, 0.35f)), BoxMesh, at, q, new Vector3(0, 0.008f, 0), new Vector3(w, 0.016f, d));
        Part(kit, Cloth(c), BoxMesh, at, q, new Vector3(0, 0.012f, 0), new Vector3(w - 0.3f, 0.016f, d - 0.3f));
        Part(kit, Cloth(Color.Lerp(c, new Color(0.95f, 0.8f, 0.45f), 0.6f)), BoxMesh, at, q, new Vector3(0, 0.016f, 0), new Vector3(w * 0.35f, 0.016f, d * 0.55f), Quaternion.Euler(0, 45f, 0));
        foreach (Renderer r in kit.Build(p, "AH_Rug")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    // a standing set of shelves (1.9 m) with things on it
    static void Shelf(Transform p, Vector3 at, Vector3 face, float len, string theme, int seed)
    {
        var kit = new Kit(); var q = Face(face); var rnd = new System.Random(seed); const float dep = 0.42f, H = 1.9f;
        foreach (var x in new[] { -len / 2f, len / 2f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, H / 2f, 0), new Vector3(0.06f, H, dep));
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, H / 2f, -dep / 2f + 0.02f), new Vector3(len, H, 0.03f));
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, H + 0.03f, 0), new Vector3(len + 0.12f, 0.06f, dep + 0.06f));
        float[] ys = { 0.1f, 0.58f, 1.06f, 1.54f };
        foreach (var y in ys) Part(kit, Wood, BoxMesh, at, q, new Vector3(0, y, 0), new Vector3(len - 0.06f, 0.04f, dep - 0.04f));
        Color[] pal = { new Color(0.7f, 0.25f, 0.2f), new Color(0.25f, 0.45f, 0.7f), new Color(0.3f, 0.6f, 0.3f), new Color(0.8f, 0.65f, 0.25f), new Color(0.55f, 0.35f, 0.6f), new Color(0.85f, 0.85f, 0.8f) };
        for (int s = 0; s < ys.Length; s++)
        {
            float y0 = ys[s] + 0.02f; float x = -len / 2f + 0.12f;
            while (x < len / 2f - 0.15f)
            {
                Color c = pal[rnd.Next(pal.Length)]; float step;
                switch (theme)
                {
                    case "bottles":
                    {
                        float hgt = 0.18f + (float)rnd.NextDouble() * 0.12f;
                        Part(kit, Glow(c, 0.9f), CylMesh, at, q, new Vector3(x, y0 + hgt / 2f, 0.02f), new Vector3(0.1f, hgt / 2f, 0.1f));
                        Part(kit, Glow(c, 0.9f), CylMesh, at, q, new Vector3(x, y0 + hgt + 0.04f, 0.02f), new Vector3(0.04f, 0.04f, 0.04f));
                        Part(kit, DarkWood, CylMesh, at, q, new Vector3(x, y0 + hgt + 0.09f, 0.02f), new Vector3(0.045f, 0.015f, 0.045f));
                        step = 0.16f; break;
                    }
                    case "jars":
                    {
                        float hgt = 0.16f + (float)rnd.NextDouble() * 0.08f;
                        Part(kit, Mat("hw_clay", new Color(0.72f, 0.5f, 0.34f), 0.3f), SphMesh, at, q, new Vector3(x, y0 + hgt / 2f, 0.02f), new Vector3(0.18f, hgt, 0.18f));
                        Part(kit, Cloth(c), CylMesh, at, q, new Vector3(x, y0 + hgt, 0.02f), new Vector3(0.12f, 0.02f, 0.12f));
                        step = 0.22f; break;
                    }
                    case "books":
                    {
                        int n = 3 + rnd.Next(6); float bx = x;
                        for (int i = 0; i < n && bx < len / 2f - 0.1f; i++) { float bh = 0.22f + (float)rnd.NextDouble() * 0.1f, bw = 0.04f + (float)rnd.NextDouble() * 0.03f; Part(kit, Cloth(pal[rnd.Next(pal.Length)]), BoxMesh, at, q, new Vector3(bx, y0 + bh / 2f, 0.02f), new Vector3(bw, bh, 0.24f)); bx += bw + 0.005f; }
                        step = bx - x + 0.12f; break;
                    }
                    case "ingots":
                    {
                        Color[] metal = { new Color(0.72f, 0.45f, 0.25f), new Color(0.6f, 0.6f, 0.62f), new Color(0.85f, 0.7f, 0.3f), new Color(0.45f, 0.45f, 0.5f) };
                        var mm = Mat("hw_metal_" + rnd.Next(4), metal[rnd.Next(4)], 0.6f);
                        for (int i = 0; i < 3; i++) Part(kit, mm, BoxMesh, at, q, new Vector3(x, y0 + 0.03f + i * 0.055f, 0.02f + (i % 2) * 0.04f), new Vector3(0.22f, 0.05f, 0.1f));
                        step = 0.3f; break;
                    }
                    case "gems":
                    {
                        Part(kit, Glow(c, 1.6f), SphMesh, at, q, new Vector3(x, y0 + 0.05f, 0.02f), Vector3.one * 0.08f);
                        Part(kit, Cloth(new Color(0.3f, 0.12f, 0.15f)), BoxMesh, at, q, new Vector3(x, y0 + 0.01f, 0.02f), new Vector3(0.14f, 0.02f, 0.14f));
                        step = 0.2f; break;
                    }
                    case "cloth":
                    {
                        int n = 2 + rnd.Next(3);
                        for (int i = 0; i < n; i++) Part(kit, Cloth(pal[rnd.Next(pal.Length)]), BoxMesh, at, q, new Vector3(x + 0.12f, y0 + 0.04f + i * 0.07f, 0.02f), new Vector3(0.34f, 0.065f, 0.28f));
                        step = 0.42f; break;
                    }
                    case "carvings":
                    {
                        if (rnd.Next(2) == 0) Part(kit, Wood, SphMesh, at, q, new Vector3(x, y0 + 0.1f, 0.02f), new Vector3(0.14f, 0.2f, 0.14f));
                        else { Part(kit, Wood, BoxMesh, at, q, new Vector3(x, y0 + 0.08f, 0.02f), new Vector3(0.1f, 0.16f, 0.1f)); Part(kit, Wood, SphMesh, at, q, new Vector3(x, y0 + 0.2f, 0.02f), Vector3.one * 0.1f); }
                        step = 0.22f; break;
                    }
                    default:   // goods: boxes and little sacks
                    {
                        if (rnd.Next(2) == 0) { float b = 0.16f + (float)rnd.NextDouble() * 0.08f; Part(kit, Wood, BoxMesh, at, q, new Vector3(x + 0.04f, y0 + b / 2f, 0.02f), new Vector3(b + 0.06f, b, 0.26f)); step = b + 0.14f; }
                        else { Part(kit, Cloth(new Color(0.78f, 0.68f, 0.48f)), SphMesh, at, q, new Vector3(x + 0.04f, y0 + 0.11f, 0.02f), new Vector3(0.22f, 0.24f, 0.2f)); step = 0.28f; }
                        break;
                    }
                }
                x += step;
            }
        }
        Done(kit, p, "AH_Shelf");
    }
    static void Showcase(Transform p, Vector3 at, Vector3 face, int seed)
    {
        // a glass-topped display table with gems and rings on velvet
        var kit = new Kit(); var q = Face(face); var rnd = new System.Random(seed); const float len = 1.8f, dep = 0.6f;
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.42f, 0), new Vector3(len, 0.84f, dep));
        Part(kit, Cloth(new Color(0.35f, 0.08f, 0.12f)), BoxMesh, at, q, new Vector3(0, 0.86f, 0), new Vector3(len - 0.1f, 0.03f, dep - 0.1f));
        Color[] gem = { new Color(0.3f, 0.7f, 1f), new Color(1f, 0.25f, 0.3f), new Color(0.3f, 1f, 0.45f), new Color(0.9f, 0.5f, 1f), new Color(1f, 0.85f, 0.3f) };
        for (int i = 0; i < 6; i++) Part(kit, Glow(gem[rnd.Next(gem.Length)], 1.6f), SphMesh, at, q, new Vector3(-len / 2f + 0.2f + i * 0.28f, 0.92f, (i % 2 - 0.5f) * 0.2f), Vector3.one * 0.07f);
        foreach (var x in new[] { -len / 2f, len / 2f }) foreach (var z in new[] { -dep / 2f, dep / 2f }) Part(kit, Mat("hw_brass", new Color(0.85f, 0.65f, 0.25f), 0.7f), BoxMesh, at, q, new Vector3(x, 0.93f, z), new Vector3(0.04f, 0.16f, 0.04f));
        Done(kit, p, "AH_Showcase");
    }
    static void Food(Transform p, Vector3 at, Vector3 face, float len, int seed)
    {
        var kit = new Kit(); var q = Face(face); var rnd = new System.Random(seed);
        Material bread = Mat("hw_bread", new Color(0.78f, 0.52f, 0.25f), 0.2f), plate = Mat("hw_plate", new Color(0.88f, 0.86f, 0.8f), 0.4f), apple = Mat("hw_apple", new Color(0.75f, 0.15f, 0.12f), 0.5f), cheese = Mat("hw_cheese", new Color(0.95f, 0.8f, 0.3f), 0.3f);
        for (float x = -len / 2f + 0.2f; x < len / 2f - 0.15f; x += 0.42f)
        {
            Part(kit, plate, CylMesh, at, q, new Vector3(x, 0.01f, 0), new Vector3(0.26f, 0.01f, 0.26f));
            int k = rnd.Next(3);
            if (k == 0) Part(kit, bread, SphMesh, at, q, new Vector3(x, 0.07f, 0), new Vector3(0.24f, 0.11f, 0.15f));
            else if (k == 1) for (int i = 0; i < 3; i++) Part(kit, apple, SphMesh, at, q, new Vector3(x + (i - 1) * 0.07f, 0.05f, (i % 2) * 0.05f), Vector3.one * 0.075f);
            else Part(kit, cheese, CylMesh, at, q, new Vector3(x, 0.05f, 0), new Vector3(0.18f, 0.04f, 0.18f));
        }
        Part(kit, Mat("hw_clay", new Color(0.72f, 0.5f, 0.34f), 0.3f), SphMesh, at, q, new Vector3(len / 2f - 0.15f, 0.11f, 0.2f), new Vector3(0.16f, 0.22f, 0.16f));
        Done(kit, p, "AH_Food");
    }
    static void Candles(Transform p, Vector3 at)
    {
        var kit = new Kit(); Material wax = Mat("hw_wax", new Color(0.95f, 0.92f, 0.82f), 0.3f), flame = Glow(new Color(1f, 0.75f, 0.35f), 3f);
        for (int i = 0; i < 3; i++) { Vector3 o = new Vector3((i - 1) * 0.07f, 0, (i % 2) * 0.05f); float hgt = 0.12f + i * 0.04f; kit.Add(wax, CylMesh, TRS(at + o + Vector3.up * hgt / 2f, Quaternion.identity, new Vector3(0.04f, hgt / 2f, 0.04f))); kit.Add(flame, SphMesh, TRS(at + o + Vector3.up * (hgt + 0.025f), Quaternion.identity, new Vector3(0.025f, 0.05f, 0.025f))); }
        Done(kit, p, "AH_Candles");
    }
    static void Book(Transform p, Vector3 at, Vector3 face, Color c)
    {
        var kit = new Kit(); var q = Face(face);
        Part(kit, Cloth(c), BoxMesh, at, q, new Vector3(0, 0.025f, 0), new Vector3(0.34f, 0.05f, 0.26f), Quaternion.Euler(0, 15f, 0));
        Part(kit, Cloth(new Color(0.93f, 0.9f, 0.8f)), BoxMesh, at, q, new Vector3(0, 0.03f, 0.005f), new Vector3(0.32f, 0.04f, 0.24f), Quaternion.Euler(0, 15f, 0));
        Done(kit, p, "AH_Book");
    }
    static void Coins(Transform p, Vector3 at, int seed)
    {
        var kit = new Kit(); var rnd = new System.Random(seed); var gold = Mat("hw_gold", new Color(0.95f, 0.75f, 0.25f), 0.8f, 0.15f);
        for (int s = 0; s < 4; s++) { Vector3 o = new Vector3((s - 1.5f) * 0.18f, 0, (s % 2) * 0.12f); int n = 3 + rnd.Next(6); for (int i = 0; i < n; i++) kit.Add(gold, CylMesh, TRS(at + o + Vector3.up * (0.012f + i * 0.022f), Quaternion.identity, new Vector3(0.1f, 0.01f, 0.1f))); }
        Done(kit, p, "AH_Coins");
    }
    static void Pillar(Transform p, Vector3 at)
    {
        var kit = new Kit(); var st = TexMat("hw_base", new Color(0.7f, 0.66f, 0.6f), Bricks);
        kit.Box(st, TRS(at + Vector3.up * 0.15f, Quaternion.identity, new Vector3(0.6f, 0.3f, 0.6f)));
        kit.Box(st, TRS(at + Vector3.up * 1.7f, Quaternion.identity, new Vector3(0.4f, 2.8f, 0.4f)));
        Done(kit, p, "AH_Pillar");
    }
    static void Trough(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face);
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.3f, 0), new Vector3(1.3f, 0.6f, 0.6f));
        Part(kit, Mat("hw_water", new Color(0.2f, 0.4f, 0.5f), 0.95f), BoxMesh, at, q, new Vector3(0, 0.58f, 0), new Vector3(1.15f, 0.04f, 0.45f));
        Part(kit, Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f), BoxMesh, at, q, new Vector3(0.3f, 0.75f, 0), new Vector3(0.05f, 0.4f, 0.05f), Quaternion.Euler(0, 0, 25f));
        Done(kit, p, "AH_Trough");
    }
    static void CoalHeap(Transform p, Vector3 at)
    {
        var kit = new Kit(); var coal = Mat("hw_coal", new Color(0.09f, 0.08f, 0.08f), 0.3f); var rnd = new System.Random(5);
        for (int i = 0; i < 14; i++) { float a = (float)rnd.NextDouble() * 6.28f, r = (float)rnd.NextDouble() * 0.35f; kit.Add(coal, SphMesh, TRS(at + new Vector3(Mathf.Cos(a) * r, 0.06f + (0.35f - r) * 0.4f, Mathf.Sin(a) * r), Quaternion.Euler(0, a * 57f, 0), new Vector3(0.16f, 0.12f, 0.14f))); }
        Done(kit, p, "AH_Coal");
    }
    static void Workbench(Transform p, Vector3 at, Vector3 face)
    {
        Table(p, at, face, 2.0f, 0.9f);
        var kit = new Kit(); var q = Face(face); Material iron = Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f);
        Part(kit, Wood, BoxMesh, at, q, new Vector3(-0.5f, 0.86f, 0), new Vector3(0.6f, 0.12f, 0.2f));     // a half-carved block
        Part(kit, iron, BoxMesh, at, q, new Vector3(0.2f, 0.81f, 0.15f), new Vector3(0.3f, 0.02f, 0.05f), Quaternion.Euler(0, 30f, 0));   // chisel
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0.45f, 0.83f, -0.1f), new Vector3(0.08f, 0.06f, 0.35f));   // mallet handle
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0.45f, 0.86f, 0.08f), new Vector3(0.16f, 0.12f, 0.12f));
        Part(kit, Mat("hw_shavings", new Color(0.92f, 0.78f, 0.55f), 0.1f), SphMesh, at, q, new Vector3(0f, 0.8f, -0.2f), new Vector3(0.3f, 0.03f, 0.2f));
        Done(kit, p, "AH_Tools");
    }
    static void Sawhorse(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face);
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.62f, 0), new Vector3(1.2f, 0.1f, 0.12f));
        foreach (var x in new[] { -0.5f, 0.5f }) foreach (var s in new[] { -1f, 1f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.31f, s * 0.12f), new Vector3(0.07f, 0.66f, 0.07f), Quaternion.Euler(s * 18f, 0, 0));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0.1f, 0.74f, 0), new Vector3(1.6f, 0.12f, 0.22f), Quaternion.Euler(0, 8f, 0));   // a plank across it
        Done(kit, p, "AH_Sawhorse");
    }
    static void Mirror(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face); at += Vector3.up * 0.1f;
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 0.45f, 0.1f), new Vector3(1.0f, 0.9f, 0.4f));   // a dresser under it
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 0.92f, 0.1f), new Vector3(1.06f, 0.05f, 0.44f));
        Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, 1.55f, 0), new Vector3(0.84f, 1.2f, 0.06f));
        Part(kit, Mat("hw_mirror", new Color(0.75f, 0.85f, 0.95f), 0.95f, 0.12f), BoxMesh, at, q, new Vector3(0, 1.55f, 0.035f), new Vector3(0.7f, 1.05f, 0.02f));
        Done(kit, p, "AH_Mirror");
    }
    static void HideRack(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face);
        Material hide = Mat("hw_hide", new Color(0.62f, 0.44f, 0.28f), 0.2f), rope = Mat("hw_rope", new Color(0.8f, 0.7f, 0.5f));
        foreach (var x in new[] { -0.85f, 0.85f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(x, 0.95f, 0), new Vector3(0.1f, 1.9f, 0.1f));
        foreach (var y in new[] { 0.35f, 1.8f }) Part(kit, DarkWood, BoxMesh, at, q, new Vector3(0, y, 0), new Vector3(1.8f, 0.09f, 0.09f));
        Part(kit, hide, SphMesh, at, q, new Vector3(0, 1.07f, 0), new Vector3(1.35f, 1.2f, 0.05f));
        for (int i = 0; i < 4; i++) Part(kit, rope, BoxMesh, at, q, new Vector3(-0.7f + i * 0.47f, 1.72f, 0), new Vector3(0.02f, 0.14f, 0.02f));
        Done(kit, p, "AH_HideRack");
    }
    static void Hearth(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var q = Face(face); var st = TexMat("hw_base", new Color(0.7f, 0.66f, 0.6f), Bricks); var soot = Mat("hw_soot", new Color(0.12f, 0.1f, 0.09f));
        Part(kit, st, BoxMesh, at, q, new Vector3(-0.95f, 0.75f, 0), new Vector3(0.55f, 1.5f, 0.9f));
        Part(kit, st, BoxMesh, at, q, new Vector3(0.95f, 0.75f, 0), new Vector3(0.55f, 1.5f, 0.9f));
        Part(kit, st, BoxMesh, at, q, new Vector3(0, 1.72f, 0), new Vector3(2.5f, 0.45f, 1.0f));
        Part(kit, Wood, BoxMesh, at, q, new Vector3(0, 1.98f, 0.1f), new Vector3(2.7f, 0.1f, 0.8f));
        Part(kit, soot, BoxMesh, at, q, new Vector3(0, 0.75f, -0.35f), new Vector3(1.4f, 1.5f, 0.1f));
        Part(kit, st, BoxMesh, at, q, new Vector3(0, 0.06f, 0.25f), new Vector3(2.7f, 0.12f, 1.4f));
        for (int i = 0; i < 3; i++) Part(kit, Mat("bark_hw", new Color(0.35f, 0.22f, 0.12f)), CylMesh, at, q, new Vector3((i - 1) * 0.25f, 0.22f, -0.05f), new Vector3(0.14f, 0.45f, 0.14f), Quaternion.Euler(0, 0, 90f));
        Done(kit, p, "AH_Hearth");
        AHStations.Flames(p, at + Vector3.up * 0.25f, 0.6f, 5, new System.Random(3));
        Candles(p, at + q * new Vector3(-0.8f, 2.03f, 0.1f)); Candles(p, at + q * new Vector3(0.8f, 2.03f, 0.1f));
    }

    // ---------- lamps along the streets and round the square ----------
    static void Lamps(AHGame g)
    {
        int n = 0;
        foreach (var s in streets)
        {
            Vector3 u = Dir(s.a), side = Vector3.Cross(Vector3.up, u);
            for (float t = Plaza + 3f; t < R - 4f; t += 9f)
                foreach (int k in new[] { -1, 1 })
                {
                    if (((int)(t / 9f) + k) % 2 != 0) continue;   // staggered either side
                    Vector3 at = C + u * t + side * k * (s.hw + 0.2f);
                    if (NearKeep(at, 1.5f)) continue;
                    var lamp = AHStations.Lamp(root, n++); Place(lamp, at, -side * k);
                }
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + Mathf.PI / 8f; Vector3 at = C + Dir(a) * (Plaza - 0.6f);
            if (OnStreet(at, 0.5f) || NearKeep(at, 2f)) continue;
            var lamp = AHStations.Lamp(root, n++); Place(lamp, at, C - at);
        }
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8f; Vector3 at = C + Dir(a) * (R - WallT / 2f - 0.9f);
            if (OnStreet(at, 0.5f) || InGate(a, 2f)) continue;
            var lamp = AHStations.Lamp(root, n++); Place(lamp, at, C - at);
        }
    }

    // ======================================================================================================
    // once everything else in the land is in place (just before the walk-through pass)
    public static void Late(AHGame g)
    {
        if (!Active || AHGame.AreaId != Area) { Active = false; return; }
        // the old pieces still standing inside the walls go: the web game's houses and props, the rebuilt stations,
        // stalls, wells and rocks, trees and bushes
        int hidden = 0;
        var w = g.World;
        if (w != null)
            foreach (Transform c in w)
            {
                if (c.name.StartsWith("AH_")) continue;
                foreach (var r in c.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.name.StartsWith("AH_") || !r.enabled) continue;
                    var b = r.bounds; if (Mathf.Max(b.size.x, b.size.z) > 30f) continue;
                    if (Inside(b.center, 2.5f) || OnPlot(b.center)) { r.enabled = false; hidden++; }
                }
            }
        foreach (var rn in new[] { "Village", "Stations", "Rebuilt props", "Rebuilt boxes", "Forest", "DreamScatter", "Ore rocks", "Ore", "Herbs", "Flowers", "Stalls and wells", "Ruins", "Crystals", "Wilds", "Tents", "Graves", "Mountains", "Town houses" })
        {
            var go = GameObject.Find(rn); if (go == null) continue;
            foreach (Transform c in go.transform)
            {
                if (!c.gameObject.activeSelf) continue;
                var rs = c.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Vector3 at = rn == "Forest" ? c.position : b.center;
                if (Inside(at, 3f) || OnPlot(at)) { c.gameObject.SetActive(false); hidden++; }
            }
        }
        // gathering spots that slipped in, and their looks
        foreach (var s in AHGather.Spots.ToArray())
        {
            if (mine.Contains(s) || !Inside(s.pos, 3f)) continue;
            if (s.kind == "mine" || s.kind == "herb" || s.kind == "chop" || s.kind == "fire" || s.kind == "furnace" || s.kind == "anvil" || s.kind == "loom" || s.kind == "brew" || s.kind == "jewel")
            { HideSpot(s); AHGather.Spots.Remove(s); }
        }
        if (PlotOutside)
        {
            foreach (var sp in AHGather.Spots.ToArray()) if (OnPlot(sp.pos) && (sp.kind == "mine" || sp.kind == "herb" || sp.kind == "chop")) { HideSpot(sp); AHGather.Spots.Remove(sp); }
            var pc = Plot.At(0f, Plot.d / 2f); g.RemoveBlockersNear(pc, 6.5f); g.RemoveBoxesNear(pc, 6.5f);
        }
        // the townsfolk to their own buildings
        foreach (var kv in pendingNpc)
            foreach (var n in AHNpc.All)
                if (n != null && n.npcName == kv.Key) { Vector3 f; pendingFace.TryGetValue(kv.Key, out f); n.MoveHome(kv.Value, f); }
        foreach (var n in AHNpc.All)
            if (n != null && (n.npcName == "Market Warden" || n.npcName == "Little Tess"))
            {
                float a = (n.npcName == "Little Tess" ? 160f : 235f) * Mathf.Deg2Rad; Vector3 at = C + Dir(a) * 9f;
                n.MoveHome(at, C - at);
            }
        // the bank vault and the shop counter into their buildings
        if (bankAt != Vector3.zero)
        {
            var v = GameObject.Find("Bank vault");
            if (v != null) { v.transform.position = bankAt; v.transform.rotation = Quaternion.LookRotation(bankFace); }
            foreach (var s in AHGather.Spots) if (s.type == "bank") { s.pos = bankAt; mine.Add(s); }
        }
        if (shopAt != Vector3.zero) foreach (var s in AHGather.Spots) if (s.type == "shop") { s.pos = shopAt; mine.Add(s); }
        // the bounty board to the square
        {
            Vector3 at = C + Dir(315f * Mathf.Deg2Rad) * 9.5f; var bd = GameObject.Find("BountyBoard");
            foreach (var s in AHGather.Spots) if (s.name == "Bounty board" && Inside(s.pos, 10f))
                {
                    if (bd != null) { Vector3 dl = at - s.pos; dl.y = 0f; bd.transform.position += dl; bd.transform.rotation = Quaternion.LookRotation(C - at); }
                    s.pos = at; mine.Add(s);
                }
        }
        // no blockers left over from what went (the town's own walls and things are read from their shapes)
        g.RemoveBlockersNear(C, R + 3f); g.RemoveBoxesNear(C, R + 3f);
        // anything else you use that ended up inside a wall or a building: out in front of it
        foreach (var s in AHGather.Spots)
        {
            if (mine.Contains(s) || !Inside(s.pos, 3f)) continue;
            foreach (var h in Halls) if (h.Contains(s.pos, 0.6f)) { Debug.Log("Ashen Hollow: moved " + s.name + " out of " + h.name); s.pos = h.Door + h.r * 2.5f; break; }
        }
        foreach (var n in AHNpc.All)
        {
            if (n == null || !Inside(n.transform.position, 3f)) continue;
            bool placed = false; foreach (var kv in pendingNpc) if (kv.Key == n.npcName) placed = true;
            if (placed) continue;
            foreach (var h in Halls) if (h.Contains(n.transform.position, 0.6f)) { n.MoveHome(h.Door - h.r * 2.5f, -h.b); break; }
        }
        // the lamps by the doors and along the streets are solid at their post
        foreach (var l in root.GetComponentsInChildren<AHLampGlow>(true))
        {
            var post = l.transform.parent != null ? l.transform.parent.position : l.transform.position;
            g.AddBlocker(new Vector3(post.x, 0f, post.z), 0.28f);
        }
        hidden += RoadSweep(g);
        RoadLamps(g);
        AHNav.Forget();
        pendingNpc.Clear(); pendingFace.Clear();
        AHTownLife.Setup(g, root);   // the crier, the stall keepers, the watch, the bell, the night at the inn
        Debug.Log("Ashen Hollow: town cleared of " + hidden + " old pieces");
    }
    static bool OnPlot(Vector3 p) { return PlotOutside && Plot != null && Plot.Contains(p, 1.5f); }
    static void HideSpot(AHSpot s)
    {
        if (s.look != null) s.look.gameObject.SetActive(false);
        if (s.look2 != null) s.look2.gameObject.SetActive(false);
        if (s.fx != null) s.fx.SetActive(false);
        if (s.body != null) s.body.enabled = false;
    }

    // a real stretch of road here (not a stray brown speck on the grass)
    static bool Road(AHGame g, Vector3 p)
    {
        if (!AHNav.IsDirt(g, p)) return false;
        int n = 0;
        for (int i = -1; i <= 1; i++) for (int k = -1; k <= 1; k++) if (AHNav.IsDirt(g, p + new Vector3(i * 0.9f, 0f, k * 0.9f))) n++;
        return n >= 5;
    }

    // ---------- the roads: nothing left standing on them ----------
    static int RoadSweep(AHGame g)
    {
        int n = 0;
        foreach (var s in AHGather.Spots.ToArray())
        {
            if (s.kind != "mine" && s.kind != "herb" && s.kind != "chop") continue;
            if (!Road(g, s.pos)) continue;
            HideSpot(s); AHGather.Spots.Remove(s); g.RemoveBlockersNear(s.pos, Mathf.Max(0.8f, s.r + 0.3f)); n++;
        }
        foreach (var rn in new[] { "Forest", "DreamScatter", "Rebuilt props", "Rebuilt boxes", "Ore rocks", "Ore", "Herbs", "Ruins", "Wilds", "Crystals" })
        {
            var go = GameObject.Find(rn); if (go == null) continue;
            foreach (Transform c in go.transform)
            {
                if (!c.gameObject.activeSelf) continue;
                var rs = c.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (Mathf.Max(b.size.x, b.size.z) > 7f) continue;
                Vector3 at = rn == "Forest" ? c.position : b.center;
                Vector3 ex = b.extents * 0.6f;
                if (!Road(g, at) && !Road(g, at + new Vector3(ex.x, 0, 0)) && !Road(g, at - new Vector3(ex.x, 0, 0)) && !Road(g, at + new Vector3(0, 0, ex.z)) && !Road(g, at - new Vector3(0, 0, ex.z))) continue;
                c.gameObject.SetActive(false); g.RemoveBlockersNear(at, Mathf.Max(0.6f, Mathf.Min(b.extents.x, b.extents.z) + 0.3f)); n++;
            }
        }
        var w = g.World;
        if (w != null)
            foreach (Transform c in w)
            {
                if (c.name.StartsWith("AH_")) continue;
                var rs = c.GetComponentsInChildren<Renderer>(false); if (rs.Length == 0) continue;
                bool any = false; Bounds b = new Bounds(); foreach (var r in rs) { if (!r.enabled) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                if (!any || Mathf.Max(b.size.x, b.size.z) > 3.2f || b.size.y > 3.2f) continue;
                if (!Road(g, b.center)) continue;
                foreach (var r in rs) r.enabled = false;
                g.RemoveBlockersNear(b.center, Mathf.Max(0.6f, Mathf.Max(b.extents.x, b.extents.z) + 0.2f)); n++;
            }
        return n;
    }

    // ---------- lamps along the roads out, lit at night ----------
    public static readonly List<Vector3> RoadLampsAt = new List<Vector3>();
    // (editor tests) where to stand to look at a gate from the road outside
    public static bool GateView(int n, out Vector3 eye, out Vector3 at)
    {
        eye = at = Vector3.zero; int k = 0;
        foreach (var s in streets)
        {
            if (!s.gate) continue;
            if (k++ != n) continue;
            Vector3 d = Dir(s.a); eye = C + d * (R + 16f) + Vector3.up * 6f; at = C + d * R + Vector3.up * 3.5f; return true;
        }
        return false;
    }
    static void RoadLamps(AHGame g)
    {
        RoadLampsAt.Clear();
        var lr = new GameObject("AH_Road lamps").transform; var placed = new List<Vector3>(); int n = 0;
        var gm = g.data.groundMap; if (gm == null) return;
        var bd = g.data.bounds;
        for (float x = bd.x0 + 1f; x < bd.x1 - 1f; x += 1.2f)
            for (float z = bd.z0 + 1f; z < bd.z1 - 1f; z += 1.2f)
            {
                Vector3 p = g.W(x, z);
                if (Inside(p, 6f) || !Road(g, p)) continue;
                bool near = false; foreach (var q in placed) { Vector3 dd = q - p; dd.y = 0; if (dd.sqrMagnitude < 20f * 20f) { near = true; break; } }
                if (near) continue;
                // the road's edge nearest this point, and the lamp just off it
                Vector3 best = Vector3.zero; float bestT = 99f;
                for (int k = 0; k < 16; k++)
                {
                    float a = k * Mathf.PI / 8f; Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    for (float t = 0.5f; t < 6f; t += 0.25f) if (!AHNav.IsDirt(g, p + dir * t)) { if (t < bestT) { bestT = t; best = dir; } break; }
                }
                if (bestT > 6f) continue;
                Vector3 at = p + best * (bestT + 0.8f); at.y = gy;
                if (!g.InArea(at) || g.Blocked(at, 0.5f) || AHSea.Sea(at, 1f)) continue;
                placed.Add(p);
                var lamp = AHStations.Lamp(lr, n++); Place(lamp, at, -best); RoadLampsAt.Add(at);
                g.AddBlocker(new Vector3(at.x, 0f, at.z), 0.28f);
            }
        Debug.Log("Ashen Hollow: " + n + " lamps along the roads");
    }
}

// the roof and upper walls lift away while the hero is inside the building, so you can see the room
public class AHHallFade : MonoBehaviour
{
    public AHHollow.Hall hall; public Renderer[] hide; public Light lamp; bool shown = true; float t;
    public static AHHollow.Hall In;   // the building the hero is in (the camera may look in over its walls)
    void Update()
    {
        t -= Time.deltaTime; if (t > 0f) return; t = 0.12f;
        var g = AHGame.I; if (g == null || g.player == null || hall == null) return;
        bool inside = hall.Contains(g.player.transform.position, -0.2f);
        if (inside) In = hall; else if (In == hall) In = null;
        if (lamp != null) lamp.enabled = inside || (g.IsNight && (g.player.transform.position - transform.position).sqrMagnitude < 40f * 40f && (hall.O - g.player.transform.position).sqrMagnitude < 40f * 40f);
        if (inside == !shown) return;
        shown = !inside;
        foreach (var r in hide) if (r != null) r.enabled = shown;
    }
}
