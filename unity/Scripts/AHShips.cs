// Ashen Hollow: ships. The ships moored at Varrow's quay, round Tidewake's isles and in the port towns were a box of a
// hull, a flat box of a deck, eight-sided poles for masts and flat boards for sails. Each one is rebuilt where it lay as
// a real little sailing ship: a planked hull with a curved, tapering bow, a flat transom stern and a painted band below
// the gunwale, a planked deck, a raised stern castle with lit windows and a lantern, tapered masts with a crow's nest,
// yards and billowing canvas (the old sails' colour, washed toward sailcloth), a bowsprit with a jib, shrouds and stays,
// and a pennant at the masthead. They ride the swell, rolling and pitching a little.
using System.Collections.Generic;
using UnityEngine;

public static class AHShips
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.2f, Texture tex = null, bool twoSided = false, float glow = 0f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ship_" + key };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f); m.enableInstancing = true;
        if (tex != null) m.SetTexture("_BaseMap", tex);
        if (twoSided) { m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; }
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * glow); }
        mats[key] = m; return m;
    }

    // planks: long boards with seams, a little grain and a different shade on each board (u runs along the boards)
    static Texture2D planks;
    static Texture2D Planks
    {
        get
        {
            if (planks != null) return planks;
            int w = 256, h = 64; planks = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "ship_planks", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h]; var rnd = new System.Random(11);
            int boards = 8, bh = h / boards; var shade = new float[boards]; var off = new int[boards];
            for (int b = 0; b < boards; b++) { shade[b] = 0.82f + 0.3f * (float)rnd.NextDouble(); off[b] = rnd.Next(w); }
            for (int y = 0; y < h; y++)
            {
                int b = y / bh; bool seam = y % bh == 0;
                for (int x = 0; x < w; x++)
                {
                    bool butt = (x + off[b]) % 128 == 0;
                    float grain = 0.92f + 0.08f * Mathf.Sin((x * 0.11f + Mathf.Sin(y * 0.9f + b) * 2.5f)) + 0.05f * Mathf.PerlinNoise(x * 0.05f, y * 0.4f + b * 7f);
                    float v = seam || butt ? 0.45f : shade[b] * grain;
                    byte c = (byte)Mathf.Clamp(v * 255f, 0, 255); px[y * w + x] = new Color32(c, c, c, 255);
                }
            }
            planks.SetPixels32(px); planks.Apply(true); return planks;
        }
    }

    public static int Setup(AHGame g, Transform world)
    {
        if (world == null) return 0;
        Transform root = null; int n = 0;
        var groups = new HashSet<Transform>(); var built = new List<Vector3>();
        foreach (var r in world.GetComponentsInChildren<MeshRenderer>(false)) if (r.enabled && r.transform.parent != null) groups.Add(r.transform.parent);
        foreach (var grp in groups)
        {
            var rs = new List<MeshRenderer>();
            foreach (Transform c in grp) { var r = c.GetComponent<MeshRenderer>(); if (r != null && r.enabled && !c.name.StartsWith("AH_")) rs.Add(r); }
            if (rs.Count < 3 || rs.Count > 24) continue;
            // the masts: tall thin poles; the hull: a long low box
            var masts = new List<MeshRenderer>(); MeshRenderer hull = null; Vector3 hs = Vector3.zero; float best = 0f;
            foreach (var r in rs)
            {
                var b = r.bounds;
                if (b.size.y > 5f && b.size.y < 13f && Mathf.Max(b.size.x, b.size.z) < 1.3f) { masts.Add(r); continue; }
                var s = TrueSize(r); float lo = Mathf.Max(s.x, s.z), sh = Mathf.Min(s.x, s.z);
                if (lo > 7f && lo < 17f && sh > 2.5f && sh < 8f && s.y > 1.2f && s.y < 2.6f && lo * sh > best) { best = lo * sh; hull = r; hs = s; }
            }
            if (masts.Count == 0 || hull == null) continue;

            // which way it lies, how big, and which end is the stern (the tall box of a cabin stands there)
            var ht = hull.transform; Vector3 along = hs.x >= hs.z ? ht.right : ht.forward; along.y = 0f; along.Normalize();
            float L = Mathf.Max(hs.x, hs.z), W = Mathf.Min(hs.x, hs.z);
            Vector3 c0 = hull.bounds.center;
            float keel = float.PositiveInfinity, deckTop = hull.bounds.max.y;
            foreach (var r in rs) if ((r.bounds.center - c0).magnitude < L) keel = Mathf.Min(keel, r.bounds.min.y);
            foreach (var r in rs)
            {
                if (masts.Contains(r) || r == hull) continue; var b = r.bounds;
                if (b.size.y < 1.0f && Vector3.Scale(b.size, new Vector3(1, 0, 1)).magnitude > W) deckTop = Mathf.Max(deckTop, b.max.y);
            }
            float sternSide = 0f, tallest = 0f; Color sail = new Color(0.92f, 0.88f, 0.78f), flag = new Color(0.7f, 0.12f, 0.14f); float flagY = -1f;
            foreach (var r in rs)
            {
                if (masts.Contains(r) || r == hull) continue; var b = r.bounds;
                float a = Vector3.Dot(b.center - c0, along);
                if (b.size.y > 2.6f && b.size.y < 6f && b.max.y < deckTop + 4.5f && Mathf.Abs(a) > L * 0.2f && b.size.y > tallest) { tallest = b.size.y; sternSide = Mathf.Sign(a); }
                Color cc; if (!ColourOf(r, out cc)) continue;
                if (b.size.y > 2.8f && b.min.y > deckTop) sail = Color.Lerp(cc, new Color(0.93f, 0.89f, 0.79f), 0.6f);
                if (b.center.y > flagY && Mathf.Max(b.size.x, b.size.y, b.size.z) < 1.4f && b.min.y > deckTop + 4f) { flagY = b.center.y; flag = cc; }
            }
            if (sternSide == 0f) sternSide = ((int)(c0.x * 3 + c0.z) & 1) == 0 ? 1f : -1f;
            Vector3 fwd = -along * sternSide;   // the bow points away from the stern castle

            float mastH = 0f; foreach (var m in masts) mastH = Mathf.Max(mastH, m.bounds.max.y);
            foreach (var r in rs) { var dd = r.bounds.center - c0; dd.y = 0f; if (dd.magnitude < L * 0.9f) r.enabled = false; }
            // two old ships drawn through each other: keep the one
            bool clash = false; foreach (var o in built) { var dd = o - c0; dd.y = 0; if (dd.magnitude < L * 0.85f) clash = true; }
            if (clash) continue; built.Add(c0);

            if (root == null) root = new GameObject("Ships").transform;
            var ship = new GameObject("ship").transform; ship.SetParent(root, false);
            float sink = 0.55f;
            ship.position = new Vector3(c0.x, keel - sink, c0.z);
            ship.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            float H = Mathf.Max(1.6f, deckTop - keel + sink + 0.25f);
            Build(ship, L * 1.02f, W * 0.94f, H, Mathf.Max(5f, mastH - (keel - sink)), masts.Count >= 2, sail, flag, new System.Random(Mathf.RoundToInt(c0.x * 7 + c0.z * 13)));
            ship.gameObject.AddComponent<AHShipRock>();
            n++;
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " ships rebuilt");
        Piers(world);
        return n;
    }

    // ---------- piers: one long board on square sticks becomes a planked jetty on round piles ----------
    static void Piers(Transform world)
    {
        Transform root = null; int n = 0;
        var groups = new HashSet<Transform>();
        foreach (var r in world.GetComponentsInChildren<MeshRenderer>(false)) if (r.enabled && r.transform.parent != null) groups.Add(r.transform.parent);
        foreach (var grp in groups)
        {
            var rs = new List<MeshRenderer>();
            foreach (Transform c in grp) { var r = c.GetComponent<MeshRenderer>(); if (r != null && r.enabled && !c.name.StartsWith("AH_")) rs.Add(r); }
            if (rs.Count < 5 || rs.Count > 40) continue;
            MeshRenderer deck = null; Vector3 ds = Vector3.zero; var posts = new List<MeshRenderer>();
            foreach (var r in rs)
            {
                var s = TrueSize(r); float lo = Mathf.Max(s.x, s.z), sh = Mathf.Min(s.x, s.z);
                if (s.y <= 0.35f && lo >= 6f && lo <= 40f && sh >= 1.2f && sh <= 6f) { if (deck == null || lo * sh > Mathf.Max(ds.x, ds.z) * Mathf.Min(ds.x, ds.z)) { deck = r; ds = s; } }
                else if (s.y >= 0.8f && s.y <= 3f && Mathf.Max(s.x, s.z) <= 0.5f) posts.Add(r);
            }
            if (deck == null || posts.Count < 4) continue;
            var dt = deck.transform; Vector3 along = ds.x >= ds.z ? dt.right : dt.forward; along.y = 0f; along.Normalize();
            float L = Mathf.Max(ds.x, ds.z), W = Mathf.Min(ds.x, ds.z); Vector3 c0 = deck.bounds.center; float top = deck.bounds.max.y;
            deck.enabled = false; foreach (var p in posts) p.enabled = false;
            if (root == null) root = new GameObject("Piers").transform;
            var pier = new GameObject("pier").transform; pier.SetParent(root, false);
            pier.position = new Vector3(c0.x, top, c0.z); pier.rotation = Quaternion.LookRotation(along, Vector3.up);
            var rnd = new System.Random(Mathf.RoundToInt(c0.x * 5 + c0.z * 11));
            var m0 = Mat("plank0", new Color(0.62f, 0.47f, 0.32f), 0.1f, Planks); var m1 = Mat("plank1", new Color(0.54f, 0.4f, 0.27f), 0.1f, Planks);
            var beam = Mat("beam", new Color(0.33f, 0.23f, 0.15f), 0.15f, Planks); var wet = Mat("pile", new Color(0.26f, 0.2f, 0.15f), 0.3f);
            var rope = Mat("rope", new Color(0.3f, 0.24f, 0.17f), 0.1f);
            // two stringers under the boards, then the boards across with a finger's gap between
            for (int s = -1; s <= 1; s += 2) Box(pier, "stringer", new Vector3(s * W * 0.32f, -0.2f, 0), new Vector3(0.2f, 0.24f, L), beam);
            float step = 0.3f; int nb = Mathf.Max(2, Mathf.RoundToInt(L / step));
            for (int i = 0; i < nb; i++)
            {
                float z = -L * 0.5f + (i + 0.5f) * (L / nb), j = (float)rnd.NextDouble();
                var b = Box(pier, "plank", new Vector3((j - 0.5f) * 0.06f, -0.04f + j * 0.012f, z), new Vector3(W * (0.97f + 0.06f * j), 0.07f, L / nb - 0.035f), j < 0.5f ? m0 : m1);
                b.localRotation = Quaternion.Euler(0, (j - 0.5f) * 1.6f, 0);
            }
            // round piles where the old posts stood, sunk deep and standing a little proud at the edges
            foreach (var p in posts)
            {
                var lp = pier.InverseTransformPoint(p.bounds.center); lp.x = Mathf.Sign(lp.x == 0 ? 1 : lp.x) * (W * 0.5f + 0.1f);
                Pole(pier, new Vector3(lp.x, -2.2f, lp.z), new Vector3(lp.x, 0.32f, lp.z), 0.15f, 0.13f, wet, 10);
                if (rnd.NextDouble() < 0.4) Pole(pier, new Vector3(lp.x, 0.05f, lp.z), new Vector3(lp.x, 0.17f, lp.z), 0.18f, 0.18f, rope, 10);
            }
            // a bollard at the seaward end and a coil of rope
            Pole(pier, new Vector3(W * 0.3f, 0, L * 0.5f - 0.4f), new Vector3(W * 0.3f, 0.45f, L * 0.5f - 0.4f), 0.14f, 0.18f, wet, 10);
            Pole(pier, new Vector3(-W * 0.25f, 0, L * 0.5f - 0.7f), new Vector3(-W * 0.25f, 0.12f, L * 0.5f - 0.7f), 0.28f, 0.26f, rope, 12);
            foreach (var r in pier.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
            n++;
        }
        if (n > 0) Debug.Log("Ashen Hollow: " + n + " piers rebuilt");
    }

    static Vector3 TrueSize(MeshRenderer r)
    {
        var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return r.bounds.size;
        var s = mf.sharedMesh.bounds.size; var l = r.transform.lossyScale;
        return new Vector3(Mathf.Abs(s.x * l.x), Mathf.Abs(s.y * l.y), Mathf.Abs(s.z * l.z));
    }
    static bool ColourOf(Renderer r, out Color c)
    {
        c = Color.white; var m = r.sharedMaterial; if (m == null) return false;
        if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); else return false;
        return true;
    }

    // ---------- the ship, in its own space: x across, y up from the keel, z toward the bow ----------
    static float HalfW(float t, float W)   // t: 0 stern .. 1 bow
    {
        float f = 1f;
        if (t > 0.55f) { float u = (t - 0.55f) / 0.45f; f = Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)); }
        else if (t < 0.15f) f = 0.8f + 0.2f * (t / 0.15f);
        return W * 0.5f * Mathf.Max(f, 0.02f);
    }
    static float Top(float t, float H) { return H + 0.55f * Mathf.Pow(Mathf.Abs(t - 0.45f) * 1.8f, 2.2f); }
    static float Bottom(float t, float H) { return t > 0.72f ? H * 0.75f * Mathf.Pow((t - 0.72f) / 0.28f, 2f) : t < 0.06f ? 0.12f * (1f - t / 0.06f) : 0f; }

    static void Build(Transform ship, float L, float W, float H, float mastTop, bool twoMasts, Color sailCol, Color flagCol, System.Random rnd)
    {
        var wood = Mat("hull", new Color(0.42f, 0.28f, 0.17f), 0.15f, Planks);
        var dark = Mat("dark", new Color(0.24f, 0.16f, 0.1f), 0.2f);
        var deckM = Mat("deck", new Color(0.72f, 0.58f, 0.4f), 0.1f, Planks);
        var paint = Mat("paint" + ColorUtility.ToHtmlStringRGB(flagCol), Color.Lerp(flagCol, new Color(0.15f, 0.25f, 0.35f), 0.25f), 0.35f);
        var cloth = Mat("sail" + ColorUtility.ToHtmlStringRGB(sailCol), sailCol, 0.05f, null, true);
        var flagM = Mat("flag" + ColorUtility.ToHtmlStringRGB(flagCol), flagCol, 0.1f, null, true);
        var rope = Mat("rope", new Color(0.3f, 0.24f, 0.17f), 0.1f);
        var glass = Mat("window", new Color(1f, 0.72f, 0.35f), 0.6f, null, false, 0.45f);
        var brass = Mat("brass", new Color(0.8f, 0.62f, 0.3f), 0.6f);

        // hull: lofted sections, mirrored; the top two rows painted
        int NS = 28, K = 9;
        var hv = new List<Vector3>(); var huv = new List<Vector2>(); var tWood = new List<int>(); var tPaint = new List<int>(); var tDeck = new List<int>();
        for (int side = -1; side <= 1; side += 2)
        {
            int baseI = hv.Count;
            for (int i = 0; i <= NS; i++)
            {
                float t = i / (float)NS, z = (t - 0.5f) * L, hw = HalfW(t, W), top = Top(t, H), bot = Bottom(t, H);
                for (int k = 0; k <= K; k++)
                {
                    float a = k / (float)K * Mathf.PI * 0.5f;
                    float x = hw * Mathf.Pow(Mathf.Cos(a), 0.45f), y = top - (top - bot) * Mathf.Sin(a);
                    hv.Add(new Vector3(x * side, y, z)); huv.Add(new Vector2(z / 3f, y / 0.9f));
                }
            }
            for (int i = 0; i < NS; i++)
                for (int k = 0; k < K; k++)
                {
                    int a0 = baseI + i * (K + 1) + k, a1 = a0 + 1, b0 = a0 + K + 1, b1 = b0 + 1;
                    var list = k < 1 ? tPaint : tWood;
                    if (side > 0) list.AddRange(new[] { a0, b1, a1, a0, b0, b1 }); else list.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                }
        }
        // transom: the flat stern, a fan from its middle
        {
            int c = hv.Count; float t = 0f, top = Top(t, H), bot = Bottom(t, H), hw = HalfW(t, W);
            hv.Add(new Vector3(0, (top + bot) * 0.5f, -L * 0.5f)); huv.Add(new Vector2(0, 0));
            int s0 = hv.Count;
            for (int k = 0; k <= K; k++) { float a = k / (float)K * Mathf.PI * 0.5f; hv.Add(new Vector3(hw * Mathf.Pow(Mathf.Cos(a), 0.45f), top - (top - bot) * Mathf.Sin(a), -L * 0.5f)); huv.Add(new Vector2(hv[hv.Count - 1].x / 0.9f, hv[hv.Count - 1].y / 0.9f)); }
            for (int k = 0; k <= K; k++) { float a = k / (float)K * Mathf.PI * 0.5f; hv.Add(new Vector3(-hw * Mathf.Pow(Mathf.Cos(a), 0.45f), top - (top - bot) * Mathf.Sin(a), -L * 0.5f)); huv.Add(new Vector2(hv[hv.Count - 1].x / 0.9f, hv[hv.Count - 1].y / 0.9f)); }
            for (int k = 0; k < K; k++) { tWood.AddRange(new[] { c, s0 + k, s0 + k + 1 }); tWood.AddRange(new[] { c, s0 + K + 2 + k, s0 + K + 1 + k }); }
            tWood.AddRange(new[] { c, s0 + K + 1, s0 });   // the top edge between the two gunwale corners
        }
        // bulwarks inside and the gunwale cap, then the deck
        float bw = 0.12f;
        for (int side = -1; side <= 1; side += 2)
        {
            int b0 = hv.Count;
            for (int i = 0; i <= NS; i++)
            {
                float t = i / (float)NS, z = (t - 0.5f) * L, hw = HalfW(t, W), top = Top(t, H), inner = Mathf.Max(0.04f, hw - bw);
                hv.Add(new Vector3(hw * side, top + 0.06f, z)); huv.Add(new Vector2(z / 3f, 0));
                hv.Add(new Vector3(inner * side, top + 0.06f, z)); huv.Add(new Vector2(z / 3f, 0.15f));
                hv.Add(new Vector3(inner * side, Deck(t, H), z)); huv.Add(new Vector2(z / 3f, 0.6f));
            }
            for (int i = 0; i < NS; i++)
            {
                int a = b0 + i * 3, b = a + 3;
                if (side < 0) { tPaint.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1 }); tWood.AddRange(new[] { a + 1, b + 1, b + 2, a + 1, b + 2, a + 2 }); }
                else { tPaint.AddRange(new[] { a, b + 1, b, a, a + 1, b + 1 }); tWood.AddRange(new[] { a + 1, b + 2, b + 1, a + 1, a + 2, b + 2 }); }
            }
            // the band of the outer gunwale between the hull's top row and the cap
            int h0 = (side < 0 ? 0 : (NS + 1) * (K + 1));
            for (int i = 0; i < NS; i++)
            {
                int o0 = h0 + i * (K + 1), o1 = o0 + K + 1, c0 = b0 + i * 3, c1 = c0 + 3;
                if (side > 0) tPaint.AddRange(new[] { o0, c1, o1, o0, c0, c1 }); else tPaint.AddRange(new[] { o0, o1, c1, o0, c1, c0 });
            }
        }
        {
            int d0 = hv.Count;
            for (int i = 0; i <= NS; i++)
            {
                float t = i / (float)NS, z = (t - 0.5f) * L, hw = Mathf.Max(0.06f, HalfW(t, W) - bw), y = Deck(t, H);
                hv.Add(new Vector3(-hw, y, z)); huv.Add(new Vector2(z / 2.5f, -hw / 0.6f));
                hv.Add(new Vector3(hw, y, z)); huv.Add(new Vector2(z / 2.5f, hw / 0.6f));
            }
            for (int i = 0; i < NS; i++) { int a = d0 + i * 2; tDeck.AddRange(new[] { a, a + 2, a + 3, a, a + 3, a + 1 }); }
        }
        var mesh = new Mesh { name = "ship_hull" }; mesh.SetVertices(hv); mesh.SetUVs(0, huv); mesh.subMeshCount = 3;
        mesh.SetTriangles(tWood, 0); mesh.SetTriangles(tPaint, 1); mesh.SetTriangles(tDeck, 2); mesh.RecalculateNormals(); Sane(mesh); mesh.RecalculateBounds();
        Part(ship, "hull", mesh, new[] { wood, paint, deckM });

        // the stern castle: a raised deck over the last fifth, its walls following the hull, windows at the back
        float tC = 0.2f, cz0 = -L * 0.5f + 0.05f, cz1 = (tC - 0.5f) * L, cy0 = Deck(0.1f, H), cy1 = Top(0.05f, H) + 0.95f;
        float cw = HalfW(0.08f, W) - bw * 0.5f;
        var castle = Box(ship, "castle", new Vector3(0, (cy0 + cy1) * 0.5f, (cz0 + cz1) * 0.5f), new Vector3(cw * 2f, cy1 - cy0, cz1 - cz0), wood);
        Box(ship, "castle_trim", new Vector3(0, cy1 + 0.05f, (cz0 + cz1) * 0.5f), new Vector3(cw * 2f + 0.16f, 0.1f, cz1 - cz0 + 0.12f), paint);
        Box(ship, "castle_door", new Vector3(0, cy0 + 0.6f, cz1 + 0.02f), new Vector3(0.7f, 1.1f, 0.05f), dark);
        for (int i = -1; i <= 1; i++) Box(ship, "window", new Vector3(i * cw * 0.55f, cy0 + (cy1 - cy0) * 0.55f, -L * 0.5f - 0.02f), new Vector3(0.38f, 0.42f, 0.05f), glass);
        // the castle's rail: posts and a top rail round three sides
        for (int s = -1; s <= 1; s += 2)
        {
            Box(ship, "rail", new Vector3(s * cw, cy1 + 0.55f, (cz0 + cz1) * 0.5f), new Vector3(0.07f, 0.07f, cz1 - cz0), dark);
            for (float z = cz0 + 0.1f; z <= cz1; z += 0.7f) Box(ship, "post", new Vector3(s * cw, cy1 + 0.3f, z), new Vector3(0.06f, 0.5f, 0.06f), dark);
        }
        Box(ship, "rail", new Vector3(0, cy1 + 0.55f, cz1), new Vector3(cw * 2f, 0.07f, 0.07f), dark);
        // the stern lantern
        var lan = Box(ship, "lantern", new Vector3(0, cy1 + 0.9f, cz0 + 0.1f), new Vector3(0.22f, 0.32f, 0.22f), glass);
        Box(ship, "lantern_cap", new Vector3(0, cy1 + 1.1f, cz0 + 0.1f), new Vector3(0.28f, 0.08f, 0.28f), brass);
        Pole(ship, new Vector3(0, cy1, cz0 + 0.1f), new Vector3(0, cy1 + 0.75f, cz0 + 0.1f), 0.035f, 0.03f, dark);
        // the tiller: a wheel would be grander, this is a working boat
        Pole(ship, new Vector3(0, cy1 + 0.1f, cz0 + 0.6f), new Vector3(0, cy1 + 0.75f, cz0 + 1.3f), 0.05f, 0.035f, dark);

        // a hatch, a few barrels and a crate on deck
        Box(ship, "hatch", new Vector3(0, Deck(0.42f, H) + 0.12f, (0.42f - 0.5f) * L), new Vector3(W * 0.32f, 0.24f, W * 0.32f), dark);
        Box(ship, "grate", new Vector3(0, Deck(0.42f, H) + 0.25f, (0.42f - 0.5f) * L), new Vector3(W * 0.26f, 0.03f, W * 0.26f), deckM);
        for (int i = 0; i < 3; i++)
        {
            float t = 0.3f + 0.08f * i + 0.03f * (float)rnd.NextDouble(), s = (i % 2 == 0 ? 1 : -1);
            var bar = Pole(ship, new Vector3(s * HalfW(t, W) * 0.55f, Deck(t, H), (t - 0.5f) * L), new Vector3(s * HalfW(t, W) * 0.55f, Deck(t, H) + 0.7f, (t - 0.5f) * L), 0.27f, 0.27f, wood, 10, 0.12f);
        }
        Box(ship, "crate", new Vector3(-HalfW(0.62f, W) * 0.45f, Deck(0.62f, H) + 0.3f, (0.62f - 0.5f) * L), new Vector3(0.6f, 0.6f, 0.6f), deckM).localRotation = Quaternion.Euler(0, 17f, 0);

        // masts, yards and sails
        float deckMid = Deck(0.5f, H);
        var mastT = twoMasts ? new[] { 0.42f, 0.7f } : new[] { 0.52f };
        var mastHs = twoMasts ? new[] { mastTop - deckMid, (mastTop - deckMid) * 0.84f } : new[] { mastTop - deckMid };
        Vector3 mainTop = Vector3.zero, foreTop = Vector3.zero;
        for (int m = 0; m < mastT.Length; m++)
        {
            float z = (mastT[m] - 0.5f) * L, d = Deck(mastT[m], H), mh = mastHs[m];
            var b = new Vector3(0, d, z); var tp = new Vector3(0, d + mh, z);
            Pole(ship, b - Vector3.up * 0.2f, tp, 0.16f, 0.08f, wood, 10);
            if (m == 0) mainTop = tp; else foreTop = tp;
            // the crow's nest on the main mast
            if (m == 0) { Pole(ship, b + Vector3.up * mh * 0.8f, b + Vector3.up * (mh * 0.8f + 0.45f), 0.45f, 0.5f, wood, 10, 0.15f); }
            float yw = W * (m == 0 ? 1.15f : 1.0f);
            float y1 = d + mh * 0.74f, y2 = d + mh * 0.44f, y3 = d + 1.9f;
            Pole(ship, new Vector3(-yw * 0.5f, y1, z + 0.18f), new Vector3(yw * 0.5f, y1, z + 0.18f), 0.07f, 0.07f, wood, 6);
            Pole(ship, new Vector3(-yw * 0.55f, y2, z + 0.18f), new Vector3(yw * 0.55f, y2, z + 0.18f), 0.08f, 0.08f, wood, 6);
            Sail(ship, new Vector3(0, y1, z + 0.22f), yw * 0.86f, yw * 0.98f, y1 - y2 - 0.08f, 0.55f, cloth);
            Sail(ship, new Vector3(0, y2, z + 0.22f), yw * 1.0f, yw * 1.1f, y2 - y3, 0.7f, cloth);
            // shrouds down to the gunwales on both sides
            for (int s = -1; s <= 1; s += 2)
                for (int k = -1; k <= 1; k++)
                {
                    float tt = mastT[m] + k * 0.035f;
                    Pole(ship, new Vector3(0, d + mh * 0.78f, z), new Vector3(s * HalfW(tt, W), Top(tt, H), (tt - 0.5f) * L), 0.018f, 0.018f, rope, 4);
                }
        }
        // the pennant at the main masthead
        Pennant(ship, mainTop + Vector3.up * 0.05f, 1.3f, 0.45f, flagM);
        Pole(ship, mainTop, mainTop + Vector3.up * 0.55f, 0.04f, 0.03f, dark);

        // the bowsprit, a stay from it to the foremost mast, and a jib between them
        Vector3 bow = new Vector3(0, Top(1f, H) - 0.15f, L * 0.5f);
        Vector3 tip = bow + new Vector3(0, 0.55f, L * 0.22f);
        Pole(ship, bow - new Vector3(0, 0.1f, 0.8f), tip, 0.12f, 0.06f, wood, 8);
        Vector3 front = twoMasts ? foreTop : mainTop; Vector3 stayTop = Vector3.Lerp(new Vector3(0, Deck(twoMasts ? 0.7f : 0.52f, H), front.z), front, 0.85f);
        Pole(ship, tip, stayTop, 0.02f, 0.02f, rope, 4);
        Jib(ship, tip + new Vector3(0, -0.05f, -0.15f), stayTop - new Vector3(0, 0.3f, 0.2f), new Vector3(0, Top(0.9f, H) + 0.6f, (0.9f - 0.5f) * L), cloth);
        // the backstay to the stern
        Pole(ship, mainTop - Vector3.up * 0.2f, new Vector3(0, cy1 + 0.55f, cz0 + 0.2f), 0.02f, 0.02f, rope, 4);
        if (twoMasts) Pole(ship, foreTop - Vector3.up * 0.3f, mainTop - Vector3.up * (mainTop.y - foreTop.y + 0.6f), 0.02f, 0.02f, rope, 4);
        // a figurehead knob and an anchor hanging at the bow
        Box(ship, "stem", new Vector3(0, Top(1f, H) - 0.4f, L * 0.5f + 0.05f), new Vector3(0.14f, 0.9f, 0.3f), paint);
        Pole(ship, new Vector3(HalfW(0.88f, W) + 0.08f, Top(0.88f, H) - 0.1f, (0.88f - 0.5f) * L), new Vector3(HalfW(0.88f, W) + 0.08f, Top(0.88f, H) - 1.1f, (0.88f - 0.5f) * L), 0.05f, 0.05f, dark, 6);
        Box(ship, "anchor", new Vector3(HalfW(0.88f, W) + 0.08f, Top(0.88f, H) - 1.15f, (0.88f - 0.5f) * L), new Vector3(0.08f, 0.1f, 0.7f), dark);
        foreach (var r in ship.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
    }
    // a vertex that only touches squashed triangles gets no normal, and a zero normal lights as NaN (a white blaze
    // through the bloom): give those a plain up normal
    static void Sane(Mesh m)
    {
        var n = m.normals; bool bad = false;
        for (int i = 0; i < n.Length; i++) { var v = n[i]; if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || v.sqrMagnitude < 1e-6f) { n[i] = Vector3.up; bad = true; } }
        if (bad) m.normals = n;
    }
    static float Deck(float t, float H) { return Top(t, H) - 0.45f; }

    static Transform Part(Transform parent, string name, Mesh mesh, Material[] m)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterials = m; return go.transform;
    }
    static Mesh cube;
    static Mesh Cube { get { if (cube == null) { var g = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cube), PrimitiveType.Cube); cube = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); } return cube; } }
    static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
    {
        var t = Part(parent, name, Cube, new[] { m }); t.localPosition = pos; t.localScale = size; return t;
    }
    // a tapered round pole from a to b (radius ra at a, rb at b); bulge > 0 swells the middle (barrels)
    static readonly Dictionary<string, Mesh> poles = new Dictionary<string, Mesh>();
    static Transform Pole(Transform parent, Vector3 a, Vector3 b, float ra, float rb, Material m, int seg = 8, float bulge = 0f)
    {
        string key = seg + "_" + Mathf.RoundToInt(rb / Mathf.Max(ra, 1e-4f) * 100f) + "_" + Mathf.RoundToInt(bulge * 100f);
        Mesh mesh; if (!poles.TryGetValue(key, out mesh) || mesh == null)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); int rings = bulge > 0f ? 6 : 1; float k = rb / Mathf.Max(ra, 1e-4f);
            for (int i = 0; i <= rings; i++)
            {
                float y = i / (float)rings, r = Mathf.Lerp(1f, k, y) * (1f + bulge * Mathf.Sin(y * Mathf.PI));
                for (int s = 0; s <= seg; s++) { float an = s * Mathf.PI * 2f / seg; v.Add(new Vector3(Mathf.Cos(an) * r, y, Mathf.Sin(an) * r)); uv.Add(new Vector2(s / (float)seg, y)); }
            }
            for (int i = 0; i < rings; i++) for (int s = 0; s < seg; s++) { int p = i * (seg + 1) + s, q = p + seg + 1; t.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
            int c0 = v.Count; v.Add(Vector3.zero); uv.Add(Vector2.zero); int c1 = v.Count; v.Add(Vector3.up); uv.Add(Vector2.zero);
            for (int s = 0; s < seg; s++) { t.AddRange(new[] { c0, s, s + 1 }); int top = rings * (seg + 1); t.AddRange(new[] { c1, top + s + 1, top + s }); }
            mesh = new Mesh { name = "ship_pole" }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); Sane(mesh); mesh.RecalculateBounds(); poles[key] = mesh;
        }
        var tr = Part(parent, "pole", mesh, new[] { m });
        Vector3 d = b - a; float len = d.magnitude;
        tr.localPosition = a; tr.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized); tr.localScale = new Vector3(ra, len, ra);
        return tr;
    }
    // a square sail hanging from a yard at 'top', wider at the foot, bellied forward (+z)
    static void Sail(Transform parent, Vector3 top, float wTop, float wFoot, float h, float belly, Material m)
    {
        int nx = 8, ny = 6; var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
        for (int j = 0; j <= ny; j++)
            for (int i = 0; i <= nx; i++)
            {
                float u = i / (float)nx, w = j / (float)ny, wid = Mathf.Lerp(wTop, wFoot, w);
                float x = (u - 0.5f) * wid, y = -w * h, z = belly * Mathf.Sin(u * Mathf.PI) * Mathf.Sin(Mathf.Min(1f, w * 1.15f) * Mathf.PI * 0.85f + 0.25f);
                v.Add(new Vector3(x, y, z)); uv.Add(new Vector2(u, w));
            }
        for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++) { int p = j * (nx + 1) + i, q = p + nx + 1; t.AddRange(new[] { p, p + 1, q + 1, p, q + 1, q }); }
        var mesh = new Mesh { name = "ship_sail" }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); Sane(mesh); mesh.RecalculateBounds();
        var tr = Part(parent, "sail", mesh, new[] { m }); tr.localPosition = top;
    }
    static void Jib(Transform parent, Vector3 a, Vector3 b, Vector3 c, Material m)
    {
        var v = new List<Vector3>(); var t = new List<int>(); int n = 6;
        Vector3 bulge = new Vector3(0.35f, 0, 0);
        for (int j = 0; j <= n; j++) for (int i = 0; i <= n - j; i++)
            {
                float u = i / (float)n, w = j / (float)n, s = 1f - u - w;
                v.Add(a * s + b * u + c * w + bulge * (4f * u * s + 4f * w * s + 4f * u * w) * 0.5f);
            }
        int row = 0;
        for (int j = 0; j < n; j++)
        {
            int len = n - j + 1, next = row + len;
            for (int i = 0; i < len - 1; i++)
            {
                t.AddRange(new[] { row + i, row + i + 1, next + i });
                if (i < len - 2) t.AddRange(new[] { row + i + 1, next + i + 1, next + i });
            }
            row = next;
        }
        var mesh = new Mesh { name = "ship_jib" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); Sane(mesh); mesh.RecalculateBounds();
        Part(parent, "jib", mesh, new[] { m });
    }
    static void Pennant(Transform parent, Vector3 at, float len, float h, Material m)
    {
        var v = new List<Vector3>(); var t = new List<int>(); int n = 8;
        for (int i = 0; i <= n; i++) { float u = i / (float)n, hh = h * (1f - u * 0.85f), z = -u * len, x = Mathf.Sin(u * 5f) * 0.12f * u; v.Add(new Vector3(x, 0.55f, z)); v.Add(new Vector3(x, 0.55f - hh, z)); }
        for (int i = 0; i < n; i++) { int p = i * 2; t.AddRange(new[] { p, p + 2, p + 3, p, p + 3, p + 1 }); }
        var mesh = new Mesh { name = "ship_pennant" }; mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); Sane(mesh); mesh.RecalculateBounds();
        var tr = Part(parent, "pennant", mesh, new[] { m }); tr.localPosition = at; tr.gameObject.AddComponent<AHShipFlag>();
    }
}

// the ships ride the swell
public class AHShipRock : MonoBehaviour
{
    Vector3 basePos; Quaternion baseRot; float ph;
    void Start() { basePos = transform.position; baseRot = transform.rotation; ph = (basePos.x * 0.37f + basePos.z * 0.21f) % 6.28f; }
    void Update()
    {
        float t = Time.time + ph;
        transform.position = basePos + Vector3.up * (Mathf.Sin(t * 0.9f) * 0.07f);
        transform.rotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.7f + 1f) * 0.9f, 0f, Mathf.Sin(t * 0.55f) * 1.8f);
    }
}
// the pennant streams and flutters
public class AHShipFlag : MonoBehaviour
{
    float ph;
    void Start() { ph = transform.position.x * 0.3f; }
    void Update() { transform.localRotation = Quaternion.Euler(0f, 20f + Mathf.Sin(Time.time * 1.3f + ph) * 25f, 0f); }
}
