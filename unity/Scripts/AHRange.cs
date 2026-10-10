// Ashen Hollow: the great mountains around every land. The rocky crags at the border stay where they are, but behind
// them now rise two ranges of real mountains: a near range of forested foothills climbing to bare rock and snowy
// peaks, and a taller far range fading blue into the haze. They are drawn past the fog (layer 30, seen up to 420 m),
// so from anywhere in a land you see a horizon of mountains instead of a grey edge.
// Each land paints them its own way: green and grey with snow, white Frostfang peaks, red-gold desert mesas, black
// ember crags with glowing seams, dark swamp hills.
using UnityEngine;

public static class AHRange
{
    public const int Layer = 30;
    static Material mat;

    class Look { public Color low, mid, high, snow; public float snowAt; public float heightK = 1f; }
    static Look LookOf(string id, Color land)
    {
        var l = new Look { low = new Color(0.16f, 0.3f, 0.15f), mid = new Color(0.33f, 0.36f, 0.4f), high = new Color(0.5f, 0.52f, 0.57f), snow = new Color(0.95f, 0.97f, 1f), snowAt = 0.66f };
        switch (id)
        {
            case "frost": case "whitepine": case "hc_city":
                l.low = new Color(0.55f, 0.6f, 0.66f); l.mid = new Color(0.6f, 0.64f, 0.7f); l.high = new Color(0.8f, 0.84f, 0.9f); l.snowAt = 0.35f; l.heightK = 1.25f; break;
            case "sands": case "scorchwind": case "ss_city":
                l.low = new Color(0.78f, 0.6f, 0.38f); l.mid = new Color(0.72f, 0.45f, 0.28f); l.high = new Color(0.82f, 0.6f, 0.4f); l.snowAt = 2f; l.heightK = 0.8f; break;
            case "ember": case "ch_city": case "ashfall":
                l.low = new Color(0.22f, 0.17f, 0.15f); l.mid = new Color(0.16f, 0.13f, 0.12f); l.high = new Color(0.3f, 0.24f, 0.22f); l.snow = new Color(0.55f, 0.5f, 0.48f); l.snowAt = 0.9f; l.heightK = 1.15f; break;
            case "mire": case "causeway": case "fenwick": case "mw_city":
                l.low = new Color(0.2f, 0.27f, 0.2f); l.mid = new Color(0.3f, 0.33f, 0.3f); l.high = new Color(0.42f, 0.44f, 0.42f); l.snowAt = 2f; l.heightK = 0.75f; break;
        }
        return l;
    }

    public static void Build(AHGame g, Transform world)
    {
        string id = AHGame.AreaId;
        if (world == null || AHDungeon.IsDungeon(id) || id == "isle" || id == "tide" || id == "co_city" || id == "tutorial" || id == "kq") return;
        Renderer ground = null; foreach (var r in world.GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("AH_GROUND")) { ground = r; break; }
        if (ground == null) return;
        if (mat == null) { mat = Resources.Load<Material>("AH/Materials/Range"); if (mat == null) { var sh = Shader.Find("AshenHollow/Range"); if (sh != null) mat = new Material(sh); } }
        if (mat == null) return;
        mat.SetFloat("_HazeNear", 110f); mat.SetFloat("_HazeFar", 650f); mat.SetFloat("_HazeMax", 0.5f);
        var b = ground.bounds; Color land = new Color(0.35f, 0.5f, 0.3f);
        // a ground whose bounds are not real numbers yet (still being built) would make a mountain ring of NaNs
        if (!Real(b.center) || !Real(b.extents) || b.extents.x < 1f || b.extents.z < 1f || b.extents.magnitude > 20000f) { Debug.Log("Ashen Hollow: no mountain ranges (ground bounds " + b + ")"); return; }
        var gm = ground.sharedMaterial; if (gm != null && gm.HasProperty("_Tint")) land = Color.Lerp(land, gm.GetColor("_Tint"), 0.3f);
        var look = LookOf(id, land);
        var root = new GameObject("Ranges"); root.layer = Layer;
        int seed = id.GetHashCode();
        // near range: foothills right behind the border crags, peaks 35-80 m
        Ring(root.transform, b, 55f, 90f, 22f * look.heightK, 30f * look.heightK, seed, look, 1f);
        // far range: taller and further, fading into the sky
        Ring(root.transform, b, 190f, 120f, 55f * look.heightK, 75f * look.heightK, seed + 77, look, 0f);
        root.AddComponent<AHRangeHaze>().mat = mat;
    }

