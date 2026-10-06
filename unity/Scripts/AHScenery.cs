// Ashen Hollow: better bridges and ruins.
// Bridges: the river crossings on the Old Mill Road and in Kingsvale were a grey slab with two grey bars. Now each is a
// wooden footbridge: stone abutments on both banks, two beams carrying a gently arched deck of planks (each a slightly
// different shade, a few set crooked), posts with caps and a handrail and middle rail following the arch.
// Ruins: the ancient ruins (Kingsvale, the Old Mill Road, the Sunscar stone ring) were plain brown poles. Now each is a
// broken stone column: a stepped plinth, a fluted shaft of drums snapped off at a different height, a jagged top,
// moss in the cracks, and its fallen drums and rubble lying around it. The stone takes a little of the old colour, so
// the desert ones are sandstone.
// Crystals: the ice spires and rift stones are clusters of faceted glowing crystals (Stylized Crystals by FinottiGames).
// Palms and cacti: the palms (a brown stick with five flat green boards) are curved, ringed trunks with a crown of
// drooping feathery fronds and coconuts; the cacti are ribbed saguaros with bent arms and pink flowers on top
// (Models/Nature/palm_*, cactus_*, made for Ashen Hollow).
// Fossil Lands and Tidewake: the bone spikes are curved tusks of some great beast rising from a mound of earth, the
// coral (once boxes) are reef clusters of branching coral, sea whips and brain coral, and the graves have real
// headstones over earth mounds (Models/Nature/bones_*, coral_*, made for Ashen Hollow).
using System.Collections.Generic;
using UnityEngine;

