// Ashen Hollow: the Wardrobe, as in the web game (v66, P.coll / P.cos / shownItem / collect / saveOutfit / wearOutfit).
// Cosmetics never take a bag slot: they go straight into the Wardrobe, together with every piece of gear you have ever
// owned. For each slot you choose what is SHOWN: your real gear (the default), any collected piece your class can wear,
// or nothing (head and cape only). Your stats always come from the gear you have equipped. Up to five outfits can be
// saved and worn again with one tap.
// The shown pieces are also drawn on the hero now: helms, hoods, crowns, horns and hats on the head, pads on the
// shoulders, plates or a robe skirt on the body, gauntlets, greaves and boots, and a cloak, coat-tails, wings or a
// quiver on the back, each in the item's own colour.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class AHOutfit { public string name; public List<AHKS> look = new List<AHKS>(); }

public static class AHWardrobe
{
    public const int OutfitMax = 5;
    public static readonly string[] Slots = { "head", "shoulders", "chest", "hands", "legs", "feet", "cape" };
    static AHGame g;

    static string Base(string id) { var d = AHItems.Get(id); return d != null && d.enh > 0 && d.baseId != null ? d.baseId : id; }
    public static bool Has(AHPlayer p, string id) { return p.prog.coll.Contains(Base(id)); }

    // web: collect
    public static bool Collect(AHPlayer p, string id)
    {
        if (p == null || id == null) return false; id = Base(id);
        var d = AHItems.Get(id); if (d == null || !d.IsGear || p.prog.coll.Contains(id)) return false;
        p.prog.coll.Add(id); return true;
    }

    public static string Cos(AHPlayer p, string slot) { foreach (var e in p.prog.cos) if (e.k == slot) return e.v; return null; }
    public static void SetCos(AHPlayer p, string slot, string id)
    {
        p.prog.cos.RemoveAll(e => e.k == slot);
        if (!string.IsNullOrEmpty(id)) p.prog.cos.Add(new AHKS { k = slot, v = id });
    }