    static bool Real(Vector3 v) { return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)); }
    static float Hash(float x) { return Mathf.Repeat(Mathf.Sin(x * 127.1f) * 43758.5453f, 1f); }
    // a ridge line along the ring: sharp peaks (folded noise), saddles between them
    static float Ridge(float u, float s)
    {
        float h = 0f, a = 1f, f = 1f, tot = 0f;
        for (int o = 0; o < 4; o++) { float n = 1f - Mathf.Abs(Mathf.PerlinNoise(u * f + s, s * 0.37f + o * 3.1f) * 2f - 1f); h += Mathf.Pow(n, 1.6f) * a; tot += a; a *= 0.5f; f *= 2.2f; }
        return h / tot;
    }

    static void Ring(Transform root, Bounds b, float off, float depth, float baseH, float varH, int seed, Look look, float alpha)
    {
        const int nu = 420, nv = 18;
        float cx = b.center.x, cz = b.center.z, ax = b.extents.x + off, az = b.extents.z + off, y0 = b.min.y;
        float s = (seed & 1023) * 0.173f;
        var v = new Vector3[(nu + 1) * (nv + 1)]; var c = new Color[v.Length]; var tri = new int[nu * nv * 6];
        float per = 2f * Mathf.PI * Mathf.Sqrt((ax * ax + az * az) * 0.5f);
        for (int i = 0; i <= nu; i++)
        {
            float th = i / (float)nu * Mathf.PI * 2f, cs = Mathf.Cos(th), sn = Mathf.Sin(th);
            // a rounded rectangle (squircle) around the land
            float ex = Mathf.Sign(cs) * Mathf.Pow(Mathf.Abs(cs), 0.4f), ez = Mathf.Sign(sn) * Mathf.Pow(Mathf.Abs(sn), 0.4f);
            Vector3 edge = new Vector3(cx + ex * ax, y0, cz + ez * az), outw = new Vector3(ex * az, 0f, ez * ax).normalized;
            if (outw.sqrMagnitude < 1e-4f) outw = new Vector3(cs, 0, sn);
            float u = i / (float)nu * per / 110f;   // one big peak every ~110 m
            float crest = baseH + varH * Ridge(u, s);
            for (int j = 0; j <= nv; j++)
            {
                float t = j / (float)nv, dd = t * depth;
                // the slope rises from below the ground to the crest at 45% of the depth, then falls a little behind
                float prof = t < 0.45f ? Mathf.SmoothStep(-0.04f, 1f, t / 0.45f) : Mathf.Lerp(1f, 0.7f, (t - 0.45f) / 0.55f);
                float side = 0.75f + 0.5f * Ridge(u * 3.1f + t * 2.3f, s + 9f);   // gullies and spurs down the faces
                float gully = (Mathf.PerlinNoise(u * 14f + s, t * 5f) - 0.5f) * 0.22f + (Mathf.PerlinNoise(u * 33f, t * 11f + s) - 0.5f) * 0.08f;   // ribs and gullies down every face
                float h = crest * prof * (j == 0 ? 0f : side + gully) - (j == 0 ? 4f : 0f);
                // wobble the ridge in and out so it is not a perfect ring
                float wob = (Mathf.PerlinNoise(u * 0.7f, s + 5f) - 0.5f) * 30f;
                var p = edge + outw * (dd + wob * t) + Vector3.up * h;
                int k = i * (nv + 1) + j; v[k] = p;
                // colour by height: forest at the foot, rock above, snow on the top; a touch of noise
                float hf = h / Mathf.Max(1f, baseH + varH), nz = Hash(i * 13.7f + j * 7.1f + s) * 0.06f;
                Color col = hf < 0.4f ? Color.Lerp(look.low, look.mid, Mathf.SmoothStep(0f, 1f, (hf - 0.18f) / 0.22f)) : Color.Lerp(look.mid, look.high, (hf - 0.4f) / 0.4f);
                float sa = look.snowAt + (alpha > 0.5f ? 0.2f : 0f);   // the near foothills keep less snow than the far peaks
                if (hf > sa) col = Color.Lerp(col, look.snow, Mathf.Clamp01((hf - sa) / 0.08f));
                col *= 0.94f + nz; col.a = alpha;
                c[k] = col;
            }
        }
        int q = 0;
        for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                int a = i * (nv + 1) + j, bb = a + nv + 1;
                tri[q++] = a; tri[q++] = a + 1; tri[q++] = bb; tri[q++] = bb; tri[q++] = a + 1; tri[q++] = bb + 1;
            }
        var m = new Mesh { name = "range", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.vertices = v; m.colors = c; m.triangles = tri; m.RecalculateNormals(); m.RecalculateBounds();
        // the ring's winding depends on the squircle's direction: make sure the faces look up, at the sky and the land
        var nrm = m.normals; int up = 0;
        for (int k = 0; k < nrm.Length; k += 7) up += nrm[k].y >= 0f ? 1 : -1;
        if (up < 0) { for (int t2 = 0; t2 < tri.Length; t2 += 3) { int x = tri[t2 + 1]; tri[t2 + 1] = tri[t2 + 2]; tri[t2 + 2] = x; } m.triangles = tri; m.RecalculateNormals(); }
        var go = new GameObject("Range"); go.layer = Layer; go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
    }
}

// keeps the ranges' haze the colour of the sky's fog (it changes with the time of day)
public class AHRangeHaze : MonoBehaviour
{
    public Material mat; float t;
    void Update() { t -= Time.deltaTime; if (t > 0f || mat == null) return; t = 1f; var f = RenderSettings.fogColor; mat.SetColor("_Haze", Color.Lerp(f, new Color(0.58f, 0.7f, 0.88f), 0.45f)); }
}
