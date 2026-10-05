// Ashen Hollow: better mining rocks. Each ore rock (copper, tin, iron, gold, obsidian, coral) was a plain lump with a
// few specks. Now it is a chunky boulder (KayKit Medieval Hexagon rocks, CC0) the size of the old one, with glinting
// crystals of its ore growing out of it in the ore's colour (copper orange, tin silver-blue, iron rust, gold, black
// glass obsidian, pink coral). A mined-out rock loses its crystals and goes dull until it is back, as before.
using System.Collections.Generic;
using UnityEngine;

public static class AHOre
{
    static readonly GameObject[] rocks = new GameObject[4];
    static Mesh crystal;
    static Shader lit;

    // a six-sided crystal: a prism with a pointed tip, 1 m tall, pivot at its foot
    static Mesh Crystal()
    {
        if (crystal != null) return crystal;
        var v = new List<Vector3>(); var t = new List<int>(); int seg = 6;
        for (int s = 0; s < seg; s++)
        {
            float a0 = s * Mathf.PI * 2 / seg, a1 = (s + 1) * Mathf.PI * 2 / seg; float r = 0.16f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r), b1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
            Vector3 u0 = b0 + Vector3.up * 0.7f, u1 = b1 + Vector3.up * 0.7f, tip = Vector3.up;
            int i = v.Count; v.AddRange(new[] { b0, b1, u1, u0 }); t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });   // side (flat shaded: own vertices)
            i = v.Count; v.AddRange(new[] { u0, u1, tip }); t.AddRange(new[] { i, i + 2, i + 1 });
        }
        crystal = new Mesh { name = "Crystal" }; crystal.SetVertices(v); crystal.SetTriangles(t, 0); crystal.RecalculateNormals(); crystal.RecalculateBounds();
        return crystal;
    }
    static Material Mat(Color c, float metal, float smooth, float glow)
    {
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(lit); m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true;
        if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
        return m;
    }
    static readonly Dictionary<string, Material> oreMats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Material> rockMats = new Dictionary<string, Material>();

    public static void Setup(AHGame g)
    {
        for (int i = 0; i < 4; i++) if (rocks[i] == null) rocks[i] = Resources.Load<GameObject>("AH/Models/KK/kk_rock_single_" + "BCDE"[i]);
        if (rocks[0] == null) return;
        var root = new GameObject("Ore rocks").transform; int n = 0;
        foreach (var s in AHGather.Spots)
        {
            if (s.kind != "mine" || s.body == null) continue;
            // the old rock: measure it, then hide it
            var old = s.body.transform.parent != null ? s.body.transform.parent : s.body.transform;
            var rs = old.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            foreach (var r in rs) r.enabled = false;
            if (s.fx != null) s.fx.SetActive(false);

            var rnd = new System.Random((int)(s.pos.x * 31 + s.pos.z * 17));
            var rock = Object.Instantiate(rocks[rnd.Next(4)], root, false);
            foreach (var c in rock.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            rock.transform.position = new Vector3(s.pos.x, b.min.y - 0.05f, s.pos.z); rock.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            rock.transform.localScale = Vector3.one;
            var rb = Measure(rock);
            float wide = Mathf.Max(b.size.x, b.size.z) * 1.05f, tall = Mathf.Max(0.45f, b.size.y * 0.95f);
            rock.transform.localScale = new Vector3(wide / Mathf.Max(0.05f, Mathf.Max(rb.size.x, rb.size.z)), tall / Mathf.Max(0.05f, rb.size.y), wide / Mathf.Max(0.05f, Mathf.Max(rb.size.x, rb.size.z)));
            // the rock's own colour by its kind
            var rr = rock.GetComponentInChildren<Renderer>();
            Material rm;
            if (!rockMats.TryGetValue(s.type, out rm) || rm == null)
            {
                rm = new Material(rr.sharedMaterial); rm.enableInstancing = true;
                Color tint = s.type == "obsidian" ? new Color(0.32f, 0.28f, 0.36f) : s.type == "coralrock" ? new Color(1f, 0.82f, 0.8f) : s.type == "gold" ? new Color(0.95f, 0.88f, 0.75f) : Color.white;
                foreach (var pr in new[] { "baseColorFactor", "_BaseColor" }) if (rm.HasProperty(pr)) rm.SetColor(pr, tint);
                rockMats[s.type] = rm;
            }
            foreach (var r2 in rock.GetComponentsInChildren<Renderer>(true)) r2.sharedMaterial = rm;
            rb = Measure(rock);

            // the ore: crystals sprouting from the top and sides
            Material om;
            if (!oreMats.TryGetValue(s.type, out om) || om == null)
            {
                var def = AHJson.O(AHJson.O(AHDB.Rules, "ROCKS"), s.type);
                Color oc = AHGame.Hex((int)AHJson.N(def, "c", 0xc0c0c0));
                bool metal = s.type == "gold" || s.type == "iron" || s.type == "tin" || s.type == "copper";
                om = Mat(oc, metal ? 0.7f : 0.1f, s.type == "obsidian" ? 0.95f : 0.65f, s.type == "obsidian" ? 0.15f : s.type == "coralrock" ? 0.25f : 0.35f);
                oreMats[s.type] = om;
            }
            var ore = new GameObject("Ore").transform; ore.SetParent(root, false);
            int count = 5 + rnd.Next(3);
            for (int i = 0; i < count; i++)
            {
                float a = (float)(i * Mathf.PI * 2 / count + rnd.NextDouble() * 0.6), up = (float)rnd.NextDouble();
                Vector3 dir = new Vector3(Mathf.Cos(a) * (1f - up * 0.6f), 0.55f + up, Mathf.Sin(a) * (1f - up * 0.6f)).normalized;
                Vector3 at = rb.center + new Vector3(Mathf.Cos(a) * rb.extents.x * (0.55f - up * 0.35f), rb.extents.y * (0.15f + up * 0.6f), Mathf.Sin(a) * rb.extents.z * (0.55f - up * 0.35f));
                var cgo = new GameObject("Crystal"); cgo.transform.SetParent(ore, false);
                cgo.transform.position = at; cgo.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, (float)rnd.NextDouble() * 60f, 0);
                float size = Mathf.Max(0.18f, wide * (0.22f + (float)rnd.NextDouble() * 0.16f));
                cgo.transform.localScale = new Vector3(size * 0.9f, size, size * 0.9f);
                cgo.AddComponent<MeshFilter>().sharedMesh = Crystal(); var mr = cgo.AddComponent<MeshRenderer>(); mr.sharedMaterial = om;
            }
            AHModel.SetShadows(rock); AHModel.SetShadows(ore.gameObject);
            // the gathering code dims the body and hides the ore while the rock is spent
            s.body = rr; s.bodyCol = rm.HasProperty("baseColorFactor") ? rm.GetColor("baseColorFactor") : rm.HasProperty("_BaseColor") ? rm.GetColor("_BaseColor") : Color.white;
            s.fx = ore.gameObject;
            n++;
        }
        if (n == 0) Object.Destroy(root.gameObject);
    }
    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true); Bounds b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds); return b;
    }
}