public static class AHScenery
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.15f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f); m.enableInstancing = true;
        mats[key] = m; return m;
    }
    static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

    static Mesh cube, cyl, sph;
    static Mesh Prim(PrimitiveType t) { var g = AHLowPoly.Fix(GameObject.CreatePrimitive(t), t); var m = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); return m; }
    static Mesh Cube { get { if (cube == null) cube = Prim(PrimitiveType.Cube); return cube; } }
    static Mesh Cyl { get { if (cyl == null) cyl = Prim(PrimitiveType.Cylinder); return cyl; } }
    static Mesh Sph { get { if (sph == null) sph = Prim(PrimitiveType.Sphere); return sph; } }

    class Parts
    {
        readonly Dictionary<Material, List<CombineInstance>> by = new Dictionary<Material, List<CombineInstance>>();
        public void Add(Material m, Mesh mesh, Vector3 p, Quaternion r, Vector3 s)
        {
            List<CombineInstance> l; if (!by.TryGetValue(m, out l)) by[m] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(p, r, s) });
        }
        // a beam between two points, w wide and t thick (t is along 'up')
        public void Beam(Material m, Vector3 a, Vector3 b, float w, float t, Vector3 side)
        {
            Vector3 d = b - a; if (d.sqrMagnitude < 1e-6f) return;
            var rot = Quaternion.LookRotation(d.normalized, Vector3.Cross(d.normalized, side).normalized * -1f);
            Add(m, Cube, (a + b) * 0.5f, rot, new Vector3(w, t, d.magnitude));
        }
        public void Build(Transform parent)
        {
            foreach (var kv in by)
            {
                var go = new GameObject(kv.Key.name); go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = "Scenery", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
            }
        }
    }
    // hides the web game's own pieces of a thing we build anew in code (a waystone, a chest...), so the two don't show
    // through each other; returns how many were hidden
    public static int HideNear(Transform world, Vector3 at, float r, float maxFoot, float maxH)
    {
        if (world == null) return 0; int n = 0;
        foreach (var rd in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!rd.enabled || rd.name.StartsWith("AH_")) continue;
            var b = rd.bounds; Vector3 d = b.center - at; d.y = 0;
            if (d.magnitude > r || Mathf.Max(b.size.x, b.size.z) > maxFoot || b.size.y > maxH) continue;
            rd.enabled = false; n++;
        }
        return n;
    }

    static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

    public static void Setup(AHGame g, Transform world)
    {
        if (world == null) return;
        int nb = Bridges(world), nr = Ruins(g, world);
        int nc = Replace(g, world, new[] { "spire" }, "crys_ice_", 3, "Crystals", 1.4f, 2.5f, 0f, 1.1f)
               + Replace(g, world, new[] { "riftstone" }, "crys_rift_", 2, "Crystals", 1.4f, 2.5f, 0f, 1.1f);
        HidePalmBatch(g, world);
        int np = Replace(g, world, new[] { "palm" }, "palm_", 3, "Palms", 1.2f, 2.6f, 0f, 1f) + Replace(g, world, new[] { "palm2" }, "palm_", 3, "Palms", 0f, 0f, 5.4f, 1f);
        int nk = Replace(g, world, new[] { "cactus" }, "cactus_", 3, "Cacti", 1.0f, 1.3f, 0f, 1f);
        int nbo = Replace(g, world, new[] { "bonespike" }, "bones_", 3, "Bones", 1.3f, 2.2f, 0f, 1.25f);
        int nco = Replace(g, world, new[] { "coral" }, "coral_", 3, "Coral", 1.2f, 1.6f, 0f, 1.1f);
        int ngr = Graves(g, world);
        // Emberreach's obsidian spikes and the black rocks round the dragon's lair: clusters of black glass crystals with
        // a faint violet glow in their cracks; Dragonscale Isle's ferns: arching fronds instead of four flat boards
        var obsidian = new Color(0.2f, 0.16f, 0.26f, 1f);
        int nob = Replace(g, world, new[] { "obsidspike" }, "crys_rift_", 2, "Crystals", 1.4f, 2.5f, 0f, 1.1f, obsidian)
                + Replace(g, world, new[] { "lairrock" }, "crys_rift_", 2, "Crystals", 1.6f, 2.7f, 0f, 1.0f, obsidian);
        int nf = Replace(g, world, new[] { "fern" }, "fern_", 3, "Ferns", 1.2f, 1.8f, 0f, 0.5f);
        // Mirewatch's giant mushrooms (a post under a flat purple disc): domed violet and blue toadstools with pale spots
        Replace(g, world, new[] { "shroom" }, "shroom_", 2, "Mushrooms", 1.3f, 2.4f, 0f, 1.0f);
        Buildings(g, world);
        Leftovers(g, world);
        AHShips.Setup(g, world);
        AHFossils.Setup(world);
        Palaces(world);
        Castles(world);
        Volcanoes(world);
        if (nb + nr + nc + np + nk + nbo + nco + ngr + nob + nf > 0) Debug.Log("Ashen Hollow: rebuilt " + nb + " bridges, " + nr + " ruins, " + (nc + nob) + " crystal clusters, " + np + " palms, " + nk + " cacti, " + nbo + " bone spikes, " + nco + " corals, " + ngr + " graves, " + nf + " ferns");
    }

    // ---------- bridges ----------
    static int Bridges(Transform world)
    {
        int n = 0; Transform root = null;
        foreach (var t in world.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("obj") || t.childCount != 3) continue;
            var rs = new List<Renderer>();
            foreach (Transform c in t) { var r = c.GetComponent<Renderer>(); if (r != null && r.enabled) rs.Add(r); }
            if (rs.Count != 3) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float lo = Mathf.Min(b.size.x, b.size.z), hi = Mathf.Max(b.size.x, b.size.z);
            if (b.size.y < 0.8f || b.size.y > 1.4f || lo < 4f || hi > 7.5f) continue;   // a flat deck with two low rails
            bool flat = true; foreach (var r in rs) if (r.bounds.size.y > 0.8f || Mathf.Max(r.bounds.size.x, r.bounds.size.z) < 3.5f) flat = false;
            if (!flat) continue;
            bool alongX = b.size.x < b.size.z;     // the rails sit on the long sides; the crossing runs along the short one
            foreach (var r in rs) r.enabled = false;
            if (root == null) root = new GameObject("Bridges").transform;
            var br = Bridge(root, lo + 1.4f, hi, Mathf.RoundToInt(b.center.x * 3f + b.center.z));
            br.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            br.transform.rotation = Quaternion.Euler(0f, alongX ? 90f : 0f, 0f);   // built along +Z
            AHModel.SetShadows(br); n++;
        }
        return n;
    }

    // a wooden footbridge along +Z, L long, W wide, its feet at y = 0
    public static GameObject Bridge(Transform parent, float L, float W, int seed)
    {
        var root = new GameObject("Bridge"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material[] wood = { Mat("br_wood0", C(0x7a5634)), Mat("br_wood1", C(0x6a4a2c)), Mat("br_wood2", C(0x86603a)) };
        Material dark = Mat("br_dark", C(0x4a3320)), stone = Mat("br_stone", C(0x8c867a)), stone2 = Mat("br_stone2", C(0x77726a)), moss = Mat("br_moss", C(0x4f6a2e));
        float half = L * 0.5f, arch = 0.28f, deckY = 0.34f;
        System.Func<float, float> Y = z => deckY + arch * (1f - (z / half) * (z / half));
        System.Func<float, float> Slope = z => -2f * arch * z / (half * half);

        // abutments: rough stone blocks on both banks
        foreach (float e in new[] { -1f, 1f })
        {
            float z0 = e * (half - 0.35f);
            P.Add(stone, Cube, new Vector3(0, -0.02f, z0), Quaternion.identity, new Vector3(W + 0.5f, 0.5f, 1.0f));
            for (int i = 0; i < 6; i++)
            {
                float x = -W * 0.5f - 0.1f + i * (W + 0.2f) / 5f;
                P.Add(i % 2 == 0 ? stone2 : stone, Cube, new Vector3(x, 0.1f + R(rnd, -0.03f, 0.03f), z0 + e * R(rnd, -0.1f, 0.15f)),
                      Quaternion.Euler(R(rnd, -4, 4), R(rnd, -10, 10), R(rnd, -4, 4)), new Vector3(R(rnd, 0.7f, 0.95f), 0.32f, R(rnd, 0.55f, 0.8f)));
            }
            P.Add(moss, Sph, new Vector3(-W * 0.5f - 0.05f, 0.22f, z0), Quaternion.identity, new Vector3(0.6f, 0.12f, 0.5f));
            P.Add(moss, Sph, new Vector3(W * 0.5f + 0.05f, 0.2f, z0 + e * 0.2f), Quaternion.identity, new Vector3(0.45f, 0.1f, 0.4f));
        }
        // two beams under the deck, following the arch
        int seg = 8;
        foreach (float x in new[] { -W * 0.5f + 0.45f, W * 0.5f - 0.45f })
            for (int i = 0; i < seg; i++)
            {
                float za = -half + L * i / seg, zb = -half + L * (i + 1) / seg;
                P.Beam(dark, new Vector3(x, Y(za) - 0.16f, za), new Vector3(x, Y(zb) - 0.16f, zb), 0.22f, 0.2f, Vector3.right);
            }
        // planks across, each its own shade, a few crooked
        float pw = 0.3f; int np = Mathf.FloorToInt(L / (pw + 0.035f));
        for (int i = 0; i < np; i++)
        {
            float z = -half + (i + 0.5f) * L / np; float ang = Mathf.Atan(Slope(z)) * Mathf.Rad2Deg;
            float tw = rnd.NextDouble() < 0.15 ? R(rnd, -4f, 4f) : R(rnd, -1f, 1f);
            P.Add(wood[rnd.Next(3)], Cube, new Vector3(R(rnd, -0.04f, 0.04f), Y(z), z), Quaternion.Euler(-ang, tw, R(rnd, -1.2f, 1.2f)), new Vector3(W - 0.1f + R(rnd, -0.08f, 0.04f), 0.08f, pw));
        }
        // posts, handrail and middle rail on both sides
        float[] pz = { -half + 0.35f, -half * 0.33f, half * 0.33f, half - 0.35f };
        foreach (float x in new[] { -W * 0.5f + 0.12f, W * 0.5f - 0.12f })
        {
            for (int i = 0; i < pz.Length; i++)
            {
                float y0 = Y(pz[i]) - 0.2f, top = Y(pz[i]) + 1.0f;
                P.Add(wood[1], Cube, new Vector3(x, (y0 + top) * 0.5f, pz[i]), Quaternion.identity, new Vector3(0.16f, top - y0, 0.16f));
                P.Add(dark, Cube, new Vector3(x, top + 0.04f, pz[i]), Quaternion.Euler(0, 45, 0), new Vector3(0.2f, 0.08f, 0.2f));
            }
            for (int i = 0; i < pz.Length - 1; i++)
            {
                P.Beam(wood[2], new Vector3(x, Y(pz[i]) + 0.95f, pz[i]), new Vector3(x, Y(pz[i + 1]) + 0.95f, pz[i + 1]), 0.1f, 0.09f, Vector3.right);
                P.Beam(wood[0], new Vector3(x, Y(pz[i]) + 0.5f, pz[i]), new Vector3(x, Y(pz[i + 1]) + 0.5f, pz[i + 1]), 0.07f, 0.07f, Vector3.right);
            }
        }
        P.Build(root.transform);
        return root;
    }

    // ---------- crystals ----------
    // the ice spires of Frostfang, Whitepine and Highcairn and the rift stones of Kingsvale were a few plain prisms;
    // now each is a cluster of faceted glowing crystals on a rock (Stylized Crystals, FinottiGames), the size of the old one
    static readonly Dictionary<string, GameObject[]> models = new Dictionary<string, GameObject[]>();
    static GameObject[] Models(string model, int count)
    {
        GameObject[] pf;
        if (!models.TryGetValue(model, out pf)) { pf = new GameObject[count]; for (int i = 0; i < count; i++) pf[i] = Resources.Load<GameObject>("AH/Models/Nature/" + model + i); models[model] = pf; }
        return pf;
    }
    static List<Vector3> Spots(AHGame g, params string[] kinds)
    {
        var spots = new List<Vector3>(); if (g.data == null || g.data.bounds == null) return spots;
        foreach (var o in AHTents.Props())
        {
            if (System.Array.IndexOf(kinds, AHJson.S(o, "kind")) < 0) continue;
            float x = (float)AHJson.N(o, "x") * AHDB.S, z = (float)AHJson.N(o, "y") * AHDB.S;
            // the area's own props, and those of its neighbours that show along its edges (the world model reaches past
            // the bounds); out there a prop is only rebuilt where its old pieces are actually found (see Inside)
            var b = g.data.bounds; if (x < b.x0 - 90 || x > b.x1 + 90 || z < b.z0 - 90 || z > b.z1 + 90) continue;
            spots.Add(g.W(x, z));
        }
        return spots;
    }
    static bool Inside(AHGame g, Vector3 w)
    {
        var b = g.data.bounds; var p = g.ToWeb(w);
        return p.x >= b.x0 - 1 && p.x <= b.x1 + 1 && p.y >= b.z0 - 1 && p.y <= b.z1 + 1;
    }
    // replaces the props of these kinds: hides the old pieces within 'near' metres (footprint under maxFoot) and puts
    // one of the models there, as tall as the old one (or 'fallbackH' when there was nothing to measure)
    static readonly Dictionary<string, Material> tinted = new Dictionary<string, Material>();
    static int Replace(AHGame g, Transform world, string[] kinds, string model, int count, string rootName, float near, float maxFoot, float fallbackH, float widen, Color? tint = null)
    {
        var pf = Models(model, count); if (pf[0] == null) return 0;
        var spots = Spots(g, kinds); if (spots.Count == 0) return 0;
        var rs = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds;
            if (Mathf.Max(bb.size.x, bb.size.z) > maxFoot || bb.size.y > 9f) continue; rs.Add(r);
        }
        Transform root = null; int n = 0;
        foreach (var s in spots)
        {
            Bounds all = new Bounds(); bool any = false;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; Vector3 c = r.bounds.center - s; c.y = 0; if (c.magnitude > near) continue;
                if (!any) { all = r.bounds; any = true; } else all.Encapsulate(r.bounds);
                r.enabled = false;
            }
            if (!any && (fallbackH <= 0f || !Inside(g, s))) continue;
            if (root == null) { var ex = GameObject.Find(rootName); root = ex != null ? ex.transform : new GameObject(rootName).transform; }
            var rnd = new System.Random(Mathf.RoundToInt(s.x * 17f + s.z * 3f));
            var go = Object.Instantiate(pf[rnd.Next(count)], root, false).transform; go.name = model.TrimEnd('_');
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            float h = any ? Mathf.Clamp(all.size.y, 1.0f, 8f) * 1.05f : fallbackH * (0.9f + 0.2f * (float)rnd.NextDouble());
            Vector3 at = any ? new Vector3(all.center.x, all.min.y - 0.08f, all.center.z) : g.Resolve(s, 0.1f);
            if (!any) at = new Vector3(s.x, at.y, s.z);
            go.position = at;
            go.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            go.localScale = new Vector3(h * widen, h, h * widen);
            if (tint.HasValue)
                foreach (var r2 in go.GetComponentsInChildren<Renderer>(true))
                {
                    var src = r2.sharedMaterial; if (src == null) continue;
                    string key = src.name + ColorUtility.ToHtmlStringRGB(tint.Value); Material tm;
                    if (!tinted.TryGetValue(key, out tm) || tm == null)
                    {
                        tm = new Material(src); tm.enableInstancing = true;
                        foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (tm.HasProperty(pr)) tm.SetColor(pr, tint.Value);
                        tinted[key] = tm;
                    }
                    r2.sharedMaterial = tm;
                }
            AHModel.SetShadows(go.gameObject); n++;
        }
        return n;
    }
    // the palms of Tidewake and Sunspire were drawn in one batch (a brown trunk mesh and a green crown mesh): hide those
    static void HidePalmBatch(AHGame g, Transform world)
    {
        if (Spots(g, "palm2").Count == 0) return;
        var trunk = new Color(0.2542f, 0.1441f, 0.0578f); var crown = new Color(0.0497f, 0.3231f, 0.0423f);
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || !r.name.StartsWith("AH_INST") || r.sharedMaterial == null) continue;
            Color c; var m = r.sharedMaterial;
            if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); else continue;
            if (Near(c, trunk) || Near(c, crown) || Near(c.linear, trunk) || Near(c.linear, crown) || Near(c.gamma, trunk) || Near(c.gamma, crown)) r.enabled = false;
        }
    }
    static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f; }

    // ---------- graves ----------
    // the Fossil Lands' graves were a grey slab, a brown box and a black square: now a weathered headstone (round-topped,
    // a cross, or one broken in two) leaning a little, with moss, over a low mound of earth with pebbles and dead flowers
    static int Graves(AHGame g, Transform world)
    {
        var spots = Spots(g, "grave"); if (spots.Count == 0) return 0;
        var rs = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds;
            if (Mathf.Max(bb.size.x, bb.size.z) > 2f || bb.size.y > 2f) continue; rs.Add(r);
        }
        Transform root = null; int n = 0;
        foreach (var s in spots)
        {
            Renderer head = null, mound = null; float ground = float.PositiveInfinity;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; Vector3 c = r.bounds.center - s; c.y = 0; if (c.magnitude > 1.1f) continue;
                if (head == null || r.bounds.size.y > head.bounds.size.y) head = r;
                float f = r.bounds.size.x * r.bounds.size.z; if (r.bounds.size.y > 0.05f && (mound == null || f > mound.bounds.size.x * mound.bounds.size.z) && r.bounds.size.y < 0.6f) mound = r;
                ground = Mathf.Min(ground, r.bounds.min.y);
                r.enabled = false;
            }
            if (head == null) continue;
            Vector3 dir = mound != null ? mound.bounds.center - head.bounds.center : Vector3.forward; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = head.bounds.size.x > head.bounds.size.z ? Vector3.forward : Vector3.right;
            if (root == null) root = new GameObject("Graves").transform;
            int seed = Mathf.RoundToInt(s.x * 13f + s.z * 29f);
            var gr = Grave(root, seed);
            gr.transform.position = new Vector3(head.bounds.center.x, float.IsInfinity(ground) ? head.bounds.min.y : ground, head.bounds.center.z);
            gr.transform.rotation = Quaternion.LookRotation(dir.normalized);
            AHModel.SetShadows(gr); n++;
        }
        return n;
    }

    // a grave along +Z: the headstone at the origin, the mound in front of it
    public static GameObject Grave(Transform parent, int seed)
    {
        var root = new GameObject("Grave"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material st = Mat("grave_stone", C(0x8c8a82)), st2 = Mat("grave_stone2", C(0x6e6c66)), earth = Mat("grave_earth", C(0x6e563a)), earth2 = Mat("grave_earth2", C(0x5e4a32)),
                 moss = Mat("grave_moss", C(0x56682e)), dead = Mat("grave_dead", C(0x7a5a3a)), petal = Mat("grave_petal", C(0x8a3a4a));
        int kind = rnd.Next(3);
        var lean = Quaternion.Euler(R(rnd, -9f, 4f), R(rnd, -6f, 6f), R(rnd, -6f, 6f));
        if (kind == 0)        // round-topped headstone
        {
            P.Add(st, Cube, lean * new Vector3(0, 0.42f, 0), lean, new Vector3(0.72f, 0.84f, 0.16f));
            P.Add(st, Cyl, lean * new Vector3(0, 0.84f, 0), lean * Quaternion.Euler(90, 0, 0), new Vector3(0.72f, 0.08f, 0.72f));
            P.Add(st2, Cube, lean * new Vector3(0, 0.62f, -0.085f), lean, new Vector3(0.36f, 0.04f, 0.01f));     // carved lines
            P.Add(st2, Cube, lean * new Vector3(0, 0.5f, -0.085f), lean, new Vector3(0.44f, 0.035f, 0.01f));
            P.Add(st2, Cube, lean * new Vector3(0, 0.4f, -0.085f), lean, new Vector3(0.3f, 0.035f, 0.01f));
        }
        else if (kind == 1)   // a stone cross
        {
            P.Add(st, Cube, lean * new Vector3(0, 0.55f, 0), lean, new Vector3(0.16f, 1.1f, 0.14f));
            P.Add(st, Cube, lean * new Vector3(0, 0.8f, 0), lean, new Vector3(0.62f, 0.15f, 0.14f));
            P.Add(st2, Cube, new Vector3(0, 0.06f, 0), Quaternion.Euler(0, R(rnd, -5, 5), 0), new Vector3(0.42f, 0.14f, 0.3f));
        }
        else                  // broken: the stump and its top lying in the grass
        {
            P.Add(st, Cube, lean * new Vector3(0, 0.26f, 0), lean, new Vector3(0.7f, 0.52f, 0.16f));
            P.Add(st2, Cube, lean * new Vector3(-0.12f, 0.55f, 0), lean * Quaternion.Euler(0, 0, 18), new Vector3(0.3f, 0.12f, 0.16f));
            P.Add(st, Cube, new Vector3(0.62f, 0.07f, 0.4f), Quaternion.Euler(84, R(rnd, -40, 40), 0), new Vector3(0.66f, 0.42f, 0.14f));
        }
        P.Add(moss, Sph, lean * new Vector3(R(rnd, -0.25f, 0.25f), 0.08f, 0.05f), Quaternion.identity, new Vector3(0.32f, 0.12f, 0.18f));
        // the mound
        // a low mound of earth, smooth along its length
        P.Add(earth, Sph, new Vector3(0, -0.04f, 0.82f), Quaternion.Euler(0, R(rnd, -4, 4), 0), new Vector3(0.86f, 0.3f, 1.55f));
        P.Add(earth2, Sph, new Vector3(R(rnd, -0.08f, 0.08f), -0.02f, 0.7f), Quaternion.Euler(0, R(rnd, -8, 8), 0), new Vector3(0.62f, 0.26f, 1.1f));
        var grass = Mat("grave_grass", C(0x6a7a36));
        for (int i = 0; i < 7; i++)
        {
            float a = R(rnd, 0, Mathf.PI * 2), x = Mathf.Cos(a) * R(rnd, 0.35f, 0.5f), z = 0.8f + Mathf.Sin(a) * R(rnd, 0.6f, 0.9f), h = R(rnd, 0.08f, 0.16f);
            for (int k = 0; k < 3; k++) P.Add(grass, Cube, new Vector3(x + k * 0.025f, h * 0.5f, z), Quaternion.Euler(R(rnd, -25, 25), R(rnd, 0, 180), R(rnd, -25, 25)), new Vector3(0.012f, h, 0.012f));
        }
        for (int i = 0; i < 6; i++)
            P.Add(i % 2 == 0 ? st2 : st, Sph, new Vector3(R(rnd, -0.45f, 0.45f), 0.06f, R(rnd, 0.1f, 1.55f)), Quaternion.Euler(0, R(rnd, 0, 90), 0), Vector3.one * R(rnd, 0.07f, 0.13f));
        if (rnd.NextDouble() < 0.6)   // withered flowers by the stone
            for (int i = 0; i < 4; i++)
            {
                float x = R(rnd, -0.2f, 0.2f), z = R(rnd, 0.15f, 0.3f), h = R(rnd, 0.14f, 0.24f);
                P.Add(dead, Cube, new Vector3(x, h * 0.5f + 0.05f, z), Quaternion.Euler(R(rnd, -20, 20), 0, R(rnd, -20, 20)), new Vector3(0.015f, h, 0.015f));
                P.Add(petal, Sph, new Vector3(x, h + 0.05f, z), Quaternion.identity, Vector3.one * 0.045f);
            }
        P.Build(root.transform);
        return root;
    }

    // ---------- windmill, keep and wells ----------
    // Kingsvale's windmill was a box with a red lid and four flat boards: now a timbered windmill (Fantasy Landscape) whose
    // sails turn in the wind. Its keep was a grey block under a huge blue cone: now a tall KayKit watchtower with a
    // lookout and pennant. The wells in Hollow Meadow and your homestead: KayKit's roofed well with its bucket.
    static void Buildings(AHGame g, Transform world)
    {
        string col = AHCityColour();
        foreach (var s in Spots(g, "windmill"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 2.8f, 9f, 12f, out b, out face)) continue;
            var pf = Resources.Load<GameObject>("AH/Models/Nature/windmill"); var bl = Resources.Load<GameObject>("AH/Models/Nature/windmill_blade");
            if (pf == null) continue;
            var root = new GameObject("Windmill").transform;
            root.position = new Vector3(b.center.x, b.min.y, b.center.z);
            root.rotation = face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(-face.normalized) : Quaternion.identity;   // its sails are on the model's -Z side
            float k = Mathf.Max(6.5f, b.size.y * 1.15f) / 0.272f; root.localScale = Vector3.one * k;
            Strip(Object.Instantiate(pf, root, false));
            if (bl != null)
            {
                var hub = new GameObject("Sails").transform; hub.SetParent(root, false); hub.localPosition = new Vector3(0f, 0.205f, -0.04f);
                var bb = Strip(Object.Instantiate(bl, hub, false)); bb.transform.localPosition = new Vector3(0f, -0.0195f, 0f);
                hub.gameObject.AddComponent<AHSpin>().speed = 22f;
            }
            AHModel.SetShadows(root.gameObject);
        }
        foreach (var s in Spots(g, "keep"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 5f, 16f, 18f, out b, out face)) continue;   // the keep and its two side towers
            var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_tower_B_" + col); if (pf == null) continue;
            var go = Strip(Object.Instantiate(pf)); go.name = "Keep";
            var mb = Measure(go); float k = 13f / Mathf.Max(0.1f, mb.size.y);
            go.transform.localScale = Vector3.one * k; go.transform.position = new Vector3(s.x, b.min.y, s.z);
            go.transform.rotation = Quaternion.Euler(0, 45f, 0);
            AHModel.SetShadows(go);
            var pa = Resources.Load<GameObject>("AH/Models/KK/kk_building_tower_A_" + col);
            if (pa != null)
                foreach (float sx in new[] { -4.2f, 4.2f })
                {
                    var t = Strip(Object.Instantiate(pa)); t.name = "Keep tower";
                    var tb = Measure(t); float kt = 11f / Mathf.Max(0.1f, tb.size.y);
                    t.transform.localScale = Vector3.one * kt; t.transform.position = new Vector3(s.x + sx, b.min.y, s.z);
                    AHModel.SetShadows(t);
                }
        }
        // the web game's lone watchtowers (a cylinder under a cone) outside the walled cities: KayKit watchtowers
        foreach (var s in Spots(g, "tower"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 2.4f, 7f, 16f, out b, out face)) continue;
            var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_tower_A_" + col); if (pf == null) continue;
            var go = Strip(Object.Instantiate(pf)); go.name = "Watchtower";
            var mb = Measure(go); float k = Mathf.Max(6f, b.size.y) / Mathf.Max(0.1f, mb.size.y);
            go.transform.localScale = Vector3.one * k; go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.rotation = Quaternion.Euler(0, (Mathf.Abs(s.x * 7f + s.z * 3f) % 6) * 60f, 0);
            AHModel.SetShadows(go);
        }
        // Tidewake's shipwreck (a brown box with a post): the ribs of a beached ship listing in the sand, its keel, the
        // planks still on one side, a snapped mast and a torn sail
        foreach (var s in Spots(g, "wreck"))
        {
            Renderer hull = null;
            foreach (var r in world.GetComponentsInChildren<Renderer>(true)) { if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds; Vector3 d = bb.center - s; d.y = 0; if (d.magnitude < 3f && bb.size.y > 1.5f && bb.size.y < 3f && Mathf.Max(bb.size.x, bb.size.z) > 6f) hull = r; }
            Vector3 dir = Vector3.forward;
            if (hull != null) { var mf = hull.GetComponent<MeshFilter>(); if (mf != null && mf.sharedMesh != null) { var ms = mf.sharedMesh.bounds.size; Vector3 ls = hull.transform.lossyScale; dir = hull.transform.TransformDirection(Mathf.Abs(ms.x * ls.x) > Mathf.Abs(ms.z * ls.z) ? Vector3.right : Vector3.forward); dir.y = 0; } }
            Vector3 face; Bounds b; if (!Take(world, s, 5.5f, 10f, 6f, out b, out face)) continue;
            var w = Wreck(null, 7);
            w.transform.position = new Vector3(b.center.x, b.min.y - 0.3f, b.center.z);
            w.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward);
            AHModel.SetShadows(w);
        }
        // the city statues (a grey block of a man): a stone statue of a knight on the old plinth, facing the way in
        foreach (var s in Spots(g, "statue"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 0.9f, 1.2f, 3.5f, out b, out face)) continue;
            var holder = new GameObject("Statue").transform; holder.position = new Vector3(b.center.x, b.min.y, b.center.z);
            AHAnim an; GameObject hero = null;
            try { hero = AHPeople.BuildHero(holder, AHClasses.Get("warrior"), new AHLook { hair = "short", beard = "full" }, g, out an); } catch { an = null; }
            if (hero == null) { Object.Destroy(holder.gameObject); continue; }
            foreach (var c in hero.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            var stone = Mat("statue_stone", C(0x9a968c), 0.1f);
            foreach (var r in hero.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = stone; r.sharedMaterials = ms; }
            var hb = Measure(hero); float k = Mathf.Max(1.8f, b.size.y * 1.05f) / Mathf.Max(0.1f, hb.size.y);
            holder.localScale = Vector3.one * k;
            Vector3 to = (g.data != null && g.data.spawn != null ? g.W(g.data.spawn.x, g.data.spawn.z) : holder.position + Vector3.back) - holder.position; to.y = 0;
            if (to.sqrMagnitude > 0.01f) holder.rotation = Quaternion.LookRotation(to.normalized);
            if (an != null) { an.Play(an.Has("Sword_Idle") ? "Sword_Idle" : "Idle", true); an.Hold(an.Has("Sword_Idle") ? "Sword_Idle" : "Idle", 0.25f); holder.gameObject.AddComponent<AHStatueFreeze>().anim = an; }
            AHModel.SetShadows(holder.gameObject);
        }
        // a garden round each city statue: a stepped plinth, flowerbeds in a ring, benches facing it and shade trees
        if (AHGame.AreaId == "city" || AHGame.AreaId.EndsWith("_city"))
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == "Statue") Garden(g, go.transform.position, go.transform.rotation);
        // the desert obelisks (a black post with a gold cube): a tall tapering sandstone obelisk on a stepped base, carved
        // bands of glyphs, a gilded pyramid tip and a few fallen blocks in the sand
        foreach (var s in Spots(g, "obelisk"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 1.3f, 2f, 7f, out b, out face)) continue;
            var o = Obelisk(Mathf.Max(5f, b.size.y * 1.15f), Mathf.RoundToInt(s.x + s.z));
            o.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); AHModel.SetShadows(o);
        }
        // Varrow's training dummies (a stick, a brown barrel and a ball): straw-stuffed sacks on a post with a crossbar for
        // arms, bound with rope, a painted target on the chest and a dented bucket for a helmet
        var dummies = new List<Vector3>();
        foreach (var s in Spots(g, "dummy"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 0.9f, 1.4f, 2.6f, out b, out face)) continue;
            var d = Dummy(Mathf.RoundToInt(s.x * 3f + s.z));
            d.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            Vector3 to = (g.data != null && g.data.spawn != null ? g.W(g.data.spawn.x, g.data.spawn.z) : d.transform.position + Vector3.back) - d.transform.position; to.y = 0;
            if (to.sqrMagnitude > 0.01f) d.transform.rotation = Quaternion.LookRotation(to.normalized);
            AHModel.SetShadows(d); dummies.Add(d.transform.position);
        }
        if (dummies.Count >= 2) TrainingYard(g, dummies, col);
        foreach (var s in Spots(g, "well"))
        {
            Vector3 face; Bounds b; if (!Take(world, s, 2.2f, 4.2f, 3.2f, out b, out face)) continue;
            var pf = Resources.Load<GameObject>("AH/Models/KK/kk_building_well_" + col); if (pf == null) continue;
            var go = Strip(Object.Instantiate(pf)); go.name = "Well";
            var mb = Measure(go); float k = Mathf.Max(b.size.x, b.size.z) * 0.85f / Mathf.Max(0.1f, Mathf.Max(mb.size.x, mb.size.z));
            go.transform.localScale = Vector3.one * k; go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
            AHModel.SetShadows(go);
        }
    }
    static string AHCityColour() { var a = AHGame.AreaId; return a == "frost" || a == "kingsroad" || a == "tide" || a == "city" || a == "hc_city" || a == "vale" ? "blue" : a == "sands" || a == "isle" || a == "ss_city" ? "yellow" : a == "ember" || a == "ch_city" ? "red" : "green"; }



    // ---------- the last of the web game's plain shapes ----------
    // basalt columns (black hexagonal sticks), market stalls (a box with four sticks, a flat lid and balls for fruit),
    // brewing cauldrons (a black cylinder), looms and jewellers' benches (boxes and sticks), the wild pillars (a brown
    // block), Dragonscale's boulders (a lumpy ball), Kingsvale's standing stones (a box), the lava vents (a black
    // cylinder with a yellow disc), the ember throne and Cinderhold's brazier: each rebuilt where its old pieces stand
    static object worldData;
    // the neighbours' herbs and ore rocks that show past the area's edges (a green blob with coloured balls, a grey
    // lump with orange cubes): the herbs become plants like the area's own, the rocks real rocks
    static int EdgeOdds(Transform world, Transform root)
    {
        int n = 0;
        foreach (Transform grp in world)
        {
            if (!grp.name.StartsWith("obj")) continue;
            var rs = grp.GetComponentsInChildren<Renderer>(false);
            if (rs.Length == 2 && rs[0].enabled && rs[1].enabled)
            {
                // a road marker: a square grey post with a flat cap -> a weathered stone with a carved cap
                Renderer post = rs[0].bounds.size.y > rs[1].bounds.size.y ? rs[0] : rs[1], cap = post == rs[0] ? rs[1] : rs[0];
                var pb = post.bounds; var cb = cap.bounds;
                if (pb.size.y > 1.8f && pb.size.y < 3.2f && Mathf.Max(pb.size.x, pb.size.z) < 1.0f && cb.size.y < 0.4f && cb.min.y > pb.center.y)
                {
                    var gp = GatePost(root, pb.size.y + cb.size.y, Mathf.Abs(Mathf.RoundToInt(pb.center.x * 3 + pb.center.z)));
                    gp.transform.position = new Vector3(pb.center.x, pb.min.y, pb.center.z); gp.transform.localScale = Vector3.one * 0.62f; AHModel.SetShadows(gp);
                    post.enabled = false; cap.enabled = false; n++;
                }
                continue;
            }
            if (rs.Length < 4 || rs.Length > 16) continue;
            bool allOn = true; foreach (var r in rs) if (!r.enabled) allOn = false;
            if (!allOn) continue;
            int stones = 0, flames = 0;
            foreach (var r in rs) { Color c; if (!Col(r, out c)) continue; var bs = r.bounds.size; if (Mathf.Abs(bs.x - 0.3f) < 0.06f && Mathf.Abs(bs.y - 0.3f) < 0.06f && c.r < 0.6f && Mathf.Abs(c.r - c.b) < 0.1f) stones++; if (c.r > 0.95f && c.b < 0.5f && bs.y > 0.3f && bs.y < 0.9f) flames++; }
            if (stones >= 6 && flames >= 2)
            {
                // a camp fire of grey balls and orange cones: the stone-ringed fire with logs and live flames
                Vector3 fc = Vector3.zero; float fy = float.MaxValue; foreach (var r in rs) { fc += r.bounds.center; fy = Mathf.Min(fy, r.bounds.min.y); } fc /= rs.Length;
                var cf = AHStations.Campfire(root, 1f, Mathf.Abs(Mathf.RoundToInt(fc.x * 5 + fc.z)));
                cf.transform.position = new Vector3(fc.x, fy, fc.z); AHModel.SetShadows(cf);
                foreach (var r in rs) r.enabled = false; n++; continue;
            }
            if (rs.Length > 12) continue;
            Renderer body = null; int balls = 0, cubes = 0; Color ball = Color.yellow; bool other = false;
            foreach (var r in rs)
            {
                var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) { other = true; break; }
                long tri = 0; for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) tri += mf.sharedMesh.GetIndexCount(k) / 3;
                var bs = r.bounds.size; float foot = Mathf.Max(bs.x, bs.z);
                if (foot < 0.35f && bs.y < 0.35f) { if (tri >= 40) { balls++; Color c; if (Col(r, out c)) ball = c; } else cubes++; }
                else if (body == null && foot > 0.8f && foot < 2.7f && bs.y < 1.6f) body = r;
                else { other = true; break; }
            }
            if (other || body == null) continue;
            var bb = body.bounds; Vector3 at = new Vector3(bb.center.x, bb.min.y, bb.center.z); int seed = Mathf.Abs(Mathf.RoundToInt(at.x * 13 + at.z * 7));
            if (balls >= 4 && cubes == 0)
            {
                // which herb: by the colour of its flowers
                string t = ball.b > 0.7f && ball.r < 0.8f ? "frostbloom" : ball.r > 0.9f && ball.g < 0.5f ? "emberthorn" : ball.r > 0.8f && ball.b > 0.6f && ball.g < 0.75f ? "reefmoss" : ball.b > ball.g && ball.r < 0.8f ? "mireroot" : ball.r > 0.85f && ball.g > 0.85f && ball.b > 0.75f ? "ashbloom" : "sunpetal";
                Renderer leaf; GameObject fl; Color lc;
                var h = AHHerbs.Build(t, seed, out leaf, out fl, out lc); if (h == null) continue;
                h.transform.SetParent(root, true); h.transform.position = at; h.transform.rotation = Quaternion.Euler(0, seed % 360, 0);
                foreach (var c in h.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            }
            else if (cubes >= 3 && balls == 0)
            {
                var pf = Resources.Load<GameObject>("AH/Models/KK/kk_rock_single_" + "BCDE"[seed % 4]); if (pf == null) continue;
                var go = Strip(Object.Instantiate(pf, root, false)); var mb = Measure(go);
                go.transform.localScale = new Vector3(bb.size.x * 1.15f / Mathf.Max(0.1f, mb.size.x), bb.size.y * 1.15f / Mathf.Max(0.1f, mb.size.y), bb.size.z * 1.15f / Mathf.Max(0.1f, mb.size.z));
                go.transform.position = at - Vector3.up * 0.1f; go.transform.rotation = Quaternion.Euler(0, seed % 360, 0); AHModel.SetShadows(go);
            }
            else continue;
            foreach (var r in rs) r.enabled = false; n++;
        }
        return n;
    }
    static bool Col(Renderer r, out Color c)
    {
        c = Color.white; var m = r.sharedMaterial; if (m == null) return false;
        if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); else return false;
        return true;
    }
    static void Leftovers(AHGame g, Transform world)
    {
        int n = 0; var root = new GameObject("Rebuilt props").transform;
        Vector3 mid = g.data != null && g.data.spawn != null ? g.W(g.data.spawn.x, g.data.spawn.z) : Vector3.zero;
        System.Func<Vector3, Vector3, Quaternion> Face = (at, f) => { Vector3 d = f.sqrMagnitude > 1e-3f ? f : mid - at; d.y = 0; return d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d.normalized) : Quaternion.identity; };
        Bounds b; Vector3 face;
        foreach (var sp in Spots(g, "basalt"))
            if (Take(world, sp, 1.3f, 1.1f, 4.2f, out b, out face))
            { var go = AHStations.Basalt(root, Mathf.Max(2.4f, b.size.y * 1.15f), Mathf.Max(b.size.x, b.size.z) + 0.6f, Mathf.RoundToInt(sp.x * 7 + sp.z)); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "stall"))
            if (Take(world, sp, 1.6f, 2.9f, 3.2f, out b, out face))
            {
                var go = AHStations.Stall(root, 2.2f, 1.5f, Mathf.Abs(Mathf.RoundToInt(sp.x * 3 + sp.z * 5)));
                go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, Vector3.zero);
                AHModel.SetShadows(go); n++;
            }
        foreach (var sp in Spots(g, "brew"))
            if (Take(world, sp, 1.2f, 1.4f, 1.6f, out b, out face))
            { var go = AHStations.Cauldron(root, Mathf.RoundToInt(sp.x + sp.z)); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, Vector3.zero); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "loom"))
            if (Take(world, sp, 1.0f, 1.2f, 1.6f, out b, out face))
            { var go = AHStations.Loom(root, Mathf.Abs(Mathf.RoundToInt(sp.x + sp.z))); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, Vector3.zero); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "jewel"))
            if (Take(world, sp, 1.1f, 1.9f, 1.6f, out b, out face))
            { var go = AHStations.JewelBench(root, Mathf.RoundToInt(sp.x + sp.z)); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, Vector3.zero); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "pillar"))
            if (Take(world, sp, 1.9f, 1.6f, 6.5f, out b, out face))
            { var go = Ruin(root, Mathf.Max(3.5f, b.size.y), new Color(0.6f, 0.58f, 0.54f), Mathf.RoundToInt(sp.x * 3 + sp.z)); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "boulder"))
            if (Take(world, sp, 1.2f, 2.3f, 1.8f, out b, out face))
            {
                var pf = Resources.Load<GameObject>("AH/Models/KK/kk_rock_single_" + "BCDE"[Mathf.Abs(Mathf.RoundToInt(sp.x + sp.z)) % 4]); if (pf == null) continue;
                var go = Strip(Object.Instantiate(pf, root, false)); var mb = Measure(go);
                go.transform.localScale = new Vector3(b.size.x * 1.25f / Mathf.Max(0.1f, mb.size.x), b.size.y * 1.2f / Mathf.Max(0.1f, mb.size.y), b.size.z * 1.25f / Mathf.Max(0.1f, mb.size.z));
                go.transform.position = new Vector3(b.center.x, b.min.y - 0.1f, b.center.z); go.transform.rotation = Quaternion.Euler(0, sp.x * 37 % 360, 0); AHModel.SetShadows(go); n++;
            }
        foreach (var sp in Spots(g, "stone"))
            if (Take(world, sp, 0.9f, 1.5f, 3.4f, out b, out face))
            { var go = Menhir(root, Mathf.Max(2.2f, b.size.y * 1.05f), Mathf.RoundToInt(sp.x * 3 + sp.z)); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); AHModel.SetShadows(go); n++; }
        foreach (var sp in Spots(g, "throne"))
            if (Take(world, sp, 3.5f, 4f, 6.5f, out b, out face))
            {
                var go = AHStations.Throne(root); go.transform.localScale = Vector3.one * 0.6f; go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, face); AHModel.SetShadows(go); n++;
                // an obsidian cluster made from the old throne's dark pieces would sit on the seat: clear the dais
                foreach (var rg in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (rg.name != "Crystals") continue;
                    foreach (Transform c in rg.transform)
                    { Vector3 d = c.position - go.transform.position; d.y = 0; if (d.magnitude < 3.6f) Object.Destroy(c.gameObject); }
                }
            }
        foreach (var sp in Spots(g, "brazier"))
            if (Take(world, sp, 1.9f, 3.8f, 2.6f, out b, out face))
            { var go = AHStations.Brazier(root, 1.2f, Mathf.RoundToInt(sp.x)); go.transform.localScale = Vector3.one * 1.7f; go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); n++; }
        // camp huts that were plain boxes with a door: a canvas ridge tent
        foreach (var sp in Spots(g, "tent"))
            if (Take(world, sp, 2.4f, 4.8f, 2.8f, out b, out face))
            { var go = AHTents.Ridge(root, Mathf.RoundToInt(sp.x * 5 + sp.z)); go.transform.localScale = Vector3.one * Mathf.Clamp(Mathf.Max(b.size.x, b.size.z) / 3f, 1.1f, 1.7f); go.transform.position = new Vector3(b.center.x, b.min.y, b.center.z); go.transform.rotation = Face(go.transform.position, face); AHModel.SetShadows(go); n++; }
        // the posts either side of the roads between regions (a plain grey block): stone gate posts with a lantern
        var posts = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var rb = r.bounds;
            if (Mathf.Abs(rb.size.y - 5.5f) < 0.2f && Mathf.Abs(rb.size.x - 1.4f) < 0.15f && Mathf.Abs(rb.size.z - 1.4f) < 0.15f) posts.Add(r);
        }
        foreach (var r in posts)
        {
            var rb = r.bounds; r.enabled = false;
            HideNear(world, new Vector3(rb.center.x, rb.min.y, rb.center.z), 0.9f, 1.8f, 1.2f);   // its cap
            var go = GatePost(root, 5.2f, Mathf.RoundToInt(rb.center.x * 3 + rb.center.z)); go.transform.position = new Vector3(rb.center.x, rb.min.y, rb.center.z); AHModel.SetShadows(go); n++;
        }
        // the lava vents (their own list in the web game)
        if (worldData == null) { var ta = Resources.Load<TextAsset>("AH/Data/world"); if (ta != null) worldData = AHJson.Parse(ta.text); }
        var vents = AHJson.A(worldData, "VENTS");
        if (vents != null && g.data != null)
        {
            var hot = Mat("vent_lava", C(0xFF6A1A), 0.5f); hot.EnableKeyword("_EMISSION"); hot.SetColor("_EmissionColor", C(0xFF5A10).linear * 2.4f);
            var bb = g.data.bounds;
            foreach (var v in vents)
            {
                float x = (float)AHJson.N(v, "x") * AHDB.S, z = (float)AHJson.N(v, "y") * AHDB.S;
                if (x < bb.x0 - 90 || x > bb.x1 + 90 || z < bb.z0 - 90 || z > bb.z1 + 90) continue;
                var sp = g.W(x, z);
                if (!Take(world, sp, 1.8f, 3.5f, 1.0f, out b, out face)) continue;
                float r = Mathf.Max(b.size.x, b.size.z) * 0.4f; Vector3 at = new Vector3(b.center.x, b.min.y, b.center.z);
                var disc = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); Object.Destroy(disc.GetComponent<Collider>()); disc.name = "Vent lava";
                disc.transform.SetParent(root, false); disc.transform.position = at + Vector3.up * 0.03f; disc.transform.localScale = new Vector3(r * 2f, 0.02f, r * 2f);
                var dr = disc.GetComponent<Renderer>(); dr.sharedMaterial = hot; dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                AHStations.LavaDress(root, at, r, Mathf.RoundToInt(x + z)); n++;
            }
        }
        n += EdgeOdds(world, root);
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " plain old props rebuilt (basalt, stalls, cauldrons, looms, benches, pillars, stones, vents)"); else Object.Destroy(root.gameObject);
    }

    // a gate post: a stepped plinth, a shaft of dressed blocks with a slight taper, a moulded cap and an iron lantern
    // on a bracket that glows at night
    static GameObject GatePost(Transform parent, float h, int seed)
    {
        var root = new GameObject("Gate post"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material st = Mat("gp_stone", C(0x9A948A), 0.12f), st2 = Mat("gp_stone2", C(0x7E786E), 0.12f), iron = Mat("gp_iron", C(0x2A2A2E), 0.45f), glow = Mat("gp_glow", C(0xFFC860), 0.3f);
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", C(0xFFB040).linear * 2f);
        P.Add(st2, Cube, new Vector3(0, 0.2f, 0), Quaternion.identity, new Vector3(1.7f, 0.4f, 1.7f));
        P.Add(st, Cube, new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(1.45f, 0.2f, 1.45f));
        float y = 0.6f; int k = 0;
        while (y < h - 0.8f)
        {
            float bh = 0.55f + (float)rnd.NextDouble() * 0.1f, w = Mathf.Lerp(1.15f, 0.95f, y / h);
            P.Add(k % 2 == 0 ? st : st2, Cube, new Vector3(0, y + bh / 2f, 0), Quaternion.Euler(0, ((float)rnd.NextDouble() - 0.5f) * 3f, 0), new Vector3(w, bh - 0.03f, w));
            y += bh; k++;
        }
        P.Add(st2, Cube, new Vector3(0, y + 0.1f, 0), Quaternion.identity, new Vector3(1.25f, 0.2f, 1.25f));
        P.Add(st, Cube, new Vector3(0, y + 0.3f, 0), Quaternion.identity, new Vector3(1.05f, 0.2f, 1.05f));
        P.Add(st2, Cube, new Vector3(0, y + 0.55f, 0), Quaternion.Euler(0, 45, 0), new Vector3(0.7f, 0.3f, 0.7f));
        P.Add(st, Sph, new Vector3(0, y + 0.85f, 0), Quaternion.identity, Vector3.one * 0.45f);
        // the lantern on a bracket
        float ly = h * 0.62f;
        P.Add(iron, Cube, new Vector3(0, ly + 0.25f, 0.75f), Quaternion.identity, new Vector3(0.06f, 0.06f, 0.5f));
        P.Add(iron, Cube, new Vector3(0, ly + 0.08f, 0.62f), Quaternion.Euler(45, 0, 0), new Vector3(0.05f, 0.05f, 0.35f));
        P.Add(iron, Cube, new Vector3(0, ly + 0.1f, 0.98f), Quaternion.identity, new Vector3(0.02f, 0.25f, 0.02f));
        P.Add(iron, Cube, new Vector3(0, ly - 0.05f, 0.98f), Quaternion.identity, new Vector3(0.26f, 0.04f, 0.26f));
        P.Add(glow, Cube, new Vector3(0, ly - 0.22f, 0.98f), Quaternion.identity, new Vector3(0.18f, 0.28f, 0.18f));
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f }) P.Add(iron, Cube, new Vector3(sx * 0.11f, ly - 0.22f, 0.98f + sz * 0.11f), Quaternion.identity, new Vector3(0.025f, 0.32f, 0.025f));
        P.Add(iron, Cube, new Vector3(0, ly - 0.4f, 0.98f), Quaternion.identity, new Vector3(0.24f, 0.04f, 0.24f));
        P.Build(root.transform);
        return root;
    }

    // a standing stone: a tall slab, slightly leaning and tapering, weathered in three blocks, with lichen patches
    static GameObject Menhir(Transform parent, float h, int seed)
    {
        var root = new GameObject("Standing stone"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material st = Mat("menhir", C(0x8E8A80), 0.1f), st2 = Mat("menhir2", C(0x7A766C), 0.1f), lichen = Mat("lichen", C(0x8AA050), 0.05f);
        float lean = ((float)rnd.NextDouble() - 0.5f) * 8f; var rot = Quaternion.Euler(lean, (float)rnd.NextDouble() * 180f, lean * 0.5f);
        float[] ws = { 1.1f, 0.95f, 0.75f }; float y = -0.2f;
        for (int i = 0; i < 3; i++)
        {
            float bh = h * (i == 2 ? 0.3f : 0.37f) + 0.1f;
            P.Add(i % 2 == 0 ? st : st2, Cube, rot * new Vector3(0, y + bh / 2f, 0), rot * Quaternion.Euler(0, ((float)rnd.NextDouble() - 0.5f) * 10f, 0), new Vector3(ws[i], bh, ws[i] * 0.55f));
            y += bh - 0.06f;
        }
        P.Add(st2, Cube, rot * new Vector3(0, y, 0), rot * Quaternion.Euler(0, 0, 25), new Vector3(0.55f, 0.35f, 0.4f));
        for (int i = 0; i < 4; i++) P.Add(lichen, Sph, rot * new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.6f, 0.3f + (float)rnd.NextDouble() * h * 0.7f, 0.29f), rot, new Vector3(0.3f, 0.22f, 0.04f));
        for (int i = 0; i < 4; i++) { float a = (float)rnd.NextDouble() * 6.28f; P.Add(st2, Sph, new Vector3(Mathf.Cos(a) * 0.9f, 0.05f, Mathf.Sin(a) * 0.9f), Quaternion.identity, new Vector3(0.4f, 0.18f, 0.3f)); }
        P.Build(root.transform); return root;
    }

    // ---------- the statue garden and the training yard ----------
    static GameObject ShadeTree(Vector3 at, float h, int seed)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_ah_tree_" + (seed % 3)); if (pf == null) return null;
        var t = Strip(Object.Instantiate(pf)); t.name = "Garden tree";
        var b = Measure(t); float k = h / Mathf.Max(0.1f, b.size.y); t.transform.localScale = Vector3.one * k;
        t.transform.position = at; t.transform.rotation = Quaternion.Euler(0, seed * 47 % 360, 0);
        var leaf = Mat("garden_leaf", C(0x5E8F3A), 0.15f);
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            if (r.name == "Crown" || r.name == "Boughs") { var src = r.sharedMaterial; var m = new Material(src); foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) m.SetColor(pr, new Color(0.36f, 0.56f, 0.24f)); r.sharedMaterial = m; }
        AHModel.SetShadows(t); return t;
    }
    static void Bench(Parts P, Vector3 at, Quaternion rot)
    {
        Material wood = Mat("bench_wood", C(0x7A5232)), iron = Mat("bench_iron", C(0x2E2E32), 0.4f);
        foreach (float sx in new[] { -0.75f, 0.75f })
        {
            P.Add(iron, Cube, at + rot * new Vector3(sx, 0.22f, 0f), rot, new Vector3(0.06f, 0.44f, 0.46f));
            P.Add(iron, Cube, at + rot * new Vector3(sx, 0.62f, -0.22f), rot, new Vector3(0.06f, 0.5f, 0.06f));
        }
        for (int i = 0; i < 3; i++) P.Add(wood, Cube, at + rot * new Vector3(0, 0.46f, -0.15f + i * 0.15f), rot, new Vector3(1.7f, 0.05f, 0.12f));
        for (int i = 0; i < 2; i++) P.Add(wood, Cube, at + rot * new Vector3(0, 0.66f + i * 0.17f, -0.24f), rot * Quaternion.Euler(-10, 0, 0), new Vector3(1.7f, 0.1f, 0.04f));
    }
    static void Garden(AHGame g, Vector3 c, Quaternion face)
    {
        var root = new GameObject("Statue garden").transform; var P = new Parts(); var rnd = new System.Random((int)(c.x * 3 + c.z));
        Material st = Mat("garden_stone", C(0xB8B2A6)), st2 = Mat("garden_stone2", C(0x8E887C)), soil = Mat("garden_soil", C(0x4A3424)), hedge = Mat("garden_hedge", C(0x3E6A2E));
        Material[] fl = { Mat("fl_red", C(0xD8425A)), Mat("fl_yellow", C(0xF0C838)), Mat("fl_white", C(0xF4F0E8)), Mat("fl_violet", C(0x8A5AC8)) };
        // stepped plinth under the statue
        P.Add(st2, Cyl, c + Vector3.up * 0.1f, Quaternion.identity, new Vector3(3.4f, 0.1f, 3.4f));
        P.Add(st, Cyl, c + Vector3.up * 0.25f, Quaternion.identity, new Vector3(2.6f, 0.06f, 2.6f));
        // eight curved flowerbeds with a low box hedge on the outside
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + Mathf.PI / 8f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var rot = Quaternion.LookRotation(dir);
            Vector3 q = c + dir * 3.4f;
            P.Add(st2, Cube, q + Vector3.up * 0.1f, rot, new Vector3(1.7f, 0.2f, 0.9f));
            P.Add(soil, Cube, q + Vector3.up * 0.18f, rot, new Vector3(1.55f, 0.08f, 0.75f));
            P.Add(hedge, Cube, q + dir * 0.55f + Vector3.up * 0.3f, rot, new Vector3(1.8f, 0.42f, 0.32f));
            var m = fl[i % fl.Length];
            for (int k = 0; k < 7; k++) P.Add(m, Sph, q + rot * new Vector3(-0.6f + k * 0.2f, 0.3f, ((float)rnd.NextDouble() - 0.5f) * 0.4f), Quaternion.identity, Vector3.one * (0.16f + (float)rnd.NextDouble() * 0.08f));
        }
        // four benches facing the statue, between the beds, and four shade trees further out
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + Mathf.PI / 4f + Mathf.PI / 8f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            Vector3 q = c + dir * 5.2f; if (g.Blocked(q, 0.5f)) continue;
            Bench(P, q, Quaternion.LookRotation(-dir));
        }
        P.Build(root);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + Mathf.PI / 8f; Vector3 q = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 7f;
            if (g.Blocked(q, 0.8f)) continue;
            var t = ShadeTree(q, 5.5f + (float)rnd.NextDouble(), i + rnd.Next(3)); if (t != null) t.transform.SetParent(root, true);
        }
        AHModel.SetShadows(root.gameObject);
    }
    static void TrainingYard(AHGame g, List<Vector3> dummies, string col)
    {
        Vector3 lo = dummies[0], hi = dummies[0]; foreach (var d in dummies) { lo = Vector3.Min(lo, d); hi = Vector3.Max(hi, d); }
        lo -= new Vector3(4f, 0, 4f); hi += new Vector3(4f, 0, 4f); float y = dummies[0].y;
        var root = new GameObject("Training yard").transform; var P = new Parts();
        Material post = Mat("yard_post", C(0x6A4A2C)), rail = Mat("yard_rail", C(0x8A6238)), straw = Mat("yard_straw", C(0xE0C070)), sand = Mat("yard_sand", C(0xC9A877));
        Vector3 mid = (lo + hi) * 0.5f; mid.y = y;
        P.Add(sand, Cube, mid + Vector3.up * 0.015f, Quaternion.identity, new Vector3(hi.x - lo.x - 0.4f, 0.03f, hi.z - lo.z - 0.4f));
        // the fence, with a gate gap on the side nearest the town's middle
        Vector3 town = g.data != null && g.data.spawn != null ? g.W(g.data.spawn.x, g.data.spawn.z) : mid; Vector3 tw = town - mid;
        string gap = Mathf.Abs(tw.x) > Mathf.Abs(tw.z) ? (tw.x > 0 ? "e" : "w") : (tw.z > 0 ? "n" : "s");
        System.Action<Vector3, Vector3, bool> Fence = (a, b, cut) =>
        {
            Vector3 d = b - a; float L = d.magnitude; int n = Mathf.Max(1, Mathf.RoundToInt(L / 2f));
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n; if (cut && t > 0.38f && t < 0.62f) continue;
                P.Add(post, Cube, Vector3.Lerp(a, b, t) + Vector3.up * 0.55f, Quaternion.identity, new Vector3(0.14f, 1.1f, 0.14f));
                if (i < n && !(cut && t + 1f / n > 0.38f && t < 0.62f))
                    foreach (float h in new[] { 0.45f, 0.9f }) P.Beam(rail, Vector3.Lerp(a, b, t) + Vector3.up * h, Vector3.Lerp(a, b, t + 1f / n) + Vector3.up * h, 0.08f, 0.1f, Vector3.up);
            }
        };
        Vector3 A = new Vector3(lo.x, y, lo.z), B = new Vector3(hi.x, y, lo.z), Cc = new Vector3(hi.x, y, hi.z), D = new Vector3(lo.x, y, hi.z);
        Fence(A, B, gap == "s"); Fence(B, Cc, gap == "e"); Fence(Cc, D, gap == "n"); Fence(D, A, gap == "w");
        // hay bales in a corner, straw targets on stands along the far side
        Vector3 far = gap == "s" ? new Vector3(0, 0, 1) : gap == "n" ? new Vector3(0, 0, -1) : gap == "e" ? new Vector3(-1, 0, 0) : new Vector3(1, 0, 0);
        Vector3 side = Vector3.Cross(Vector3.up, far);
        float half = Mathf.Abs(Vector3.Dot(hi - lo, far)) * 0.5f, wide = Mathf.Abs(Vector3.Dot(hi - lo, side)) * 0.5f;
        for (int i = 0; i < 3; i++) P.Add(straw, Cyl, mid + far * (half - 1.2f) + side * (wide - 1.4f - i * 1.15f) + Vector3.up * 0.5f, Quaternion.Euler(0, 0, 90) * Quaternion.identity, new Vector3(1f, 0.55f, 1f));
        P.Add(straw, Cyl, mid + far * (half - 1.2f) + side * (wide - 1.95f) + Vector3.up * 1.45f, Quaternion.Euler(0, 0, 90), new Vector3(1f, 0.55f, 1f));
        Material red = Mat("target_red", C(0xC0392B)), white = Mat("target_white", C(0xF2EEE4));
        for (int i = 0; i < 2; i++)
        {
            Vector3 q = mid + far * (half - 1.3f) - side * (wide - 1.6f - i * 2.6f); var rot = Quaternion.LookRotation(-far);
            foreach (float sx in new[] { -0.45f, 0.45f }) P.Add(post, Cube, q + rot * new Vector3(sx, 0.75f, 0.15f), rot * Quaternion.Euler(-12, 0, 0), new Vector3(0.08f, 1.55f, 0.08f));
            P.Add(post, Cube, q + rot * new Vector3(0, 0.7f, -0.35f), rot * Quaternion.Euler(25, 0, 0), new Vector3(0.08f, 1.5f, 0.08f));
            float[] rr = { 1.15f, 0.85f, 0.55f, 0.25f }; Material[] mm = { straw, white, red, white };
            for (int k = 0; k < 4; k++) P.Add(k == 0 ? straw : mm[k], Cyl, q + rot * new Vector3(0, 1.25f, 0.22f + k * 0.012f), rot * Quaternion.Euler(90, 0, 0), new Vector3(rr[k], 0.04f, rr[k]));
        }
        P.Build(root);
        var rack = Resources.Load<GameObject>("AH/Models/KK/kk_weaponrack");
        if (rack != null)
        {
            var r = Strip(Object.Instantiate(rack, root, false)); var rb = Measure(r); float k = 1.6f / Mathf.Max(0.1f, rb.size.y);
            r.transform.localScale = Vector3.one * k; r.transform.position = mid - far * (half - 0.9f) + side * (wide - 1.5f); r.transform.rotation = Quaternion.LookRotation(far);
        }
        AHModel.SetShadows(root.gameObject);
    }

    // ---------- volcanoes ----------
    // Emberreach's and Dragonscale Isle's volcanoes were smooth black cones. Now a craggy cone of ash rock with ridges
    // and gullies running down, a jagged crater with a notch on one side, a glowing lava pool and lava trickling down
    // from the notch (Models/Nature/volcano, made for Ashen Hollow), and embers rising from the crater.
    static void Volcanoes(Transform world)
    {
        if (AHGame.AreaId != "ember" && AHGame.AreaId != "isle") return;   // the only two volcanoes
        var pf = Resources.Load<GameObject>("AH/Models/Nature/volcano"); if (pf == null) return;
        var cones = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue;
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || mf.sharedMesh.subMeshCount > 8) continue;
            long ic = 0; for (int k = 0; k < mf.sharedMesh.subMeshCount; k++) ic += mf.sharedMesh.GetIndexCount(k); if (ic > 2400) continue;
            var b = r.bounds; float h = b.size.y, foot = Mathf.Min(b.size.x, b.size.z);
            if (h < 15f || foot < 1.2f * h || foot > 4f * h) continue;
            cones.Add(r);
        }
        foreach (var cone in cones)
        {
            var b = cone.bounds; cone.enabled = false;
            // the glow and streaks that sat on the old cone's skin
            foreach (var r in world.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || r.name.StartsWith("AH_")) continue; var rb = r.bounds;
                if ((Mathf.Max(rb.size.x, rb.size.z) > 12f && r.transform.parent != cone.transform.parent) || rb.center.y < b.min.y + 1f) continue;   // its own long streaks too
                float t = Mathf.Clamp01((rb.center.y - b.min.y) / b.size.y);
                Vector2 d = new Vector2(rb.center.x - b.center.x, rb.center.z - b.center.z);
                if (d.magnitude < b.size.x * 0.5f * (1f - t) + 1.2f) r.enabled = false;
            }
            var go = Strip(Object.Instantiate(pf)); go.name = "Volcano";
            var mb = Measure(go); var rnd = new System.Random((int)(b.center.x * 3 + b.center.z));
            go.transform.localScale = new Vector3(b.size.x * 1.1f / mb.size.x, b.size.y * 1.08f / mb.size.y, b.size.z * 1.1f / mb.size.z);
            go.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            go.transform.position = new Vector3(b.center.x, b.min.y - 0.4f, b.center.z);
            AHModel.SetShadows(go);
            var top = new GameObject("Crater").transform; top.SetParent(go.transform, false); top.localPosition = new Vector3(0f, 0.8f, 0f);
            go.AddComponent<AHVolcano>().crater = top;
        }
        if (cones.Count > 0) Debug.Log("Ashen Hollow: " + cones.Count + " volcano(es) rebuilt");
    }

    // ---------- Varrow's castle ----------
    // Was a grey box with a box and a pyramid on top and four seven-sided posts with cone hats. Now a walled castle:
    // KayKit round towers at the four corners, a tall KayKit keep inside, crenellated curtain walls of dressed stone with
    // a darker footing, a gatehouse with a pointed arch, a portcullis and banners, and a paved courtyard.
    static void Castles(Transform world)
    {
        string col = AHCityColour();
        foreach (var t in world.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("obj")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length < 12 || rs.Length > 60) continue;
            Bounds body = default(Bounds); bool hasBody = false; int towers = 0;
            foreach (var r in rs)
            {
                var b = r.bounds;
                if (b.size.x >= 18f && b.size.z >= 14f && b.size.y >= 4f && b.size.y <= 8f) { body = b; hasBody = true; }
                if (b.size.y >= 8f && b.size.y <= 14f && Mathf.Max(b.size.x, b.size.z) <= 6.5f) towers++;
            }
            if (!hasBody || towers < 3) continue;
            Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            if (bb.size.y > 25f || Mathf.Max(bb.size.x, bb.size.z) > 45f) continue;
            // the door is the darkest thin piece low on one side
            Vector3 face = Vector3.zero;
            foreach (var r in rs)
            {
                var b = r.bounds; var m = r.sharedMaterial; if (m == null) continue;
                Color c = m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                if (c.r + c.g + c.b < 0.25f && b.center.y < 3f && Mathf.Min(b.size.x, b.size.z) < 0.4f && Mathf.Max(b.size.x, b.size.z) > 2f) { face = b.center - body.center; face.y = 0; }
            }
            if (face.sqrMagnitude < 1e-3f) face = Vector3.forward;
            face = Mathf.Abs(face.x) > Mathf.Abs(face.z) ? new Vector3(Mathf.Sign(face.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(face.z));
            foreach (var r in rs) r.enabled = false;
            bool turned = Mathf.Abs(face.x) > 0.5f;
            float W = (turned ? bb.size.z : bb.size.x), D = (turned ? bb.size.x : bb.size.z);
            var root = new GameObject("Castle").transform;
            root.position = new Vector3(bb.center.x, body.min.y, bb.center.z); root.rotation = Quaternion.LookRotation(face);
            Castle(root, W - 4f, D - 4f, col);
            AHModel.SetShadows(root.gameObject);
            Debug.Log("Ashen Hollow: castle rebuilt (" + W.ToString("0.0") + " x " + D.ToString("0.0") + ")");
        }
    }

    static GameObject KK(Transform parent, string file, float height, Vector3 at, float yaw)
    {
        var pf = Resources.Load<GameObject>("AH/Models/KK/" + file); if (pf == null) return null;
        var go = Strip(Object.Instantiate(pf)); go.name = file;
        var b = Measure(go); float k = height / Mathf.Max(0.1f, b.size.y);
        go.transform.SetParent(parent, false); go.transform.localScale = Vector3.one * k;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localPosition = at + go.transform.localRotation * new Vector3(-b.center.x * k, -b.min.y * k, -b.center.z * k);
        return go;
    }

    static void Castle(Transform root, float W, float D, string col)
    {
        var P = new Parts(); var I = Quaternion.identity;
        Material st = Mat("cas_stone", C(0xb4aea2)), st2 = Mat("cas_stone2", C(0x8c867c)), st3 = Mat("cas_stone3", C(0xc8c2b6)),
                 dark = Mat("cas_dark", C(0x221a14)), iron = Mat("cas_iron", C(0x3a3a40), 0.45f), pave = Mat("cas_pave", C(0x9c968a)),
                 wood = Mat("cas_wood", C(0x6a4426), 0.2f), gold = Mat("cas_gold", C(0xd9ab3a), 0.6f),
                 cloth = Mat("cas_cloth_" + col, col == "blue" ? C(0x2a4f9a) : col == "red" ? C(0xa42a2a) : col == "yellow" ? C(0xc89a2a) : C(0x2e7a3e));
        float wh = 6.5f, th = 1.4f;
        P.Add(pave, Cube, new Vector3(0, 0.05f, 0), I, new Vector3(W, 0.1f, D));
        // curtain walls (front split for the gate)
        System.Action<Vector3, Vector3> Wall = (a, b) =>
        {
            Vector3 d = b - a; float len = d.magnitude; var rot = Quaternion.LookRotation(d.normalized); Vector3 m = (a + b) * 0.5f;
            P.Add(st2, Cube, m + Vector3.up * 0.6f, rot, new Vector3(th + 0.5f, 1.2f, len));
            P.Add(st, Cube, m + Vector3.up * (wh * 0.5f), rot, new Vector3(th, wh, len));
            P.Add(st3, Cube, m + Vector3.up * (wh + 0.12f), rot, new Vector3(th + 0.35f, 0.24f, len));
            Vector3 side = rot * Vector3.right;
            for (float u = 0.6f; u < len - 0.4f; u += 1.3f)
                foreach (float sd in new[] { -1f, 1f })
                    P.Add(st, Cube, a + d.normalized * u + side * sd * (th * 0.5f + 0.05f) + Vector3.up * (wh + 0.6f), rot, new Vector3(0.35f, 0.8f, 0.7f));
            // arrow slits along the outside
            for (float u = 2.5f; u < len - 2f; u += 4f) P.Add(dark, Cube, a + d.normalized * u - side * (th * 0.5f + 0.02f) + Vector3.up * (wh * 0.6f), rot, new Vector3(0.06f, 1.3f, 0.25f));
        };
        float x = W / 2, z = D / 2, gw = 3.6f;
        Wall(new Vector3(-x, 0, -z), new Vector3(x, 0, -z));
        Wall(new Vector3(x, 0, -z), new Vector3(x, 0, z));
        Wall(new Vector3(-x, 0, z), new Vector3(-x, 0, -z));
        Wall(new Vector3(x, 0, z), new Vector3(gw + 1.6f, 0, z));
        Wall(new Vector3(-gw - 1.6f, 0, z), new Vector3(-x, 0, z));
        // gatehouse: two square towers joined over an arch
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 c = new Vector3(sx * (gw / 2 + 1.5f), 0, z + 0.4f);
            P.Add(st2, Cube, c + Vector3.up * 0.7f, I, new Vector3(3.6f, 1.4f, 3.6f));
            P.Add(st, Cube, c + Vector3.up * 4.6f, I, new Vector3(3.2f, 9.2f, 3.2f));
            P.Add(st3, Cube, c + Vector3.up * 9.3f, I, new Vector3(3.6f, 0.26f, 3.6f));
            for (int i = 0; i < 4; i++) { var r = Quaternion.Euler(0, i * 90f, 0); foreach (float u in new[] { -1.2f, 0f, 1.2f }) P.Add(st, Cube, c + Vector3.up * 9.85f + r * new Vector3(u, 0, 1.6f), r, new Vector3(0.6f, 0.85f, 0.4f)); }
            P.Add(dark, Cube, c + new Vector3(0, 6.5f, 1.62f), I, new Vector3(0.25f, 1.2f, 0.06f));
            // a banner on the face of each gate tower
            P.Add(gold, Cyl, c + new Vector3(0, 7.9f, 1.75f), Quaternion.Euler(0, 0, 90), new Vector3(0.08f, 0.8f, 0.08f));
            P.Add(cloth, Cube, c + new Vector3(0, 6.2f, 1.72f), I, new Vector3(1.4f, 3.3f, 0.04f));
            P.Add(gold, Cube, c + new Vector3(0, 6.6f, 1.75f), I, new Vector3(0.5f, 0.5f, 0.03f));
            P.Add(cloth, Cube, c + new Vector3(0, 4.45f, 1.72f), Quaternion.Euler(0, 0, 45), new Vector3(0.99f, 0.99f, 0.04f));
        }
        P.Add(st, Cube, new Vector3(0, 6.6f, z + 0.4f), I, new Vector3(gw + 0.2f, 4f, 3.2f));
        P.Add(st3, Cube, new Vector3(0, 8.7f, z + 0.4f), I, new Vector3(gw + 0.4f, 0.24f, 3.6f));
        for (float u = -gw / 2 + 0.4f; u <= gw / 2; u += 1.2f) P.Add(st, Cube, new Vector3(u, 9.2f, z + 2f), I, new Vector3(0.6f, 0.8f, 0.4f));
        // the arch: a dark passage with a pointed top and a portcullis
        P.Add(st3, Cube, new Vector3(0, 2.4f, z + 2.02f), I, new Vector3(gw + 0.6f, 4.8f, 0.06f));
        P.Add(st3, Cube, new Vector3(0, 4.8f, z + 2.02f), Quaternion.Euler(0, 0, 45), new Vector3((gw + 0.6f) * 0.7071f, (gw + 0.6f) * 0.7071f, 0.06f));
        P.Add(dark, Cube, new Vector3(0, 2.25f, z + 0.4f), I, new Vector3(gw, 4.5f, 3.3f));
        P.Add(dark, Cube, new Vector3(0, 4.5f, z + 0.4f), Quaternion.Euler(0, 0, 45), new Vector3(gw * 0.7071f, gw * 0.7071f, 3.3f));
        for (float u = -gw / 2 + 0.35f; u < gw / 2; u += 0.55f) P.Add(iron, Cube, new Vector3(u, 3.3f, z + 1.4f), I, new Vector3(0.09f, 2.4f, 0.09f));
        for (float v = 2.4f; v < 4.6f; v += 0.55f) P.Add(iron, Cube, new Vector3(0, v, z + 1.4f), I, new Vector3(gw, 0.08f, 0.08f));
        P.Build(root);
        // the four corner towers and the keep (KayKit Medieval Builder, in the city's colour)
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            KK(root, "kk_building_tower_A_" + col, 12.5f, new Vector3(sx * x, 0, sz * z), sx * sz > 0 ? 0f : 90f);
        KK(root, "kk_building_tower_B_" + col, 17f, new Vector3(0, 0, -z * 0.25f), 0f);
        KK(root, "kk_building_well_" + col, 2.6f, new Vector3(-x * 0.45f, 0, z * 0.45f), 20f);
        KK(root, "kk_weaponrack", 1.6f, new Vector3(x * 0.55f, 0, z * 0.2f), -90f);
        KK(root, "kk_crate_A_big", 1.0f, new Vector3(x * 0.6f, 0, z * 0.55f), 15f);
        KK(root, "kk_crate_B_small", 0.7f, new Vector3(x * 0.6f + 1.1f, 0, z * 0.6f), 40f);
        KK(root, "kk_barrel", 1.0f, new Vector3(x * 0.6f - 0.3f, 0, z * 0.55f + 1.2f), 0f);
    }

    // ---------- the Sunspire palace ----------
    // The web game's palace was a plain sand box with a huge glossy ball on top and two posts. Now: a stepped plinth,
    // walls with a blue tile band, pointed-arch windows framed in pale stone and a crenellated parapet; a tall arched
    // portal with carved doors; an onion dome on a drum with a gilt finial, two smaller domes over the wings and four
    // minarets with balconies, lanterns and little domes.
    static readonly Dictionary<string, Mesh> lathes = new Dictionary<string, Mesh>();
    static Mesh Onion()
    {
        Mesh m; if (lathes.TryGetValue("onion", out m) && m != null) return m;
        float[] ys = { 0f, 0.1f, 0.25f, 0.4f, 0.55f, 0.7f, 0.85f, 1.0f, 1.15f, 1.28f, 1.36f, 1.4f };
        float[] rs = { 1.0f, 1.07f, 1.11f, 1.09f, 1.01f, 0.87f, 0.67f, 0.45f, 0.25f, 0.11f, 0.04f, 0f };
        m = Lathe(rs, ys, 36); lathes["onion"] = m; return m;
    }
    static Mesh Lathe(float[] r, float[] y, int seg)
    {
        int n = r.Length; var v = new List<Vector3>(); var nr = new List<Vector3>(); var tri = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int a = Mathf.Max(0, i - 1), b = Mathf.Min(n - 1, i + 1);
            float dr = r[b] - r[a], dy = y[b] - y[a]; var pn = new Vector2(dy, -dr).normalized;
            for (int j = 0; j <= seg; j++)
            {
                float t = j * Mathf.PI * 2f / seg, c = Mathf.Cos(t), sn = Mathf.Sin(t);
                v.Add(new Vector3(r[i] * c, y[i], r[i] * sn)); nr.Add(new Vector3(pn.x * c, pn.y, pn.x * sn).normalized);
            }
        }
        for (int i = 0; i < n - 1; i++) for (int j = 0; j < seg; j++)
            {
                int q = i * (seg + 1) + j, w = q + seg + 1;
                tri.Add(q); tri.Add(w); tri.Add(q + 1); tri.Add(q + 1); tri.Add(w); tri.Add(w + 1);
            }
        var m = new Mesh { name = "Lathe" }; m.SetVertices(v); m.SetNormals(nr); m.SetTriangles(tri, 0); m.RecalculateBounds(); return m;
    }

    static void Palaces(Transform world)
    {
        foreach (var t in world.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith("obj")) continue;
            var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length < 3) continue;
            Renderer dome = null;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; var m = r.sharedMaterial; if (m == null) continue;
                Color c = m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                var b0 = r.bounds;
                if (c.r < 0.25f && c.g > 0.15f && c.g > c.r * 2f && Mathf.Abs(c.g - c.b) < 0.1f && b0.size.x > 9f && b0.size.y > 5f && b0.size.x < 20f) { dome = r; break; }
            }
            if (dome == null) continue;
            Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            if (Mathf.Max(bb.size.x, bb.size.z) < 20f) continue;
            // the body: the widest low box
            Bounds body = bb; float best = 0f;
            foreach (var r in rs) { var b0 = r.bounds; float a = b0.size.x * b0.size.z; if (b0.size.y > 2f && b0.size.y < 8f && a > best && a < bb.size.x * bb.size.z * 1.01f) { best = a; body = b0; } }
            foreach (var r in rs) r.enabled = false;
            // it faces the middle of the city
            Vector3 mid = Vector3.zero; foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { mid = r.bounds.center; break; }
            Vector3 f = mid - body.center; f.y = 0f;
            Vector3 face = Mathf.Abs(f.x) > Mathf.Abs(f.z) ? new Vector3(Mathf.Sign(f.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(f.z));
            bool turned = Mathf.Abs(face.x) > 0.5f;
            float W = turned ? body.size.z : body.size.x, D = turned ? body.size.x : body.size.z;
            var root = new GameObject("Palace").transform;
            root.position = new Vector3(body.center.x, body.min.y, body.center.z); root.rotation = Quaternion.LookRotation(face);
            Palace(root, W, D);
            AHModel.SetShadows(root.gameObject);
            Debug.Log("Ashen Hollow: palace rebuilt (" + W.ToString("0.0") + " x " + D.ToString("0.0") + ")");
        }
    }

    static void Palace(Transform root, float W, float D)
    {
        var P = new Parts(); var I = Quaternion.identity;
        Material sand = Mat("pal_sand", C(0xf0dcb0)), sand2 = Mat("pal_sand2", C(0xdcc092)), trim = Mat("pal_trim", C(0xfbf4e2)),
                 tile = Mat("pal_tile", C(0x3a78c4), 0.45f), dome = Mat("pal_dome", C(0x3cb8b0), 0.4f), gold = Mat("pal_gold", C(0xdcae44), 0.65f),
                 dark = Mat("pal_dark", C(0x2a1e16)), wood = Mat("pal_wood", C(0x6e4222), 0.2f);
        float hb = 0.6f, H = 6.2f, top = hb + H;
        // plinth and steps
        P.Add(sand2, Cube, new Vector3(0, hb * 0.5f, 0), I, new Vector3(W + 2f, hb, D + 2f));
        // walls, tile band, cornice
        P.Add(sand, Cube, new Vector3(0, hb + H * 0.5f, 0), I, new Vector3(W, H, D));
        P.Add(tile, Cube, new Vector3(0, hb + H - 1.25f, 0), I, new Vector3(W + 0.08f, 0.45f, D + 0.08f));
        P.Add(trim, Cube, new Vector3(0, top + 0.17f, 0), I, new Vector3(W + 0.6f, 0.34f, D + 0.6f));
        // crenellations
        for (float x = -W / 2 + 0.4f; x <= W / 2 - 0.3f; x += 1.25f)
            foreach (float z in new[] { -D / 2 - 0.1f, D / 2 + 0.1f }) P.Add(trim, Cube, new Vector3(x, top + 0.7f, z), I, new Vector3(0.6f, 0.75f, 0.4f));
        for (float z = -D / 2 + 1.0f; z <= D / 2 - 0.8f; z += 1.25f)
            foreach (float x in new[] { -W / 2 - 0.1f, W / 2 + 0.1f }) P.Add(trim, Cube, new Vector3(x, top + 0.7f, z), I, new Vector3(0.4f, 0.75f, 0.6f));
        // pointed-arch windows: a pale frame, a dark opening, a diamond for the point
        System.Action<Vector3, Quaternion, float, float> Arch = (at, rot, w, h) =>
        {
            Vector3 o = rot * Vector3.forward;
            P.Add(trim, Cube, at + Vector3.up * (h * 0.5f + w * 0.1f), rot, new Vector3(w + 0.4f, h + w * 0.2f + 0.3f, 0.06f));
            P.Add(trim, Cube, at + Vector3.up * h, rot * Quaternion.Euler(0, 0, 45), new Vector3((w + 0.4f) * 0.7071f, (w + 0.4f) * 0.7071f, 0.06f));
            P.Add(dark, Cube, at + o * 0.03f + Vector3.up * (h * 0.5f), rot, new Vector3(w, h, 0.06f));
            P.Add(dark, Cube, at + o * 0.03f + Vector3.up * h, rot * Quaternion.Euler(0, 0, 45), new Vector3(w * 0.7071f, w * 0.7071f, 0.06f));
        };
        for (float x = -W / 2 + 2f; x <= W / 2 - 1.9f; x += 3f)
        {
            if (Mathf.Abs(x) > 5.2f) Arch(new Vector3(x, hb + 1.4f, D / 2 + 0.02f), I, 1.2f, 2.2f);
            Arch(new Vector3(x, hb + 1.4f, -D / 2 - 0.02f), Quaternion.Euler(0, 180, 0), 1.2f, 2.2f);
        }
        for (float z = -D / 2 + 2.2f; z <= D / 2 - 2f; z += 3f)
        {
            Arch(new Vector3(W / 2 + 0.02f, hb + 1.4f, z), Quaternion.Euler(0, 90, 0), 1.2f, 2.2f);
            Arch(new Vector3(-W / 2 - 0.02f, hb + 1.4f, z), Quaternion.Euler(0, -90, 0), 1.2f, 2.2f);
        }
        // the great portal
        float pw = 8.4f, ph = 9.6f, pz = D / 2 + 0.9f;
        P.Add(sand, Cube, new Vector3(0, hb + ph * 0.5f, pz - 0.3f), I, new Vector3(pw, ph, 2.4f));
        P.Add(tile, Cube, new Vector3(0, hb + ph - 1.0f, pz + 0.92f), I, new Vector3(pw - 0.6f, 0.9f, 0.06f));
        P.Add(trim, Cube, new Vector3(0, hb + ph + 0.17f, pz - 0.3f), I, new Vector3(pw + 0.5f, 0.34f, 2.9f));
        for (float x = -pw / 2 + 0.4f; x <= pw / 2 - 0.3f; x += 1.25f) P.Add(trim, Cube, new Vector3(x, hb + ph + 0.7f, pz + 0.95f), I, new Vector3(0.6f, 0.75f, 0.4f));
        Arch(new Vector3(0, hb, pz + 0.9f), I, 4.2f, 4.6f);
        P.Add(wood, Cube, new Vector3(-0.78f, hb + 1.7f, pz + 0.96f), I, new Vector3(1.5f, 3.4f, 0.08f));
        P.Add(wood, Cube, new Vector3(0.78f, hb + 1.7f, pz + 0.96f), I, new Vector3(1.5f, 3.4f, 0.08f));
        foreach (float sx in new[] { -0.25f, 0.25f }) P.Add(gold, Sph, new Vector3(sx, hb + 1.7f, pz + 1.02f), I, Vector3.one * 0.16f);
        foreach (float sx in new[] { -2.75f, 2.75f })
        {
            P.Add(trim, Cyl, new Vector3(sx, hb + 2.4f, pz + 1.05f), I, new Vector3(0.45f, 2.4f, 0.45f));
            P.Add(gold, Cube, new Vector3(sx, hb + 4.9f, pz + 1.05f), I, new Vector3(0.6f, 0.2f, 0.6f));
        }
        // steps up to the portal
        for (int i = 0; i < 3; i++) P.Add(sand2, Cube, new Vector3(0, 0.1f + i * 0.2f - 0.1f, pz + 1.9f - i * 0.45f), I, new Vector3(6f - i * 0.2f, 0.2f + i * 0.2f, 0.5f));
        // drum and great dome
        float dr = Mathf.Min(W, D) * 0.36f;
        P.Add(sand, Cyl, new Vector3(0, top + 1.3f, -0.4f), I, new Vector3(dr * 2.05f, 1.3f, dr * 2.05f));
        P.Add(tile, Cyl, new Vector3(0, top + 2.2f, -0.4f), I, new Vector3(dr * 2.1f, 0.22f, dr * 2.1f));
        P.Add(gold, Cyl, new Vector3(0, top + 2.62f, -0.4f), I, new Vector3(dr * 2.12f, 0.06f, dr * 2.12f));
        for (int i = 0; i < 12; i++) { float a = i * 30f; var rot = Quaternion.Euler(0, a, 0); Arch(new Vector3(0, top + 0.7f, -0.4f) + rot * new Vector3(0, 0, dr * 1.03f), rot, 0.55f, 0.9f); }
        P.Add(dome, Onion(), new Vector3(0, top + 2.6f, -0.4f), I, new Vector3(dr * 0.97f, dr * 1.15f, dr * 0.97f));
        float apex = top + 2.6f + dr * 1.15f * 1.4f;
        P.Add(gold, Cyl, new Vector3(0, apex + 0.6f, -0.4f), I, new Vector3(0.14f, 0.8f, 0.14f));
        P.Add(gold, Sph, new Vector3(0, apex + 0.35f, -0.4f), I, Vector3.one * 0.5f);
        P.Add(gold, Sph, new Vector3(0, apex + 0.95f, -0.4f), I, Vector3.one * 0.32f);
        // the two wing domes
        foreach (float sx in new[] { -W * 0.32f, W * 0.32f })
        {
            P.Add(sand, Cyl, new Vector3(sx, top + 0.6f, -0.4f), I, new Vector3(4.6f, 0.6f, 4.6f));
            P.Add(tile, Cyl, new Vector3(sx, top + 1.15f, -0.4f), I, new Vector3(4.7f, 0.12f, 4.7f));
            P.Add(dome, Onion(), new Vector3(sx, top + 1.2f, -0.4f), I, new Vector3(2.15f, 2.2f, 2.15f));
            P.Add(gold, Sph, new Vector3(sx, top + 1.2f + 2.2f * 1.4f + 0.2f, -0.4f), I, Vector3.one * 0.3f);
            P.Add(gold, Cyl, new Vector3(sx, top + 1.2f + 2.2f * 1.4f + 0.55f, -0.4f), I, new Vector3(0.09f, 0.35f, 0.09f));
        }
        // four minarets
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 b = new Vector3(sx * (W / 2 + 0.3f), 0, sz * (D / 2 + 0.3f)); float mh = 13f;
                P.Add(sand2, Cube, b + Vector3.up * 0.8f, I, new Vector3(3f, 1.6f, 3f));
                P.Add(sand, Cyl, b + Vector3.up * (mh * 0.5f), I, new Vector3(2.3f, mh * 0.5f, 2.3f));
                foreach (float y in new[] { 4.5f, 8.5f }) P.Add(tile, Cyl, b + Vector3.up * y, I, new Vector3(2.38f, 0.22f, 2.38f));
                P.Add(trim, Cyl, b + Vector3.up * 11f, I, new Vector3(3.3f, 0.12f, 3.3f));
                P.Add(trim, Cyl, b + Vector3.up * 11.45f, I, new Vector3(3.2f, 0.32f, 3.2f));
                P.Add(sand, Cyl, b + Vector3.up * (mh + 0.8f), I, new Vector3(1.7f, 0.8f, 1.7f));
                for (int i = 0; i < 4; i++) { var rot = Quaternion.Euler(0, i * 90f, 0); Arch(b + Vector3.up * (mh + 0.25f) + rot * new Vector3(0, 0, 0.86f), rot, 0.45f, 0.75f); }
                P.Add(gold, Cyl, b + Vector3.up * (mh + 1.65f), I, new Vector3(1.85f, 0.06f, 1.85f));
                P.Add(dome, Onion(), b + Vector3.up * (mh + 1.6f), I, new Vector3(0.95f, 1.0f, 0.95f));
                P.Add(gold, Sph, b + Vector3.up * (mh + 1.6f + 1.4f + 0.1f), I, Vector3.one * 0.22f);
                P.Add(gold, Cyl, b + Vector3.up * (mh + 1.6f + 1.4f + 0.4f), I, new Vector3(0.07f, 0.3f, 0.07f));
            }
        P.Build(root);
    }

    static GameObject Strip(GameObject go) { foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c); return go; }
    static Bounds Measure(GameObject go) { var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
    // hides the old pieces round a spot; gives their bounds and which way the small pieces (door, sails) sit from the big one
    static bool Take(Transform world, Vector3 s, float near, float maxFoot, float maxH, out Bounds all, out Vector3 face)
    {
        all = new Bounds(); face = Vector3.zero; Renderer big = null; var small = new List<Renderer>(); bool any = false;
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds; Vector3 d = bb.center - s; d.y = 0;
            if (d.magnitude > near || Mathf.Max(bb.size.x, bb.size.z) > maxFoot || bb.size.y > maxH) continue;
            if (!any) { all = bb; any = true; } else all.Encapsulate(bb);
            if (big == null || bb.size.x * bb.size.z * bb.size.y > big.bounds.size.x * big.bounds.size.z * big.bounds.size.y) { if (big != null) small.Add(big); big = r; } else small.Add(r);
            r.enabled = false;
        }
        if (!any) return false;
        if (big != null && small.Count > 0) { Vector3 c = Vector3.zero; foreach (var r in small) c += r.bounds.center; c /= small.Count; face = c - big.bounds.center; face.y = 0; }
        return true;
    }

    // a beached wreck along +Z, about 10 m long
    public static GameObject Wreck(Transform parent, int seed)
    {
        var root = new GameObject("Shipwreck"); if (parent != null) root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material wood = Mat("wreck_wood", C(0x5e4430)), wood2 = Mat("wreck_wood2", C(0x4a3424)), pale = Mat("wreck_pale", C(0x7a6248)),
                 sail = Mat("wreck_sail", C(0xcfc2a0)), weed = Mat("wreck_weed", C(0x3e5a2a)), sand = Mat("wreck_sand", C(0xc8b48a));
        sail.SetFloat("_Cull", 0f);
        var list = Quaternion.Euler(0, 0, 16f);                 // the ship lies over on its side
        float L = 10f, B = 1.7f, D = 2.3f;
        System.Func<float, float> half = z => B * Mathf.Sqrt(Mathf.Max(0.05f, 1f - (z / (L * 0.5f)) * (z / (L * 0.5f))));
        // keel
        for (int i = 0; i < 8; i++) { float za = -L / 2 + L * i / 8f, zb = -L / 2 + L * (i + 1) / 8f; P.Beam(wood2, list * new Vector3(0, 0.1f + 0.25f * Mathf.Abs(za) / L, za), list * new Vector3(0, 0.1f + 0.25f * Mathf.Abs(zb) / L, zb), 0.28f, 0.3f, Vector3.right); }
        // ribs: arcs from the keel up both sides, some broken short
        for (int i = 1; i < 10; i++)
        {
            float z = -L / 2 + L * i / 10f, hw = half(z); bool broken = rnd.NextDouble() < 0.3;
            for (int side = -1; side <= 1; side += 2)
            {
                int segs = broken && side > 0 ? 2 : 4;
                Vector3 prev = new Vector3(0, 0.15f, z);
                for (int k = 1; k <= segs; k++)
                {
                    float t = k / 4f; Vector3 q = new Vector3(side * hw * Mathf.Sin(t * Mathf.PI * 0.5f) * 1.05f, 0.15f + D * (1f - Mathf.Cos(t * Mathf.PI * 0.5f)) * 1.1f, z);
                    P.Beam(i % 2 == 0 ? wood : pale, list * prev, list * q, 0.16f, 0.14f, Vector3.forward); prev = q;
                }
            }
        }
        // planks still on the low side, a few gaps
        for (int row = 0; row < 4; row++)
        {
            float t = (row + 0.5f) / 4f;
            for (int i = 0; i < 9; i++)
            {
                if (rnd.NextDouble() < 0.22) continue;
                float za = -L / 2 + L * i / 9f + 0.3f, zb = za + L / 9f - 0.05f;
                Vector3 a = new Vector3(-half(za) * Mathf.Sin(t * Mathf.PI * 0.5f) * 1.08f, 0.15f + D * (1f - Mathf.Cos(t * Mathf.PI * 0.5f)) * 1.1f, za);
                Vector3 bq = new Vector3(-half(zb) * Mathf.Sin(t * Mathf.PI * 0.5f) * 1.08f, 0.15f + D * (1f - Mathf.Cos(t * Mathf.PI * 0.5f)) * 1.1f, zb);
                P.Beam(row % 2 == 0 ? wood : wood2, list * a, list * bq, 0.06f, 0.42f, Vector3.up);
            }
        }
        // snapped mast and its torn sail, a spar on the sand
        var mastTilt = list * Quaternion.Euler(-24f, 0, 12f);
        P.Add(wood2, Cyl, list * new Vector3(0, 0.3f, 0.6f) + mastTilt * new Vector3(0, 1.9f, 0), mastTilt, new Vector3(0.26f, 1.9f, 0.26f));
        P.Add(pale, Cube, list * new Vector3(0, 0.3f, 0.6f) + mastTilt * new Vector3(0, 3.4f, 0), mastTilt * Quaternion.Euler(0, 0, 90), new Vector3(0.12f, 2.6f, 0.12f));
        P.Add(sail, Cube, list * new Vector3(0, 0.3f, 0.6f) + mastTilt * new Vector3(0.2f, 2.6f, 0.08f), mastTilt * Quaternion.Euler(0, 0, -6f), new Vector3(1.9f, 1.4f, 0.02f));
        P.Add(wood, Cyl, new Vector3(2.6f, 0.12f, -2.4f), Quaternion.Euler(0, 30, 90), new Vector3(0.18f, 1.8f, 0.18f));
        for (int i = 0; i < 9; i++) P.Add(weed, Sph, list * new Vector3(R(rnd, -1.4f, 1.4f), 0.2f, R(rnd, -4.5f, 4.5f)), Quaternion.identity, new Vector3(R(rnd, 0.3f, 0.6f), 0.14f, R(rnd, 0.3f, 0.6f)));
        for (int i = 0; i < 6; i++) P.Add(sand, Sph, new Vector3(R(rnd, -2f, 2f), 0.05f, R(rnd, -5f, 5f)), Quaternion.Euler(0, R(rnd, 0, 90), 0), new Vector3(R(rnd, 1f, 2.2f), 0.4f, R(rnd, 1f, 2.4f)));
        P.Build(root.transform);
        return root;
    }

    public static GameObject Obelisk(float h, int seed)
    {
        var root = new GameObject("Obelisk"); var P = new Parts(); var rnd = new System.Random(seed);
        Material sand = Mat("ob_sand", C(0xd2b47e)), sand2 = Mat("ob_sand2", C(0xb8955e)), carve = Mat("ob_carve", C(0x7a5a34)), gold = Mat("ob_gold", C(0xe8b84a), 0.75f);
        P.Add(sand2, Cube, new Vector3(0, 0.15f, 0), Quaternion.identity, new Vector3(2.2f, 0.3f, 2.2f));
        P.Add(sand, Cube, new Vector3(0, 0.45f, 0), Quaternion.identity, new Vector3(1.7f, 0.3f, 1.7f));
        P.Add(sand2, Cube, new Vector3(0, 0.72f, 0), Quaternion.identity, new Vector3(1.3f, 0.24f, 1.3f));
        float sh = h - 1.5f; int n = 6;
        for (int i = 0; i < n; i++)   // the shaft, tapering, in courses
        {
            float t0 = i / (float)n, t1 = (i + 1) / (float)n, w0 = Mathf.Lerp(0.95f, 0.6f, t0), w1 = Mathf.Lerp(0.95f, 0.6f, t1);
            float y = 0.84f + sh * (t0 + t1) * 0.5f, w = (w0 + w1) * 0.5f;
            P.Add(i % 2 == 0 ? sand : sand2, Cube, new Vector3(0, y, 0), Quaternion.identity, new Vector3(w, sh / n + 0.002f, w));
            if (i == 1 || i == 3)   // a band of glyphs on every face
                for (int f = 0; f < 4; f++)
                {
                    var q = Quaternion.Euler(0, f * 90f, 0);
                    for (int k = -1; k <= 1; k++) P.Add(carve, Cube, new Vector3(0, y, 0) + q * new Vector3(k * w * 0.28f, (k == 0 ? 0.08f : -0.05f), w * 0.5f + 0.004f), q, new Vector3(w * 0.14f, sh / n * 0.45f, 0.01f));
                }
        }
        float top = 0.84f + sh;
        P.Add(gold, Cube, new Vector3(0, top + 0.18f, 0), Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 0, 0), new Vector3(0.42f, 0.36f, 0.42f));
        P.Add(gold, Cyl, new Vector3(0, top + 0.5f, 0), Quaternion.identity, new Vector3(0.12f, 0.16f, 0.12f));
        for (int i = 0; i < 3; i++) { float a = R(rnd, 0, Mathf.PI * 2), d = R(rnd, 1.6f, 2.4f); P.Add(sand2, Cube, new Vector3(Mathf.Cos(a) * d, 0.2f, Mathf.Sin(a) * d), Quaternion.Euler(R(rnd, -15, 15), R(rnd, 0, 90), R(rnd, -10, 10)), new Vector3(0.6f, 0.4f, 0.5f)); }
        P.Build(root.transform);
        return root;
    }

    public static GameObject Dummy(int seed)
    {
        var root = new GameObject("Training dummy"); var P = new Parts(); var rnd = new System.Random(seed);
        Material wood = Mat("dm_wood", C(0x6a4a2c)), sack = Mat("dm_sack", C(0xc8ac78)), straw = Mat("dm_straw", C(0xe0c060)), rope = Mat("dm_rope", C(0x8a6a3a)),
                 red = Mat("dm_red", C(0xb02a22)), white = Mat("dm_white", C(0xe8e0cc)), iron = Mat("dm_iron", C(0x6a6a70), 0.4f);
        P.Add(wood, Cube, new Vector3(0, 0.95f, 0), Quaternion.identity, new Vector3(0.12f, 1.9f, 0.12f));
        P.Add(wood, Cube, new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(0.7f, 0.1f, 0.14f));
        P.Add(wood, Cube, new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(0.14f, 0.1f, 0.7f));
        P.Add(sack, Cyl, new Vector3(0, 1.25f, 0), Quaternion.Euler(R(rnd, -4, 4), 0, R(rnd, -4, 4)), new Vector3(0.5f, 0.38f, 0.42f));
        foreach (float y in new[] { 0.98f, 1.5f }) P.Add(rope, Cyl, new Vector3(0, y, 0), Quaternion.identity, new Vector3(0.53f, 0.025f, 0.45f));
        P.Add(wood, Cube, new Vector3(0, 1.42f, 0), Quaternion.Euler(0, 0, R(rnd, -6, 6)), new Vector3(1.2f, 0.08f, 0.08f));
        foreach (float x in new[] { -0.6f, 0.6f }) P.Add(straw, Sph, new Vector3(x, 1.42f, 0), Quaternion.identity, new Vector3(0.14f, 0.1f, 0.14f));
        P.Add(sack, Sph, new Vector3(0, 1.8f, 0), Quaternion.identity, new Vector3(0.34f, 0.36f, 0.32f));
        P.Add(iron, Cyl, new Vector3(0, 1.98f, 0), Quaternion.Euler(R(rnd, -10, 10), 0, R(rnd, -12, 12)), new Vector3(0.36f, 0.09f, 0.36f));
        // the target painted on the chest (facing +Z)
        P.Add(red, Cyl, new Vector3(0, 1.28f, 0.215f), Quaternion.Euler(90, 0, 0), new Vector3(0.3f, 0.005f, 0.3f));
        P.Add(white, Cyl, new Vector3(0, 1.28f, 0.22f), Quaternion.Euler(90, 0, 0), new Vector3(0.19f, 0.005f, 0.19f));
        P.Add(red, Cyl, new Vector3(0, 1.28f, 0.225f), Quaternion.Euler(90, 0, 0), new Vector3(0.08f, 0.005f, 0.08f));
        for (int i = 0; i < 5; i++) P.Add(straw, Cube, new Vector3(R(rnd, -0.2f, 0.2f), 1.02f + R(rnd, -0.05f, 0.4f), R(rnd, -0.2f, 0.2f)), Quaternion.Euler(R(rnd, -40, 40), R(rnd, 0, 180), R(rnd, -40, 40)), new Vector3(0.015f, 0.18f, 0.015f));
        P.Build(root.transform);
        return root;
    }

    // ---------- bank vault ----------
    // the bank was the web game's plain box chest: now a big iron-bound strongbox (KayKit) on a stone plinth, with
    // stacks of gold coins beside it and a lantern on a post, facing the way the old chest's lock faced
    public static void Bank(AHGame g, Vector3 at)
    {
        var w = g.World; Vector3 face = Vector3.zero;
        if (w != null)
        {
            Vector3 all = Vector3.zero, lockp = Vector3.zero; int na = 0, nl = 0;
            foreach (var rd in w.GetComponentsInChildren<Renderer>(true))
            {
                if (!rd.enabled || rd.name.StartsWith("AH_")) continue; var b = rd.bounds; Vector3 d = b.center - at; d.y = 0;
                if (d.magnitude > 1.3f || Mathf.Max(b.size.x, b.size.z) > 1.6f || b.size.y > 1.6f) continue;
                rd.enabled = false; all += b.center; na++;
                var m = rd.sharedMaterial; Color c = Color.black;
                if (m != null) { if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); }
                if (c.r > 0.5f && c.g > 0.3f && c.b < 0.2f && b.size.y < 0.4f && Mathf.Min(b.size.x, b.size.z) < 0.3f) { lockp += b.center; nl++; }
            }
            if (na > 0 && nl > 0) { face = lockp / nl - all / na; face.y = 0; }
        }
        var root = new GameObject("Bank vault").transform; root.position = g.Resolve(at, 0.6f);
        root.rotation = face.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(face.normalized) : Quaternion.identity;
        var P = new Parts(); var rnd = new System.Random(7);
        Material st = Mat("bank_stone", C(0x8a8478)), st2 = Mat("bank_stone2", C(0x6e6a62)), gold = Mat("bank_gold", C(0xe0b040), 0.7f), iron = Mat("bank_iron", C(0x3a3a40), 0.5f),
                 wood = Mat("bank_wood", C(0x5a3e26)), glow = Mat("bank_glow", C(0xffc860));
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", C(0xffb040).linear * 2f);
        P.Add(st2, Cube, new Vector3(0, 0.1f, 0), Quaternion.identity, new Vector3(1.9f, 0.2f, 1.4f));
        P.Add(st, Cube, new Vector3(0, 0.26f, 0), Quaternion.identity, new Vector3(1.6f, 0.14f, 1.15f));
        for (int i = 0; i < 3; i++)   // stacks of coins at the front corners
            foreach (float sx in new[] { -0.62f, 0.62f })
            {
                int h = 3 + rnd.Next(5);
                for (int k = 0; k < h; k++) P.Add(gold, Cyl, new Vector3(sx + R(rnd, -0.05f, 0.05f) + (i - 1) * 0.09f, 0.35f + k * 0.035f, 0.42f + (i % 2) * 0.08f), Quaternion.identity, new Vector3(0.11f, 0.016f, 0.11f));
            }
        // a lantern on a post at one side
        P.Add(wood, Cube, new Vector3(1.05f, 0.95f, -0.3f), Quaternion.identity, new Vector3(0.1f, 1.9f, 0.1f));
        P.Add(wood, Cube, new Vector3(1.05f, 1.86f, -0.12f), Quaternion.identity, new Vector3(0.08f, 0.06f, 0.42f));
        P.Add(iron, Cube, new Vector3(1.05f, 1.68f, 0.06f), Quaternion.identity, new Vector3(0.18f, 0.24f, 0.18f));
        P.Add(glow, Cube, new Vector3(1.05f, 1.68f, 0.06f), Quaternion.identity, new Vector3(0.13f, 0.18f, 0.2f));
        P.Build(root);
        var pf = Resources.Load<GameObject>("AH/Models/KK/kk_dg_chest_gold") ?? Resources.Load<GameObject>("AH/Models/KK/kk_dg_chest");
        if (pf != null)
        {
            var ch = Object.Instantiate(pf, root, false); ch.transform.localPosition = new Vector3(0, 0.33f, -0.05f); ch.transform.localScale = Vector3.one * 1.05f;
            foreach (var cl in ch.GetComponentsInChildren<Collider>(true)) Object.Destroy(cl);
        }
        var lg = new GameObject("Lantern"); lg.transform.SetParent(root, false); lg.transform.localPosition = new Vector3(1.05f, 1.68f, 0.3f);
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.75f, 0.4f); li.range = 4f; li.intensity = 0.9f; li.shadows = LightShadows.None;
        AHModel.SetShadows(root.gameObject);
    }

    // ---------- ruins ----------
    static int Ruins(AHGame g, Transform world)
    {
        if (g.data == null || g.data.bounds == null) return 0;
        var spots = new List<Vector3>();
        foreach (var o in AHTents.Props())
        {
            if (AHJson.S(o, "kind") != "ruin") continue;
            float x = (float)AHJson.N(o, "x") * AHDB.S, z = (float)AHJson.N(o, "y") * AHDB.S;
            var b = g.data.bounds; if (x < b.x0 - 1 || x > b.x1 + 1 || z < b.z0 - 1 || z > b.z1 + 1) continue;
            spots.Add(g.W(x, z));
        }
        if (spots.Count == 0) return 0;
        var rs = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds;
            if (Mathf.Max(bb.size.x, bb.size.z) > 2.2f || bb.size.y > 9f) continue; rs.Add(r);
        }
        Transform root = null; int n = 0;
        foreach (var s in spots)
        {
            float ground = float.PositiveInfinity, top = float.NegativeInfinity; Vector3 c0 = s; bool any = false; Color old = C(0x8a7a62);
            foreach (var r in rs)
            {
                if (!r.enabled) continue; Vector3 c = r.bounds.center - s; c.y = 0; if (c.magnitude > 1.2f) continue;
                ground = Mathf.Min(ground, r.bounds.min.y); top = Mathf.Max(top, r.bounds.max.y);
                if (!any) { c0 = r.bounds.center; any = true; var m = r.sharedMaterial; if (m != null) foreach (var pr in new[] { "baseColorFactor", "_BaseColor", "_Color" }) if (m.HasProperty(pr)) { old = m.GetColor(pr).gamma; break; } }
                r.enabled = false;
            }
            if (!any)
            {
                Renderer best = null; float bd = 99f;
                foreach (var r in world.GetComponentsInChildren<Renderer>(true)) { Vector3 c = r.bounds.center - s; c.y = 0; if (c.magnitude < bd) { bd = c.magnitude; best = r; } }
                if (best != null) Debug.Log("Ashen Hollow: no ruin at " + s + ", nearest " + best.name + " " + bd + " size " + best.bounds.size + " on " + best.enabled);
                continue;
            }
            if (root == null) root = new GameObject("Ruins").transform;
            int seed = Mathf.RoundToInt(s.x * 11f + s.z * 5f);
            var col = Ruin(root, Mathf.Clamp(top - ground, 1.2f, 7f), old, seed);
            col.transform.position = new Vector3(c0.x, ground, c0.z);
            col.transform.rotation = Quaternion.Euler(0f, (seed % 360 + 360) % 360, 0f);
            AHModel.SetShadows(col); n++;
        }
        return n;
    }

    // a broken column about h tall (before breaking), its foot at y = 0
    public static GameObject Ruin(Transform parent, float h, Color tint, int seed)
    {
        var root = new GameObject("Ruin"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Color baseC = Color.Lerp(C(0xbdb5a6), tint, 0.18f);
        string key = "ruin_" + ColorUtility.ToHtmlStringRGB(baseC);
        Material st = Mat(key, baseC), st2 = Mat(key + "_d", baseC * 0.9f), crack = Mat(key + "_c", baseC * 0.68f), moss = Mat("ruin_moss", C(0x56702f));
        // stepped plinth
        P.Add(st2, Cube, new Vector3(0, 0.15f, 0), Quaternion.Euler(0, R(rnd, -3, 3), 0), new Vector3(1.75f, 0.34f, 1.75f));
        P.Add(st, Cube, new Vector3(0, 0.42f, 0), Quaternion.Euler(0, R(rnd, -3, 3), 0), new Vector3(1.38f, 0.22f, 1.38f));
        P.Add(st, Cyl, new Vector3(0, 0.6f, 0), Quaternion.identity, new Vector3(1.12f, 0.07f, 1.12f));
        // shaft of drums, snapped off
        float shaft = Mathf.Max(0.8f, (h - 0.7f) * R(rnd, 0.5f, 0.92f)); int drums = Mathf.Max(1, Mathf.RoundToInt(shaft / 0.95f));
        float y = 0.67f, dh = shaft / drums, lean = R(rnd, -2.5f, 2.5f);
        for (int i = 0; i < drums; i++)
        {
            float rad = 0.46f - i * 0.012f; var q = Quaternion.Euler(lean * (i + 1) * 0.4f, R(rnd, 0, 360), 0);
            Vector3 c = new Vector3(Mathf.Sin(lean * Mathf.Deg2Rad) * y * 0.4f, y + dh * 0.5f, 0);
            P.Add(i % 2 == 0 ? st : st2, Cyl, c, q, new Vector3(rad * 2f, dh * 0.5f - 0.01f, rad * 2f));
            for (int k = 0; k < 10; k++)   // flutes
            {
                float a = k * Mathf.PI * 2f / 10f;
                P.Add(crack, Cube, c + q * new Vector3(Mathf.Cos(a) * rad * 0.97f, 0, Mathf.Sin(a) * rad * 0.97f), q * Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(0.05f, dh - 0.04f, 0.05f));
            }
            y += dh;
        }
        // jagged broken top
        float tx = Mathf.Sin(lean * Mathf.Deg2Rad) * y * 0.4f;
        for (int k = 0; k < 4; k++)
        {
            float a = R(rnd, 0, Mathf.PI * 2), d = R(rnd, 0.05f, 0.25f), hh = R(rnd, 0.12f, 0.4f);
            P.Add(st, Cube, new Vector3(tx + Mathf.Cos(a) * d, y + hh * 0.4f, Mathf.Sin(a) * d), Quaternion.Euler(R(rnd, -18, 18), R(rnd, 0, 90), R(rnd, -18, 18)), new Vector3(R(rnd, 0.25f, 0.45f), hh, R(rnd, 0.25f, 0.45f)));
        }
        P.Add(moss, Sph, new Vector3(tx, y + 0.02f, 0), Quaternion.identity, new Vector3(0.5f, 0.08f, 0.45f));
        P.Add(moss, Sph, new Vector3(0.55f, 0.55f, 0.3f), Quaternion.identity, new Vector3(0.45f, 0.12f, 0.6f));
        P.Add(moss, Sph, new Vector3(-0.6f, 0.32f, -0.45f), Quaternion.identity, new Vector3(0.55f, 0.14f, 0.4f));
        // a fallen drum or two and rubble
        int fallen = rnd.Next(1, 3);
        for (int i = 0; i < fallen; i++)
        {
            float a = R(rnd, 0, Mathf.PI * 2), d = R(rnd, 1.5f, 2.3f);
            P.Add(st2, Cyl, new Vector3(Mathf.Cos(a) * d, 0.4f, Mathf.Sin(a) * d), Quaternion.Euler(0, R(rnd, 0, 180), 90f + R(rnd, -6, 6)), new Vector3(0.84f, R(rnd, 0.3f, 0.45f), 0.84f));
        }
        for (int i = 0; i < 7; i++)
        {
            float a = R(rnd, 0, Mathf.PI * 2), d = R(rnd, 1.0f, 2.6f), sz = R(rnd, 0.15f, 0.42f);
            P.Add(i % 3 == 0 ? st : st2, Cube, new Vector3(Mathf.Cos(a) * d, sz * 0.3f, Mathf.Sin(a) * d), Quaternion.Euler(R(rnd, -25, 25), R(rnd, 0, 90), R(rnd, -25, 25)), new Vector3(sz * 1.3f, sz, sz));
        }
        P.Build(root.transform);
        return root;
    }
}