    // web: shownItem
    public static string Shown(AHPlayer p, string slot)
    {
        if (p == null || p.prog == null) return null;
        string c = Cos(p, slot); if (c == "none") return null;
        if (c != null) return c;
        return p.bag != null ? p.bag.Worn(slot) : null;
    }
    public static string Sig(AHPlayer p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in Slots) { sb.Append('|'); sb.Append(Shown(p, s)); }
        return sb.ToString();
    }

    public static void Setup(AHGame game)
    {
        g = game; var p = g.player; if (p == null) return;
        // older saves: cosmetics that sat in the bag or were worn as gear move into the Wardrobe
        foreach (var id in new List<string>(p.bag.inv.Keys)) { var d = AHItems.Get(id); if (d == null || !d.IsGear) continue; Collect(p, id); if (d.cosmetic) { p.bag.inv.Remove(id); p.bag.order.Remove(id); } }
        foreach (var s in AHItems.GearSlots)
        {
            string w = p.bag.Worn(s); if (w == null) continue; Collect(p, w);
            var d = AHItems.Get(w); if (d != null && d.cosmetic) { p.bag.gear.Remove(s); if (Cos(p, s) == null) SetCos(p, s, w); }
        }
        foreach (var e in new List<AHKS>(p.prog.cos)) if (e.v != "none" && (AHItems.Get(e.v) == null || !AHItems.CanUse(e.v, p.cls.id))) p.prog.cos.Remove(e);
        AHBag.CosHook = (id, n) =>
        {
            var pl = g != null ? g.player : null; var d = AHItems.Get(id); if (pl == null || d == null) return false;
            if (d.cosmetic) { if (Collect(pl, id)) g.ui.Toast(d.name + " added to your Wardrobe."); return true; }
            if (d.IsGear) Collect(pl, id);
            return false;
        };
        p.bag.Touch();
    }

    static float t;
    public static void Tick(AHGame game)
    {
        t -= Time.deltaTime; if (t > 0) return; t = 0.3f;
        var p = game.player; if (p == null || p.prog == null) return;
        foreach (var s in AHItems.GearSlots) { string w = p.bag.Worn(s); if (w != null) Collect(p, w); }
        p.CheckOutfit();
    }

    public static int CosmeticCount(AHPlayer p) { int n = 0; foreach (var k in p.prog.coll) { var d = AHItems.Get(k); if (d != null && d.cosmetic) n++; } return n; }

    // the choices for one slot: show gear, hide (head and cape), then every collected piece your class can wear
    public static List<string> Options(AHPlayer p, string slot)
    {
        var o = new List<string> { "" }; if (slot == "head" || slot == "cape") o.Add("none");
        foreach (var k in p.prog.coll) { var d = AHItems.Get(k); if (d != null && d.slot == slot && AHItems.CanUse(k, p.cls.id)) o.Add(k); }
        return o;
    }
    public static string OptName(string v) { if (v == "") return "Show gear"; if (v == "none") return "Hidden"; var d = AHItems.Get(v); return d != null ? d.name : v; }

    // web: saveOutfit / wearOutfit
    public static string SaveOutfit(AHPlayer p)
    {
        if (p.prog.outfits.Count >= OutfitMax) return "You can keep " + OutfitMax + " outfits. Delete one first.";
        var o = new AHOutfit { name = "Outfit " + (p.prog.outfits.Count + 1) };
        foreach (var s in Slots) { string c = Cos(p, s); string id = c == "none" ? "none" : Shown(p, s); if (id != null) o.look.Add(new AHKS { k = s, v = id == "none" ? "none" : Base(id) }); }
        p.prog.outfits.Add(o); return "Outfit saved. Tap it any time to wear that look.";
    }
    public static string WearOutfit(AHPlayer p, int i)
    {
        if (i < 0 || i >= p.prog.outfits.Count) return null; var o = p.prog.outfits[i]; int n = 0, skip = 0;
        foreach (var e in o.look)
        {
            if (e.v == "none") { SetCos(p, e.k, e.k == "head" || e.k == "cape" ? "none" : null); n++; }
            else if (AHItems.Get(e.v) != null && Has(p, e.v) && AHItems.CanUse(e.v, p.cls.id)) { string w = p.bag.Worn(e.k); SetCos(p, e.k, w != null && Base(w) == e.v ? null : e.v); n++; }
            else skip++;
        }
        return n > 0 ? "Wearing " + o.name + "." + (skip > 0 ? " " + skip + " piece" + (skip > 1 ? "s" : "") + " your class can’t wear stayed as they were." : "") : "None of " + o.name + " fits your class.";
    }

    // ================= the pieces drawn on the hero =================
    static Mesh cone, frustum, torus, shell;
    static Shader lit;
    static readonly HashSet<string> Metal = new HashSet<string> { "helm", "forgehelm", "crown", "circlet", "plates", "spikes", "armor", "plate", "molten", "gauntlet", "greaves", "tricorn_no" };

    static Mesh Frustum(float rTop, float rBot, float h, int seg = 20)
    {
        var m = new Mesh(); var v = new List<Vector3>(); var tr = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2 / seg; float c = Mathf.Cos(a), s = Mathf.Sin(a);
            v.Add(new Vector3(c * rBot, 0, s * rBot)); v.Add(new Vector3(c * rTop, h, s * rTop));
        }
        int n0 = v.Count; for (int i = 0; i < n0; i++) v.Add(v[i]);   // a second copy of the side for the inside face
        for (int i = 0; i < seg; i++) { int a = i * 2; tr.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 }); int c = n0 + a; tr.AddRange(new[] { c, c + 2, c + 1, c + 2, c + 3, c + 1 }); }
        int cb = v.Count; v.Add(Vector3.zero); for (int i = 0; i < seg; i++) tr.AddRange(new[] { cb, i * 2, i * 2 + 2 });
        m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    // ---- feathered wing meshes (span along +X, the leading edge arching up, feathers hanging down and out) ----
    static Mesh wingF, wingC, wingA;
    static Vector3 Lead(float t) { return new Vector3(t, 0.2f * Mathf.Sin(t * Mathf.PI * 0.75f) - 0.04f * t, 0f); }
    static void Quad2(List<Vector3> v, List<int> tr, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        // both faces, so the wing shows from in front and behind
        int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        tr.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        int j = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        tr.AddRange(new[] { j, j + 2, j + 1, j, j + 3, j + 2 });
    }
    static Mesh Done(List<Vector3> v, List<int> tr) { var m = new Mesh(); m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m; }
    public static Mesh WingFeathers
    {
        get
        {
            if (wingF != null) return wingF;
            var v = new List<Vector3>(); var tr = new List<int>(); int n = 9;
            for (int i = 0; i < n; i++)
            {
                float t = 0.12f + 0.88f * i / (n - 1f);
                Vector3 root = Lead(t) + new Vector3(0, -0.02f, 0.006f * i);
                float ang = Mathf.Lerp(-95f, -25f, Mathf.Pow(i / (n - 1f), 1.3f)) * Mathf.Deg2Rad, len = Mathf.Lerp(0.42f, 0.7f, i / (n - 1f)), w = 0.115f;
                Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0), side = new Vector3(-dir.y, dir.x, 0);
                // a long feather: square-ish base, widest at two thirds, a rounded point
                Vector3 b0 = root - side * w * 0.5f, b1 = root + side * w * 0.5f, m0 = root + dir * len * 0.7f - side * w * 0.62f, m1 = root + dir * len * 0.7f + side * w * 0.62f, tip = root + dir * len;
                Quad2(v, tr, b0, m0, m1, b1);
                int a = v.Count; v.Add(m0); v.Add(tip); v.Add(m1); tr.AddRange(new[] { a, a + 1, a + 2 }); int c = v.Count; v.Add(m0); v.Add(tip); v.Add(m1); tr.AddRange(new[] { c, c + 2, c + 1 });
            }
            return wingF = Done(v, tr);
        }
    }
    public static Mesh WingCoverts
    {
        get
        {
            if (wingC != null) return wingC;
            // two rows of short rounded coverts along the arm
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 10; i++)
                {
                    float t = 0.04f + 0.92f * i / 9f; Vector3 root = Lead(t) + new Vector3(0, -0.01f - row * 0.06f, -0.002f * row);
                    float len = (row == 0 ? 0.13f : 0.2f) * (1f - t * 0.35f), w = 0.1f;
                    float ang = Mathf.Lerp(-95f, -40f, t) * Mathf.Deg2Rad; Vector3 dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0), side = new Vector3(-dir.y, dir.x, 0);
                    Quad2(v, tr, root - side * w * 0.5f, root + dir * len - side * w * 0.3f, root + dir * len + side * w * 0.3f, root + side * w * 0.5f);
                }
            return wingC = Done(v, tr);
        }
    }
    public static Mesh WingArm
    {
        get
        {
            if (wingA != null) return wingA;
            // the leading edge: a thin band that tapers to the tip
            var v = new List<Vector3>(); var tr = new List<int>(); int n = 14;
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1f) / n, w0 = 0.035f * (1f - t0 * 0.7f), w1 = 0.035f * (1f - t1 * 0.7f);
                Vector3 a = Lead(t0), b = Lead(t1);
                Quad2(v, tr, a + Vector3.up * w0, b + Vector3.up * w1, b - Vector3.up * w1, a - Vector3.up * w0);
            }
            return wingA = Done(v, tr);
        }
    }

    static Mesh Torus(float R, float r, int seg = 28, int side = 8)
    {
        var m = new Mesh(); var v = new List<Vector3>(); var tr = new List<int>();
        for (int i = 0; i <= seg; i++) for (int j = 0; j <= side; j++)
            {
                float a = i * Mathf.PI * 2 / seg, b = j * Mathf.PI * 2 / side;
                v.Add(new Vector3((R + r * Mathf.Cos(b)) * Mathf.Cos(a), r * Mathf.Sin(b), (R + r * Mathf.Cos(b)) * Mathf.Sin(a)));
            }
        for (int i = 0; i < seg; i++) for (int j = 0; j < side; j++)
            {
                int a = i * (side + 1) + j, b = a + side + 1;
                tr.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
            }
        m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    class Kit
    {
        public Transform root; public float k; public Color col; public bool metal; public float glow;
        Material mat, dark, gold;
        public Material Mat { get { if (mat == null) mat = Make(col, metal, glow); return mat; } }
        public Material Dark { get { if (dark == null) dark = Make(Color.Lerp(col, Color.black, 0.45f), metal, 0); return dark; } }
        public Material Gold { get { if (gold == null) gold = Make(new Color(1f, 0.8f, 0.3f), true, 0); return gold; } }
        // one material per colour and finish, shared by every piece that wears it (so the figures batch together)
        static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
        static Material Make(Color c, bool metal, float glow)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + (metal ? "m" : "") + glow.ToString("0.00"); Material cm;
            if (made.TryGetValue(key, out cm) && cm != null) return cm;
            var m = new Material(lit); m.SetColor("_BaseColor", c); m.enableInstancing = true; made[key] = m;
            m.SetFloat("_Metallic", metal ? 0.75f : 0f); m.SetFloat("_Smoothness", metal ? 0.62f : 0.18f);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
            return m;
        }
        public Transform Prim(PrimitiveType pt, Vector3 pos, Vector3 size, Vector3 rot, Material m = null)
        {
            var o = AHLowPoly.Fix(GameObject.CreatePrimitive(pt), pt); UnityEngine.Object.Destroy(o.GetComponent<Collider>());
            o.name = "Piece"; o.transform.SetParent(root, false); o.transform.localPosition = pos * k; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size * k;
            o.GetComponent<Renderer>().sharedMaterial = m ?? Mat; return o.transform;
        }
        public Transform Mesh(Mesh mesh, Vector3 pos, Vector3 size, Vector3 rot, Material m = null)
        {
            var o = new GameObject("Piece"); o.transform.SetParent(root, false); o.transform.localPosition = pos * k; o.transform.localRotation = Quaternion.Euler(rot); o.transform.localScale = size * k;
            o.AddComponent<MeshFilter>().sharedMesh = mesh; o.AddComponent<MeshRenderer>().sharedMaterial = m ?? Mat; return o.transform;
        }
    }

    // the cloak's cloth, in the cape kit's own space (y up from the chest, z toward the front), for a drop of L:
    // rows v0..v1 of it (0 = collar, 1 = hem), pushed out by 'lift' (a trim laid over the cloth); two faces, so it
    // shows from inside as well
    static readonly Dictionary<string, Mesh> cloaks = new Dictionary<string, Mesh>();
    static Mesh Cloak(float L, float v0, float v1, float lift)
    {
        string key = Mathf.RoundToInt(L * 100f) + "_" + Mathf.RoundToInt(v0 * 100f) + "_" + Mathf.RoundToInt(v1 * 100f);
        Mesh m; if (cloaks.TryGetValue(key, out m) && m != null) return m;
        int nu = 14, nv = Mathf.Max(2, Mathf.RoundToInt(12 * (v1 - v0))); var v = new List<Vector3>(); var t = new List<int>();
        for (int side = 0; side < 2; side++)
        {
            int b0 = v.Count; float th = side == 0 ? 0f : 0.012f;
            for (int j = 0; j <= nv; j++)
                for (int i = 0; i <= nu; i++)
                {
                    float u = i / (float)nu, w = Mathf.Lerp(v0, v1, j / (float)nv), e = 2f * u - 1f;
                    float wid = Mathf.Lerp(0.36f, 0.6f, Mathf.Pow(w, 0.8f));
                    float y = 0.07f - w * L + Mathf.Pow(w, 4f) * 0.06f * e * e;
                    float z = -0.15f - w * L * 0.2f + Mathf.Lerp(0.13f, 0.035f, w) * e * e - 0.024f * w * (0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 6f + 0.5f));
                    v.Add(new Vector3(e * wid * 0.5f, y, z + th - lift));
                }
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    int p = b0 + j * (nu + 1) + i, q = p + nu + 1;
                    if (side == 0) t.AddRange(new[] { p, p + 1, q + 1, p, q + 1, q }); else t.AddRange(new[] { p, q + 1, p + 1, p, q, q + 1 });
                }
        }
        m = new Mesh { name = "cloak" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        cloaks[key] = m; return m;
    }

    // a full robe: a fitted bodice from the chest to the waist, then a skirt that falls straight from the hips and
    // only opens out toward the hem (not a bell). Every ring is skinned: the bodice to the spine, the skirt more and
    // more to the thigh, then the calf, of its own side, so it walks with the legs
    static Vector4 waistR;   // the robe's waist ring (height, side and front radius) for the belt
    static bool Skirt(GameObject rig, Transform hold, Material mat, float k, float ground)
    {
        Transform pel = Bone(rig, "pelvis"), tl = Bone(rig, "thigh_l"), tr = Bone(rig, "thigh_r"), cl = Bone(rig, "calf_l"), cr = Bone(rig, "calf_r");
        if (pel == null || tl == null || tr == null || cl == null || cr == null) return false;
        Transform s3 = Bone(rig, "spine_03") ?? pel, s1 = Bone(rig, "spine_01") ?? pel, ual = Bone(rig, "upperarm_l"), uar = Bone(rig, "upperarm_r");
        var go = new GameObject("Outfit_robe"); go.transform.SetParent(rig.transform, false);
        Vector3 lr = tl.position - tr.position; lr.y = 0f; float half = lr.magnitude * 0.5f; Vector3 side = lr.sqrMagnitude > 1e-6f ? lr.normalized : hold.right * -1f;
        Vector3 fwd = hold.forward; fwd.y = 0f; fwd.Normalize();
        float sh = ual != null && uar != null ? Vector3.Distance(ual.position, uar.position) * 0.5f : 0.18f * k;
        Vector3 c = (tl.position + tr.position) * 0.5f;
        float chestY = s3.position.y - 0.02f * k, waistY = Mathf.Lerp(pel.position.y, s1.position.y, 0.6f), hipY = pel.position.y - 0.04f * k, hem = ground + 0.1f * k;
        // how far the body itself reaches at each height (sides, and front or back), measured from its posed mesh,
        // so the cloth always sits just outside it whatever the build or sex
        var body = BodyPoints(rig);
        Func<float, float, Vector2> reach = (y, band) =>
        {
            float sx = 0f, fz = 0f;
            foreach (var bp0 in body) { if (Mathf.Abs(bp0.y - y) > band) continue; Vector3 d = bp0 - c; sx = Mathf.Max(sx, Mathf.Abs(Vector3.Dot(d, side))); fz = Mathf.Max(fz, Mathf.Abs(Vector3.Dot(d, fwd))); }
            return new Vector2(sx, fz);
        };
        Func<float, float, float, float, float, Vector4> fit = (y, rs, rf, w, band) =>
        {
            var r = reach(y, band); return new Vector4(y, Mathf.Max(rs, r.x * 1.12f + 0.03f * k), Mathf.Max(rf, r.y * 1.12f + 0.03f * k), w);
        };
        // rings from the chest down: height, side radius, front radius, how much of the legs it follows (<0 bodice)
        var ring = new List<Vector4>();
        ring.Add(fit(chestY, sh * 0.7f, 0.12f * k, -1f, 0.04f * k));
        ring.Add(fit(Mathf.Lerp(chestY, waistY, 0.5f), sh * 0.62f, 0.11f * k, -0.5f, 0.04f * k));
        var wr = fit(waistY, half + 0.04f * k, 0.105f * k, 0f, 0.03f * k); ring.Add(wr);
        const int nv = 9;
        float prevS = 0f, prevF = 0f;
        for (int j = 0; j <= nv; j++)
        {
            // fitted over the hips, then an A-line that opens out to a wide hem, so the legs never push through
            float v = j / (float)nv, fl = Mathf.Pow(v, 1.1f), y = Mathf.Lerp(hipY, hem, v);
            var R = fit(y, Mathf.Lerp(wr.y + 0.05f * k, half + 0.27f * k, fl), Mathf.Lerp(wr.z + 0.015f * k, 0.235f * k, fl), v, 0.07f * k);
            R.y = Mathf.Max(R.y, prevS); R.z = Mathf.Max(R.z, prevF); prevS = R.y; prevF = R.z;
            ring.Add(R);
        }
        waistR = wr;
        const int nu = 24;
        var verts = new List<Vector3>(); var bw = new List<BoneWeight>(); var tris = new List<int>();
        int rows = ring.Count;
        for (int face = 0; face < 2; face++)
        {
            int b0 = verts.Count; float inset = face == 0 ? 0f : -0.008f * k;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i <= nu; i++)
                {
                    var R = ring[j]; float v = Mathf.Max(0f, R.w), th = i / (float)nu * Mathf.PI * 2f;
                    float wave = 1f + 0.03f * v * Mathf.Sin(th * 7f);   // soft folds toward the hem
                    Vector3 wp = c + side * Mathf.Cos(th) * (R.y + inset) * wave + fwd * Mathf.Sin(th) * (R.z + inset) * wave; wp.y = R.x;
                    verts.Add(go.transform.InverseTransformPoint(wp));
                    var ws = new float[7];   // pelvis, thigh l, thigh r, calf l, calf r, chest, low spine
                    if (R.w < 0f) { float up = -R.w; ws[5] = up; ws[6] = 1f - up; }
                    else
                    {
                        float sd = Mathf.Cos(th), fl = Mathf.Clamp01(0.5f + 0.8f * sd), fr = 1f - fl;
                        float leg = Mathf.SmoothStep(0f, 0.6f, Mathf.InverseLerp(0.1f, 0.95f, v)), cs = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, v));
                        ws[0] = 1f - leg; ws[1] = leg * (1f - cs) * fl; ws[2] = leg * (1f - cs) * fr; ws[3] = leg * cs * fl; ws[4] = leg * cs * fr;
                        if (j == 2) { ws[0] = 0.6f; ws[6] = 0.4f; }
                    }
                    // keep the four biggest (a skinned vertex takes four bones)
                    var idx = new List<int> { 0, 1, 2, 3, 4, 5, 6 }; idx.Sort((x, y) => ws[y].CompareTo(ws[x]));
                    float sum = ws[idx[0]] + ws[idx[1]] + ws[idx[2]] + ws[idx[3]]; sum = Mathf.Max(1e-5f, sum);
                    bw.Add(new BoneWeight { boneIndex0 = idx[0], weight0 = ws[idx[0]] / sum, boneIndex1 = idx[1], weight1 = ws[idx[1]] / sum, boneIndex2 = idx[2], weight2 = ws[idx[2]] / sum, boneIndex3 = idx[3], weight3 = ws[idx[3]] / sum });
                }
            for (int j = 0; j < rows - 1; j++)
                for (int i = 0; i < nu; i++)
                {
                    int a = b0 + j * (nu + 1) + i, b = a + nu + 1;
                    if (face == 0) tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); else tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
        }
        var bones = new[] { pel, tl, tr, cl, cr, s3, s1 };
        var mesh = new Mesh { name = "robe" };
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.boneWeights = bw.ToArray();
        var bp = new Matrix4x4[bones.Length]; for (int i = 0; i < bones.Length; i++) bp[i] = bones[i].worldToLocalMatrix * go.transform.localToWorldMatrix;
        mesh.bindposes = bp; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var smr = go.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = mesh; smr.bones = bones; smr.rootBone = pel; smr.sharedMaterial = mat;
        smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
        return true;
    }

    // the posed body's skin (torso and legs, not arms, head or hair) as world points
    static List<Vector3> BodyPoints(GameObject rig)
    {
        var pts = new List<Vector3>(); var baked = new Mesh();
        foreach (var smr in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            string n = smr.name; if (n.StartsWith("Outfit") || n.Contains("Hair") || n.Contains("Head") || n.Contains("Brow") || n.Contains("Eye") || n.Contains("Beard") || n.Contains("Hood")) continue;
            var sm = smr.sharedMesh; if (sm == null || smr.bones == null || smr.bones.Length == 0) continue;
            smr.BakeMesh(baked, true); var vs = baked.vertices; var bws = sm.boneWeights; if (bws.Length != vs.Length) continue;
            var keep = new bool[smr.bones.Length];
            for (int i = 0; i < keep.Length; i++) { var b = smr.bones[i]; if (b == null) continue; string bn = b.name; keep[i] = bn.StartsWith("spine") || bn == "pelvis" || bn.StartsWith("thigh") || bn.StartsWith("calf"); }
            var M = smr.transform.localToWorldMatrix;
            for (int i = 0; i < vs.Length; i += 2) { int bi = bws[i].boneIndex0; if (bi < keep.Length && keep[bi] && bws[i].weight0 > 0.5f) pts.Add(M.MultiplyPoint3x4(vs[i])); }
        }
        UnityEngine.Object.Destroy(baked);
        return pts;
    }

    // a cloth hood that follows the head with the face left open, falling to a short cape over the shoulders (a
    // cowl wraps the neck as well); 'point' gives the wizard's peak at the back
    static readonly Dictionary<int, Mesh> hoods = new Dictionary<int, Mesh>();
    static Mesh Hood(bool point, bool cowl)
    {
        int key = (point ? 1 : 0) + (cowl ? 2 : 0); Mesh m;
        if (hoods.TryGetValue(key, out m) && m != null) return m;
        var v = new List<Vector3>(); var tr = new List<int>();
        const int nu = 28, nv = 16;
        Vector3 ctr = new Vector3(0, 0.1f, -0.025f);
        Func<float, float, Vector3> P = (u, w) =>
        {
            // u: round the head from the front (0) ; w: 0 top .. 1 the shoulders
            float az = u * Mathf.PI * 2f, cs = Mathf.Cos(az), sn = Mathf.Sin(az);
            Vector3 p;
            if (w <= 0.62f)
            {
                float el = Mathf.Lerp(Mathf.PI * 0.5f, -Mathf.PI * 0.28f, w / 0.62f);   // elevation: crown to below the jaw
                float r = Mathf.Cos(el);
                p = ctr + new Vector3(sn * r * 0.112f, Mathf.Sin(el) * 0.13f, cs * r * 0.135f);
                { float back = Mathf.Max(0f, -cs); p += (point ? new Vector3(0, 0.07f, -0.05f) : new Vector3(0, 0.015f, -0.035f)) * back * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(el)), 3f); }
            }
            else
            {
                float t = (w - 0.62f) / 0.38f; float el = -Mathf.PI * 0.28f, r0 = Mathf.Cos(el);
                Vector3 top = ctr + new Vector3(sn * r0 * 0.112f, Mathf.Sin(el) * 0.13f, cs * r0 * 0.135f);
                // down over the shoulders, wider at the sides than front and back
                float spread = Mathf.Lerp(1f, cowl ? 1.75f : 1.55f, t) ;
                p = new Vector3(top.x * Mathf.Lerp(1f, 1.7f, t) * spread / 1.2f, top.y - t * (cowl ? 0.2f : 0.15f), (top.z + 0.03f) * spread * 0.8f - 0.03f);
            }
            return p;
        };
        for (int face = 0; face < 2; face++)
        {
            int b0 = v.Count;
            for (int j = 0; j <= nv; j++) for (int i = 0; i <= nu; i++) { var p = P(i / (float)nu, j / (float)nv); if (face == 1) p *= 0.985f; v.Add(p); }
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    float u = (i + 0.5f) / nu, w = (j + 0.5f) / nv; float du = Mathf.Min(u, 1f - u);
                    // the face opening: the front from the brow to the chin (a cowl closes under the chin)
                    if (du < 0.19f && w > 0.16f && w < (cowl ? 0.6f : 0.7f)) continue;
                    if (!cowl && du < 0.09f && w >= 0.7f) continue;   // a hood stays open down the throat
                    int a = b0 + j * (nu + 1) + i, b = a + nu + 1;
                    if (face == 0) tr.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); else tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
        }
        m = new Mesh { name = "hood" }; m.SetVertices(v); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds();
        hoods[key] = m; return m;
    }

    static Transform Bone(GameObject rig, string name) { foreach (var t in rig.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null; }

    // called right after the hero model is built (still in its rest pose)
    public static void Dress(AHPlayer p, GameObject rig)
    {
        if (rig == null || p == null || p.prog == null) return;
        dye = AHPeople.OutfitColor(p.look); dyeOn = p.look != null;
        DressWith(rig, p.transform, s => Shown(p, s), () => p.Moving);
        dyeOn = false;
    }
    // the outfit colour chosen in the look editor dyes the cloth and leather of worn gear (metal keeps its own colour)
    static Color dye = Color.white; static bool dyeOn;

    // dresses any rig of the hero's kind: 'shown' gives the item id for a slot (or null), 'moving' drives the cape
    public static void DressWith(GameObject rig, Transform hold, Func<string, string> shown, Func<bool> moving)
    {
        if (rig == null) return;
        Transform head = Bone(rig, "Head"), chest = Bone(rig, "spine_03"), pelvis = Bone(rig, "pelvis");
        if (head == null || chest == null) return;   // the KayKit class hero has no such rig: nothing to dress
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        if (cone == null) { cone = Frustum(0f, 0.5f, 1f); frustum = Frustum(0.55f, 1f, 1f); torus = Torus(0.5f, 0.06f); shell = Frustum(1f, 0.85f, 1f); }
        float ground = hold.position.y;
        float k = Mathf.Max(0.5f, (head.position.y - ground) / 1.55f);
        Quaternion face = hold.rotation;

        Func<Transform, Vector3, string, Kit> start = (bone, at, id) =>
        {
            var d = AHItems.Get(id);
            var go = new GameObject("Outfit_" + d.slot); go.transform.position = at; go.transform.rotation = face;
            go.transform.SetParent(bone, true);
            bool isMetal = Metal.Contains(d.form ?? "");
            return new Kit { root = go.transform, k = k, col = dyeOn && !isMetal ? Color.Lerp(d.color, dye, 0.55f) : d.color, metal = isMetal, glow = d.form == "molten" || d.form == "ember" || d.form == "halo" ? 1.6f : 0f };
        };

        // ---- head ----
        string hid = shown("head"); var hd = hid != null ? AHItems.Get(hid) : null;
        if (hd != null)
        {
            var K = start(head, head.position + hold.up * 0.08f * k, hid); string f = hd.form ?? "helm";
            bool covers = f == "helm" || f == "forgehelm" || f == "hood" || f == "mhood" || f == "cowl";
            if (f == "helm" || f == "forgehelm")
            {
                K.Prim(PrimitiveType.Sphere, new Vector3(0, 0.1f, -0.045f), new Vector3(0.27f, 0.22f, 0.28f), Vector3.zero);
                K.Mesh(torus, new Vector3(0, 0.085f, -0.04f), new Vector3(0.275f, 0.5f, 0.285f), Vector3.zero, K.Dark);
                if (f == "forgehelm") K.Prim(PrimitiveType.Cube, new Vector3(0, 0.22f, -0.05f), new Vector3(0.03f, 0.07f, 0.24f), Vector3.zero, K.Dark);
            }
            else if (f == "hood" || f == "mhood" || f == "cowl")
            {
                K.Mesh(Hood(f == "mhood", f == "cowl"), Vector3.zero, Vector3.one, Vector3.zero);
            }
            else if (f == "crown" || f == "circlet")
            {
                K.Mesh(torus, new Vector3(0, 0.085f, -0.03f), new Vector3(0.222f, 0.32f, 0.238f), Vector3.zero);
                if (f == "crown") for (int i = 0; i < 7; i++) { float a = i * Mathf.PI * 2 / 7; K.Mesh(cone, new Vector3(Mathf.Cos(a) * 0.115f, 0.095f, Mathf.Sin(a) * 0.12f - 0.03f), new Vector3(0.05f, 0.08f, 0.05f), Vector3.zero); }
                else K.Prim(PrimitiveType.Sphere, new Vector3(0, 0.085f, 0.09f), Vector3.one * 0.03f, Vector3.zero, K.Gold);
            }
            else if (f == "halo") K.Mesh(torus, new Vector3(0, 0.3f, -0.04f), new Vector3(0.26f, 0.7f, 0.26f), Vector3.zero);
            else if (f == "horns" || f == "antlers")
            {
                foreach (int sx in new[] { -1, 1 })
                {
                    if (f == "horns") { K.Mesh(frustum, new Vector3(sx * 0.075f, 0.05f, -0.02f), new Vector3(0.05f, 0.12f, 0.05f), new Vector3(-10, 0, -sx * 62)); K.Mesh(cone, new Vector3(sx * 0.17f, 0.1f, -0.03f), new Vector3(0.075f, 0.17f, 0.075f), new Vector3(-20, 0, -sx * 18)); }
                    else K.Mesh(cone, new Vector3(sx * 0.07f, 0.07f, -0.03f), new Vector3(0.06f, 0.26f, 0.06f), new Vector3(-15, 0, -sx * 22));
                    if (f == "antlers") { K.Mesh(cone, new Vector3(sx * 0.12f, 0.19f, -0.04f), new Vector3(0.04f, 0.12f, 0.04f), new Vector3(10, 0, -sx * 60)); K.Mesh(cone, new Vector3(sx * 0.1f, 0.22f, -0.07f), new Vector3(0.04f, 0.1f, 0.04f), new Vector3(-30, 0, -sx * 10)); }
                }
            }
            else if (f == "tricorn")
            {
                K.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.06f, -0.04f), new Vector3(0.4f, 0.008f, 0.38f), Vector3.zero);
                K.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.11f, -0.04f), new Vector3(0.24f, 0.05f, 0.24f), Vector3.zero);
                foreach (int sx in new[] { -1, 1 }) K.Prim(PrimitiveType.Cube, new Vector3(sx * 0.13f, 0.09f, -0.06f), new Vector3(0.02f, 0.07f, 0.24f), new Vector3(0, sx * 30, 0));
                K.Prim(PrimitiveType.Cube, new Vector3(0, 0.09f, -0.18f), new Vector3(0.26f, 0.07f, 0.02f), Vector3.zero);
                K.Prim(PrimitiveType.Cube, new Vector3(0, 0.08f, 0.04f), new Vector3(0.25f, 0.012f, 0.02f), Vector3.zero, K.Gold);
            }
            else K.Prim(PrimitiveType.Sphere, new Vector3(0, 0.1f, -0.045f), new Vector3(0.27f, 0.22f, 0.28f), Vector3.zero);
            if (covers) foreach (var r in rig.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains("Hair") && !r.name.Contains("Beard")) r.enabled = false;
        }

        // ---- shoulders ----
        string sid = shown("shoulders"); var sd = sid != null ? AHItems.Get(sid) : null;
        if (sd != null)
            foreach (var side in new[] { "l", "r" })
            {
                Transform ua = Bone(rig, "upperarm_" + side), cl = Bone(rig, "clavicle_" + side); if (ua == null) continue;
                var K = start(cl ?? ua, ua.position + hold.up * 0.05f * k, sid); string f = sd.form ?? "plates"; float sx = Vector3.Dot(ua.position - chest.position, hold.right) >= 0 ? 1 : -1;
                if (f == "plates" || f == "spikes") { K.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.17f, 0.09f, 0.17f), new Vector3(0, 0, -sx * 18)); K.Prim(PrimitiveType.Sphere, new Vector3(sx * 0.03f, -0.035f, 0), new Vector3(0.15f, 0.07f, 0.15f), new Vector3(0, 0, -sx * 30), K.Dark); }
                if (f == "spikes") for (int i = -1; i <= 1; i++) K.Mesh(cone, new Vector3(sx * 0.02f, 0.03f, i * 0.045f), new Vector3(0.035f, 0.1f, 0.035f), new Vector3(0, 0, -sx * 15), K.Dark);
                if (f == "fur") { K.Prim(PrimitiveType.Sphere, new Vector3(-sx * 0.02f, 0, 0), new Vector3(0.21f, 0.12f, 0.2f), Vector3.zero); K.Prim(PrimitiveType.Sphere, new Vector3(-sx * 0.08f, 0.01f, -0.03f), new Vector3(0.16f, 0.1f, 0.18f), Vector3.zero, K.Dark); }
                if (f == "feathers") for (int i = 0; i < 5; i++) K.Prim(PrimitiveType.Cube, new Vector3(sx * (0.02f + i * 0.012f), 0.02f, -0.06f + i * 0.03f), new Vector3(0.03f, 0.015f, 0.15f), new Vector3(-30 + i * 15, 0, -sx * (20 + i * 6)), i % 2 == 0 ? K.Mat : K.Dark);
            }

        // ---- chest: plates over the body, or a robe skirt ----
        string cid = shown("chest"); var cd = cid != null ? AHItems.Get(cid) : null;
        if (cd != null)
        {
            string f = cd.form ?? "vest";
            TintCloth(rig, cd.color);
            if (f == "armor" || f == "plate" || f == "molten")
            {
                var K = start(chest, chest.position, cid);
                K.Mesh(shell, new Vector3(0, -0.16f, 0.01f), new Vector3(0.17f, 0.32f, 0.125f), Vector3.zero);
                K.Mesh(torus, new Vector3(0, -0.16f, 0.01f), new Vector3(0.33f, 0.7f, 0.25f), Vector3.zero, K.Dark);
                K.Mesh(torus, new Vector3(0, 0.15f, 0.0f), new Vector3(0.3f, 0.6f, 0.22f), Vector3.zero, K.Dark);
                if (f == "molten") K.Prim(PrimitiveType.Cube, new Vector3(0, 0.0f, 0.13f), new Vector3(0.04f, 0.2f, 0.02f), Vector3.zero, K.Gold);
            }
            else if (f == "robe" && pelvis != null)
            {
                // a skirt of cloth skinned to the hips, thighs and calves: it walks with the legs instead of
                // swinging out as one stiff cone
                var K = start(pelvis, pelvis.position, cid);
                if (!Skirt(rig, hold, K.Mat, k, ground)) { K.Mesh(frustum, new Vector3(0, -0.62f, 0), new Vector3(0.27f, 0.68f, 0.24f), Vector3.zero); K.Mesh(torus, new Vector3(0, 0.04f, 0), new Vector3(0.32f, 0.8f, 0.27f), Vector3.zero, K.Dark); }
                else
                {
                    // a belt round the waist, tied at the front
                    var belt = K.Mesh(torus, Vector3.zero, Vector3.one, Vector3.zero, K.Dark);
                    belt.position = new Vector3(pelvis.position.x, waistR.x, pelvis.position.z); belt.localScale = new Vector3(waistR.y * 2.06f, 0.5f * k, waistR.z * 2.06f) / Mathf.Max(1e-4f, belt.parent.lossyScale.x);
                    belt.rotation = Quaternion.LookRotation(hold.forward);
                    K.Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(0.05f, 0.045f, 0.02f), Vector3.zero, K.Gold).position = belt.position + hold.forward * waistR.z * 1.03f;
                }
            }
        }

        // ---- hands, legs, feet ----
        string gid = shown("hands"); var gd = gid != null ? AHItems.Get(gid) : null;
        if (gd != null) foreach (var side in new[] { "l", "r" }) { var hb = Bone(rig, "hand_" + side); var la = Bone(rig, "lowerarm_" + side); if (hb == null) continue; var K = start(hb, Vector3.Lerp(la != null ? la.position : hb.position, hb.position, 0.82f), gid); bool gnt = gd.form == "gauntlet"; K.Prim(PrimitiveType.Sphere, Vector3.zero, gnt ? new Vector3(0.1f, 0.1f, 0.1f) : new Vector3(0.085f, 0.085f, 0.085f), Vector3.zero); if (gnt) K.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.12f, 0.075f, 0.12f), Vector3.zero, K.Dark); }
        string lid = shown("legs"); var ld = lid != null ? AHItems.Get(lid) : null;
        if (ld != null) foreach (var side in new[] { "l", "r" }) { var cb = Bone(rig, "calf_" + side); var ft = Bone(rig, "foot_" + side); if (cb == null || ft == null) continue; var K = start(cb, Vector3.Lerp(cb.position, ft.position, 0.42f) + hold.forward * 0.03f * k, lid); K.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.1f, 0.26f, 0.1f), Vector3.zero); K.Prim(PrimitiveType.Sphere, new Vector3(0, 0.15f, 0.02f), new Vector3(0.1f, 0.08f, 0.09f), Vector3.zero, K.Dark); }
        string fid = shown("feet"); var fd = fid != null ? AHItems.Get(fid) : null;
        if (fd != null) foreach (var side in new[] { "l", "r" }) { var ft = Bone(rig, "foot_" + side); if (ft == null) continue; var K = start(ft, new Vector3(ft.position.x, ground, ft.position.z) + hold.forward * 0.035f * k, fid); K.Prim(PrimitiveType.Sphere, new Vector3(0, 0.055f, 0.04f), new Vector3(0.115f, 0.1f, 0.27f), Vector3.zero); K.Mesh(shell, new Vector3(0, 0.04f, -0.015f), new Vector3(0.06f, 0.18f, 0.064f), Vector3.zero); K.Mesh(torus, new Vector3(0, 0.215f, -0.015f), new Vector3(0.13f, 0.4f, 0.135f), Vector3.zero, K.Dark); }

        // ---- cape ----
        string kid = shown("cape"); var kd = kid != null ? AHItems.Get(kid) : null;
        if (kd != null)
        {
            var K = start(chest, chest.position, kid); string f = kd.form ?? "cloak";
            // the back piece follows the chest but stays upright, and swings back as you run
            K.root.SetParent(rig.transform, true); K.root.gameObject.AddComponent<AHCapeSwing>().Setup(hold, moving, chest, f == "cloak" || f == "ember" || f == "coat" ? 1f : f == "wings" ? 0.3f : 0.15f);
            float L = Mathf.Max(0.6f, (chest.position.y - ground) / k * 0.82f);
            if (f == "cloak" || f == "ember")
            {
                // a draped cloak: wraps round the shoulders, falls in folds, flares and rounds off at the hem
                K.Mesh(Cloak(L, 0f, 1f, 0f), Vector3.zero, Vector3.one, Vector3.zero);
                K.Prim(PrimitiveType.Cube, new Vector3(0, 0.07f, -0.12f), new Vector3(0.42f, 0.06f, 0.08f), Vector3.zero, K.Dark);
                foreach (int sx in new[] { -1, 1 }) K.Prim(PrimitiveType.Sphere, new Vector3(sx * 0.17f, 0.06f, -0.06f), new Vector3(0.055f, 0.055f, 0.03f), Vector3.zero, K.Gold);   // the clasps
                if (f == "ember") K.Mesh(Cloak(L, 0.93f, 1f, -0.006f), Vector3.zero, Vector3.one, Vector3.zero, K.Gold);
            }
            else if (f == "coat")
                foreach (int sx in new[] { -1, 1 }) K.Prim(PrimitiveType.Cube, new Vector3(sx * 0.1f, -L * 0.62f, -0.2f - L * 0.08f), new Vector3(0.2f, L * 0.55f, 0.025f), new Vector3(9, 0, sx * 4));
            else if (f == "wings")
            {
                var back = K.root;
                foreach (int sx in new[] { -1, 1 })
                {
                    // each wing: an arm rising out from the shoulder blade, with feathers hanging from it
                    var w = new GameObject("Wing").transform; w.SetParent(back, false); w.localPosition = new Vector3(sx * 0.06f, 0.0f, -0.19f) * k; w.localRotation = Quaternion.Euler(0, sx * 24f, 0); K.root = w;
                    // a feathered wing: a curved arm along the leading edge, layered coverts over long flight feathers that
                    // fan out and grow toward the tip; it flaps slowly, faster while you run
                    var ws = new Vector3(sx * 0.56f, 0.56f, 0.56f);
                    K.Mesh(WingFeathers, Vector3.zero, ws, Vector3.zero, K.Mat);
                    K.Mesh(WingCoverts, new Vector3(0, 0, -0.004f), ws, Vector3.zero, K.Dark);
                    K.Mesh(WingArm, new Vector3(0, 0, -0.008f), ws, Vector3.zero, K.Gold);
                    w.gameObject.AddComponent<AHWingFlap>().Setup(sx, moving);
                    K.root = back;
                }
            }
            else if (f == "quiver")
            {
                K.Prim(PrimitiveType.Cylinder, new Vector3(0.06f, -0.1f, -0.16f), new Vector3(0.1f, 0.2f, 0.1f), new Vector3(0, 0, 25));
                for (int i = 0; i < 4; i++) K.Prim(PrimitiveType.Cube, new Vector3(0.14f + i * 0.012f, 0.12f + (i % 2) * 0.01f, -0.16f + (i - 1.5f) * 0.02f), new Vector3(0.012f, 0.08f, 0.03f), new Vector3(0, 0, 25), K.Gold);
            }
        }
        foreach (Transform tf in rig.GetComponentsInChildren<Transform>(true)) if (tf.name == "Piece") { var r = tf.GetComponent<Renderer>(); if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
    }

    // a chest piece colours the clothes of the body under it
    static void TintCloth(GameObject rig, Color c)
    {
        foreach (var r in rig.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "Piece") continue; var mats = r.materials; bool ch = false;
            foreach (var m in mats)
            {
                if (m == null || !(m.name.Contains("Peasant") || m.name.Contains("Ranger"))) continue;
                foreach (var prop in new[] { "baseColorFactor", "_BaseColor" }) if (m.HasProperty(prop)) { var b = m.GetColor(prop); var t2 = Color.Lerp(b, c.linear * 1.6f, 0.55f); t2.a = b.a; m.SetColor(prop, t2); ch = true; }
            }
            if (ch) r.materials = mats;
        }
    }
}

