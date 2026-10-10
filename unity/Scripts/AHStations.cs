// Ashen Hollow: better crafting stations. The campfire was a cone on a ring of lumps, the furnace a grey box with an
// orange square, the anvil a block on a block. Now:
//   campfire - a ring of stones round a bed of embers, logs leaning together, and live flames that lick and flicker,
//              with a glowing core, a warm breathing light and sparks drifting up (your own campfires look the same);
//   furnace  - laid up course by course from rough stone bricks, an arched mouth glowing with the fire inside, and a
//              brick chimney;
//   anvil    - a proper horned iron anvil on a sawn tree stump, with a hammer resting on it.
// They stand where the old ones stood; the work you do at them is unchanged.
using System.Collections.Generic;
using UnityEngine;

public static class AHStations
{
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Material Mat(string key, Color c, float smooth = 0.2f, float metal = 0f, float glow = 0f, bool twoSided = false)
    {
        Material m; if (mats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = key }; m.SetColor("_BaseColor", c.linear); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal); m.enableInstancing = true;
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c.linear * glow); }
        if (twoSided) { m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; }
        mats[key] = m; return m;
    }
    static Color C(int hex) { return new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f); }

    static Mesh cube, cyl, sphere, flame;
    static Mesh Prim(PrimitiveType t) { var go = AHLowPoly.Fix(GameObject.CreatePrimitive(t), t); var m = go.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(go); return m; }
    static Mesh Cube { get { if (cube == null) cube = Prim(PrimitiveType.Cube); return cube; } }
    static Mesh Cyl { get { if (cyl == null) cyl = Prim(PrimitiveType.Cylinder); return cyl; } }
    static Mesh Sph { get { if (sphere == null) sphere = Prim(PrimitiveType.Sphere); return sphere; } }
    // a flame tongue: a lathe, fat at the bottom and drawn to a point, 1 m tall
    static Mesh Flame
    {
        get
        {
            if (flame != null) return flame;
            int seg = 8, rings = 8; var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= rings; i++)
            {
                float y = i / (float)rings, r = 0.5f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, y * 0.62f + 0.38f))), 1.2f) * (1f - y * 0.85f);
                for (int k = 0; k <= seg; k++) { float a = k * Mathf.PI * 2 / seg; v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); }
            }
            for (int i = 0; i < rings; i++) for (int k = 0; k < seg; k++) { int a = i * (seg + 1) + k, b = a + seg + 1; t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            flame = new Mesh { name = "Flame" }; flame.SetVertices(v); flame.SetTriangles(t, 0); flame.RecalculateNormals(); flame.RecalculateBounds();
            return flame;
        }
    }

    class Parts
    {
        readonly Dictionary<Material, List<CombineInstance>> by = new Dictionary<Material, List<CombineInstance>>();
        public void Add(Material m, Mesh mesh, Vector3 p, Quaternion r, Vector3 s)
        {
            if (mesh == Cube) { mesh = AHBevel.Box(s); s = Vector3.one; }   // no bare boxes: chamfered edges
            List<CombineInstance> l; if (!by.TryGetValue(m, out l)) by[m] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(p, r, s) });
        }
        public void Build(Transform parent)
        {
            foreach (var kv in by)
            {
                var go = new GameObject(kv.Key.name); go.transform.SetParent(parent, false);
                var mesh = new Mesh { name = "Station", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; mesh.CombineMeshes(kv.Value.ToArray(), true, true); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
            }
        }
    }

    // live flames: tongues that stretch, sway and flicker
    static Material sparkMat;
    public static void Flames(Transform parent, Vector3 at, float size, int n, System.Random rnd)
    {
        var outer = Mat("flame_outer", C(0xff6a1a), 0f, 0f, 2.2f, true); var inner = Mat("flame_inner", C(0xffd25a), 0f, 0f, 2.6f, true);
        var f = new GameObject("Flames"); f.transform.SetParent(parent, false); f.transform.localPosition = at;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n + (float)rnd.NextDouble(), d = i == 0 ? 0f : size * 0.16f;
            foreach (bool core in new[] { false, true })
            {
                var t = new GameObject(core ? "Core" : "Tongue"); t.transform.SetParent(f.transform, false);
                t.transform.localPosition = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
                float h = size * (i == 0 ? 0.95f : 0.55f + (float)rnd.NextDouble() * 0.3f) * (core ? 0.55f : 1f), w = size * (i == 0 ? 0.42f : 0.3f) * (core ? 0.55f : 1f);
                t.transform.localScale = new Vector3(w, h, w);
                t.AddComponent<MeshFilter>().sharedMesh = Flame; var r = t.AddComponent<MeshRenderer>(); r.sharedMaterial = core ? inner : outer;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var fl = t.AddComponent<AHFlameLick>(); fl.baseScale = t.transform.localScale; fl.phase = (float)rnd.NextDouble() * 10f;
            }
        }
        // a fire big enough to sit by also throws soft glowing flame-licks and sparks up into the air
        if (size >= 0.5f)
        {
            var sp = new GameObject("Sparks"); sp.transform.SetParent(f.transform, false); sp.transform.localPosition = Vector3.up * size * 0.25f;
            var ps = sp.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * size, 1.4f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f); main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(1f, 0.85f, 0.35f));
            var em = ps.emission; em.rateOverTime = 6f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = size * 0.2f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true; var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            if (sparkMat == null) sparkMat = AHGame.LoadMat("AH/Materials/Spark", "AshenHollow/Spark");
            var pr = sp.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = sparkMat != null ? sparkMat : AHFx.Mat; ps.Play();
        }
        var lg = new GameObject("Firelight"); lg.transform.SetParent(f.transform, false); lg.transform.localPosition = Vector3.up * size * 0.8f;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.6f, 0.28f); li.range = 7f * Mathf.Max(0.6f, size); li.intensity = 2f; li.shadows = LightShadows.None;
        lg.AddComponent<AHTorchFlicker>().col = new Color(1f, 0.6f, 0.25f);
    }

    // ---------- campfire ----------
    public static GameObject Campfire(Transform parent, float k, int seed)
    {
        var rnd = new System.Random(seed); var root = new GameObject("Campfire"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material stoneA = Mat("stone_a", C(0x7d7a74)), stoneB = Mat("stone_b", C(0x5f5c58)), bark = Mat("bark", C(0x5a3a22)), cut = Mat("log_end", C(0xc8a070)), ash = Mat("ash", C(0x2a2422)), ember = Mat("ember", C(0xff4a10), 0f, 0f, 1.8f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * Mathf.PI * 2 / 9 + (float)rnd.NextDouble() * 0.2f, r = 0.52f * k;
            P.Add(i % 2 == 0 ? stoneA : stoneB, Sph, new Vector3(Mathf.Cos(a) * r, 0.06f * k, Mathf.Sin(a) * r), Quaternion.Euler((float)rnd.NextDouble() * 40, (float)rnd.NextDouble() * 360, 0), new Vector3(0.26f, 0.18f, 0.22f) * k * (0.85f + (float)rnd.NextDouble() * 0.3f));
        }
        P.Add(ash, Cyl, new Vector3(0, 0.01f * k, 0), Quaternion.identity, new Vector3(0.78f, 0.012f, 0.78f) * k);
        for (int i = 0; i < 6; i++) P.Add(ember, Sph, new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.4f * k, 0.03f * k, ((float)rnd.NextDouble() - 0.5f) * 0.4f * k), Quaternion.identity, new Vector3(0.1f, 0.05f, 0.08f) * k);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 20f; Quaternion q = Quaternion.Euler(0, a, 0) * Quaternion.Euler(35f, 0, 0);
            Vector3 c = Quaternion.Euler(0, a, 0) * new Vector3(0, 0.2f * k, -0.14f * k);
            P.Add(bark, Cyl, c, q, new Vector3(0.09f, 0.26f, 0.09f) * k);
            P.Add(cut, Cyl, c + q * Vector3.up * 0.26f * k, q, new Vector3(0.085f, 0.004f, 0.085f) * k);
        }
        P.Build(root.transform);
        Flames(root.transform, new Vector3(0, 0.04f * k, 0), 0.75f * k, 4, rnd);
        return root;
    }

    // ---------- furnace ----------
    public static GameObject Furnace(Transform parent, float w, float d, int seed)
    {
        var rnd = new System.Random(seed); var root = new GameObject("Furnace"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material[] stones = { Mat("brick_a", C(0x8a8378)), Mat("brick_b", C(0x6e6860)), Mat("brick_c", C(0x9a8e7e)) };
        Material soot = Mat("soot", C(0x1c1816)), glowM = Mat("furnace_glow", C(0xff5a14), 0f, 0f, 2.4f), mortar = Mat("mortar", C(0x4a4642));
        float bh = 0.2f; int courses = 7;
        P.Add(mortar, Cube, new Vector3(0, 0.08f, 0), Quaternion.identity, new Vector3(w * 1.08f, 0.16f, d * 1.08f));
        for (int c = 0; c < courses; c++)
        {
            float y = 0.16f + c * bh + bh / 2f, shrink = 1f - Mathf.Max(0, c - 4) * 0.08f;
            float hw = w / 2f * shrink, hd = d / 2f * shrink;
            float per = 2 * (hw * 2 + hd * 2); int nb = Mathf.Max(8, Mathf.RoundToInt(per / 0.34f));
            for (int b = 0; b < nb; b++)
            {
                float u = (b + (c % 2) * 0.5f) / nb * per, x, z, rot;
                if (u < 2 * hw) { x = -hw + u; z = hd; rot = 0; }
                else if (u < 2 * hw + 2 * hd) { x = hw; z = hd - (u - 2 * hw); rot = 90; }
                else if (u < 4 * hw + 2 * hd) { x = hw - (u - 2 * hw - 2 * hd); z = -hd; rot = 0; }
                else { x = -hw; z = -hd + (u - 4 * hw - 2 * hd); rot = 90; }
                // the mouth: an opening at the front, two courses tall, arched
                if (z > hd - 0.01f && Mathf.Abs(x) < w * 0.22f && c >= 1 && c <= 2) continue;
                if (z > hd - 0.01f && Mathf.Abs(x) < w * 0.13f && c == 3) continue;
                var m = stones[rnd.Next(3)];
                P.Add(m, Cube, new Vector3(x, y, z), Quaternion.Euler((float)rnd.NextDouble() * 4 - 2, rot + (float)rnd.NextDouble() * 6 - 3, (float)rnd.NextDouble() * 4 - 2),
                    new Vector3(0.3f + (float)rnd.NextDouble() * 0.05f, bh * 0.92f, 0.22f));
            }
        }
        float top = 0.16f + courses * bh;
        P.Add(stones[1], Cube, new Vector3(0, top + 0.06f, 0), Quaternion.identity, new Vector3(w * 0.72f, 0.12f, d * 0.72f));
        // the chimney, brick by brick, toward the back
        for (int c = 0; c < 6; c++)
            for (int s = 0; s < 4; s++)
            {
                float y = top + 0.12f + c * bh + bh / 2f, cw = 0.21f; Vector3 off = new Vector3(0, 0, -d * 0.12f);
                Vector3 p = s == 0 ? new Vector3(0, y, cw) : s == 1 ? new Vector3(cw, y, 0) : s == 2 ? new Vector3(0, y, -cw) : new Vector3(-cw, y, 0);
                P.Add(stones[rnd.Next(3)], Cube, off + p, Quaternion.Euler(0, s % 2 == 1 ? 90 : 0, 0), new Vector3(0.44f, bh * 0.92f, 0.12f));
            }
        // inside: soot walls and a bed of glowing coals behind the mouth
        P.Add(soot, Cube, new Vector3(0, 0.16f + bh * 1.6f, d * 0.2f), Quaternion.identity, new Vector3(w * 0.5f, bh * 3.2f, d * 0.6f));
        P.Add(glowM, Cube, new Vector3(0, 0.16f + bh * 0.62f, d * 0.43f), Quaternion.identity, new Vector3(w * 0.42f, 0.1f, 0.12f));
        P.Build(root.transform);
        Flames(root.transform, new Vector3(0, 0.16f + bh * 0.8f, d * 0.42f), 0.42f, 3, rnd);
        return root;
    }

    // ---------- anvil ----------
    public static GameObject Anvil(Transform parent, float k)
    {
        var root = new GameObject("Anvil"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material iron = Mat("anvil_iron", C(0x3a3c40), 0.55f, 0.85f), face = Mat("anvil_face", C(0x8a8c90), 0.75f, 0.9f), bark = Mat("bark", C(0x5a3a22)), cut = Mat("stump_top", C(0xb08a5a)), handle = Mat("handle", C(0x7a5232));
        P.Add(bark, Cyl, new Vector3(0, 0.27f, 0) * k, Quaternion.identity, new Vector3(0.48f, 0.27f, 0.48f) * k);
        P.Add(cut, Cyl, new Vector3(0, 0.545f, 0) * k, Quaternion.identity, new Vector3(0.45f, 0.006f, 0.45f) * k);
        float y0 = 0.55f;
        P.Add(iron, Cube, new Vector3(0, y0 + 0.05f, 0) * k, Quaternion.identity, new Vector3(0.44f, 0.1f, 0.3f) * k);      // feet
        P.Add(iron, Cube, new Vector3(0, y0 + 0.16f, 0) * k, Quaternion.identity, new Vector3(0.24f, 0.14f, 0.18f) * k);    // waist
        P.Add(iron, Cube, new Vector3(-0.03f, y0 + 0.29f, 0) * k, Quaternion.identity, new Vector3(0.5f, 0.13f, 0.22f) * k); // body
        P.Add(face, Cube, new Vector3(-0.03f, y0 + 0.358f, 0) * k, Quaternion.identity, new Vector3(0.5f, 0.012f, 0.2f) * k); // the face
        P.Add(iron, Flame, new Vector3(0.21f, y0 + 0.3f, 0) * k, Quaternion.Euler(0, 0, -90), new Vector3(0.2f, 0.36f, 0.17f) * k);   // the horn, tapering to a point
        P.Add(iron, Cube, new Vector3(-0.33f, y0 + 0.31f, 0) * k, Quaternion.identity, new Vector3(0.12f, 0.08f, 0.18f) * k);      // heel
        // a hammer resting on the face
        P.Add(handle, Cyl, new Vector3(-0.05f, y0 + 0.385f, 0.02f) * k, Quaternion.Euler(0, 30, 90), new Vector3(0.025f, 0.16f, 0.025f) * k);
        P.Add(iron, Cube, new Vector3(-0.19f, y0 + 0.395f, 0.1f) * k, Quaternion.Euler(0, 30, 0), new Vector3(0.06f, 0.06f, 0.13f) * k);
        P.Build(root.transform);
        return root;
    }

    // ---------- street lamp ----------
    // a black iron post on a stepped foot, a curled arm, and a glass lantern hanging from it that lights up at night
    public static GameObject Lamp(Transform parent, int seed)
    {
        var root = new GameObject("Street lamp"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material iron = Mat("lamp_iron", C(0x24262a), 0.45f, 0.7f), glass = Mat("lamp_glass", C(0xffc66a), 0.8f, 0f, 1.6f);
        P.Add(iron, Cyl, new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(0.34f, 0.06f, 0.34f));
        P.Add(iron, Cyl, new Vector3(0, 0.16f, 0), Quaternion.identity, new Vector3(0.22f, 0.05f, 0.22f));
        P.Add(iron, Cyl, new Vector3(0, 1.55f, 0), Quaternion.identity, new Vector3(0.09f, 1.4f, 0.09f));
        P.Add(iron, Cyl, new Vector3(0, 1.0f, 0), Quaternion.identity, new Vector3(0.14f, 0.03f, 0.14f));
        P.Add(iron, Sph, new Vector3(0, 2.98f, 0), Quaternion.identity, Vector3.one * 0.14f);
        // the arm, bending out and down a little, with a curl under it
        P.Add(iron, Cyl, new Vector3(0, 2.82f, 0.28f), Quaternion.Euler(90, 0, 0), new Vector3(0.06f, 0.28f, 0.06f));
        for (int i = 0; i < 6; i++) { float a = i * 0.55f; P.Add(iron, Sph, new Vector3(0, 2.62f + Mathf.Cos(a) * 0.1f, 0.18f + Mathf.Sin(a) * 0.1f), Quaternion.identity, Vector3.one * 0.04f); }
        P.Add(iron, Cyl, new Vector3(0, 2.72f, 0.55f), Quaternion.identity, new Vector3(0.02f, 0.1f, 0.02f));
        // the lantern: a cap, four corner bars round glowing glass, a foot
        Vector3 L = new Vector3(0, 2.38f, 0.55f);
        P.Add(iron, Cyl, L + new Vector3(0, 0.26f, 0), Quaternion.identity, new Vector3(0.3f, 0.03f, 0.3f));
        P.Add(iron, Cyl, L + new Vector3(0, 0.31f, 0), Quaternion.identity, new Vector3(0.16f, 0.04f, 0.16f));
        foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f }) P.Add(iron, Cube, L + new Vector3(sx * 0.1f, 0.05f, sz * 0.1f), Quaternion.identity, new Vector3(0.03f, 0.4f, 0.03f));
        P.Add(glass, Cube, L + new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(0.18f, 0.34f, 0.18f));
        P.Add(iron, Cube, L + new Vector3(0, -0.16f, 0), Quaternion.identity, new Vector3(0.24f, 0.03f, 0.24f));
        P.Build(root.transform);
        var lg = new GameObject("Lamplight"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = L;
        var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.74f, 0.4f); li.range = 8f; li.intensity = 1.7f; li.shadows = LightShadows.None; li.enabled = false;
        lg.AddComponent<AHLampGlow>();
        return root;
    }

    // ---------- brazier ----------
    // a carved stone pillar (h tall) topped by an iron fire bowl on three curled legs, burning with live flames
    public static GameObject Brazier(Transform parent, float h, int seed)
    {
        var root = new GameObject("Brazier"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material stone = Mat("braz_stone", C(0x6a5e52), 0.15f), stoneD = Mat("braz_stoneD", C(0x4a4038), 0.15f),
                 iron = Mat("braz_iron", C(0x2a2624), 0.4f, 0.65f), coal = Mat("braz_coal", C(0xff5a14), 0.3f, 0f, 1.8f);
        P.Add(stoneD, Cube, new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(0.75f, 0.24f, 0.75f));
        P.Add(stone, Cube, new Vector3(0, 0.3f, 0), Quaternion.identity, new Vector3(0.6f, 0.14f, 0.6f));
        P.Add(stone, Cube, new Vector3(0, 0.37f + (h - 0.6f) / 2f, 0), Quaternion.identity, new Vector3(0.42f, h - 0.6f, 0.42f));
        for (int i = 1; i <= 2; i++) P.Add(stoneD, Cube, new Vector3(0, 0.37f + (h - 0.6f) * i / 3f, 0), Quaternion.identity, new Vector3(0.46f, 0.05f, 0.46f));
        P.Add(stone, Cube, new Vector3(0, h - 0.16f, 0), Quaternion.identity, new Vector3(0.56f, 0.12f, 0.56f));
        // three legs and the bowl
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2 / 3f; Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            P.Add(iron, Cyl, new Vector3(0, h, 0) + dir * 0.17f + Vector3.up * 0.12f, Quaternion.FromToRotation(Vector3.up, (Vector3.up + dir * 0.45f).normalized), new Vector3(0.04f, 0.13f, 0.04f));
            P.Add(iron, Sph, new Vector3(0, h - 0.06f, 0) + dir * 0.13f, Quaternion.identity, Vector3.one * 0.07f);
        }
        P.Add(iron, Cyl, new Vector3(0, h + 0.27f, 0), Quaternion.identity, new Vector3(0.62f, 0.06f, 0.62f));
        P.Add(iron, Cyl, new Vector3(0, h + 0.33f, 0), Quaternion.identity, new Vector3(0.7f, 0.025f, 0.7f));
        P.Add(iron, Cyl, new Vector3(0, h + 0.2f, 0), Quaternion.identity, new Vector3(0.42f, 0.05f, 0.42f));
        for (int i = 0; i < 7; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2, d = (float)rnd.NextDouble() * 0.2f;
            P.Add(coal, Sph, new Vector3(Mathf.Cos(a) * d, h + 0.36f, Mathf.Sin(a) * d), Quaternion.identity, Vector3.one * (0.09f + (float)rnd.NextDouble() * 0.05f));
        }
        P.Build(root.transform);
        Flames(root.transform, new Vector3(0, h + 0.36f, 0), 0.75f, 5, rnd);
        return root;
    }

    // ---------- raid column ----------
    // a dark volcanic column: square plinth, round fluted shaft (eight ribs), gold bands, a flared capital and a
    // smouldering ember bowl on top
    public static GameObject Column(Transform parent, float h, int seed)
    {
        var root = new GameObject("Column"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material st = Mat("col_stone", C(0x4a3a34), 0.2f), dk = Mat("col_dark", C(0x2a1c18), 0.2f), gold = Mat("col_gold", C(0xd9ab3a), 0.65f, 0.85f),
                 ember = Mat("col_ember", C(0xff5a14), 0.3f, 0f, 2.2f);
        P.Add(dk, Cube, new Vector3(0, 0.25f, 0), Quaternion.identity, new Vector3(1.9f, 0.5f, 1.9f));
        P.Add(st, Cube, new Vector3(0, 0.62f, 0), Quaternion.identity, new Vector3(1.6f, 0.25f, 1.6f));
        float sh = h - 1.6f;
        P.Add(st, Cyl, new Vector3(0, 0.75f + sh / 2f, 0), Quaternion.identity, new Vector3(1.05f, sh / 2f, 1.05f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            P.Add(dk, Cube, new Vector3(Mathf.Cos(a) * 0.5f, 0.75f + sh / 2f, Mathf.Sin(a) * 0.5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0), new Vector3(0.12f, sh, 0.12f));
        }
        foreach (float y in new[] { 0.85f, 0.75f + sh * 0.5f, 0.65f + sh })
            P.Add(gold, Cyl, new Vector3(0, y, 0), Quaternion.identity, new Vector3(1.18f, 0.06f, 1.18f));
        P.Add(st, Cyl, new Vector3(0, 0.8f + sh + 0.15f, 0), Quaternion.identity, new Vector3(1.5f, 0.15f, 1.5f));
        P.Add(dk, Cube, new Vector3(0, 0.8f + sh + 0.42f, 0), Quaternion.identity, new Vector3(1.8f, 0.24f, 1.8f));
        P.Add(gold, Cyl, new Vector3(0, h - 0.02f, 0), Quaternion.identity, new Vector3(0.95f, 0.12f, 0.95f));
        for (int i = 0; i < 6; i++) { float a = (float)rnd.NextDouble() * Mathf.PI * 2, d = (float)rnd.NextDouble() * 0.28f; P.Add(ember, Sph, new Vector3(Mathf.Cos(a) * d, h + 0.08f, Mathf.Sin(a) * d), Quaternion.identity, Vector3.one * 0.2f); }
        P.Build(root.transform);
        Flames(root.transform, new Vector3(0, h + 0.1f, 0), 0.9f, 5, rnd);
        return root;
    }

    // ---------- lava pool dressing ----------
    // around a lava disc of radius r: a ring of dark rocks, floating plates of cooling crust, and bubbles that swell
    // and pop (AHLavaBubbles)
    public static void LavaDress(Transform parent, Vector3 at, float r, int seed)
    {
        var root = new GameObject("Lava pool"); root.transform.SetParent(parent, false); root.transform.position = at;
        var P = new Parts(); var rnd = new System.Random(seed);
        Material rock = Mat("lava_rock", C(0x2a1e1a), 0.15f), crust = Mat("lava_crust", C(0x3a2420), 0.1f), hot = Mat("lava_hot", C(0xff7a1a), 0.4f, 0f, 2.6f);
        int n = 16;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2 / n + (float)rnd.NextDouble() * 0.2f, rr = r * (1.0f + (float)rnd.NextDouble() * 0.12f), sz = 0.35f + (float)rnd.NextDouble() * 0.35f;
            P.Add(rock, Sph, new Vector3(Mathf.Cos(a) * rr, sz * 0.25f, Mathf.Sin(a) * rr), Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0), new Vector3(sz * 1.4f, sz * 0.8f, sz));
        }
        for (int i = 0; i < 5; i++)
        {
            // small round islands of cooling crust
            float a = (float)rnd.NextDouble() * Mathf.PI * 2, d = r * (0.25f + (float)rnd.NextDouble() * 0.45f), sz = 0.18f + (float)rnd.NextDouble() * 0.2f;
            P.Add(crust, Sph, new Vector3(Mathf.Cos(a) * d, 0.05f, Mathf.Sin(a) * d), Quaternion.identity, new Vector3(sz * 1.2f, 0.05f, sz));
        }
        P.Build(root.transform);
        var bub = root.AddComponent<AHLavaBubbles>(); bub.r = r * 0.8f; bub.mat = hot; bub.seed = seed;
    }

    // ---------- basalt columns ----------
    // a cluster of hexagonal basalt columns of different heights, each with a slightly tilted cracked top, darker joints
    // every so often and a faint ember glow in the cracks at their feet
    static Mesh hexm;
    static Mesh Hex()
    {
        if (hexm != null) return hexm;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            float a0 = i * Mathf.PI / 3f, a1 = (i + 1) * Mathf.PI / 3f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f), p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
            int b = v.Count; v.Add(p0); v.Add(p1); v.Add(p1 + Vector3.up); v.Add(p0 + Vector3.up);
            t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            int c = v.Count; v.Add(Vector3.up); v.Add(p0 + Vector3.up); v.Add(p1 + Vector3.up); t.AddRange(new[] { c, c + 2, c + 1 });
        }
        hexm = new Mesh { name = "Hex" }; hexm.SetVertices(v); hexm.SetTriangles(t, 0); hexm.RecalculateNormals(); hexm.RecalculateBounds(); return hexm;
    }
    public static GameObject Basalt(Transform parent, float h, float w, int seed)
    {
        var root = new GameObject("Basalt columns"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material[] st = { Mat("bas_a", C(0x3a3538), 0.3f), Mat("bas_b", C(0x48424a), 0.3f), Mat("bas_c", C(0x302c30), 0.35f) };
        Material joint = Mat("bas_joint", C(0x161314), 0.2f), glow = Mat("bas_glow", C(0xff6a1a), 0.3f, 0f, 1.6f), top = Mat("bas_top", C(0x5A5458), 0.15f);
        int n = 9 + rnd.Next(5); float cw = Mathf.Clamp(w * 0.34f, 0.45f, 0.7f);
        for (int i = 0; i < n; i++)
        {
            float a = i * 2.4f + (float)rnd.NextDouble() * 0.5f, d = i == 0 ? 0f : cw * (1.0f + 0.42f * Mathf.Sqrt(i));
            Vector3 at = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d);
            // terraced: tallest in the middle, stepping down outwards, a few stumps at the edge
            float fall = 1f - Mathf.Clamp01(d / (cw * 2.6f)) * 0.75f;
            float ch = h * (i == 0 ? 1f : Mathf.Clamp(fall * (0.55f + (float)rnd.NextDouble() * 0.5f), 0.12f, 0.92f)), cr = cw * (0.85f + (float)rnd.NextDouble() * 0.3f);
            var rot = Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 5f, (float)rnd.NextDouble() * 60f, ((float)rnd.NextDouble() - 0.5f) * 5f);
            P.Add(st[rnd.Next(3)], Hex(), at - Vector3.up * 0.2f, rot, new Vector3(cr, ch + 0.2f, cr));
            for (float y = 0.9f + (float)rnd.NextDouble() * 0.5f; y < ch - 0.3f; y += 0.9f + (float)rnd.NextDouble() * 0.7f)
                P.Add(joint, Hex(), at + Vector3.up * y, rot, new Vector3(cr * 1.03f, 0.04f, cr * 1.03f));
            P.Add(top, Hex(), at + rot * (Vector3.up * ch), rot, new Vector3(cr * 0.96f, 0.03f, cr * 0.96f));
            if (rnd.NextDouble() < 0.5) P.Add(glow, Sph, at + Vector3.up * 0.05f + rot * new Vector3(cr * 0.45f, 0, 0), Quaternion.identity, new Vector3(0.18f, 0.06f, 0.18f));
        }
        P.Build(root.transform); return root;
    }

    // ---------- market stall ----------
    // a timber counter with a planked front, a striped cloth awning on four posts, crates and baskets of produce
    public static GameObject Stall(Transform parent, float w, float d, int seed)
    {
        var root = new GameObject("Market stall"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        int[] cloths = { 0xC0392B, 0x2F6E9C, 0xD49A2A, 0x3C8C5A, 0x7A3A8C };
        Material wood = Mat("st_wood", C(0x7A5232)), dark = Mat("st_dark", C(0x4A3020)), clothA = Mat("st_cloth" + seed % 5, C(cloths[seed % 5]), 0.1f, 0f, 0f, true), clothB = Mat("st_clothW", C(0xF2EADA), 0.1f, 0f, 0f, true),
                 basket = Mat("st_basket", C(0xB58A4A));
        Material[] fruit = { Mat("fr_red", C(0xC8302A), 0.5f), Mat("fr_orange", C(0xE8892A), 0.5f), Mat("fr_green", C(0x7AB040), 0.5f), Mat("fr_yellow", C(0xEED23A), 0.5f), Mat("fr_purple", C(0x6A2A7A), 0.5f) };
        float hw = w / 2f, hd = d / 2f;
        P.Add(wood, Cube, new Vector3(0, 0.45f, 0.1f), Quaternion.identity, new Vector3(w, 0.9f, d * 0.55f));
        for (float x = -hw + 0.12f; x < hw; x += 0.24f) P.Add(dark, Cube, new Vector3(x, 0.45f, 0.1f + d * 0.275f + 0.01f), Quaternion.identity, new Vector3(0.02f, 0.86f, 0.02f));
        P.Add(dark, Cube, new Vector3(0, 0.92f, 0.1f), Quaternion.identity, new Vector3(w + 0.1f, 0.06f, d * 0.55f + 0.1f));
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            { float ph = sz > 0 ? 2.2f : 2.5f; P.Add(dark, Cube, new Vector3(sx * (hw - 0.05f), ph / 2f, sz * (hd - 0.05f)), Quaternion.identity, new Vector3(0.09f, ph, 0.09f)); }
        // the striped awning, sloping down to the front, with a scalloped valance
        int stripes = Mathf.Max(4, Mathf.RoundToInt(w / 0.32f)); float sw = (w + 0.3f) / stripes;
        var tilt = Quaternion.Euler(Mathf.Atan2(0.3f, d) * Mathf.Rad2Deg, 0, 0);
        for (int i = 0; i < stripes; i++)
        {
            float x = -(w + 0.3f) / 2f + (i + 0.5f) * sw; var m = i % 2 == 0 ? clothA : clothB;
            P.Add(m, Cube, new Vector3(x, 2.38f, 0), tilt, new Vector3(sw, 0.03f, d + 0.4f));
            P.Add(m, Cube, new Vector3(x, 2.1f, hd + 0.2f), Quaternion.identity, new Vector3(sw, 0.25f, 0.02f));
        }
        // produce: crates and baskets heaped with fruit
        for (int i = 0; i < 3; i++)
        {
            float x = -hw + w * (i + 0.5f) / 3f; var fm = fruit[(seed + i) % fruit.Length];
            bool crate = (i + seed) % 2 == 0;
            P.Add(crate ? wood : basket, crate ? Cube : Cyl, new Vector3(x, 1.05f, 0.15f), Quaternion.identity, crate ? new Vector3(w / 3.6f, 0.22f, d * 0.4f) : new Vector3(w / 3.8f, 0.1f, d * 0.38f));
            for (int k = 0; k < 9; k++) P.Add(fm, Sph, new Vector3(x + ((float)rnd.NextDouble() - 0.5f) * w / 4.5f, 1.2f + (float)rnd.NextDouble() * 0.08f, 0.15f + ((float)rnd.NextDouble() - 0.5f) * d * 0.3f), Quaternion.identity, Vector3.one * (0.1f + (float)rnd.NextDouble() * 0.04f));
        }
        P.Add(wood, Cube, new Vector3(-hw + 0.35f, 0.2f, hd + 0.35f), Quaternion.Euler(0, 15, 0), new Vector3(0.5f, 0.4f, 0.4f));
        P.Add(basket, Cyl, new Vector3(hw - 0.35f, 0.15f, hd + 0.3f), Quaternion.identity, new Vector3(0.45f, 0.15f, 0.45f));
        P.Build(root.transform); return root;
    }

    // ---------- brewing cauldron ----------
    // a pot-bellied iron cauldron on three legs over a small wood fire, a rolled rim, bubbling green brew with a soft
    // glow, a ladle leaning in and a rack of bottles beside it
    public static GameObject Cauldron(Transform parent, int seed)
    {
        var root = new GameObject("Cauldron"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material iron = Mat("cau_iron", C(0x2A2A2E), 0.45f, 0.6f), brew = Mat("cau_brew", C(0x5AD04A), 0.7f, 0f, 1.4f), wood = Mat("cau_wood", C(0x6A4428)),
                 stone = Mat("cau_stone", C(0x6E6A62)), glass = Mat("cau_glass", C(0x8ACCE0), 0.85f), glass2 = Mat("cau_glass2", C(0xC05AA0), 0.85f);
        for (int i = 0; i < 9; i++) { float a = i * Mathf.PI * 2 / 9f; P.Add(stone, Sph, new Vector3(Mathf.Cos(a) * 0.7f, 0.07f, Mathf.Sin(a) * 0.7f), Quaternion.identity, new Vector3(0.3f, 0.18f, 0.26f)); }
        for (int i = 0; i < 4; i++) P.Add(wood, Cyl, new Vector3(0, 0.1f, 0), Quaternion.Euler(80, i * 45f, 0), new Vector3(0.09f, 0.4f, 0.09f));
        // the pot: stacked rings make a belly
        float[] ys = { 0.38f, 0.48f, 0.6f, 0.72f, 0.84f, 0.94f }; float[] rs = { 0.62f, 0.86f, 0.98f, 1.0f, 0.94f, 0.86f };
        for (int i = 0; i < ys.Length; i++) P.Add(iron, Cyl, new Vector3(0, ys[i], 0), Quaternion.identity, new Vector3(rs[i], 0.065f, rs[i]));
        P.Add(iron, Cyl, new Vector3(0, 1.0f, 0), Quaternion.identity, new Vector3(0.98f, 0.035f, 0.98f));
        P.Add(brew, Cyl, new Vector3(0, 0.98f, 0), Quaternion.identity, new Vector3(0.8f, 0.02f, 0.8f));
        for (int i = 0; i < 3; i++) { float a = i * Mathf.PI * 2 / 3f + 0.5f; P.Add(iron, Cube, new Vector3(Mathf.Cos(a) * 0.38f, 0.2f, Mathf.Sin(a) * 0.38f), Quaternion.Euler(Mathf.Sin(a) * 12f, 0, -Mathf.Cos(a) * 12f), new Vector3(0.08f, 0.4f, 0.08f)); }
        for (int i = 0; i < 5; i++) P.Add(brew, Sph, new Vector3(((float)rnd.NextDouble() - 0.5f) * 0.5f, 1.0f, ((float)rnd.NextDouble() - 0.5f) * 0.5f), Quaternion.identity, Vector3.one * (0.06f + (float)rnd.NextDouble() * 0.06f));
        P.Add(wood, Cube, new Vector3(0.15f, 1.25f, 0.1f), Quaternion.Euler(0, 0, 28), new Vector3(0.04f, 0.8f, 0.04f));
        // a little table of bottles
        P.Add(wood, Cube, new Vector3(1.2f, 0.5f, 0), Quaternion.identity, new Vector3(0.6f, 0.05f, 0.5f));
        foreach (float sx in new[] { -0.25f, 0.25f }) foreach (float sz in new[] { -0.2f, 0.2f }) P.Add(wood, Cube, new Vector3(1.2f + sx, 0.25f, sz), Quaternion.identity, new Vector3(0.05f, 0.5f, 0.05f));
        for (int i = 0; i < 4; i++) { var g = i % 2 == 0 ? glass : glass2; float x = 1.02f + i * 0.12f; P.Add(g, Cyl, new Vector3(x, 0.6f, 0.05f * (i % 2 == 0 ? 1 : -1)), Quaternion.identity, new Vector3(0.09f, 0.08f, 0.09f)); P.Add(g, Cyl, new Vector3(x, 0.72f, 0.05f * (i % 2 == 0 ? 1 : -1)), Quaternion.identity, new Vector3(0.035f, 0.05f, 0.035f)); }
        P.Build(root.transform);
        Flames(root.transform, new Vector3(0, 0.12f, 0), 0.5f, 4, rnd);
        return root;
    }

    // ---------- loom ----------
    // an upright timber loom: two side frames, a breast beam and a cloth beam, a coloured length of woven cloth, the warp
    // threads above it, a shuttle and a bench in front
    public static GameObject Loom(Transform parent, int seed)
    {
        var root = new GameObject("Loom"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        int[] cl = { 0x9A2A2A, 0x2A4A8A, 0x3A6A3A, 0xB08A2A };
        Material wood = Mat("loom_wood", C(0x7A5232)), dark = Mat("loom_dark", C(0x4A3020)), cloth = Mat("loom_cloth" + seed % 4, C(cl[seed % 4]), 0.1f, 0f, 0f, true),
                 thread = Mat("loom_thread", C(0xE8DCC0), 0.1f), stripe = Mat("loom_stripe", C(0xE8C860), 0.1f, 0f, 0f, true);
        foreach (float sx in new[] { -0.75f, 0.75f })
        {
            P.Add(wood, Cube, new Vector3(sx, 0.9f, -0.2f), Quaternion.identity, new Vector3(0.1f, 1.8f, 0.1f));
            P.Add(wood, Cube, new Vector3(sx, 0.6f, 0.35f), Quaternion.identity, new Vector3(0.1f, 1.2f, 0.1f));
            P.Add(dark, Cube, new Vector3(sx, 0.1f, 0.07f), Quaternion.identity, new Vector3(0.12f, 0.1f, 0.9f));
            P.Add(dark, Cube, new Vector3(sx, 1.2f, 0.07f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.7f));
        }
        P.Add(dark, Cyl, new Vector3(0, 1.75f, -0.2f), Quaternion.Euler(0, 0, 90), new Vector3(0.09f, 0.8f, 0.09f));
        P.Add(dark, Cyl, new Vector3(0, 1.15f, 0.35f), Quaternion.Euler(0, 0, 90), new Vector3(0.1f, 0.8f, 0.1f));
        P.Add(cloth, Cube, new Vector3(0, 1.0f, 0.36f), Quaternion.Euler(-35, 0, 0), new Vector3(1.3f, 0.5f, 0.015f));
        P.Add(stripe, Cube, new Vector3(0, 1.0f, 0.375f), Quaternion.Euler(-35, 0, 0), new Vector3(1.3f, 0.06f, 0.012f));
        for (int i = 0; i < 24; i++) { float x = -0.62f + i * 1.24f / 23f; Vector3 a0 = new Vector3(x, 1.73f, -0.18f), a1 = new Vector3(x, 1.18f, 0.32f); P.Add(thread, Cube, (a0 + a1) * 0.5f, Quaternion.LookRotation(a1 - a0), new Vector3(0.012f, 0.012f, (a1 - a0).magnitude)); }
        P.Add(dark, Cube, new Vector3(0, 1.45f, 0.08f), Quaternion.identity, new Vector3(1.4f, 0.06f, 0.05f));
        P.Add(wood, Cube, new Vector3(0.2f, 1.2f, 0.45f), Quaternion.identity, new Vector3(0.35f, 0.05f, 0.08f));
        P.Add(wood, Cube, new Vector3(0, 0.45f, 0.95f), Quaternion.identity, new Vector3(1.0f, 0.06f, 0.32f));
        foreach (float sx in new[] { -0.4f, 0.4f }) P.Add(dark, Cube, new Vector3(sx, 0.22f, 0.95f), Quaternion.identity, new Vector3(0.06f, 0.44f, 0.28f));
        P.Build(root.transform); return root;
    }

    // ---------- jeweller's bench ----------
    // a sturdy bench with a drawer front, a bench pin and vice, a little anvil, tools, a lamp, and gems that glint
    public static GameObject JewelBench(Transform parent, int seed)
    {
        var root = new GameObject("Jeweller's bench"); root.transform.SetParent(parent, false);
        var P = new Parts(); var rnd = new System.Random(seed);
        Material wood = Mat("jw_wood", C(0x6A4428)), dark = Mat("jw_dark", C(0x3E2818)), iron = Mat("jw_iron", C(0x3A3A40), 0.5f, 0.6f), brass = Mat("jw_brass", C(0xC8962E), 0.7f, 0.8f),
                 felt = Mat("jw_felt", C(0x1E4A3A), 0.05f), lamp = Mat("jw_lamp", C(0xFFD27A), 0.3f, 0f, 1.6f);
        Material[] gems = { Mat("gem_r", C(0xE0203A), 0.9f, 0f, 0.8f), Mat("gem_b", C(0x2A6AE8), 0.9f, 0f, 0.8f), Mat("gem_g", C(0x2AC860), 0.9f, 0f, 0.8f), Mat("gem_w", C(0xE8F4FF), 0.95f, 0f, 0.8f) };
        P.Add(wood, Cube, new Vector3(0, 0.88f, 0), Quaternion.identity, new Vector3(1.6f, 0.08f, 0.8f));
        P.Add(felt, Cube, new Vector3(-0.1f, 0.925f, 0), Quaternion.identity, new Vector3(0.9f, 0.01f, 0.5f));
        P.Add(dark, Cube, new Vector3(0.5f, 0.65f, 0), Quaternion.identity, new Vector3(0.55f, 0.4f, 0.74f));
        for (int i = 0; i < 3; i++) { P.Add(wood, Cube, new Vector3(0.5f, 0.52f + i * 0.13f, 0.38f), Quaternion.identity, new Vector3(0.48f, 0.1f, 0.02f)); P.Add(brass, Sph, new Vector3(0.5f, 0.52f + i * 0.13f, 0.4f), Quaternion.identity, Vector3.one * 0.03f); }
        foreach (float sx in new[] { -0.72f, 0.72f }) foreach (float sz in new[] { -0.33f, 0.33f }) P.Add(dark, Cube, new Vector3(sx, 0.42f, sz), Quaternion.identity, new Vector3(0.08f, 0.84f, 0.08f));
        P.Add(iron, Cube, new Vector3(-0.62f, 0.97f, 0.2f), Quaternion.identity, new Vector3(0.12f, 0.1f, 0.2f));
        P.Add(iron, Cube, new Vector3(-0.45f, 0.98f, -0.15f), Quaternion.identity, new Vector3(0.18f, 0.08f, 0.1f));
        P.Add(iron, Cube, new Vector3(-0.45f, 0.94f, -0.15f), Quaternion.identity, new Vector3(0.08f, 0.04f, 0.06f));
        P.Add(brass, Cyl, new Vector3(0.55f, 1.05f, -0.25f), Quaternion.identity, new Vector3(0.12f, 0.12f, 0.12f));
        P.Add(brass, Cube, new Vector3(0.55f, 1.3f, -0.15f), Quaternion.Euler(-30, 0, 0), new Vector3(0.03f, 0.45f, 0.03f));
        P.Add(lamp, Sph, new Vector3(0.55f, 1.48f, 0.0f), Quaternion.identity, Vector3.one * 0.14f);
        for (int i = 0; i < 6; i++) P.Add(gems[i % 4], Sph, new Vector3(-0.3f + (float)rnd.NextDouble() * 0.4f, 0.95f, -0.15f + (float)rnd.NextDouble() * 0.3f), Quaternion.Euler(0, 45, 0), Vector3.one * 0.06f);
        P.Add(wood, Cube, new Vector3(0, 0.45f, 0.75f), Quaternion.identity, new Vector3(0.45f, 0.05f, 0.35f));
        P.Add(dark, Cube, new Vector3(0, 0.22f, 0.75f), Quaternion.identity, new Vector3(0.06f, 0.44f, 0.06f));
        P.Build(root.transform); return root;
    }

    // ---------- the Ember Throne ----------
    // a stepped dais, a seat with a tall pointed back, armrests ending in gold knobs, curling horns, gold trims and
    // two braziers at its sides; it faces along its forward axis
    public static GameObject Throne(Transform parent)
    {
        var root = new GameObject("Ember Throne"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material st = Mat("col_stone", C(0x4a3a34), 0.2f), dk = Mat("col_dark", C(0x2a1c18), 0.2f), gold = Mat("col_gold", C(0xd9ab3a), 0.65f, 0.85f),
                 red = Mat("thr_red", C(0x7a1410), 0.35f), bone = Mat("thr_horn", C(0xe0d4b8), 0.4f), ember = Mat("col_ember", C(0xff5a14), 0.3f, 0f, 2.2f);
        // the dais: three steps
        P.Add(dk, Cube, new Vector3(0, 0.2f, 0), Quaternion.identity, new Vector3(7f, 0.4f, 5f));
        P.Add(st, Cube, new Vector3(0, 0.55f, -0.3f), Quaternion.identity, new Vector3(5.6f, 0.3f, 3.8f));
        P.Add(dk, Cube, new Vector3(0, 0.82f, -0.6f), Quaternion.identity, new Vector3(4.2f, 0.25f, 2.8f));
        P.Add(gold, Cube, new Vector3(0, 0.41f, 2.5f), Quaternion.identity, new Vector3(7.02f, 0.05f, 0.05f));
        P.Add(red, Cube, new Vector3(0, 0.42f, 1.6f), Quaternion.identity, new Vector3(1.6f, 0.03f, 2.4f));   // the carpet up the steps
        // the seat: the carved skull throne (Models/Town/throne_arcadia) when it is there, else one built of blocks
        var tm = Resources.Load<GameObject>("AH/Models/Town/throne_arcadia"); bool fancy = tm != null;
        if (!fancy)
        {
            // the seat and its back
            P.Add(st, Cube, new Vector3(0, 1.45f, -0.6f), Quaternion.identity, new Vector3(2.4f, 1.0f, 1.8f));
            P.Add(red, Cube, new Vector3(0, 1.98f, -0.55f), Quaternion.identity, new Vector3(2.0f, 0.12f, 1.5f));
            P.Add(dk, Cube, new Vector3(0, 3.6f, -1.35f), Quaternion.identity, new Vector3(2.6f, 4.4f, 0.5f));
            P.Add(red, Cube, new Vector3(0, 3.2f, -1.08f), Quaternion.identity, new Vector3(1.7f, 2.6f, 0.06f));
            P.Add(dk, Cube, new Vector3(0, 6.0f, -1.35f), Quaternion.Euler(0, 0, 45), new Vector3(1.85f, 1.85f, 0.5f));   // the pointed top
            P.Add(gold, Sph, new Vector3(0, 6.5f, -1.1f), Quaternion.identity, Vector3.one * 0.55f);
            P.Add(gold, Cube, new Vector3(0, 1.4f + 4.4f / 2f + 0.05f, -1.08f), Quaternion.identity, new Vector3(2.7f, 0.1f, 0.1f));
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            if (!fancy)
            {
                P.Add(gold, Cube, new Vector3(sx * 1.32f, 3.6f, -1.08f), Quaternion.identity, new Vector3(0.1f, 4.4f, 0.1f));
                // armrests
                P.Add(dk, Cube, new Vector3(sx * 1.35f, 2.25f, -0.5f), Quaternion.identity, new Vector3(0.4f, 0.3f, 1.9f));
                P.Add(st, Cube, new Vector3(sx * 1.35f, 1.6f, -0.5f), Quaternion.identity, new Vector3(0.35f, 1.1f, 1.7f));
                P.Add(gold, Sph, new Vector3(sx * 1.35f, 2.45f, 0.45f), Quaternion.identity, Vector3.one * 0.38f);
                // horns curling out from the back
                for (int i = 0; i < 18; i++)
                {
                    float k = i / 17f;
                    P.Add(bone, Sph, new Vector3(sx * (1.35f + k * 1.25f), 5.1f + Mathf.Sin(k * 2.6f) * 1.15f, -1.3f + k * 0.45f), Quaternion.identity, Vector3.one * (0.46f - k * 0.36f));
                }
            }
            // side braziers
            P.Add(dk, Cyl, new Vector3(sx * 3.0f, 1.0f, 0.8f), Quaternion.identity, new Vector3(0.45f, 0.6f, 0.45f));
            P.Add(gold, Cyl, new Vector3(sx * 3.0f, 1.65f, 0.8f), Quaternion.identity, new Vector3(0.9f, 0.08f, 0.9f));
            P.Add(ember, Sph, new Vector3(sx * 3.0f, 1.72f, 0.8f), Quaternion.identity, new Vector3(0.7f, 0.18f, 0.7f));
        }
        P.Build(root.transform);
        if (fancy)
        {
            var t = Object.Instantiate(tm, root.transform, false); t.name = "Skull throne";
            var rs = t.GetComponentsInChildren<Renderer>(); Bounds bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            float k = 5.4f / Mathf.Max(0.01f, bb.size.y);
            t.transform.localScale = Vector3.one * k; t.transform.localPosition = new Vector3(0f, 0.95f, -0.75f);
            foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        var rnd = new System.Random(77);
        Flames(root.transform, new Vector3(-3.0f, 1.75f, 0.8f), 0.9f, 5, rnd);
        Flames(root.transform, new Vector3(3.0f, 1.75f, 0.8f), 0.9f, 5, rnd);
        return root;
    }

    // ---------- banner ----------
    // a wooden pole with a gilt finial and crossbar, hanging a cloth in the given colour with a gold hem and a
    // swallow-tailed foot; it faces along its forward axis
    public static GameObject Banner(Transform parent, Color col)
    {
        var root = new GameObject("Banner"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material wood = Mat("ban_wood", C(0x4a3020), 0.25f), gold = Mat("ban_gold", C(0xd9ab3a), 0.6f, 0.8f),
                 cloth = Mat("ban_" + ColorUtility.ToHtmlStringRGB(col), col, 0.1f, 0f, 0f, true), stone = Mat("braz_stoneD", C(0x4a4038), 0.15f);
        P.Add(stone, Cyl, new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(0.5f, 0.12f, 0.5f));
        P.Add(wood, Cyl, new Vector3(0, 1.75f, 0), Quaternion.identity, new Vector3(0.1f, 1.75f, 0.1f));
        P.Add(gold, Sph, new Vector3(0, 3.55f, 0), Quaternion.identity, Vector3.one * 0.18f);
        P.Add(wood, Cyl, new Vector3(0, 3.25f, 0.06f), Quaternion.Euler(0, 0, 90), new Vector3(0.06f, 0.65f, 0.06f));
        foreach (var sx in new[] { -1f, 1f }) P.Add(gold, Sph, new Vector3(sx * 0.66f, 3.25f, 0.06f), Quaternion.identity, Vector3.one * 0.1f);
        P.Add(cloth, Cube, new Vector3(0, 2.4f, 0.1f), Quaternion.identity, new Vector3(1.1f, 1.6f, 0.03f));
        P.Add(gold, Cube, new Vector3(0, 3.17f, 0.1f), Quaternion.identity, new Vector3(1.14f, 0.07f, 0.04f));
        P.Add(gold, Cube, new Vector3(0, 1.62f, 0.1f), Quaternion.identity, new Vector3(1.14f, 0.05f, 0.04f));
        foreach (var sx in new[] { -1f, 1f }) P.Add(cloth, Cube, new Vector3(sx * 0.3f, 1.42f, 0.1f), Quaternion.Euler(0, 0, sx * 45f), new Vector3(0.4f, 0.4f, 0.03f));
        P.Add(gold, Sph, new Vector3(0, 2.5f, 0.12f), Quaternion.identity, new Vector3(0.36f, 0.36f, 0.04f));
        P.Build(root.transform);
        return root;
    }

    // ---------- fountain ----------
    // an eight-sided stone basin with a moulded rim, rippling water, a carved pedestal holding an upper bowl with a
    // gilded finial, and water spilling over the bowl's edge in streams that splash into the pool
    public static GameObject Fountain(Transform parent, float r)
    {
        var root = new GameObject("Fountain"); root.transform.SetParent(parent, false);
        var P = new Parts();
        Material stone = Mat("ftn_stone", C(0xb8b0a2)), stone2 = Mat("ftn_stone2", C(0x9a9284)), gold = Mat("ftn_gold", C(0xe8b84a), 0.6f, 0.9f),
                 water = Mat("ftn_water", C(0x3a8ac8), 0.95f, 0f, 0.25f), stream = Mat("ftn_stream", C(0xbfe8ff), 0.9f, 0f, 0.6f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + Mathf.PI / 8f, side = 2f * r * Mathf.Tan(Mathf.PI / 8f);
            Vector3 c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; Quaternion q = Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0);
            P.Add(stone2, Cube, c * 0.97f + Vector3.up * 0.3f, q, new Vector3(side * 1.02f, 0.6f, 0.34f));
            P.Add(stone, Cube, c * 0.97f + Vector3.up * 0.66f, q, new Vector3(side * 1.08f, 0.12f, 0.46f));   // the rim
        }
        P.Add(stone2, Cyl, new Vector3(0, 0.05f, 0), Quaternion.identity, new Vector3(r * 2f, 0.05f, r * 2f));
        P.Add(stone, Cyl, new Vector3(0, 0.35f, 0), Quaternion.identity, new Vector3(0.9f, 0.35f, 0.9f));
        P.Add(stone2, Cyl, new Vector3(0, 0.95f, 0), Quaternion.identity, new Vector3(0.55f, 0.35f, 0.55f));
        P.Add(stone, Cyl, new Vector3(0, 1.32f, 0), Quaternion.identity, new Vector3(0.75f, 0.06f, 0.75f));
        P.Add(stone, Sph, new Vector3(0, 1.62f, 0), Quaternion.identity, new Vector3(1.9f, 0.55f, 1.9f));                 // the upper bowl
        P.Add(water, Cyl, new Vector3(0, 1.82f, 0), Quaternion.identity, new Vector3(1.6f, 0.02f, 1.6f));
        P.Add(stone2, Cyl, new Vector3(0, 2.1f, 0), Quaternion.identity, new Vector3(0.22f, 0.3f, 0.22f));
        P.Add(gold, Sph, new Vector3(0, 2.5f, 0), Quaternion.identity, Vector3.one * 0.32f);
        P.Add(gold, Cyl, new Vector3(0, 2.75f, 0), Quaternion.identity, new Vector3(0.05f, 0.12f, 0.05f));
        P.Build(root.transform);
        var pool = new GameObject("Pool"); pool.transform.SetParent(root.transform, false); pool.transform.localPosition = new Vector3(0, 0.55f, 0);
        pool.AddComponent<MeshFilter>().sharedMesh = Cyl; pool.AddComponent<MeshRenderer>().sharedMaterial = water; pool.transform.localScale = new Vector3(r * 1.9f, 0.02f, r * 1.9f);
        var falls = new GameObject("Streams"); falls.transform.SetParent(root.transform, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f; var st = new GameObject("Stream"); st.transform.SetParent(falls.transform, false);
            st.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.93f, 1.2f, Mathf.Sin(a) * 0.93f); st.transform.localScale = new Vector3(0.05f, 0.33f, 0.05f);
            st.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, -8f);
            st.AddComponent<MeshFilter>().sharedMesh = Cyl; var mr = st.AddComponent<MeshRenderer>(); mr.sharedMaterial = stream; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        root.AddComponent<AHFountainPlay>().Setup(pool.transform, falls.transform, r);
        return root;
    }

    // every campfire, furnace and anvil of the area: the old one hidden, the new one in its place
    public static void Setup(AHGame g, Transform world)
    {
        if (world == null) return;
        var spots = new List<AHSpot>(); foreach (var s in AHGather.Spots) if (!s.temp && (s.kind == "fire" || s.kind == "furnace" || s.kind == "anvil")) spots.Add(s);
        if (spots.Count == 0) return;
        var rs = new List<Renderer>(); foreach (var r in world.GetComponentsInChildren<Renderer>(true)) { if (!r.enabled || r.name.StartsWith("AH_")) continue; var b = r.bounds; if (Mathf.Max(b.size.x, b.size.z) > 3f || b.size.y > 4f) continue; rs.Add(r); }
        var root = new GameObject("Stations").transform; int n = 0;
        Vector3 mid = g.W(g.data.spawn.x, g.data.spawn.z);
        foreach (var s in spots)
        {
            float rad = s.kind == "furnace" ? 1.5f : s.kind == "fire" ? 1.2f : 0.9f;
            float ground = float.PositiveInfinity; Bounds old = new Bounds(s.pos, Vector3.zero); bool any = false;
            foreach (var r in rs)
            {
                if (!r.enabled) continue; Vector3 c = r.bounds.center - s.pos; c.y = 0; if (c.magnitude > rad) continue;
                ground = Mathf.Min(ground, r.bounds.min.y); if (any) old.Encapsulate(r.bounds); else { old = r.bounds; any = true; }
                r.enabled = false;
            }
            Vector3 at = new Vector3(s.pos.x, float.IsInfinity(ground) ? s.pos.y : ground, s.pos.z);
            if (any) { at.x = old.center.x; at.z = old.center.z; }
            GameObject go;
            if (s.kind == "fire") go = Campfire(root, 1.25f, n + 1);
            else if (s.kind == "furnace") go = Furnace(root, any ? Mathf.Clamp(Mathf.Max(old.size.x, old.size.z) * 0.9f, 1.2f, 1.9f) : 1.6f, any ? Mathf.Clamp(Mathf.Min(old.size.x, old.size.z) * 0.95f, 1f, 1.5f) : 1.2f, n + 1);
            else go = Anvil(root, 1.25f);
            go.transform.position = at;
            Vector3 dir = mid - at; dir.y = 0; if (dir.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            AHModel.SetShadows(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) if (r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("flame")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            n++;
        }
        Debug.Log("Ashen Hollow: " + n + " campfires, furnaces and anvils rebuilt");
    }
}

// a flame tongue that licks: stretches, narrows and sways, never twice the same
public class AHFlameLick : MonoBehaviour
{
    public Vector3 baseScale; public float phase;
    void Update()
    {
        float t = Time.time * 1.3f + phase;
        float s = 1f + 0.18f * Mathf.Sin(t * 9.3f) + 0.1f * Mathf.Sin(t * 23.1f) + 0.08f * (Mathf.PerlinNoise(t * 2.7f, phase) - 0.5f);
        transform.localScale = new Vector3(baseScale.x * (1.08f - (s - 1f) * 0.6f), baseScale.y * s, baseScale.z * (1.08f - (s - 1f) * 0.6f));
        transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 3.1f) * 7f, t * 40f, Mathf.Sin(t * 2.3f + 1f) * 7f);
    }
}

// a street lamp's light: on at night, and only near the hero (so a street full of lamps stays cheap)
public class AHLampGlow : MonoBehaviour
{
    Light li; float t;
    void Start() { li = GetComponent<Light>(); t = Random.value * 0.5f; }
    void Update()
    {
        t -= Time.deltaTime; if (t > 0f) return; t = 0.5f;
        var g = AHGame.I; if (g == null || g.player == null || li == null) return;
        li.enabled = g.IsNight && (g.player.transform.position - transform.position).sqrMagnitude < 35f * 35f;
    }
}

// the fountain at play: the pool shimmers, the streams wobble, and now and then a ring of ripples spreads
public class AHFountainPlay : MonoBehaviour
{
    Transform pool, falls; float r, t;
    public void Setup(Transform p, Transform f, float rad) { pool = p; falls = f; r = rad; }
    void Update()
    {
        float tm = Time.time;
        for (int i = 0; i < falls.childCount; i++) { var s = falls.GetChild(i); s.localScale = new Vector3(0.05f + Mathf.Sin(tm * 11f + i) * 0.008f, 0.33f + Mathf.Sin(tm * 7f + i * 1.7f) * 0.02f, 0.05f); }
        t -= Time.deltaTime;
        if (t <= 0f)
        {
            t = 0.9f + Random.value * 0.6f;
            var g = AHGame.I; if (g != null && g.player != null && (g.player.transform.position - transform.position).sqrMagnitude > 40f * 40f) return;
            float a = Random.value * Mathf.PI * 2f; Vector3 at = transform.position + new Vector3(Mathf.Cos(a) * 0.95f, 0.57f, Mathf.Sin(a) * 0.95f);
            AHFx.Ring(at, 0.1f, 0.6f, new Color(0.85f, 0.95f, 1f, 0.6f), 0.9f);
        }
    }
}

// lava bubbles: a few glowing domes that swell up, pop with a spark and start again somewhere else
public class AHLavaBubbles : MonoBehaviour
{
    public float r = 1.5f; public Material mat; public int seed;
    Transform[] b; float[] t, life; System.Random rnd;
    void Start()
    {
        rnd = new System.Random(seed); b = new Transform[4]; t = new float[4]; life = new float[4];
        var sph = AHLowPoly.Fix(GameObject.CreatePrimitive(PrimitiveType.Sphere), PrimitiveType.Sphere); var mesh = sph.GetComponent<MeshFilter>().sharedMesh; Destroy(sph);
        for (int i = 0; i < b.Length; i++)
        {
            var o = new GameObject("Bubble"); o.transform.SetParent(transform, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b[i] = o.transform; Reset(i); t[i] = (float)rnd.NextDouble() * life[i];
        }
    }
    void Reset(int i)
    {
        float a = (float)rnd.NextDouble() * Mathf.PI * 2, d = (float)rnd.NextDouble() * r;
        b[i].localPosition = new Vector3(Mathf.Cos(a) * d, 0.04f, Mathf.Sin(a) * d); t[i] = 0f; life[i] = 1.2f + (float)rnd.NextDouble() * 1.6f;
    }
    void Update()
    {
        if (b == null) return;
        for (int i = 0; i < b.Length; i++)
        {
            t[i] += Time.deltaTime; float k = t[i] / life[i];
            if (k >= 1f) { AHSpark.Burst(b[i].position + Vector3.up * 0.1f, new Color(1f, 0.55f, 0.15f, 0.9f), 6, 2.5f, 0.5f, 0.08f, 1.5f); Reset(i); continue; }
            float s = Mathf.Lerp(0.05f, 0.42f, k * k);
            b[i].localScale = new Vector3(s, s * 0.7f, s);
        }
    }
}
