// Ashen Hollow: camp tents. The web game's tents were plain brown pyramids; these are built in code to match the rest
// of the world: a round striped pavilion (canvas walls and a peaked roof with a scalloped valance, a centre pole and a
// pennant, the door flaps tied open) and a ridge tent (canvas over a ridge pole, open front flaps, a back wall),
// both pegged down with guy ropes and stakes. Each camp gets its own stripe colour. Setup finds the tents of the
// area (world.json 'tent' props), hides the old pyramid and puts a new tent there, its door facing the campfire.
using System.Collections.Generic;
using UnityEngine;

public static class AHTents
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.25f, float metal = 0f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
        m.SetFloat("_Cull", 0f); m.doubleSidedGI = true;   // canvas is seen from inside and out
        mats[key] = m; return m;
    }
    static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

    // a mesh builder that keeps one triangle list per material
    class MB
    {
        public readonly Dictionary<Material, List<Vector3>> v = new Dictionary<Material, List<Vector3>>();
        public void Tri(Material m, Vector3 a, Vector3 b, Vector3 c)
        {
            List<Vector3> l; if (!v.TryGetValue(m, out l)) v[m] = l = new List<Vector3>();
            l.Add(a); l.Add(b); l.Add(c);
        }
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(m, a, b, c); Tri(m, a, c, d); }
        // a round pole or rope between two points
        public void Rod(Material m, Vector3 a, Vector3 b, float r, int n = 6)
        {
            Vector3 d = (b - a).normalized, s = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized, u = Vector3.Cross(d, s);
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                Vector3 o0 = (s * Mathf.Cos(a0) + u * Mathf.Sin(a0)) * r, o1 = (s * Mathf.Cos(a1) + u * Mathf.Sin(a1)) * r;
                Quad(m, a + o0, a + o1, b + o1, b + o0);
            }
        }
        public void Build(Transform parent)
        {
            foreach (var kv in v)
            {
                var go = new GameObject(kv.Key.name); go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = "Tent" }; mesh.SetVertices(kv.Value);
                var idx = new int[kv.Value.Count]; for (int i = 0; i < idx.Length; i++) idx[i] = i;
                mesh.SetTriangles(idx, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
            }
        }
    }

    static readonly int[] Stripes = { 0xa3242a, 0x2a5aa0, 0x2e7a4a, 0xc08a2a, 0x6a3a8a };

    // a round pavilion facing +Z, radius about 1.6 m
    public static GameObject Pavilion(Transform parent, int seed)
    {
        var root = new GameObject("Tent"); root.transform.SetParent(parent, false);
        var rnd = new System.Random(seed); int sc = Stripes[rnd.Next(Stripes.Length)];
        Material cream = Mat("tent_cream", C(0xe8dcc0)), stripe = Mat("tent_stripe_" + sc.ToString("x"), C(sc)), wood = Mat("tent_wood", C(0x5a3e26), 0.2f),
                 rope = Mat("tent_rope", C(0xbfa77a), 0.1f), gold = Mat("tent_gold", C(0xd9ab3a), 0.6f, 0.8f), dark = Mat("tent_inside", C(0x3a2c20), 0.1f);
        var mb = new MB(); int n = 16; float R = 1.55f, wallH = 1.25f, roofR = 1.75f, apex = 3.0f, door = 0.5f;   // door: half-angle (radians) of the opening
        for (int i = 0; i < n; i++)
        {
            float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n, mid = (a0 + a1) * 0.5f;
            Vector3 w0 = new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)), w1 = new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1));
            Material m = i % 2 == 0 ? cream : stripe;
            // the roof panel (stripes run from the edge to the peak)
            mb.Tri(m, w0 * roofR + Vector3.up * wallH, Vector3.up * apex, w1 * roofR + Vector3.up * wallH);
            // the valance: a pointed tongue hanging under each panel
            Vector3 v0 = w0 * roofR + Vector3.up * wallH, v1 = w1 * roofR + Vector3.up * wallH;
            Vector3 vm = Vector3.Lerp(v0, v1, 0.5f) - Vector3.up * 0.28f;
            mb.Tri(i % 2 == 0 ? stripe : cream, v0, v1, vm);
            // the wall, except across the door
            bool inDoor = Mathf.Abs(Mathf.DeltaAngle(mid * Mathf.Rad2Deg, 0f)) < door * Mathf.Rad2Deg;
            if (!inDoor) mb.Quad(cream, w0 * R, w0 * R + Vector3.up * wallH, w1 * R + Vector3.up * wallH, w1 * R);
            else mb.Quad(dark, w0 * R * 0.2f + Vector3.up * 0.01f, w0 * R * 0.95f + Vector3.up * 0.01f, w1 * R * 0.95f + Vector3.up * 0.01f, w1 * R * 0.2f + Vector3.up * 0.01f);   // floor cloth seen inside
            // guy ropes and stakes from every other panel corner
            if (i % 2 == 0 && !inDoor)
            {
                Vector3 top = w0 * roofR + Vector3.up * wallH, stake = w0 * (roofR + 0.9f);
                mb.Rod(rope, top, stake + Vector3.up * 0.08f, 0.012f, 4);
                mb.Rod(wood, stake - Vector3.up * 0.05f, stake + Vector3.up * 0.14f, 0.025f, 5);
            }
        }
        // the door flaps, tied back to each side
        foreach (float sx in new[] { -1f, 1f })
        {
            float ea = sx * door; Vector3 e = new Vector3(Mathf.Sin(ea), 0, Mathf.Cos(ea)) * R;
            Vector3 fold = e + new Vector3(sx * 0.35f, 0, 0.12f);
            mb.Tri(stripe, e + Vector3.up * wallH, fold + Vector3.up * 0.25f, e);
            mb.Tri(cream, e + Vector3.up * wallH, e + Vector3.up * 0.6f + new Vector3(sx * 0.18f, 0, 0.1f), fold + Vector3.up * 0.25f);
            mb.Rod(wood, e, e + Vector3.up * (wallH + 0.05f), 0.035f);
        }
        mb.Rod(wood, Vector3.zero, Vector3.up * (apex + 0.55f), 0.05f);   // the centre pole, through the peak
        mb.Build(root.transform);
        // a gilt knob and a pennant on top
        var knob = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); Object.Destroy(knob.GetComponent<Collider>());
        knob.transform.SetParent(root.transform, false); knob.transform.localPosition = Vector3.up * (apex + 0.58f); knob.transform.localScale = Vector3.one * 0.12f; knob.GetComponent<Renderer>().sharedMaterial = gold;
        Pennant(root.transform, Vector3.up * (apex + 0.45f), stripe);
        return root;
    }

    // a ridge tent facing +Z: 2.6 m wide, 3 m deep, 2 m high
    public static GameObject Ridge(Transform parent, int seed)
    {
        var root = new GameObject("Tent"); root.transform.SetParent(parent, false);
        var rnd = new System.Random(seed); int sc = Stripes[rnd.Next(Stripes.Length)];
        Material canvas = Mat("tent_canvas", C(0xd8c8a2)), stripe = Mat("tent_stripe_" + sc.ToString("x"), C(sc)), wood = Mat("tent_wood", C(0x5a3e26), 0.2f),
                 rope = Mat("tent_rope", C(0xbfa77a), 0.1f), dark = Mat("tent_inside", C(0x3a2c20), 0.1f);
        var mb = new MB(); float W = 1.35f, D = 1.5f, H = 2.0f, sag = 0.06f;
        int seg = 6;
        // the two roof slopes, in bands; a coloured band along the eaves
        for (int k = 0; k < seg; k++)
        {
            float z0 = -D + 2f * D * k / seg, z1 = -D + 2f * D * (k + 1) / seg;
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 r0 = new Vector3(0, H, z0), r1 = new Vector3(0, H, z1);
                Vector3 m0 = new Vector3(sx * W * 0.55f, H * 0.42f - sag, z0), m1 = new Vector3(sx * W * 0.55f, H * 0.42f - sag, z1);
                Vector3 e0 = new Vector3(sx * W, 0.12f, z0), e1 = new Vector3(sx * W, 0.12f, z1);
                mb.Quad(canvas, r0, r1, m1, m0);
                mb.Quad(stripe, m0, m1, e1, e0);
            }
        }
        // the back wall and the open front: flaps tied back
        Vector3 bl = new Vector3(-W, 0.12f, -D), br = new Vector3(W, 0.12f, -D), bt = new Vector3(0, H, -D);
        mb.Tri(canvas, bl, bt, br);
        mb.Quad(dark, new Vector3(-W * 0.9f, 0.02f, -D), new Vector3(-W * 0.9f, 0.02f, D), new Vector3(W * 0.9f, 0.02f, D), new Vector3(W * 0.9f, 0.02f, -D));   // ground sheet
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 top = new Vector3(0, H, D), low = new Vector3(sx * W, 0.12f, D), tied = new Vector3(sx * W * 0.95f, 0.75f, D + 0.25f);
            mb.Tri(canvas, top, tied, low);
            // guy ropes from the eaves to stakes, front and back
            foreach (float z in new[] { -D * 0.8f, D * 0.8f })
            {
                Vector3 eave = new Vector3(sx * W, 0.5f, z), stake = new Vector3(sx * (W + 0.75f), 0.06f, z);
                mb.Rod(rope, eave, stake, 0.012f, 4);
                mb.Rod(wood, stake - Vector3.up * 0.05f, stake + Vector3.up * 0.14f, 0.025f, 5);
            }
        }
        // poles front and back, the ridge pole, front guy ropes
        mb.Rod(wood, new Vector3(0, 0, D + 0.02f), new Vector3(0, H + 0.25f, D + 0.02f), 0.045f);
        mb.Rod(wood, new Vector3(0, 0, -D - 0.02f), new Vector3(0, H + 0.12f, -D - 0.02f), 0.045f);
        mb.Rod(wood, new Vector3(0, H + 0.02f, -D - 0.1f), new Vector3(0, H + 0.02f, D + 0.1f), 0.04f);
        mb.Rod(rope, new Vector3(0, H + 0.15f, D), new Vector3(0, 0.06f, D + 1.3f), 0.012f, 4);
        mb.Rod(rope, new Vector3(0, H + 0.1f, -D), new Vector3(0, 0.06f, -D - 1.3f), 0.012f, 4);
        mb.Build(root.transform);
        Pennant(root.transform, new Vector3(0, H + 0.12f, D + 0.02f), stripe);
        return root;
    }

    static void Pennant(Transform parent, Vector3 at, Material m)
    {
        var p = new GameObject("Pennant"); p.transform.SetParent(parent, false); p.transform.localPosition = at;
        var mesh = new Mesh(); mesh.SetVertices(new List<Vector3> { new Vector3(0, 0.1f, 0), new Vector3(0, -0.1f, 0), new Vector3(0, 0, -0.55f) });
        mesh.SetTriangles(new[] { 0, 1, 2 }, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        p.AddComponent<MeshFilter>().sharedMesh = mesh; p.AddComponent<MeshRenderer>().sharedMaterial = m;
        p.AddComponent<AHPennant>();
    }

    // ---------- replacing the web game's pyramids ----------
    public static void Setup(AHGame g, Transform world)
    {
        if (world == null || g.data == null) return;
        var spots = new List<Vector3>(); var fires = new List<Vector3>();
        foreach (var o in Props())
        {
            string k = AHJson.S(o, "kind"); if (k != "tent" && k != "fire") continue;
            float x = (float)AHJson.N(o, "x") * AHDB.S, z = (float)AHJson.N(o, "y") * AHDB.S;
            var b = g.data.bounds; if (b == null || x < b.x0 - 1 || x > b.x1 + 1 || z < b.z0 - 1 || z > b.z1 + 1) continue;
            (k == "tent" ? spots : fires).Add(g.W(x, z));
        }
        if (spots.Count == 0) return;
        var rs = new List<Renderer>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; var bb = r.bounds;
            if (Mathf.Max(bb.size.x, bb.size.z) > 3.6f || bb.size.y > 3.6f) continue; rs.Add(r);
        }
        var root = new GameObject("Tents").transform; int n = 0;
        foreach (var s in spots)
        {
            float ground = float.PositiveInfinity; Vector3 c0 = s; bool any = false;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; Vector3 c = r.bounds.center - s; c.y = 0; if (c.magnitude > 1.5f) continue;
                ground = Mathf.Min(ground, r.bounds.min.y); if (!any) { c0 = r.bounds.center; any = true; }
                r.enabled = false;
            }
            Vector3 at = new Vector3(any ? c0.x : s.x, float.IsInfinity(ground) ? g.Resolve(s, 0.1f).y : ground, any ? c0.z : s.z);
            int seed = Mathf.RoundToInt(s.x * 13f + s.z * 7f);
            var t = (seed & 1) == 0 ? Pavilion(root, seed) : Ridge(root, seed);
            t.transform.position = at;
            // the door looks at the nearest campfire (or the middle of the camp)
            Vector3 look = at + Vector3.forward; float best = 30f;
            foreach (var f in fires) { float d = (f - at).magnitude; if (d < best) { best = d; look = f; } }
            if (best >= 30f) { Vector3 sum = Vector3.zero; foreach (var o2 in spots) sum += o2; look = sum / spots.Count; }
            Vector3 dir = look - at; dir.y = 0; if (dir.sqrMagnitude > 0.01f) t.transform.rotation = Quaternion.LookRotation(dir.normalized);
            AHModel.SetShadows(t);
            n++;
        }
        Debug.Log("Ashen Hollow: " + n + " camp tents rebuilt");
    }

    static List<object> props;
    public static List<object> Props()
    {
        if (props != null) return props;
        props = new List<object>();
        var ta = Resources.Load<TextAsset>("AH/Data/world"); if (ta == null) return props;
        Walk(AHJson.Parse(ta.text), props);
        return props;
    }
    static void Walk(object o, List<object> into)
    {
        var d = o as Dictionary<string, object>;
        if (d != null) { if (d.ContainsKey("kind") && d.ContainsKey("x")) into.Add(d); foreach (var v in d.Values) if (v is Dictionary<string, object> || v is List<object>) Walk(v, into); return; }
        var l = o as List<object>; if (l != null) foreach (var v in l) Walk(v, into);
    }
}

// a pennant fluttering in the wind
public class AHPennant : MonoBehaviour
{
    float ph;
    void Start() { ph = Random.value * 10f; }
    void Update() { transform.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * 3.1f + ph) * 18f + Mathf.Sin(Time.time * 7.3f + ph) * 6f, Mathf.Sin(Time.time * 4.7f + ph) * 5f); }
}