public partial class AHUI
{
    public void OpenWardrobe() { wkMode = "ward"; wkPageI = 0; ShowWork(true); RenderWork(); }
    void RenderWardrobe(AHPlayer p)
    {
        var rows = new List<Action<int>>();
        wkTitle.text = "Wardrobe";
        wkHint.text = "Cosmetics change only how you look. Your stats always come from your equipped gear. Every piece you have ever owned is saved here · " + AHWardrobe.CosmeticCount(p) + " cosmetics.";
        foreach (var sl in AHWardrobe.Slots)
        {
            string s0 = sl; var opts = AHWardrobe.Options(p, sl); string cur = AHWardrobe.Cos(p, sl) ?? ""; int ci = Mathf.Max(0, opts.IndexOf(cur));
            var shown = AHWardrobe.Shown(p, sl); var sd = shown != null ? AHItems.Get(shown) : null;
            Action<int> step = d => { int ni = (ci + d + opts.Count) % opts.Count; AHWardrobe.SetCos(p, s0, opts[ni] == "" ? null : opts[ni]); p.CheckOutfit(); g.SaveProgress(); RenderWork(); };
            rows.Add(s => Row(s, AHItems.SlotName(s0) + ": " + AHWardrobe.OptName(cur), sd != null ? AHItems.Quality(sd) : new Color(1f, 1f, 1f, 0.7f),
                (cur == "" ? (sd != null ? "Showing your " + sd.name : "Nothing equipped") : cur == "none" ? "Hidden" : "Shown over your gear") + " · " + (opts.Count - 1) + " choices", "",
                new WkBtn { label = "◀", on = opts.Count > 1, col = Plain, act = () => step(-1) }, new WkBtn { label = "▶", on = opts.Count > 1, col = Plain, act = () => step(1) }));
        }
        rows.Add(s => Row(s, "Save this look", new Color(1f, 0.85f, 0.55f), p.prog.outfits.Count + "/" + AHWardrobe.OutfitMax + " outfits saved", "",
            new WkBtn { label = "Save outfit", on = p.prog.outfits.Count < AHWardrobe.OutfitMax, col = Go, act = () => { Toast(AHWardrobe.SaveOutfit(p)); g.SaveProgress(); RenderWork(); } }));
        for (int i = 0; i < p.prog.outfits.Count; i++)
        {
            int ii = i; var o = p.prog.outfits[i]; var names = new List<string>(); foreach (var e in o.look) names.Add(AHWardrobe.OptName(e.v));
            rows.Add(s => Row(s, o.name, new Color(0.85f, 0.55f, 1f), string.Join(" · ", names.ToArray()), "",
                new WkBtn { label = "Wear", on = true, col = Go, act = () => { Toast(AHWardrobe.WearOutfit(p, ii)); p.CheckOutfit(); g.SaveProgress(); RenderWork(); } },
                new WkBtn { label = "Delete", on = true, col = Plain, act = () => { p.prog.outfits.RemoveAt(ii); g.SaveProgress(); RenderWork(); } }));
        }
        int from = Paged(rows.Count);
        for (int i = from; i < Mathf.Min(rows.Count, from + RowsPerPage); i++) rows[i](i - from);
    }
}

