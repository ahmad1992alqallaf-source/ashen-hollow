// Ashen Hollow: the Fossil Lands' bones. The rib cages were a flat box with half-hoops standing on it, and the great
// skull a ball with two black balls for eyes and a stick of a horn. Now each rib cage is the skeleton of some huge beast
// half sunk in the sand: a knobbly spine with spines on its vertebrae, sagging into the ground at both ends, and pairs of
// curved ribs tapering down into the sand (a few broken short). The skull is a long horned skull lying on its jaw: a
// domed cranium with brow ridges, deep dark eye sockets, a tapering snout with nostril holes and a row of teeth, two
// great horns sweeping back, the lower jaw half buried. Made for Ashen Hollow, in code.
using System.Collections.Generic;
using UnityEngine;

public static class AHFossils
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.25f)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "fossil_" + key };
        m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f); m.enableInstancing = true;
        mats[key] = m; return m;
    }

    // ---------- a little mesh builder: tubes along curves and squashed balls ----------
    class MB
    {
        public readonly List<Vector3> v = new List<Vector3>(); public readonly List<int> t = new List<int>();
        public void Tube(Vector3[] pts, float r0, float r1, int seg = 7, float flat = 1f)
        {
            int n = pts.Length, b = v.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 d = (pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                Vector3 side = Vector3.Cross(d, Mathf.Abs(d.y) > 0.9f ? Vector3.right : Vector3.up).normalized, up = Vector3.Cross(side, d);
                float r = Mathf.Lerp(r0, r1, i / (float)(n - 1));
                for (int s = 0; s <= seg; s++) { float a = s * Mathf.PI * 2f / seg; v.Add(pts[i] + (side * Mathf.Cos(a) * flat + up * Mathf.Sin(a)) * r); }
            }
            for (int i = 0; i < n - 1; i++) for (int s = 0; s < seg; s++) { int p = b + i * (seg + 1) + s, q = p + seg + 1; t.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
            // close the thin end with a point
            int tip = v.Count; Vector3 dd = (pts[n - 1] - pts[n - 2]).normalized; v.Add(pts[n - 1] + dd * r1);
            int last = b + (n - 1) * (seg + 1); for (int s = 0; s < seg; s++) t.AddRange(new[] { last + s, tip, last + s + 1 });
        }
        public void Ball(Vector3 c, Vector3 r, Quaternion rot, int seg = 10, int rings = 7, System.Func<Vector3, Vector3> warp = null)
        {
            int b = v.Count;
            for (int i = 0; i <= rings; i++)
            {
                float la = Mathf.PI * (i / (float)rings - 0.5f);
                for (int s = 0; s <= seg; s++)
                {
                    float lo = s * Mathf.PI * 2f / seg; var p = new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo));
                    if (warp != null) p = warp(p);
                    v.Add(c + rot * Vector3.Scale(p, r));
                }
            }
            for (int i = 0; i < rings; i++) for (int s = 0; s < seg; s++) { int p = b + i * (seg + 1) + s, q = p + seg + 1; t.AddRange(new[] { p, q, q + 1, p, q + 1, p + 1 }); }
        }
        public Mesh Make(string name)
        {
            var m = new Mesh { name = name }; m.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }
    static Vector3[] Bez(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int n)
    {
        var p = new Vector3[n];
        for (int i = 0; i < n; i++) { float u = i / (float)(n - 1), w = 1 - u; p[i] = w * w * w * a + 3 * w * w * u * b + 3 * w * u * u * c + u * u * u * d; }
        return p;
    }
    static GameObject Part(Transform parent, string name, Mesh mesh, Material m)
    {
        // bone and wood wear a weathered grain (cracks and pitting) instead of a smooth plastic finish
        if (m != null && (m.name == "fossil_bone" || m.name == "fossil_nestwood") && mesh != null)
        {
            var gm = AHBevel.GrainMat(m.name, m.GetColor("_BaseColor").gamma, m.name == "fossil_bone" ? 1.1f : 1.6f);
            if (gm != null)
            {
                if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)) { var cs = new Color[mesh.vertexCount]; for (int i = 0; i < cs.Length; i++) cs[i] = Color.white; mesh.colors = cs; }
                m = gm;
            }
        }
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m; return go;
    }

    public static void Setup(Transform world)
    {
        if (world == null) return;
        if (AHGame.AreaId == "isle") { Nest(world); return; }
        if (AHGame.AreaId != "fossil" && AHGame.AreaId != "tide" && AHGame.AreaId != "sands") return;
        Transform root = null; int nr = 0, ns = 0;
        foreach (Transform grp in world)
        {
            if (!grp.name.StartsWith("obj")) continue;
            var rs = grp.GetComponentsInChildren<Renderer>(false); if (rs.Length < 5 || rs.Length > 14) continue;
            int arcs = 0, dark = 0, bone = 0; Renderer flat = null, big = null; var arcC = new List<Vector3>(); var eyes = new List<Vector3>(); bool off = false;
            foreach (var r in rs)
            {
                if (!r.enabled) { off = true; break; }
                Color c; if (!Col(r, out c)) continue; var b = r.bounds;
                bool isBone = c.r > 0.7f && c.g > 0.65f && c.b > 0.5f && c.r - c.b < 0.3f;
                if (isBone) bone++;
                if (c.r < 0.2f && c.g < 0.2f) { dark++; eyes.Add(b.center); }
                var mf = r.GetComponent<MeshFilter>(); long tri = mf != null && mf.sharedMesh != null ? mf.sharedMesh.GetIndexCount(0) / 3 : 0;
                if (isBone && tri >= 100 && tri <= 200 && b.size.y > 0.6f && b.size.y < 3.5f) { arcs++; arcC.Add(b.center); }
                if (isBone && b.size.y <= 0.6f && Mathf.Max(b.size.x, b.size.z) > 1f) flat = r;
                if (big == null || b.size.x * b.size.y * b.size.z > big.bounds.size.x * big.bounds.size.y * big.bounds.size.z) big = r;
            }
            if (off) continue;
            if (root == null) root = new GameObject("Fossils").transform;
            int seed = Mathf.Abs(Mathf.RoundToInt(grp.position.x * 7 + grp.position.z * 13 + rs[0].bounds.center.x * 3));
            if (arcs >= 4 && dark == 0 && bone >= arcs)
            {
                // the line of the spine: from the first hoop to the last
                Vector3 a = arcC[0], z = arcC[arcC.Count - 1]; Vector3 dir = z - a; dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) continue;
                Bounds all = rs[0].bounds; foreach (var r in rs) all.Encapsulate(r.bounds);
                float L = dir.magnitude * 1.6f + 1.2f, H = 0f, W = 0f;
                foreach (var r in rs) { var b = r.bounds; if (b.size.y > 0.6f) { H = Mathf.Max(H, b.size.y); W = Mathf.Max(W, Vector3.ProjectOnPlane(b.size, Vector3.up).magnitude * 0.5f); } }
                var go = new GameObject("Ribcage").transform; go.SetParent(root, false);
                go.position = new Vector3((a.x + z.x) * 0.5f, all.min.y, (a.z + z.z) * 0.5f); go.rotation = Quaternion.LookRotation(dir.normalized);
                Ribcage(go, L, H * 1.15f, Mathf.Clamp(W, H * 0.7f, H * 1.6f), arcs, seed);
                nr++;
            }
            else if (dark >= 2 && bone >= 3 && big != null)
            {
                Vector3 eye = Vector3.zero; foreach (var e in eyes) eye += e; eye /= eyes.Count;
                var bb = big.bounds; Vector3 face = eye - bb.center; face.y = 0f; if (face.sqrMagnitude < 0.01f) face = Vector3.forward;
                var go = new GameObject("Great skull").transform; go.SetParent(root, false);
                go.position = new Vector3(bb.center.x, bb.min.y - 0.3f, bb.center.z); go.rotation = Quaternion.LookRotation(face.normalized);
                Skull(go, Mathf.Max(bb.size.x, bb.size.z) * 1.25f, seed);
                ns++;
            }
            else continue;
            foreach (var r in rs) r.enabled = false;
        }
        if (root != null) { foreach (var r in root.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; } }
        if (nr + ns > 0) Debug.Log("Ashen Hollow: " + nr + " rib cages and " + ns + " skulls rebuilt");
    }
    // Dragonscale Isle's dragon nest: a ring of flat white sticks round the eggs becomes a deep nest of woven branches
    // and bones on a bed of straw (the eggs stay)
    static void Nest(Transform world)
    {
        var sticks = new List<Renderer>(); Vector3 c = Vector3.zero;
        foreach (var r in world.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.name.StartsWith("AH_")) continue; Color col; if (!Col(r, out col)) continue; var b = r.bounds;
            if (col.r > 0.88f && col.g > 0.84f && b.size.y < 0.3f && Mathf.Max(b.size.x, b.size.z) > 1.4f && Mathf.Max(b.size.x, b.size.z) < 2.6f) { sticks.Add(r); c += b.center; }
        }
        if (sticks.Count < 5) return;
        c /= sticks.Count; float rad = 0f; foreach (var r in sticks) { var d = r.bounds.center - c; d.y = 0; rad = Mathf.Max(rad, d.magnitude); }
        foreach (var r in sticks) r.enabled = false;
        rad += 0.6f; var rnd = new System.Random(7); System.Func<float, float, float> R = (a, bb) => a + (float)rnd.NextDouble() * (bb - a);
        var go = new GameObject("Dragon nest").transform; go.position = new Vector3(c.x, sticks[0].bounds.min.y - 0.05f, c.z);
        var wood = new MB(); var bone = new MB(); var straw = new MB();
        // woven branches: arcs round the rim, three layers, leaning in
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < 14; i++)
            {
                float a0 = i * Mathf.PI * 2f / 14f + layer * 0.2f, a1 = a0 + R(0.6f, 1.0f), y = 0.15f + layer * 0.28f, rr = rad * (1f - layer * 0.06f);
                var pts = new Vector3[6];
                for (int k = 0; k < 6; k++) { float a = Mathf.Lerp(a0, a1, k / 5f), w = rr + Mathf.Sin(k * 1.3f + i) * 0.12f; pts[k] = new Vector3(Mathf.Cos(a) * w, y + Mathf.Sin(k + i * 0.7f) * 0.06f, Mathf.Sin(a) * w); }
                wood.Tube(pts, R(0.08f, 0.13f), 0.05f, 6);
            }
        // a few sticks poking out and some old bones in the weave
        for (int i = 0; i < 10; i++)
        {
            float a = R(0f, 6.28f), y = R(0.2f, 0.8f); var p0 = new Vector3(Mathf.Cos(a) * rad * 0.9f, y, Mathf.Sin(a) * rad * 0.9f);
            var dir = new Vector3(Mathf.Cos(a + R(-0.6f, 0.6f)), R(0.1f, 0.6f), Mathf.Sin(a + R(-0.6f, 0.6f))).normalized;
            if (i < 7) wood.Tube(new[] { p0, p0 + dir * 0.6f, p0 + dir * 1.2f }, 0.06f, 0.02f, 5);
            else { bone.Tube(new[] { p0, p0 + dir * 0.5f, p0 + dir * 1.0f }, 0.08f, 0.07f, 6); bone.Ball(p0 + dir * 1.05f, Vector3.one * 0.12f, Quaternion.identity, 6, 4); }
        }
        straw.Ball(new Vector3(0, 0.05f, 0), new Vector3(rad * 0.95f, 0.2f, rad * 0.95f), Quaternion.identity, 16, 6);
        Part(go, "branches", wood.Make("nest"), Mat("nestwood", new Color(0.36f, 0.25f, 0.16f), 0.1f));
        Part(go, "bones", bone.Make("nestbones"), Mat("bone", new Color(0.92f, 0.88f, 0.78f), 0.3f));
        Part(go, "straw", straw.Make("neststraw"), Mat("straw", new Color(0.7f, 0.58f, 0.32f), 0.05f));
        foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; r.receiveShadows = true; }
        Debug.Log("Ashen Hollow: the dragon nest rebuilt");
    }
    static bool Col(Renderer r, out Color c)
    {
        c = Color.white; var m = r.sharedMaterial; if (m == null) return false;
        if (m.HasProperty("baseColorFactor")) c = m.GetColor("baseColorFactor"); else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor"); else return false;
        return true;
    }

    // along +z, the ground at y = 0; L long, the spine arching H high, the ribs reaching W out to each side
    static void Ribcage(Transform go, float L, float H, float W, int pairs, int seed)
    {
        var rnd = new System.Random(seed); System.Func<float, float, float> R = (a, b) => a + (float)rnd.NextDouble() * (b - a);
        var mb = new MB(); var dark = new MB();
        // the spine: high in the middle, sinking into the sand at both ends
        int nv = Mathf.Max(10, Mathf.RoundToInt(L / 0.42f)); var spine = new Vector3[nv];
        for (int i = 0; i < nv; i++)
        {
            float u = i / (float)(nv - 1), y = H * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.1f - 0.05f)), 0.6f) - 0.25f;
            spine[i] = new Vector3(Mathf.Sin(u * 5f + seed) * 0.12f, y, (u - 0.5f) * L);
        }
        float vr = Mathf.Clamp(H * 0.075f, 0.1f, 0.3f);
        for (int i = 0; i < nv; i++)
        {
            if (spine[i].y < -vr) continue;
            Vector3 along = i < nv - 1 ? spine[i + 1] - spine[i] : spine[i] - spine[i - 1];
            var q = along.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(along) : Quaternion.identity;   // a tiny ribcage: no direction to face
            mb.Ball(spine[i], new Vector3(vr * 1.1f, vr * 0.95f, vr * 0.8f), q, 8, 5);
            // the spine on top of each vertebra, leaning back
            float sh = vr * R(1.4f, 2.4f) * Mathf.Sin(Mathf.PI * i / (nv - 1));
            if (sh > vr * 0.6f) mb.Tube(new[] { spine[i] + Vector3.up * vr * 0.6f, spine[i] + new Vector3(0, vr * 0.6f + sh * 0.6f, -sh * 0.15f), spine[i] + new Vector3(0, vr * 0.6f + sh, -sh * 0.35f) }, vr * 0.35f, vr * 0.1f, 5);
        }
        // the ribs, in pairs along the middle of the spine
        int np = Mathf.Clamp(pairs + 2, 5, 10);
        for (int k = 0; k < np; k++)
        {
            float u = 0.2f + 0.6f * k / (np - 1f); int i = Mathf.RoundToInt(u * (nv - 1)); var top = spine[i];
            if (top.y < H * 0.3f) continue;
            float size = Mathf.Sin(Mathf.PI * (0.15f + 0.7f * k / (np - 1f)));   // longest in the middle
            for (int s = -1; s <= 1; s += 2)
            {
                float w = W * (0.75f + 0.3f * size) * R(0.92f, 1.06f), back = -R(0.2f, 0.6f);
                var pts = Bez(top, top + new Vector3(s * w * 0.75f, H * 0.18f, back * 0.2f), new Vector3(s * w * 1.08f, top.y * 0.45f, top.z + back * 0.7f), new Vector3(s * w * 0.8f, -0.35f, top.z + back), 9);
                bool broken = rnd.NextDouble() < 0.2;
                if (broken) { int keep = 4 + rnd.Next(3); System.Array.Resize(ref pts, keep); }
                mb.Tube(pts, vr * 0.55f, vr * (broken ? 0.35f : 0.18f), 6, 0.7f);
            }
        }
        // a hip bone at the back end and a scatter of loose bones
        int hi = Mathf.RoundToInt(0.86f * (nv - 1)); if (spine[hi].y > 0f) mb.Ball(spine[hi] + Vector3.down * vr * 0.3f, new Vector3(W * 0.45f, vr * 1.2f, vr * 2.4f), Quaternion.identity, 10, 6);
        for (int j = 0; j < 3; j++)
        {
            var c = new Vector3(R(-W * 1.6f, W * 1.6f), 0.05f, R(-L * 0.5f, L * 0.5f)); float l = R(0.6f, 1.4f) * H * 0.4f, ang = R(0f, 6.28f);
            Vector3 d = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * l * 0.5f;
            mb.Tube(new[] { c - d, c, c + d }, vr * 0.4f, vr * 0.35f, 5);
            mb.Ball(c - d, Vector3.one * vr * 0.5f, Quaternion.identity, 6, 4); mb.Ball(c + d, Vector3.one * vr * 0.45f, Quaternion.identity, 6, 4);
        }
        // the sand heaped against the bones where they sink in
        foreach (var e in new[] { 0, nv - 1 }) dark.Ball(new Vector3(spine[e].x, -0.1f, spine[e].z * 0.92f), new Vector3(vr * 4f, vr * 1.4f, vr * 5f), Quaternion.identity, 10, 5);
        Part(go, "bones", mb.Make("ribcage"), Mat("bone", new Color(0.92f, 0.88f, 0.78f), 0.3f));
    }

    // along +z (the snout forward), lying on the ground; S long
    static void Skull(Transform go, float S, int seed)
    {
        var mb = new MB(); var hole = new MB(); var tooth = new MB();
        float h = S * 0.36f;
        // the cranium: a dome, broad at the back, with a ridge over the eyes
        mb.Ball(new Vector3(0, h * 0.95f, -S * 0.12f), new Vector3(S * 0.27f, h * 0.7f, S * 0.3f), Quaternion.identity, 16, 10,
                p => new Vector3(p.x * (1f + 0.15f * Mathf.Max(0f, -p.z)), p.y < 0 ? p.y * 0.6f : p.y, p.z));
        foreach (int s in new[] { -1, 1 })
        {
            mb.Ball(new Vector3(s * S * 0.17f, h * 1.25f, S * 0.08f), new Vector3(S * 0.1f, h * 0.17f, S * 0.12f), Quaternion.Euler(0, s * 20f, s * -12f), 10, 6);   // brow ridge
            hole.Ball(new Vector3(s * S * 0.2f, h * 1.02f, S * 0.12f), new Vector3(S * 0.06f, h * 0.17f, S * 0.08f), Quaternion.Euler(0, s * 25f, 0), 10, 6);         // eye socket
            hole.Ball(new Vector3(s * S * 0.045f, h * 0.88f, S * 0.48f), new Vector3(S * 0.025f, h * 0.06f, S * 0.04f), Quaternion.identity, 8, 5);                  // nostril
            // the horns: up and out from behind the brow, sweeping back and curling forward at the tips
            var hp = Bez(new Vector3(s * S * 0.2f, h * 1.35f, -S * 0.05f), new Vector3(s * S * 0.42f, h * 2.0f, -S * 0.15f), new Vector3(s * S * 0.55f, h * 2.6f, -S * 0.5f), new Vector3(s * S * 0.48f, h * 2.85f, -S * 0.32f), 12);
            mb.Tube(hp, S * 0.075f, S * 0.012f, 9);
            // cheek bone and the lower jaw, half sunk in the sand
            mb.Tube(new[] { new Vector3(s * S * 0.22f, h * 0.65f, -S * 0.15f), new Vector3(s * S * 0.2f, h * 0.6f, S * 0.12f), new Vector3(s * S * 0.11f, h * 0.55f, S * 0.42f) }, S * 0.06f, S * 0.04f, 8);
            mb.Tube(new[] { new Vector3(s * S * 0.2f, h * 0.15f, -S * 0.2f), new Vector3(s * S * 0.15f, h * 0.2f, S * 0.2f), new Vector3(s * S * 0.05f, h * 0.2f, S * 0.55f) }, S * 0.07f, S * 0.045f, 8);
        }
        // the snout, tapering forward and dipping a little
        mb.Tube(Bez(new Vector3(0, h * 0.95f, S * 0.05f), new Vector3(0, h * 1.0f, S * 0.25f), new Vector3(0, h * 0.85f, S * 0.45f), new Vector3(0, h * 0.62f, S * 0.6f), 10), S * 0.16f, S * 0.07f, 12, 1.35f);
        // teeth along the upper jaw
        for (int i = 0; i < 7; i++)
            foreach (int s in new[] { -1, 1 })
            {
                float z = S * (0.18f + i * 0.055f), x = s * S * (0.13f - i * 0.012f), y = h * (0.68f - i * 0.012f);
                tooth.Tube(new[] { new Vector3(x, y, z), new Vector3(x, y - h * 0.12f, z + S * 0.005f), new Vector3(x * 0.98f, y - h * 0.22f, z + S * 0.012f) }, S * 0.02f, S * 0.003f, 5);
            }
        // sand drifted round its base
        var sand = new MB(); sand.Ball(new Vector3(0, 0, 0), new Vector3(S * 0.42f, h * 0.35f, S * 0.6f), Quaternion.identity, 14, 6);
        Part(go, "skull", mb.Make("skull"), Mat("bone", new Color(0.92f, 0.88f, 0.78f), 0.3f));
        Part(go, "sockets", hole.Make("sockets"), Mat("socket", new Color(0.08f, 0.06f, 0.05f), 0.05f));
        Part(go, "teeth", tooth.Make("teeth"), Mat("tooth", new Color(0.93f, 0.9f, 0.8f), 0.4f));
    }
}
