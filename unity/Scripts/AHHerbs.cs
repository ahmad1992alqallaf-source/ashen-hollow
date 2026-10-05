// Ashen Hollow: better herbs. Each herb was a green blob with a few coloured balls on it. Now every herb spot is a
// small plant of its own kind, built here from curved leaves, stems and flowers:
//   sunpetal   - a leafy clump with golden daisies;            ashbloom  - grey-green leaves, pale flowers, ember hearts
//   frostbloom - blue-green leaves and glowing ice-crystal bells; emberthorn - dark red stems bristling with glowing thorns
//   mireroot   - olive leaves over pale lilac bulbs with tiny purple flowers
//   dragonfern - long arching fronds with red curled fiddleheads; reefmoss - a mossy mound sprouting pink coral
// Picking it takes the flowers (or bulbs, thorns, coral) and dulls the leaves until it grows back, as before.
using System.Collections.Generic;
using UnityEngine;

public static class AHHerbs
{
    class Look { public Color leaf, petal, centre; public string form; public bool glow; }
    static Look For(string t)
    {
        switch (t)
        {
            case "ashbloom": return new Look { form = "daisy", leaf = C(0x5a6050), petal = C(0xe0dcd4), centre = C(0xff7a2a), glow = true };
            case "frostbloom": return new Look { form = "crystal", leaf = C(0x4a8088), petal = C(0xb8ecff), centre = C(0xe8faff), glow = true };
            case "emberthorn": return new Look { form = "thorn", leaf = C(0x5a2418), petal = C(0xff5a1a), centre = C(0xffb040), glow = true };
            case "mireroot": return new Look { form = "bulb", leaf = C(0x5a6a2a), petal = C(0xd8c4e0), centre = C(0x8a4ab0) };
            case "dragonfern": return new Look { form = "fern", leaf = C(0x2a7a34), petal = C(0xb83a22), centre = C(0xb83a22) };
            case "reefmoss": return new Look { form = "coral", leaf = C(0x2a8a7a), petal = C(0xff7a90), centre = C(0xffb0c0) };
        }
        return new Look { form = "daisy", leaf = C(0x3f8a2a), petal = C(0xffd23a), centre = C(0x8a4a14) };   // sunpetal
    }
    static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