// keeps a cape on the hero's back: at the chest bone, upright, swinging back with speed and a little sway
public class AHCapeSwing : MonoBehaviour
{
    Transform who, bone; Func<bool> moving; float amt, tilt, ph;
    public void Setup(Transform w, Func<bool> mv, Transform b, float k) { who = w; moving = mv; bone = b; amt = k; }
    void LateUpdate()
    {
        if (who == null || bone == null) return;
        bool m = moving != null && moving();
        float want = m ? 13f : 0f; tilt = Mathf.Lerp(tilt, want, Time.deltaTime * 4f); ph += Time.deltaTime * (m ? 8f : 1.6f);
        float sway = Mathf.Sin(ph) * (m ? 2.5f : 1.2f);
        transform.position = bone.position;
        transform.rotation = who.rotation * Quaternion.Euler((tilt + sway) * amt, 0, Mathf.Sin(ph * 0.5f) * 2f * amt);
    }
}

// a cosmetic wing's slow beat: folds back and forth about its root, quicker and wider while the hero runs
public class AHWingFlap : MonoBehaviour
{
    float sx = 1f, t, amp; Func<bool> moving; Quaternion rest;
    public void Setup(float side, Func<bool> mv) { sx = side; moving = mv; rest = transform.localRotation; }
    void Update()
    {
        bool run = moving != null && moving();
        amp = Mathf.Lerp(amp, run ? 1f : 0.35f, 1f - Mathf.Exp(-Time.deltaTime * 3f));
        t += Time.deltaTime * (1.6f + amp * 3.4f);
        float beat = Mathf.Sin(t) * (6f + amp * 16f);
        transform.localRotation = rest * Quaternion.Euler(0, sx * beat * 0.6f, -sx * beat);
    }
}