// turns the windmill's sails
public class AHSpin : MonoBehaviour { public float speed = 20f; void Update() { transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self); } }

// holds a statue still: one frame of its idle pose, blended in, then left alone
public class AHStatueFreeze : MonoBehaviour
{
    [System.NonSerialized] public AHAnim anim; float t;
    void Update() { t += Time.deltaTime; if (anim != null) anim.Tick(Time.deltaTime); if (t > 0.6f) Destroy(this); }
}

// embers drifting up out of a volcano's crater
public class AHVolcano : MonoBehaviour
{
    public Transform crater; float t;
    void Update()
    {
        if (crater == null) return; t += Time.deltaTime; if (t < 0.12f) return; t = 0f;
        var cam = Camera.main; if (cam != null && (cam.transform.position - crater.position).sqrMagnitude > 250f * 250f) return;
        float w = transform.lossyScale.x * 0.12f;
        var o = Random.insideUnitCircle * w;
        AHSpark.Emit(crater.position + new Vector3(o.x, 0.5f, o.y), new Vector3(Random.Range(-1f, 1f), Random.Range(4f, 8f), Random.Range(-1f, 1f)), Random.Range(0.25f, 0.55f), Random.Range(1.6f, 3f), Random.value < 0.5f ? new Color(1f, 0.45f, 0.1f) : new Color(1f, 0.75f, 0.25f));
    }
}