    // ---------- shapes ----------
    static Mesh leaf, cone, ball, stick;
    // a leaf 1 m long along +z, arching up then drooping, with a raised mid-rib (drawn from both sides)
    static Mesh Leaf()
    {
        if (leaf != null) return leaf;
        int n = 8; var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= n; i++)
        {
            float z = i / (float)n, w = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(z * 1.08f, 1f))), 0.8f) * 0.19f, y = 0.32f * z - 0.42f * z * z;
            v.Add(new Vector3(-w, y - 0.02f * w, z)); v.Add(new Vector3(0f, y + 0.015f, z)); v.Add(new Vector3(w, y - 0.02f * w, z));
        }
        for (int i = 0; i < n; i++) for (int k = 0; k < 2; k++) { int a = i * 3 + k, b = a + 3; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
        leaf = Make("Leaf", v, t); return leaf;
    }
    static Mesh Cone()
    {
        if (cone != null) return cone;
        var v = new List<Vector3>(); var t = new List<int>(); int s = 6;
        for (int i = 0; i < s; i++) { float a = i * Mathf.PI * 2 / s, b = (i + 1) * Mathf.PI * 2 / s; int k = v.Count; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); v.Add(new Vector3(0, 1, 0)); v.Add(new Vector3(Mathf.Cos(b) * 0.5f, 0, Mathf.Sin(b) * 0.5f)); t.AddRange(new[] { k, k + 1, k + 2 }); }
        cone = Make("Cone", v, t); return cone;
    }
    static Mesh Ball()
    {
        if (ball != null) return ball;
        var go = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); ball = go.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(go); return ball;
    }
    static Mesh Stick()
    {
        if (stick != null) return stick;
        var go = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Cylinder), PrimitiveType.Cylinder); stick = go.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(go); return stick;
    }
    static Mesh Make(string name, List<Vector3> v, List<int> t)
    {
        var m = new Mesh { name = name }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    // ---------- materials (shared per herb kind, so the meadow's herbs draw together) ----------
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, bool glow, bool twoSided)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", 0.25f); m.enableInstancing = true;
        if (twoSided) { m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; }
        if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * 0.9f); }
        mats[key] = m; return m;
    }

    class Parts
    {
        public readonly Dictionary<Material, List<CombineInstance>> by = new Dictionary<Material, List<CombineInstance>>();
        public void Add(Material m, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            List<CombineInstance> l; if (!by.TryGetValue(m, out l)) by[m] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, rot, scale) });
        }
        public Renderer Build(Transform parent, string name)
        {
            Renderer first = null;
            foreach (var kv in by)
            {
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = name }; mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key;
                if (first == null) first = r;
            }
            return first;
        }
    }

    // one plant: returns its root; 'body' is the leaves, 'flowers' what picking takes
    public static GameObject Build(string type, int seed, out Renderer body, out GameObject flowers, out Color leafCol)
    {
        var L = For(type); var rnd = new System.Random(seed); leafCol = L.leaf.linear;
        System.Func<float, float, float> R = (a, b) => a + (float)rnd.NextDouble() * (b - a);
        var root = new GameObject("Herb " + type);
        Material leafM = Mat(type + "_leaf", L.leaf, false, true), petalM = Mat(type + "_petal", L.petal, L.glow && L.form != "daisy", true), centreM = Mat(type + "_centre", L.centre, L.glow, false);
        var leaves = new Parts(); var fl = new Parts();

        bool fern = L.form == "fern", coral = L.form == "coral";
        if (coral) leaves.Add(leafM, Ball(), new Vector3(0, 0.04f, 0), Quaternion.identity, new Vector3(0.75f, 0.22f, 0.65f));
        int nl = fern ? 7 : coral ? 5 : 9;
        for (int i = 0; i < nl; i++)
        {
            float yaw = i * 360f / nl + R(-15f, 15f), pitch = fern ? R(-55f, -35f) : R(-50f, -20f), len = fern ? R(0.55f, 0.8f) : R(0.3f, 0.45f);
            leaves.Add(leafM, Leaf(), new Vector3(0, 0.02f, 0), Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, 0), new Vector3(fern ? 0.55f : 1f, 1f, 1f) * len);
            if (fern) for (int k = 1; k < 6; k++)   // little leaflets along each frond
                {
                    float z = len * k / 6.5f; Quaternion fr = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(pitch, 0, 0);
                    Vector3 at = fr * new Vector3(0, (0.32f * (k / 6.5f) - 0.42f * (k / 6.5f) * (k / 6.5f)) * len, z);
                    foreach (int sx in new[] { -1, 1 }) leaves.Add(leafM, Leaf(), at + Vector3.up * 0.02f, fr * Quaternion.Euler(0, sx * 70f, 0), Vector3.one * len * 0.28f * (1f - k / 7f));
                }
        }
        switch (L.form)
        {
            case "daisy":
                for (int h = 0; h < 4; h++)
                {
                    float a = h * 1.7f + R(0, 0.6f), d = R(0.05f, 0.2f), ht = R(0.28f, 0.48f);
                    Vector3 top = new Vector3(Mathf.Cos(a) * d, ht, Mathf.Sin(a) * d);
                    leaves.Add(leafM, Stick(), top * 0.5f, Quaternion.FromToRotation(Vector3.up, top), new Vector3(0.018f, ht * 0.5f, 0.018f));
                    Quaternion face = Quaternion.FromToRotation(Vector3.up, (top.normalized + Vector3.up * 2f).normalized);
                    fl.Add(centreM, Ball(), top, face, new Vector3(0.07f, 0.035f, 0.07f));
                    for (int p = 0; p < 10; p++) fl.Add(petalM, Leaf(), top, face * Quaternion.Euler(0, p * 36f, 0) * Quaternion.Euler(-8f, 0, 0), new Vector3(0.11f, 0.11f, 0.11f));
                }
                break;
            case "crystal":
                for (int h = 0; h < 5; h++)
                {
                    float a = h * 1.3f + R(0, 0.5f), d = R(0.04f, 0.18f), ht = R(0.08f, 0.2f);
                    Vector3 at = new Vector3(Mathf.Cos(a) * d, ht, Mathf.Sin(a) * d);
                    for (int p = 0; p < 4; p++) fl.Add(petalM, Cone(), at, Quaternion.Euler(R(-25f, 25f), p * 90f + R(0, 40f), R(-25f, 25f)), new Vector3(0.06f, R(0.18f, 0.32f), 0.06f));
                    fl.Add(centreM, Ball(), at, Quaternion.identity, Vector3.one * 0.05f);
                }
                break;
            case "thorn":
                for (int h = 0; h < 5; h++)
                {
                    float a = h * 1.25f + R(0, 0.5f), ht = R(0.35f, 0.6f); Vector3 top = new Vector3(Mathf.Cos(a) * 0.14f, ht, Mathf.Sin(a) * 0.14f);
                    leaves.Add(leafM, Stick(), top * 0.5f, Quaternion.FromToRotation(Vector3.up, top), new Vector3(0.03f, ht * 0.5f, 0.03f));
                    for (int k = 1; k <= 4; k++)
                    {
                        Vector3 at = top * (k / 4.5f); float yaw = R(0, 360f);
                        fl.Add(petalM, Cone(), at, Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(0, 0, -70f), new Vector3(0.035f, 0.1f, 0.035f));
                    }
                    fl.Add(centreM, Cone(), top, Quaternion.identity, new Vector3(0.06f, 0.12f, 0.06f));
                }
                break;
            case "bulb":
                for (int h = 0; h < 3; h++)
                {
                    float a = h * 2.1f + R(0, 0.5f); Vector3 at = new Vector3(Mathf.Cos(a) * 0.13f, 0.06f, Mathf.Sin(a) * 0.13f);
                    fl.Add(petalM, Ball(), at, Quaternion.identity, new Vector3(0.16f, 0.2f, 0.16f));
                    fl.Add(petalM, Cone(), at + Vector3.up * 0.08f, Quaternion.identity, new Vector3(0.05f, 0.1f, 0.05f));
                    Vector3 top = at + new Vector3(0, R(0.25f, 0.38f), 0);
                    leaves.Add(leafM, Stick(), (at + top) * 0.5f, Quaternion.identity, new Vector3(0.014f, (top.y - at.y) * 0.5f, 0.014f));
                    for (int p = 0; p < 5; p++) fl.Add(centreM, Leaf(), top, Quaternion.Euler(0, p * 72f, 0) * Quaternion.Euler(-30f, 0, 0), Vector3.one * 0.07f);
                }
                break;
            case "fern":
                for (int h = 0; h < 3; h++)
                {
                    float a = h * 2.1f + R(0, 0.6f); Vector3 at = new Vector3(Mathf.Cos(a) * 0.08f, 0, Mathf.Sin(a) * 0.08f);
                    Vector3 top = at + new Vector3(0, R(0.3f, 0.42f), 0);
                    fl.Add(centreM, Stick(), (at + top) * 0.5f, Quaternion.identity, new Vector3(0.025f, (top.y - at.y) * 0.5f, 0.025f));
                    for (int k = 0; k < 6; k++)   // the curl at the top
                    {
                        float c = k * 0.9f, r = 0.06f * (1f - k / 7f);
                        fl.Add(centreM, Ball(), top + new Vector3(Mathf.Sin(c) * r, Mathf.Cos(c) * r, 0), Quaternion.identity, Vector3.one * (0.045f - k * 0.004f));
                    }
                }
                break;
            case "coral":
                for (int h = 0; h < 6; h++)
                {
                    float a = h * 1.05f + R(0, 0.4f); Vector3 at = new Vector3(Mathf.Cos(a) * R(0.05f, 0.25f), 0.08f, Mathf.Sin(a) * R(0.05f, 0.2f));
                    Vector3 dir = new Vector3(R(-0.4f, 0.4f), 1f, R(-0.4f, 0.4f)).normalized; float len = R(0.18f, 0.32f);
                    fl.Add(petalM, Stick(), at + dir * len * 0.5f, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(0.035f, len * 0.5f, 0.035f));
                    fl.Add(centreM, Ball(), at + dir * len, Quaternion.identity, Vector3.one * 0.05f);
                    Vector3 mid = at + dir * len * 0.55f, d2 = (dir + new Vector3(R(-0.8f, 0.8f), 0.2f, R(-0.8f, 0.8f))).normalized;
                    fl.Add(petalM, Stick(), mid + d2 * len * 0.25f, Quaternion.FromToRotation(Vector3.up, d2), new Vector3(0.028f, len * 0.25f, 0.028f));
                    fl.Add(centreM, Ball(), mid + d2 * len * 0.5f, Quaternion.identity, Vector3.one * 0.04f);
                }
                break;
        }
        body = leaves.Build(root.transform, "Leaves");
        var fgo = new GameObject("Flowers"); fgo.transform.SetParent(root.transform, false); fl.Build(fgo.transform, "Bloom"); flowers = fgo;
        root.transform.rotation = Quaternion.Euler(0, R(0, 360f), 0);
        return root;
    }

    // every herb spot of the area: the old blob hidden, the new plant in its place
    public static void Setup(AHGame g)
    {
        var root = new GameObject("Herbs").transform; int n = 0;
        foreach (var s in AHGather.Spots)
        {
            if (s.kind != "herb") continue;
            float ground = s.pos.y;
            Transform old = s.body != null ? s.body.transform.parent : s.fx != null ? s.fx.transform.parent : null;
            if (old != null)
            {
                var rs = old.GetComponentsInChildren<Renderer>(true);
                if (rs.Length > 0) { Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); ground = b.min.y; }
                foreach (var r in rs) r.enabled = false;
            }
            Renderer body; GameObject flowers; Color lc;
            var plant = Build(s.type, (int)(s.pos.x * 73 + s.pos.z * 31), out body, out flowers, out lc);
            plant.transform.SetParent(root, true); plant.transform.position = new Vector3(s.pos.x, ground, s.pos.z);
            plant.transform.localScale = Vector3.one * 1.15f;
            AHModel.SetShadows(plant);
            s.body = body; s.bodyCol = lc; s.fx = flowers;
            n++;
        }
        if (n == 0) Object.Destroy(root.gameObject);
        else Debug.Log("Ashen Hollow: " + n + " herbs planted");
    }
}
