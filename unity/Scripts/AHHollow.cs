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
        root = new GameObject("AH_Hollow town").transform;

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
            l.Add(new CombineInstance { mesh = mesh, transform = M });
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
    static Mesh boxM, cylM, prismM, coneM;
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
        Material stone = Mat("hw_stone", new Color(0.62f, 0.58f, 0.52f)), stone2 = Mat("hw_stone_dark", new Color(0.47f, 0.44f, 0.40f)),
                 slate = Mat("hw_slate", new Color(0.24f, 0.34f, 0.30f)), wood = Mat("hw_beam", new Color(0.33f, 0.22f, 0.13f));
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
        foreach (var a in towers)
        {
            Vector3 at = C + Dir(a) * R;
            kit.Add(stone, CylMesh, TRS(at + Vector3.up * 3.6f, Quaternion.identity, new Vector3(4.6f, 3.6f, 4.6f)));
            kit.Add(stone2, CylMesh, TRS(at + Vector3.up * 0.4f, Quaternion.identity, new Vector3(5.1f, 0.4f, 5.1f)));
            kit.Add(stone2, CylMesh, TRS(at + Vector3.up * 7.35f, Quaternion.identity, new Vector3(5.2f, 0.15f, 5.2f)));
            for (int k = 0; k < 8; k++)
            {
                float b = k * Mathf.PI / 4f; Vector3 o = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * 2.3f;
                kit.Box(stone, TRS(at + o + Vector3.up * 7.8f, Quaternion.LookRotation(o), new Vector3(0.8f, 0.8f, 0.45f)));
            }
            kit.Add(slate, Cone, TRS(at + Vector3.up * 7.5f, Quaternion.identity, new Vector3(4.2f, 3.4f, 4.2f)));
        }
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
            // the town's name over the road in, and which gate it is over the road out
            Sign(root, at - dir * (WallT / 2f + 0.3f) + Vector3.up * (WallH + 0.9f), -dir, s.label, Hex("ffd27a"), s.hw * 2f, 1.1f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
            Sign(root, at + dir * (WallT / 2f + 0.3f) + Vector3.up * (WallH + 0.9f), -dir, "Hollow Meadow", Hex("ffd27a"), s.hw * 2f, 1.1f, Mat("hw_sign", new Color(0.16f, 0.11f, 0.07f)));
            Banner(at + tan * (s.hw + 2.6f) - dir * 2.45f, -dir);
            Banner(at - tan * (s.hw + 2.6f) - dir * 2.45f, -dir);
        }
        var rs = kit.Build(root, "AH_TownWall");
        foreach (var r in rs) AHModel.SetShadows(r.gameObject);
    }
    static bool InGate(float a, float pad)
    {
        foreach (var s in streets) if (s.gate && Mathf.Abs(Mathf.DeltaAngle(s.a * Mathf.Rad2Deg, a * Mathf.Rad2Deg)) * Mathf.Deg2Rad * R < s.hw + pad) return true;
        return false;
    }
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
        f.name = "AH_Fountain";
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
        foreach (var s in spare)
        {
            bool taken = false; foreach (var h in Halls) if (h.Contains(s.O + s.b * (s.d / 2f), 1f)) taken = true;
            if (taken) continue;
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
        Material plaster = Mat("hw_plaster", new Color(0.86f, 0.80f, 0.68f)), stoneB = Mat("hw_base", new Color(0.52f, 0.49f, 0.45f)), timber = Mat("hw_timber", new Color(0.30f, 0.20f, 0.12f)),
                 floor = Mat("hw_floor", new Color(0.74f, 0.58f, 0.4f), 0.15f, 0.12f), roof = Mat("hw_roof_" + def.id, Color.Lerp(h.col, new Color(0.25f, 0.22f, 0.2f), 0.55f)),
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
        var fade = go.gameObject.AddComponent<AHHallFade>(); fade.hall = h; fade.lamp = li; li.intensity = 5f; li.range = Mathf.Max(w, d) * 1.2f;
        var hide = new List<Renderer>(); hide.AddRange(upR); hide.AddRange(topR);
        foreach (Transform c in go) if (c.name.StartsWith("Sign ")) hide.AddRange(c.GetComponentsInChildren<Renderer>());
        fade.hide = hide.ToArray();
        Furnish(g, h);
    }

    // ---------- inside each building ----------
    static void Furnish(AHGame g, Hall h)
    {
        float w = h.w, d = h.d; Vector3 front = -h.b;
        Transform p = h.go;
        switch (h.id)
        {
            case "forge":
            {
                var fu = Place(AHStations.Furnace(p, 1.8f, 1.3f, 3), h.At(-w * 0.25f, d - 1.3f), front); fu.name = "AH_Furnace";
                Station(g, "furnace", "Furnace", h.At(-w * 0.25f, d - 1.3f), 1.0f);
                var a1 = Place(AHStations.Anvil(p, 1.3f), h.At(w * 0.12f, d * 0.5f), h.r); a1.name = "AH_Anvil";
                var a2 = Place(AHStations.Anvil(p, 1.3f), h.At(w * 0.32f, d * 0.62f), h.r); a2.name = "AH_Anvil";
                Station(g, "anvil", "Anvil", h.At(w * 0.12f, d * 0.5f), 0.7f);
                Station(g, "anvil", "Anvil", h.At(w * 0.32f, d * 0.62f), 0.7f);
                Prop("kk_weaponrack", p, h.At(w / 2f - 0.6f, d * 0.8f), 1.6f, -h.r);
                Prop("kk_barrel", p, h.At(-w / 2f + 0.7f, 1.2f), 1.0f, h.r);
                Prop("kk_dg_crates_stacked", p, h.At(w / 2f - 0.9f, 1.3f), 1.4f, -h.r);
                break;
            }
            case "kitchen":
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 at = h.At((i == 0 ? -1 : 1) * w * 0.22f, d * 0.62f);
                    Place(Cookpot(p, i), at, front);
                    Station(g, "fire", "Cooking pot", at, 0.8f);
                }
                Prop("kk_sack", p, h.At(-w / 2f + 0.6f, d - 0.7f), 0.9f, h.r);
                Prop("kk_sack", p, h.At(-w / 2f + 1.4f, d - 0.6f), 0.8f, h.r);
                Prop("kk_dg_barrel_small_stack", p, h.At(w / 2f - 0.8f, d - 0.9f), 1.3f, -h.r);
                Prop("kk_crate_A_big", p, h.At(w / 2f - 0.7f, 1.3f), 1.0f, -h.r);
                Npc(g, "Baker Maudie", h, 0f, d * 0.82f);
                break;
            }
            case "apothecary":
            {
                var c = Place(AHStations.Cauldron(p, 5), h.At(0f, d * 0.6f), front); c.name = "AH_Cauldron";
                Station(g, "brew", "Brewing cauldron", h.At(0f, d * 0.6f), 0.9f);
                Prop("kk_dg_keg", p, h.At(-w / 2f + 0.7f, d - 0.8f), 1.0f, h.r);
                Prop("kk_dg_candle_triple", p, h.At(w / 2f - 0.6f, d - 0.6f), 0.6f, -h.r);
                Prop("kk_dg_barrel_large", p, h.At(w / 2f - 0.8f, 1.4f), 1.2f, -h.r);
                Prop("kk_bucket_water", p, h.At(-w * 0.2f, d * 0.75f), 0.5f, front);
                break;
            }
            case "tannery":
            {
                var lo = Place(AHStations.Loom(p, 7), h.At(-w * 0.18f, d * 0.6f), front); lo.name = "AH_Loom";
                Station(g, "loom", "Loom", h.At(-w * 0.18f, d * 0.6f), 0.9f);
                HideRack(p, h.At(w * 0.28f, d * 0.65f), front);
                Prop("kk_barrel", p, h.At(w / 2f - 0.7f, 1.3f), 1.0f, -h.r);
                Prop("kk_barrel", p, h.At(-w / 2f + 0.7f, d - 0.8f), 1.0f, h.r);
                break;
            }
            case "jeweler":
            {
                var j = Place(AHStations.JewelBench(p, 9), h.At(0f, d * 0.62f), front); j.name = "AH_JewelBench";
                Station(g, "jewel", "Jeweler’s bench", h.At(0f, d * 0.62f), 0.9f);
                Prop("kk_dg_chest_gold", p, h.At(w / 2f - 0.8f, d - 0.8f), 0.9f, -h.r);
                Prop("kk_dg_coin_stack_large", p, h.At(-w / 2f + 0.7f, d - 0.7f), 0.6f, h.r);
                Prop("kk_dg_candle_triple", p, h.At(-w * 0.25f, d - 0.5f), 0.55f, front);
                break;
            }
            case "bank":
            {
                bankAt = h.At(0f, d * 0.68f); bankFace = front;   // the vault is moved in once it is built (Late)
                Prop("kk_dg_chest_gold", p, h.At(-w / 2f + 0.8f, d - 0.8f), 1.0f, h.r);
                Prop("kk_dg_chest_gold", p, h.At(w / 2f - 0.8f, d - 0.8f), 1.0f, -h.r);
                Prop("kk_dg_coin_stack_large", p, h.At(-w / 2f + 0.7f, 1.4f), 0.6f, h.r);
                break;
            }
            case "store":
            {
                Counter(p, h, d * 0.55f, w * 0.6f);
                shopAt = h.At(0f, d * 0.4f);
                Npc(g, "Farmer Tobin", h, 0f, d * 0.75f);
                Prop("kk_sack", p, h.At(-w / 2f + 0.6f, d - 0.6f), 0.9f, h.r);
                Prop("kk_crate_A_big", p, h.At(w / 2f - 0.7f, d - 0.7f), 1.0f, -h.r);
                Prop("kk_crate_B_small", p, h.At(w / 2f - 0.6f, 1.3f), 0.7f, -h.r);
                Prop("kk_barrel", p, h.At(-w / 2f + 0.7f, 1.3f), 1.0f, h.r);
                break;
            }
            case "guild":
            {
                Counter(p, h, d * 0.55f, w * 0.45f);
                Npc(g, "Guild Clerk Mina", h, 0f, d * 0.75f);
                Prop("kk_dg_banner_blue", p, h.At(-w * 0.3f, d - 0.25f), 2.2f, front);
                Prop("kk_dg_banner_blue", p, h.At(w * 0.3f, d - 0.25f), 2.2f, front);
                Prop("kk_dg_candle_triple", p, h.At(-w / 2f + 0.6f, 1.2f), 0.6f, h.r);
                Prop("kk_dg_trunk_medium_A", p, h.At(w / 2f - 0.8f, d - 0.9f), 1.0f, -h.r);
                break;
            }
            case "inn":
            {
                var at = h.At(0f, d - 1.0f);
                Hearth(p, at, front);
                var spot = new AHSpot { kind = "use", type = "inn_rest", name = "Rest by the inn's hearth", pos = h.At(0f, d - 2.2f), r = 0.6f, reach = 2.6f };
                spot.use = () => Rest(g);
                AHGather.Spots.Add(spot); mine.Add(spot);
                Prop("kk_dg_keg", p, h.At(-w / 2f + 0.7f, d - 0.8f), 1.0f, h.r);
                Prop("kk_dg_barrel_small_stack", p, h.At(w / 2f - 0.9f, d - 0.9f), 1.3f, -h.r);
                Table(p, h.At(-w * 0.25f, d * 0.4f)); Table(p, h.At(w * 0.25f, d * 0.4f));
                break;
            }
            case "barracks":
            {
                Npc(g, "Captain Mara", h, -w * 0.15f, d * 0.7f);
                Npc(g, "Arena crier Bess", h, w * 0.18f, d * 0.6f);
                Prop("kk_weaponrack", p, h.At(-w / 2f + 0.5f, d * 0.5f), 1.6f, h.r);
                Prop("kk_weaponrack", p, h.At(w / 2f - 0.5f, d * 0.5f), 1.6f, -h.r);
                Prop("kk_dg_banner_red", p, h.At(0f, d - 0.25f), 2.2f, front);
                Prop("kk_dg_sword_shield_broken", p, h.At(w * 0.3f, d - 0.7f), 0.9f, front);
                break;
            }
            case "auction":
            {
                Counter(p, h, d * 0.55f, w * 0.5f);
                Npc(g, "Auctioneer Wren", h, 0f, d * 0.75f);
                Prop("kk_dg_crates_stacked", p, h.At(-w / 2f + 0.9f, d - 0.9f), 1.4f, h.r);
                Prop("kk_dg_coin_stack_large", p, h.At(w / 2f - 0.7f, d - 0.7f), 0.6f, -h.r);
                Prop("kk_dg_chest", p, h.At(w / 2f - 0.8f, 1.3f), 0.9f, -h.r);
                break;
            }
            case "barber":
            {
                Npc(g, "Barber Finn", h, 0f, d * 0.68f);
                Mirror(p, h.At(-w * 0.3f, d - 0.2f), front);
                Mirror(p, h.At(w * 0.3f, d - 0.2f), front);
                Table(p, h.At(-w * 0.3f, d * 0.55f));
                Prop("kk_dg_candle_triple", p, h.At(w / 2f - 0.6f, 1.2f), 0.55f, -h.r);
                break;
            }
            case "carver":
            {
                Npc(g, "Woodcarver Ansel", h, w * 0.1f, d * 0.65f);
                Prop("kk_resource_lumber", p, h.At(-w / 2f + 1.0f, d - 1.0f), 1.6f, h.r);
                Prop("kk_resource_lumber", p, h.At(w / 2f - 1.0f, d - 1.0f), 1.6f, -h.r);
                Prop("kk_wheelbarrow", p, h.At(-w * 0.25f, 1.6f), 1.3f, h.r);
                Table(p, h.At(w * 0.2f, d * 0.45f));
                break;
            }
        }
    }
    static Vector3 bankAt, bankFace, shopAt;
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

    // small furniture built from boxes
    static GameObject Cookpot(Transform parent, int seed)
    {
        var go = new GameObject("AH_Cookpot"); go.transform.SetParent(parent, false);
        var fire = AHStations.Campfire(go.transform, 0.75f, 20 + seed); fire.transform.localPosition = Vector3.zero;
        var kit = new Kit(); Material iron = Mat("hw_iron", new Color(0.15f, 0.15f, 0.17f), 0.4f), soup = Mat("hw_soup", new Color(0.85f, 0.55f, 0.2f), 0.6f, 0.4f);
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f; Vector3 foot = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.75f;
            kit.Box(iron, TRS(foot * 0.5f + Vector3.up * 0.75f, Quaternion.FromToRotation(Vector3.up, (Vector3.up * 1.5f - foot).normalized), new Vector3(0.05f, 1.6f, 0.05f)));
        }
        kit.Add(iron, CylMesh, TRS(Vector3.up * 0.75f, Quaternion.identity, new Vector3(0.6f, 0.22f, 0.6f)));
        kit.Add(soup, CylMesh, TRS(Vector3.up * 0.98f, Quaternion.identity, new Vector3(0.52f, 0.01f, 0.52f)));
        kit.Box(iron, TRS(Vector3.up * 1.42f, Quaternion.identity, new Vector3(0.04f, 0.5f, 0.04f)));
        kit.Build(go.transform, "AH_Pot");
        foreach (Transform c in go.transform) if (c.name.StartsWith("AH_Pot")) { c.localPosition = Vector3.zero; }
        return go;
    }
    static void Counter(Transform p, Hall h, float z, float len)
    {
        var kit = new Kit(); var Q = Quaternion.LookRotation(h.b);
        kit.Box(Mat("hw_counter", new Color(0.42f, 0.28f, 0.16f)), TRS(h.At(0, z) + Vector3.up * 0.5f, Q, new Vector3(len, 1.0f, 0.7f)));
        kit.Box(Mat("hw_counter_top", new Color(0.58f, 0.42f, 0.26f)), TRS(h.At(0, z) + Vector3.up * 1.03f, Q, new Vector3(len + 0.1f, 0.08f, 0.85f)));
        kit.Build(p, "AH_Counter");
    }
    static void Table(Transform p, Vector3 at)
    {
        var kit = new Kit(); Material wd = Mat("hw_table", new Color(0.5f, 0.34f, 0.2f));
        kit.Box(wd, TRS(at + Vector3.up * 0.78f, Quaternion.identity, new Vector3(1.6f, 0.08f, 0.9f)));
        foreach (var x in new[] { -0.7f, 0.7f }) foreach (var z in new[] { -0.35f, 0.35f }) kit.Box(wd, TRS(at + new Vector3(x, 0.38f, z), Quaternion.identity, new Vector3(0.08f, 0.76f, 0.08f)));
        kit.Build(p, "AH_Table");
    }
    static void Mirror(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var Q = Quaternion.LookRotation(face);
        kit.Box(Mat("hw_timber", new Color(0.30f, 0.20f, 0.12f)), TRS(at + Vector3.up * 1.5f, Q, new Vector3(1.0f, 1.4f, 0.08f)));
        kit.Box(Mat("hw_mirror", new Color(0.75f, 0.85f, 0.95f), 0.95f, 0.15f), TRS(at + Vector3.up * 1.5f + face * 0.05f, Q, new Vector3(0.8f, 1.2f, 0.02f)));
        kit.Build(p, "AH_Mirror");
    }
    static void HideRack(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var Q = Quaternion.LookRotation(face); Vector3 side = Vector3.Cross(Vector3.up, face);
        Material wd = Mat("hw_timber", new Color(0.30f, 0.20f, 0.12f)), hide = Mat("hw_hide", new Color(0.62f, 0.44f, 0.28f));
        foreach (var x in new[] { -0.8f, 0.8f }) kit.Box(wd, TRS(at + side * x + Vector3.up * 0.9f, Q, new Vector3(0.1f, 1.8f, 0.1f)));
        kit.Box(wd, TRS(at + Vector3.up * 1.75f, Q, new Vector3(1.7f, 0.1f, 0.1f)));
        kit.Box(hide, TRS(at + Vector3.up * 1.05f, Q * Quaternion.Euler(0, 0, 4f), new Vector3(1.3f, 1.25f, 0.03f)));
        kit.Build(p, "AH_HideRack");
    }
    static void Hearth(Transform p, Vector3 at, Vector3 face)
    {
        var kit = new Kit(); var Q = Quaternion.LookRotation(face); Vector3 side = Vector3.Cross(Vector3.up, face);
        Material st = Mat("hw_base", new Color(0.52f, 0.49f, 0.45f)), soot = Mat("hw_soot", new Color(0.12f, 0.1f, 0.09f));
        kit.Box(st, TRS(at + side * 0.9f + Vector3.up * 0.7f, Q, new Vector3(0.5f, 1.4f, 0.9f)));
        kit.Box(st, TRS(at - side * 0.9f + Vector3.up * 0.7f, Q, new Vector3(0.5f, 1.4f, 0.9f)));
        kit.Box(st, TRS(at + Vector3.up * 1.6f, Q, new Vector3(2.4f, 0.45f, 1.0f)));
        kit.Box(soot, TRS(at - face * 0.3f + Vector3.up * 0.7f, Q, new Vector3(1.3f, 1.4f, 0.1f)));
        kit.Box(st, TRS(at + face * 0.2f + Vector3.up * 0.06f, Q, new Vector3(2.6f, 0.12f, 1.4f)));
        kit.Build(p, "AH_Hearth");
        AHStations.Flames(p, at + Vector3.up * 0.15f, 0.55f, 4, new System.Random(3));
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
                    if (Inside(b.center, 2.5f)) { r.enabled = false; hidden++; }
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
                if (Inside(at, 3f)) { c.gameObject.SetActive(false); hidden++; }
            }
        }
        // gathering spots that slipped in, and their looks
        foreach (var s in AHGather.Spots.ToArray())
        {
            if (mine.Contains(s) || !Inside(s.pos, 3f)) continue;
            if (s.kind == "mine" || s.kind == "herb" || s.kind == "chop" || s.kind == "fire" || s.kind == "furnace" || s.kind == "anvil" || s.kind == "loom" || s.kind == "brew" || s.kind == "jewel")
            { HideSpot(s); AHGather.Spots.Remove(s); }
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
        Debug.Log("Ashen Hollow: town cleared of " + hidden + " old pieces");
    }
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
